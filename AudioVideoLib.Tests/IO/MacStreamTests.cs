/*
 * Test suite for MacStream — walks a Monkey's Audio (.ape) container, exposes
 * the parsed APE_DESCRIPTOR + APE_HEADER + seek table + per-frame byte ranges,
 * and round-trips the byte range verbatim through WriteTo. See spec §7.2.
 *
 * Real-sample fixtures derived from a public Monkey's Audio demo file
 * (luckynight.ape) — see TestFiles/mac/PROVENANCE.md for the full acquisition
 * story. Truncated to a single frame to keep the test corpus small while
 * preserving real upstream-encoded audio bytes.
 */
namespace AudioVideoLib.Tests.IO;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.Formats;
using AudioVideoLib.IO;
using AudioVideoLib.Tags;

using Xunit;

public sealed class MacStreamTests
{
    private const string SamplePath = "TestFiles/mac/sample.ape";
    private const string SampleWithTagPath = "TestFiles/mac/sample-with-apev2.ape";
    private const string SampleNoWavHeaderPath = "TestFiles/mac/sample-no-wavheader.ape";

    private const string ExpectedDetachedMessage =
        "Source stream was detached or never read. WriteTo requires a live source.";

    // ================================================================
    // Lifetime / detached-source contract — unconditional, no fixtures.
    // ================================================================

    [Fact]
    public void WriteTo_ThrowsWhenSourceIsNull()
    {
        using var walker = new MacStream();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(ExpectedDetachedMessage, ex.Message);
    }

    // ================================================================
    // Magic-byte dispatch — accepts both APE variants, rejects foreign
    // magic. The skeleton in Bundle B sets Format after detection but
    // does not yet parse the descriptor; full parse lands in Bundle C.
    // ================================================================

    [Fact]
    public void ReadStream_RejectsForeignMagic()
    {
        using var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("fLaC"));
        ms.SetLength(64);
        ms.Position = 0;

