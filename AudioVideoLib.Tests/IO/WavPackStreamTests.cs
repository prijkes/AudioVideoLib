/*
 * Test suite for WavPackStream — walks a .wv container, exposes the parsed
 * 32-byte block header + per-block sub-block summaries, and round-trips the
 * byte range verbatim through WriteTo. See spec §7.2.
 *
 * The tests in this file all run against an in-memory synthetic block produced
 * by BuildSyntheticBlock() — no real .wv sample is required. Tests that need a
 * real corpus file live in a sibling file added in Bundle D.
 */
namespace AudioVideoLib.Tests.IO;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AudioVideoLib.Formats;
using AudioVideoLib.IO;
using AudioVideoLib.Tags;

using Xunit;

public sealed class WavPackStreamTests
{
    private const string DetachedMessage =
        "Source stream was detached or never read. WriteTo requires a live source.";

    /// <summary>
    /// Builds a minimal valid <c>wvpk</c> block with two sub-blocks back-to-back:
    /// <list type="bullet">
    ///   <item>Sub-block 1: <c>ID_DECORR_TERMS</c> (id=<c>0x02</c>), 4-byte payload (2 words, no flag bits).</item>
    ///   <item>Sub-block 2: <c>ID_DUMMY</c> (id=<c>0x00</c>), 0-byte payload.</item>
    /// </list>
    /// Header flags select sample-rate index 9 (44100 Hz), 2-byte samples
    /// (<c>BYTES_STORED == 1</c>), and stereo (<c>MONO_FLAG</c> clear).
    /// </summary>
    private static byte[] BuildSyntheticBlock()
    {
        // Sub-block 1: 0x02 (id), 0x02 (word count = 2 -> 4 bytes), payload 0xAA 0xBB 0xCC 0xDD.
        var sub1 = new byte[] { 0x02, 0x02, 0xAA, 0xBB, 0xCC, 0xDD };

        // Sub-block 2: 0x00 (ID_DUMMY), 0x00 (word count = 0 -> 0 bytes), no payload.
        var sub2 = new byte[] { 0x00, 0x00 };

        var subAll = new byte[sub1.Length + sub2.Length];
        Buffer.BlockCopy(sub1, 0, subAll, 0, sub1.Length);
        Buffer.BlockCopy(sub2, 0, subAll, sub1.Length, sub2.Length);

        var block = new byte[WavPackBlockHeader.Size + subAll.Length];

        // ckID = "wvpk".
        block[0] = (byte)'w';
        block[1] = (byte)'v';
        block[2] = (byte)'p';
        block[3] = (byte)'k';

        // ckSize = block size minus 8 (per wavpack.h:62-67).
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(4, 4), (uint)(block.Length - 8));

