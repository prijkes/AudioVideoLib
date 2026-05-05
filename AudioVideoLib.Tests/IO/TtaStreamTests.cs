/*
 * Test suite for TtaStream — walks a TrueAudio (.tta) container, exposes the
 * parsed TTA1 fixed header + per-frame seek-table-derived ranges, and round-
 * trips the byte range verbatim through WriteTo. See spec §7.2.
 *
 * Sample fixtures generated via ffmpeg's built-in `tta` encoder; the
 * APEv2-tagged variant uses the in-tree ApeTag serializer (mirrors WavPack
 * Bundle D). See TestFiles/tta/PROVENANCE.md for the full acquisition story.
 */
namespace AudioVideoLib.Tests.IO;

using System;
using System.IO;
using System.Linq;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

using Xunit;

public sealed class TtaStreamTests
{
    private const string SamplePath = "TestFiles/tta/sample-stereo-16bit.tta";
    private const string SampleWithTagPath = "TestFiles/tta/sample-with-apev2.tta";
    private const string DetachedMessage =
        "Source stream was detached or never read. WriteTo requires a live source.";

    // ================================================================
    // Lifetime / detached-source contract — null-source case is
    // unconditional; dispose case needs a real sample to populate state.
    // ================================================================

    [Fact]
    public void WriteTo_ThrowsWhenSourceIsNull()
    {
        using var walker = new TtaStream();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(DetachedMessage, ex.Message);
    }

    [Fact]
    public void WriteTo_ThrowsAfterDispose()
    {
        var walker = new TtaStream();
        using (var fs = File.OpenRead(SamplePath))
        {
            Assert.True(walker.ReadStream(fs));
        }

        walker.Dispose();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(DetachedMessage, ex.Message);
    }

    // ================================================================
    // Header parse — sample required.
    // ================================================================

    [Fact]
    public void ReadStream_ParsesTta1Header()
    {
        using var fs = File.OpenRead(SamplePath);
        using var walker = new TtaStream();

        Assert.True(walker.ReadStream(fs));

        var header = walker.Header;
        Assert.NotNull(header);
        Assert.Equal(1, header!.Format);              // TTA_FORMAT_SIMPLE
        Assert.Equal(2, header.NumChannels);
        Assert.Equal(16, header.BitsPerSample);
        Assert.Equal(44100u, header.SampleRate);
        Assert.True(header.TotalSamples > 0);
    }

    // ================================================================
    // Frame enumeration — sample required.
    // ================================================================

    [Fact]
    public void ReadStream_EnumeratesFrames()
    {
        using var fs = File.OpenRead(SamplePath);
        using var walker = new TtaStream();

        Assert.True(walker.ReadStream(fs));

        Assert.NotNull(walker.SeekTable);
        Assert.Equal(walker.SeekTable!.FrameSizes.Count, walker.Frames.Count);
        Assert.NotEmpty(walker.Frames);

        // Sum of frame lengths matches the audio span between the first
        // frame's start and the byte just past the last frame.
        long sumLen = 0;
        foreach (var f in walker.Frames)
        {
            sumLen += f.Length;
        }

        var first = walker.Frames[0];
        var last = walker.Frames[^1];
        Assert.Equal(sumLen, last.StartOffset + last.Length - first.StartOffset);

        // Standard frame length is 256 * sps / 245; only the last frame
        // may differ (libtta MUL_FRAME_TIME, libtta.c:274).
        var expectedStd = (uint)(256UL * walker.Header!.SampleRate / 245UL);
        for (var i = 0; i < walker.Frames.Count - 1; i++)
        {
            Assert.Equal(expectedStd, walker.Frames[i].SampleCount);
        }

        // Per-frame size equals the seek-table entry.
        for (var i = 0; i < walker.Frames.Count; i++)
        {
            Assert.Equal(walker.SeekTable.FrameSizes[i], (uint)walker.Frames[i].Length);
        }
    }

    // ================================================================
    // Round-trip identity — sample required.
    // ================================================================