        using var walker = new MacStream();
        Assert.False(walker.ReadStream(ms));
    }

    [Fact]
    public void ReadStream_DispatchesIntegerMagic()
    {
        using var ms = new MemoryStream(MakeMinimalDescriptorPlusHeader("MAC "));
        ms.Position = 0;

        using var walker = new MacStream();
        Assert.True(walker.ReadStream(ms));
        Assert.Equal(MacFormat.Integer, walker.Format);
    }

    [Fact]
    public void ReadStream_DispatchesFloatMagic()
    {
        using var ms = new MemoryStream(MakeMinimalDescriptorPlusHeader("MACF"));
        ms.Position = 0;

        using var walker = new MacStream();
        Assert.True(walker.ReadStream(ms));
        Assert.Equal(MacFormat.Float, walker.Format);
    }

    // ================================================================
    // Frame enumeration — synthetic input with a real seek table.
    // ================================================================

    [Fact]
    public void ReadStream_BuildsFrameTableFromSeekEntries()
    {
        // 3 frames at offsets [200, 280, 360], audio region runs to offset 480 (so
        // the last frame is 120 bytes long). All frame body bytes are zero — the walker
        // doesn't decode them, only ranges them.
        const int totalFrames = 3;
        const uint blocksPerFrame = 73728 * 4;
        const uint finalFrameBlocks = 17_000;
        var seekOffsets = new uint[] { 200, 280, 360 };
        const uint apeFrameDataBytes = 480 - 200;

        var descSize = 52;
        var hdrSize = 24;
        var seekSize = (uint)(seekOffsets.Length * 4);
        var bytes = new byte[480]; // file ends exactly at audioEnd

        // Descriptor.
        Encoding.ASCII.GetBytes("MAC ", bytes.AsSpan(0, 4));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(4, 2), 3990);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), (uint)descSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12, 4), (uint)hdrSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16, 4), seekSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20, 4), 0); // headerDataBytes
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24, 4), apeFrameDataBytes);

        // Header.
        var hdr = bytes.AsSpan(descSize);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[0..2], 2000);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[2..4], MacHeader.CreateWavHeaderFlag);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[4..8], blocksPerFrame);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[8..12], finalFrameBlocks);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[12..16], totalFrames);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[16..18], 16);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[18..20], 2);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[20..24], 44100);

        // Seek table at offset descSize + hdrSize = 76.
        var seek = bytes.AsSpan(descSize + hdrSize);
        for (var i = 0; i < seekOffsets.Length; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(seek.Slice(i * 4, 4), seekOffsets[i]);
        }

        using var ms = new MemoryStream(bytes);
        using var walker = new MacStream();
        Assert.True(walker.ReadStream(ms));

        Assert.Equal(totalFrames, walker.SeekEntries.Count);
        Assert.Equal(totalFrames, walker.Frames.Count);
        Assert.Equal(200, walker.Frames[0].StartOffset);
        Assert.Equal(80, walker.Frames[0].Length);
        Assert.Equal(280, walker.Frames[1].StartOffset);
        Assert.Equal(80, walker.Frames[1].Length);
        Assert.Equal(360, walker.Frames[2].StartOffset);
        Assert.Equal(120, walker.Frames[2].Length);

        Assert.Equal(blocksPerFrame, walker.Frames[0].BlockCount);
        Assert.Equal(blocksPerFrame, walker.Frames[1].BlockCount);
        Assert.Equal(finalFrameBlocks, walker.Frames[2].BlockCount);

        // Sum of frame lengths equals descriptor.TotalApeFrameDataBytes.
        var sum = 0L;
        foreach (var f in walker.Frames)
        {
            sum += f.Length;
        }

        Assert.Equal((long)apeFrameDataBytes, sum);
    }

    // ================================================================
    // 32-bit seek table wrap-correction — audit follow-up. For files
    // larger than 4 GiB, the on-disk 32-bit seek offsets wrap; the C++
    // reference (APEHeader.cpp:116-131, Convert32BitSeekTable) accumulates
    // 0x1_0000_0000 each time a value decreases. The earlier C# port
    // stored uint and silently rolled over.
    // ================================================================

    [Fact]
    public void ReadStream_AppliesWrapCorrectionToSeekTable()
    {
        // Four seek entries: the fourth value wraps below the third, so the
        // walker must add 0x1_0000_0000 to it.
        var rawOffsets = new uint[] { 0x10_0000u, 0x20_0000u, 0x30_0000u, 0x00_0000u };
        var expectedOffsets = new long[] { 0x10_0000L, 0x20_0000L, 0x30_0000L, 0x1_0000_0000L };

        const uint totalFrames = 4;
        const uint blocksPerFrame = 73728 * 4;
        const uint finalFrameBlocks = 17_000;

        var descSize = 52;
        var hdrSize = 24;
        var seekSize = (uint)(rawOffsets.Length * 4);

        // ApeFrameDataBytesHigh = 1 — total frame bytes spans > 4 GiB so the wrap is
        // physically meaningful. We don't allocate a 4 GiB buffer; only the seek-table
        // bytes need to encode the wrap.
        const uint apeFrameDataBytesLo = 0x4000_0000u;
        const uint apeFrameDataBytesHi = 1u;

        var bytes = new byte[descSize + hdrSize + seekSize];
        Encoding.ASCII.GetBytes("MAC ", bytes.AsSpan(0, 4));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(4, 2), 3990);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), (uint)descSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12, 4), (uint)hdrSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16, 4), seekSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20, 4), 0u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24, 4), apeFrameDataBytesLo);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(28, 4), apeFrameDataBytesHi);

        var hdr = bytes.AsSpan(descSize);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[0..2], 2000);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[2..4], MacHeader.CreateWavHeaderFlag);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[4..8], blocksPerFrame);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[8..12], finalFrameBlocks);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[12..16], totalFrames);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[16..18], 16);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[18..20], 2);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[20..24], 44100);

        var seek = bytes.AsSpan(descSize + hdrSize);
        for (var i = 0; i < rawOffsets.Length; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(seek.Slice(i * 4, 4), rawOffsets[i]);
        }

        using var ms = new MemoryStream(bytes);
        using var walker = new MacStream();
        Assert.True(walker.ReadStream(ms));

        Assert.Equal(rawOffsets.Length, walker.SeekEntries.Count);
        for (var i = 0; i < expectedOffsets.Length; i++)
        {
            Assert.Equal(expectedOffsets[i], walker.SeekEntries[i].FileOffset);
        }
    }

    // ================================================================
    // Real-sample frame enumeration — counts the upstream-encoded
    // .ape file's frames and confirms the per-frame length budget
    // matches the descriptor's total audio bytes.
    // ================================================================

    [Fact]
    public void ReadStream_EnumeratesFramesWithCorrectLengths()
    {
        Assert.True(File.Exists(SamplePath), $"Test sample missing: {SamplePath}");

        using var fs = File.OpenRead(SamplePath);
        using var walker = new MacStream();
        Assert.True(walker.ReadStream(fs));

        Assert.NotEmpty(walker.Frames);
        Assert.Equal(walker.SeekEntries.Count, walker.Frames.Count);
        Assert.Equal(walker.Header!.TotalFrames, (uint)walker.Frames.Count);

        // The sum of per-frame lengths must equal descriptor.TotalApeFrameDataBytes.
        var sum = 0L;
        foreach (var frame in walker.Frames)
        {
            Assert.True(frame.Length > 0, $"Frame at {frame.StartOffset} has non-positive length");
            sum += frame.Length;
        }

        Assert.Equal((long)walker.Descriptor!.TotalApeFrameDataBytes, sum);

        // The first frame begins at the first seek-table offset, which is also
        // the documented start of the audio region per APEInfo.cpp:439.
        Assert.Equal((long)walker.SeekEntries[0].FileOffset, walker.Frames[0].StartOffset);
    }

    // ================================================================
    // Round-trip identity on a real .ape sample — read, write,
    // expect byte-identical output. This closes the loop on byte-exact
    // passthrough through WriteTo's _source.CopyTo.
    // ================================================================

    [Fact]
    public void RoundTrip_ProducesByteIdenticalOutput()
    {
        Assert.True(File.Exists(SamplePath), $"Test sample missing: {SamplePath}");
        var original = File.ReadAllBytes(SamplePath);

        using var input = new MemoryStream(original);
        using var walker = new MacStream();
        Assert.True(walker.ReadStream(input));

        using var output = new MemoryStream();
        walker.WriteTo(output);
        Assert.Equal(original, output.ToArray());
    }

    [Fact]
    public void RoundTrip_HeaderFieldsMatchKnownSampleValues()
    {
        // The trimmed `sample.ape` is single-frame stereo 16-bit @ 44.1 kHz.
        // See TestFiles/mac/PROVENANCE.md.
        Assert.True(File.Exists(SamplePath));

        using var fs = File.OpenRead(SamplePath);
        using var walker = new MacStream();
        Assert.True(walker.ReadStream(fs));

        Assert.NotNull(walker.Header);
        Assert.Equal(MacFormat.Integer, walker.Format);
        Assert.True(walker.Header!.SampleRate is 44_100u or 48_000u,
            $"Unexpected sample rate {walker.Header.SampleRate}");
        Assert.InRange(walker.Header.Channels, (ushort)1, (ushort)8);
        Assert.True(walker.Header.BitsPerSample is 16 or 24);
        Assert.True(walker.Header.TotalFrames > 0);
    }

    // ================================================================
    // Tag-edit round-trip — APEv2 footer mutation must not perturb
    // the audio frame ranges. Mirrors the WavPack Bundle D pattern:
    // parse via AudioTags -> edit ApeTag -> splice -> re-parse ->
    // assert audio bytes preserved.
    // ================================================================

    [Fact]
    public void TagEdit_PreservesAudioBytes()
    {
        Assert.True(File.Exists(SampleWithTagPath), $"Test sample missing: {SampleWithTagPath}");
        var original = File.ReadAllBytes(SampleWithTagPath);

        // Capture the original audio span and bytes.
        long originalAudioStart;
        long originalAudioLength;
        byte[] originalAudioBytes;
        IReadOnlyList<MacFrame> originalFrames;
        using (var ms = new MemoryStream(original))
        using (var walker = new MacStream())
        {
            Assert.True(walker.ReadStream(ms));
            Assert.NotEmpty(walker.Frames);
            originalAudioStart = walker.Frames[0].StartOffset;
            var lastEnd = walker.Frames[^1].StartOffset + walker.Frames[^1].Length;
            originalAudioLength = lastEnd - originalAudioStart;
            originalAudioBytes = new byte[originalAudioLength];
            Array.Copy(original, originalAudioStart, originalAudioBytes, 0, originalAudioLength);
            originalFrames = [.. walker.Frames];
        }

        // Drive the tag edit via AudioTags' canonical APEv2 path (mirrors WavPack
        // Bundle D). AudioVideoLib does not expose a one-shot "edit and save"
        // facade; the composition is parse with AudioTags + serialize via
        // ApeTag.ToByteArray.
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
            newTitle.Values.Add("Mac walker test (edited)");
            apeTag.SetItem(newTitle);

            // Re-emit: keep everything before the tag verbatim, replace the tag bytes.
            using var output = new MemoryStream();
            output.Write(original, 0, (int)apeOffset!.StartOffset);
            output.Write(apeTag.ToByteArray());
            edited = output.ToArray();
        }

        // Re-parse the saved bytes: the audio frame ranges and audio bytes must match.
        using var ms2 = new MemoryStream(edited);
        using var walker2 = new MacStream();
        Assert.True(walker2.ReadStream(ms2));

        Assert.Equal(originalFrames.Count, walker2.Frames.Count);
        for (var i = 0; i < originalFrames.Count; i++)
        {
            Assert.Equal(originalFrames[i].StartOffset, walker2.Frames[i].StartOffset);
            Assert.Equal(originalFrames[i].Length, walker2.Frames[i].Length);
            Assert.Equal(originalFrames[i].BlockCount, walker2.Frames[i].BlockCount);
        }

        var editedAudioStart = walker2.Frames[0].StartOffset;
        var editedLastEnd = walker2.Frames[^1].StartOffset + walker2.Frames[^1].Length;
        var editedAudioLength = editedLastEnd - editedAudioStart;

        Assert.Equal(originalAudioStart, editedAudioStart);
        Assert.Equal(originalAudioLength, editedAudioLength);

        var editedAudioBytes = new byte[editedAudioLength];
        Array.Copy(edited, editedAudioStart, editedAudioBytes, 0, editedAudioLength);
        Assert.Equal(originalAudioBytes, editedAudioBytes);
    }

    // ================================================================
    // MAC_FORMAT_FLAG_CREATE_WAV_HEADER toggle — when CLEAR, the
    // descriptor's HeaderDataBytes is the preserved-WAV-header byte
    // count and the audio region begins after that block. When SET,
    // HeaderDataBytes is zero (per APEHeader.cpp:255). The walker
    // must compute correct audio offsets in both cases.
    // ================================================================

    [Fact]
    public void ReadStream_HandlesWavHeaderFlag_Off()
    {
        // Real sample with CreateWavHeaderFlag CLEAR: 44 bytes of preserved WAV
        // header sit between the seek table and the audio region.
        Assert.True(File.Exists(SamplePath), $"Test sample missing: {SamplePath}");

        using var fs = File.OpenRead(SamplePath);
        using var walker = new MacStream();
        Assert.True(walker.ReadStream(fs));

        Assert.False(walker.Header!.CreatesWavHeaderOnDecode);
        Assert.True(walker.Descriptor!.HeaderDataBytes > 0,
            $"Expected preserved WAV header in sample.ape, got HeaderDataBytes={walker.Descriptor.HeaderDataBytes}");

        // First frame begins after descriptor + header + seek table + preserved WAV header.
        var expectedAudioStart =
            walker.Descriptor.DescriptorBytes
            + walker.Descriptor.HeaderBytes
            + walker.Descriptor.SeekTableBytes
            + walker.Descriptor.HeaderDataBytes;
        Assert.Equal((long)expectedAudioStart, walker.Frames[0].StartOffset);
    }

    [Fact]
    public void ReadStream_HandlesWavHeaderFlag_On()
    {
        // Real sample with CreateWavHeaderFlag SET: HeaderDataBytes==0 and the
        // audio region begins immediately after the seek table.
        Assert.True(File.Exists(SampleNoWavHeaderPath), $"Test sample missing: {SampleNoWavHeaderPath}");

        using var fs = File.OpenRead(SampleNoWavHeaderPath);
        using var walker = new MacStream();
        Assert.True(walker.ReadStream(fs));

        Assert.True(walker.Header!.CreatesWavHeaderOnDecode);
        Assert.Equal(0u, walker.Descriptor!.HeaderDataBytes);

        var expectedAudioStart =
            walker.Descriptor.DescriptorBytes
            + walker.Descriptor.HeaderBytes
            + walker.Descriptor.SeekTableBytes;
        Assert.Equal((long)expectedAudioStart, walker.Frames[0].StartOffset);
    }

    [Fact]
    public void ReadStream_AccountsForPreservedWavHeader()
    {
        // CreateWavHeaderFlag CLEAR → 44 bytes of preserved WAV header sit between
        // the seek table and the audio region. Synthetic complement to the
        // real-sample _Off / _On tests above; pins the parse path on a
        // hand-crafted minimal input.
        const uint wavHeaderBytes = 44;
        const uint apeFrameDataBytes = 100;

        var descSize = 52;
        var hdrSize = 24;
        var seekSize = 4u; // 1 entry
        var fileSize = (int)(descSize + hdrSize + seekSize + wavHeaderBytes + apeFrameDataBytes);
        var bytes = new byte[fileSize];

        Encoding.ASCII.GetBytes("MAC ", bytes.AsSpan(0, 4));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(4, 2), 3990);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), (uint)descSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12, 4), (uint)hdrSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16, 4), seekSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20, 4), wavHeaderBytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24, 4), apeFrameDataBytes);

        var hdr = bytes.AsSpan(descSize);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[0..2], 2000);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[2..4], 0); // CreateWavHeaderFlag CLEAR
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[4..8], 73728);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[8..12], 1024);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[12..16], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[16..18], 16);
        BinaryPrimitives.WriteUInt16LittleEndian(hdr[18..20], 2);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr[20..24], 44100);

        // Seek table: single entry pointing past the WAV header to the start of the audio region.
        var audioStart = (uint)(descSize + hdrSize + seekSize + wavHeaderBytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(descSize + hdrSize, 4), audioStart);

        using var ms = new MemoryStream(bytes);
        using var walker = new MacStream();
        Assert.True(walker.ReadStream(ms));

        Assert.False(walker.Header!.CreatesWavHeaderOnDecode);
        Assert.Equal(wavHeaderBytes, walker.Descriptor!.HeaderDataBytes);

        Assert.Single(walker.Frames);
        Assert.Equal(audioStart, (uint)walker.Frames[0].StartOffset);
        Assert.Equal((long)apeFrameDataBytes, walker.Frames[0].Length);
    }

    // ================================================================
    // Phase 2 dispatch — registered in MediaContainers later. Skipped
    // until that registration lands.
    // ================================================================

    [Fact]
    public void MediaContainers_DispatchesMacByMagic()
    {
        // Phase 2 (format-pack integration bundle) registered MacStream in
        // MediaContainers' walker registry and added the `MAC ` and `MACF`
        // magic-byte probes, so MediaContainers.ReadStream auto-detects .ape
        // files in O(1).
        using var fs = File.OpenRead(SamplePath);
        using var streams = MediaContainers.ReadStream(fs);
        var walker = streams.FirstOrDefault();
        Assert.NotNull(walker);
        Assert.IsType<MacStream>(walker);
    }

    /// <summary>
    /// Builds the smallest input that the Bundle C parse will accept: a 52-byte descriptor
    /// (with <c>nVersion = 3990</c> and <c>nDescriptorBytes = 52</c>) followed by a 24-byte
    /// header (with <c>nHeaderBytes = 24</c>). All other fields are zero, so the seek table
    /// and audio region are empty.
    /// </summary>
    private static byte[] MakeMinimalDescriptorPlusHeader(string magic, ushort version = 3990)
    {
        var bytes = new byte[52 + 24];
        Encoding.ASCII.GetBytes(magic, bytes.AsSpan(0, 4));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(4, 2), version);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), 52); // nDescriptorBytes
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12, 4), 24); // nHeaderBytes
        // remaining descriptor + header fields default to zero.
        return bytes;
    }
}