        // version = 0x0410 (current WavPack stream version).
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(8, 2), 0x0410);

        // block_index_u8 (offset 10) and total_samples_u8 (offset 11) left zero.

        // total_samples = 1024 (low 32 bits).
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(12, 4), 1024u);

        // block_index = 0.
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(16, 4), 0u);

        // block_samples = 1024.
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(20, 4), 1024u);

        // flags: BYTES_STORED = 1 (16-bit samples), SRATE index 9 (44100 Hz), stereo.
        var flags = 0x1u | (9u << 23);
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(24, 4), flags);

        // crc: leave zero — walker doesn't verify.

        Buffer.BlockCopy(subAll, 0, block, WavPackBlockHeader.Size, subAll.Length);
        return block;
    }

    // ================================================================
    // Lifetime / detached-source contract — no input required.
    // ================================================================

    [Fact]
    public void WriteTo_ThrowsWhenSourceIsNull()
    {
        using var walker = new WavPackStream();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(DetachedMessage, ex.Message);
    }

    [Fact]
    public void WriteTo_ThrowsAfterDispose()
    {
        var bytes = BuildSyntheticBlock();
        using var input = new MemoryStream(bytes);
        var walker = new WavPackStream();
        Assert.True(walker.ReadStream(input));
        walker.Dispose();

        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(DetachedMessage, ex.Message);
    }

    // ================================================================
    // Magic-byte rejection — no real sample required.
    // ================================================================

    [Fact]
    public void ReadStream_RejectsNonWvPkInput()
    {
        var notWvpk = new byte[64];
        notWvpk[0] = (byte)'X';
        using var ms = new MemoryStream(notWvpk);
        using var walker = new WavPackStream();
        Assert.False(walker.ReadStream(ms));
        Assert.Empty(walker.Blocks);
    }

    // ================================================================
    // Synthetic-block header parse + sub-block enumeration.
    // ================================================================

    [Fact]
    public void ReadStream_DecodesSyntheticBlock()
    {
        var bytes = BuildSyntheticBlock();
        using var ms = new MemoryStream(bytes);
        using var walker = new WavPackStream();

        Assert.True(walker.ReadStream(ms));
        Assert.Single(walker.Blocks);

        var block = walker.Blocks[0];
        Assert.Equal(0L, block.StartOffset);
        Assert.Equal(bytes.Length, block.Length);
        Assert.Equal(44100, block.Header.SampleRate);
        Assert.Equal(2, block.Header.BytesPerSample);
        Assert.False(block.Header.IsMono);
    }

    [Fact]
    public void ReadStream_EnumeratesSubBlocks()
    {
        var bytes = BuildSyntheticBlock();
        using var ms = new MemoryStream(bytes);
        using var walker = new WavPackStream();

        Assert.True(walker.ReadStream(ms));

        var subs = walker.Blocks[0].SubBlocks;
        Assert.Equal(2, subs.Count);

        Assert.Equal(0x02, subs[0].RawId);
        Assert.Equal(4, subs[0].PayloadLength);
        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, subs[0].ReadPayload());

        Assert.Equal(0x00, subs[1].RawId);
        Assert.Equal(0, subs[1].PayloadLength);
    }

    // ================================================================
    // Round-trip identity — synthetic block back through WriteTo verbatim.
    // ================================================================

    [Fact]
    public void WriteTo_RoundTripsBytesIdentically()
    {
        var bytes = BuildSyntheticBlock();
        using var input = new MemoryStream(bytes);
        using var walker = new WavPackStream();
        Assert.True(walker.ReadStream(input));

        using var output = new MemoryStream();
        walker.WriteTo(output);

        Assert.Equal(bytes, output.ToArray());
    }

    // ================================================================
    // Real-file fixtures (Bundle D) — generated via ffmpeg's libwavpack
    // encoder; see TestFiles/wavpack/PROVENANCE.md.
    // ================================================================

    private const string StereoFixture = "TestFiles/wavpack/sample-stereo-44100-16.wv";
    private const string MonoFixture = "TestFiles/wavpack/sample-mono-48000-24.wv";
    private const string ApeFixture = "TestFiles/wavpack/sample-with-apev2.wv";

    [Fact]
    public void Stereo_HeaderReportsExpectedRateChannelsAndDepth()
    {
        using var fs = File.OpenRead(StereoFixture);
        using var walker = new WavPackStream();
        Assert.True(walker.ReadStream(fs));

        var first = walker.Blocks[0].Header;
        Assert.Equal(44100, first.SampleRate);
        Assert.Equal(2, first.BytesPerSample);
        Assert.False(first.IsMono);
        Assert.False(first.IsFloat);
        Assert.True(first.TotalSamples > 0);
    }

    [Fact]
    public void Mono_HeaderReportsMonoAnd24BitDepth()
    {
        using var fs = File.OpenRead(MonoFixture);
        using var walker = new WavPackStream();
        Assert.True(walker.ReadStream(fs));

        var first = walker.Blocks[0].Header;
        Assert.Equal(48000, first.SampleRate);
        Assert.Equal(3, first.BytesPerSample);
        Assert.True(first.IsMono);
    }

    [Fact]
    public void Stereo_BlockLengthsSumToAudioSpan()
    {
        using var fs = File.OpenRead(StereoFixture);
        using var walker = new WavPackStream();
        Assert.True(walker.ReadStream(fs));

        var sumLen = 0L;
        foreach (var b in walker.Blocks)
        {
            sumLen += b.Length;
        }

        // The block stream is contiguous, starting at the file's first wvpk byte.
        var firstStart = walker.Blocks[0].StartOffset;
        var lastEnd = walker.Blocks[^1].StartOffset + walker.Blocks[^1].Length;
        Assert.Equal(lastEnd - firstStart, sumLen);
    }

    [Fact]
    public void Stereo_FirstBlockExposesKnownSubBlockIds()
    {
        // ID_DECORR_TERMS (0x02), ID_DECORR_WEIGHTS (0x03), and ID_WV_BITSTREAM (0x0a)
        // are present in essentially every wavpack-encoded stereo block per
        // 3rdparty/WavPack/include/wavpack.h:167-175.
        using var fs = File.OpenRead(StereoFixture);
        using var walker = new WavPackStream();
        Assert.True(walker.ReadStream(fs));

        var ids = new HashSet<byte>();
        foreach (var sb in walker.Blocks[0].SubBlocks)
        {
            ids.Add(sb.UniqueId);
        }

        Assert.Contains((byte)0x02, ids);
        Assert.Contains((byte)0x03, ids);
        Assert.Contains((byte)0x0a, ids);
    }

    [Fact]
    public void Stereo_RoundTripIsByteIdentical()
    {
        var original = File.ReadAllBytes(StereoFixture);

        using var fs = new MemoryStream(original);
        using var walker = new WavPackStream();
        Assert.True(walker.ReadStream(fs));

        using var output = new MemoryStream();
        walker.WriteTo(output);

        // The walker covers only the wvpk block stream. The stereo fixture has no
        // tag footer (ffmpeg's auto-appended APEv2 trailer was stripped in the
        // PROVENANCE-documented post-processing step), so the round-trip is exact.
        // The footer-aware case is exercised by the APEv2 fixture below.
        Assert.Equal(original, output.ToArray());
    }

    // ================================================================
    // Tag-edit round-trip via AudioTags / ApeTag — wvpk audio bytes
    // must be preserved across the tag mutation (spec §7.2 task 4).
    // ================================================================

    [Fact]
    public void TagEdit_DoesNotPerturbWvpkBlockBytes()
    {
        var original = File.ReadAllBytes(ApeFixture);

        // Capture the wvpk block-stream span and bytes from the original file.
        long originalAudioStart;
        long originalAudioLength;
        byte[] originalAudioBytes;
        using (var ms = new MemoryStream(original))
        using (var walker = new WavPackStream())
        {
            Assert.True(walker.ReadStream(ms));
            originalAudioStart = walker.Blocks[0].StartOffset;
            var lastEnd = walker.Blocks[^1].StartOffset + walker.Blocks[^1].Length;
            originalAudioLength = lastEnd - originalAudioStart;
            originalAudioBytes = new byte[originalAudioLength];
            Array.Copy(original, originalAudioStart, originalAudioBytes, 0, originalAudioLength);
        }

        // Drive the tag edit via AudioTags' canonical APEv2 path (mirrors what the
        // project does for MPA/FLAC tag edits): scan -> mutate -> emit.
        // AudioVideoLib does not expose a one-shot "edit and save" facade; the
        // composition is parse with AudioTags + serialize via ApeTag.ToByteArray.
        byte[] edited;
        using (var ms = new MemoryStream(original))
        {
            var tags = AudioTags.ReadStream(ms);
            ApeTag? apeTag = null;
            IAudioTagOffset? apeOffset = null;
            foreach (var off in tags)
            {
                if (off.AudioTag is ApeTag ape)
                {
                    apeTag = ape;
                    apeOffset = off;
                    break;
                }
            }

            Assert.NotNull(apeTag);
            Assert.NotNull(apeOffset);

            // Mutate the Title field — confirms the round-trip survives an actual
            // change to the APEv2 payload, not a no-op.
            var newTitle = new ApeUtf8Item(apeTag!.Version, "Title");
            newTitle.Values.Add("WavPack walker test (edited)");
            apeTag.SetItem(newTitle);

            // Re-emit: keep the audio span verbatim, replace the tag bytes.
            using var output = new MemoryStream();
            output.Write(original, 0, (int)apeOffset!.StartOffset);
            output.Write(apeTag.ToByteArray());
            edited = output.ToArray();
        }

        // Re-parse the saved bytes: the wvpk block-stream span must match exactly.
        using var ms2 = new MemoryStream(edited);
        using var walker2 = new WavPackStream();
        Assert.True(walker2.ReadStream(ms2));
        var editedAudioStart = walker2.Blocks[0].StartOffset;
        var editedLastEnd = walker2.Blocks[^1].StartOffset + walker2.Blocks[^1].Length;
        var editedAudioLength = editedLastEnd - editedAudioStart;

        Assert.Equal(originalAudioStart, editedAudioStart);
        Assert.Equal(originalAudioLength, editedAudioLength);

        var editedAudioBytes = new byte[editedAudioLength];
        Array.Copy(edited, editedAudioStart, editedAudioBytes, 0, editedAudioLength);
        Assert.Equal(originalAudioBytes, editedAudioBytes);
    }

    // ================================================================
    // Magic-byte / direct-invocation dispatch (Phase 1 stand-in for the
    // MediaContainers.ReadStream walk-by-magic test, which lands in
    // Phase 2 once the registry update is in).
    // ================================================================

    [Fact]
    public void DirectInvocation_OnWavPackBytes_Succeeds()
    {
        using var fs = File.OpenRead(StereoFixture);
        using var walker = new WavPackStream();
        Assert.True(walker.ReadStream(fs));
    }

    [Fact]
    public void DirectInvocation_OnNonWavPackBytes_Fails()
    {
        var bogus = new byte[256];
        for (var i = 0; i < bogus.Length; i++)
        {
            bogus[i] = (byte)i;
        }

        using var ms = new MemoryStream(bogus);
        using var walker = new WavPackStream();
        Assert.False(walker.ReadStream(ms));
    }

    [Fact]
    public void MediaContainers_DispatchesWavPackByMagic()
    {
        // Phase 2 (format-pack integration bundle) registered WavPackStream in
        // MediaContainers' walker registry and added the `wvpk` magic-byte probe,
        // so MediaContainers.ReadStream auto-detects .wv files in O(1).
        using var fs = File.OpenRead(StereoFixture);
        using var streams = MediaContainers.ReadStream(fs);
        var walker = streams.FirstOrDefault();
        Assert.NotNull(walker);
        Assert.IsType<WavPackStream>(walker);
    }
}