    [Fact]
    public void WriteTo_RoundTripsByteIdentical()
    {
        var original = File.ReadAllBytes(SamplePath);

        using var fs = new MemoryStream(original, writable: false);
        using var walker = new TtaStream();
        Assert.True(walker.ReadStream(fs));

        using var output = new MemoryStream();
        walker.WriteTo(output);

        // The walker emits the source view (StartOffset .. EndOffset). For
        // the clean fixture there is no tag prefix or footer, so this
        // reduces to a whole-file equality check.
        var expected = original.AsSpan(
            (int)walker.StartOffset,
            (int)(walker.EndOffset - walker.StartOffset)).ToArray();
        Assert.Equal(expected, output.ToArray());
    }

    // ================================================================
    // Tag-edit round-trip — audio frame bytes must survive a tag mutation
    // routed through AudioTags / ApeTag, byte-for-byte. Spec §7.2 #4.
    // ================================================================

    [Fact]
    public void TagEdit_PreservesAudioBytes()
    {
        var original = File.ReadAllBytes(SampleWithTagPath);

        // Capture each original frame's byte slice (start, length, payload).
        long[] origStarts;
        long[] origLengths;
        byte[][] origFrameBytes;
        using (var fs = new MemoryStream(original, writable: false))
        using (var walker = new TtaStream())
        {
            Assert.True(walker.ReadStream(fs));
            Assert.NotEmpty(walker.Frames);
            origStarts = [.. walker.Frames.Select(f => f.StartOffset)];
            origLengths = [.. walker.Frames.Select(f => f.Length)];
            origFrameBytes =
            [
                .. walker.Frames.Select(f => original.AsSpan((int)f.StartOffset, (int)f.Length).ToArray())
            ];
        }

        // Drive the tag edit via AudioTags' canonical APEv2 path (mirrors
        // WavPack Bundle D): scan -> mutate -> emit. AudioVideoLib does
        // not expose a one-shot "edit and save" facade; the composition is
        // parse with AudioTags + serialize via ApeTag.ToByteArray.
        byte[] edited;
        using (var ms = new MemoryStream(original, writable: false))
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

            // Mutate the Title field — confirms the round-trip survives an
            // actual change to the APEv2 payload, not a no-op.
            var newTitle = new ApeUtf8Item(apeTag!.Version, "Title");
            newTitle.Values.Add("TTA walker test (edited)");
            apeTag.SetItem(newTitle);

            // Re-emit: keep the audio span verbatim, replace the tag bytes.
            using var output = new MemoryStream();
            output.Write(original, 0, (int)apeOffset!.StartOffset);
            output.Write(apeTag.ToByteArray());
            edited = output.ToArray();
        }

        // Re-parse the saved bytes: every frame's range must point to the
        // same payload bytes as before the edit.
        using var fs2 = new MemoryStream(edited, writable: false);
        using var walker2 = new TtaStream();
        Assert.True(walker2.ReadStream(fs2));
        Assert.Equal(origStarts.Length, walker2.Frames.Count);
        for (var i = 0; i < walker2.Frames.Count; i++)
        {
            var f = walker2.Frames[i];
            Assert.Equal(origStarts[i], f.StartOffset);
            Assert.Equal(origLengths[i], f.Length);
            var sliced = edited.AsSpan((int)f.StartOffset, (int)f.Length).ToArray();
            Assert.Equal(origFrameBytes[i], sliced);
        }
    }

    // ================================================================
    // Magic-byte / direct-invocation dispatch — Phase 1 stand-in for the
    // MediaContainers.ReadStream walk-by-magic test, which lands in
    // Phase 2 once the registry update is in.
    // ================================================================

    [Fact]
    public void MediaContainers_DispatchesTtaByMagic()
    {
        // Phase 2 (format-pack integration bundle) registered TtaStream in
        // MediaContainers' walker registry and added the `TTA1` magic-byte probe,
        // so MediaContainers.ReadStream auto-detects .tta files in O(1).
        using var fs = File.OpenRead(SamplePath);
        using var streams = MediaContainers.ReadStream(fs);
        var walker = streams.FirstOrDefault();
        Assert.NotNull(walker);
        Assert.IsType<TtaStream>(walker);
    }
}
