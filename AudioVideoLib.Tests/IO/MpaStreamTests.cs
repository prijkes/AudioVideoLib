/*
 * Test suite for MpaStream — the round-trip identity contract introduced by
 * the format-pack MPA retrofit. See plans/2026-05-05-format-pack-mpa-retrofit.md
 * Tasks 1, 15, 16, 17, 18.
 *
 * Bundle A dropped the audio-frame encoder paths from MpaFrame / MpaFrameHeader /
 * MpaFrameData and added the Red round-trip test below. Bundle B wired up the
 * ISourceReader byte-passthrough loop so the round-trip identity test goes Green.
 * Bundle C (this file in its current form) adds:
 *   - detached-source error tests (Task 15),
 *   - an offset-list inspection test (Task 16),
 *   - a "WriteTo with non-zero source position" test that surfaces the
 *     absolute-vs-relative offset bug fixed in MpaStream (Task 17),
 *   - a documentation note on tag-edit round-trip (Task 18).
 *
 * The sample stream is synthesised inline (three valid MPEG-1 Layer III
 * frames at 128 kbps / 44.1 kHz / stereo with no padding and no CRC),
 * matching the header pattern used by MpaFrameHeaderTests so we don't need
 * a checked-in MP3 fixture.
 *
 * ----------------------------------------------------------------------------
 * Tag-edit round-trip note (Task 18).
 *
 * FLAC has Vorbis comments embedded inside the audio stream's metadata-block
 * chain, so its retrofit ships with a "modify-a-tag-save-reparse-audio-still-
 * identical" test. MPA is different: ID3v1 and ID3v2 are appended/prepended
 * to the audio stream, not embedded inside it. Tag mutation goes through the
 * AudioTags scanner (outside MpaStream's surface), so the relevant retrofit
 * assertion for MPA is just "WriteTo is byte-identical for unedited input"
 * (see WriteTo_RoundTripsBytesIdentically and
 * WriteTo_RoundTripsWhenSourceHasLeadingPaddingBytes — the latter simulates
 * "ID3v2 was prepended, scanner skipped past it" from the write-side
 * perspective).
 * ----------------------------------------------------------------------------
 */
namespace AudioVideoLib.Tests.IO;

using System;
using System.IO;
using System.Linq;

using AudioVideoLib.IO;

using Xunit;

public sealed class MpaStreamTests
{
    // MPEG-1 Layer III, 128 kbps, 44.1 kHz, stereo, no padding, no CRC.
    // FrameLength = 144 * 128000 / 44100 = 417 bytes per frame.
    private static readonly byte[] CanonicalHeader = [0xFF, 0xFB, 0x90, 0x00];
    private const int CanonicalFrameLength = 417;

    private const string DetachedSourceMessage =
        "Source stream was detached or never read. WriteTo requires a live source.";

    private static byte[] BuildThreeFrameStream()
    {
        var bytes = new byte[CanonicalFrameLength * 3];
        for (var i = 0; i < 3; i++)
        {
            var off = i * CanonicalFrameLength;
            Buffer.BlockCopy(CanonicalHeader, 0, bytes, off, 4);
            // Distinct payload bytes per frame so a swap would be visible.
            for (var j = 4; j < CanonicalFrameLength; j++)
            {
                bytes[off + j] = (byte)((i * 31) + (j & 0xFF));
            }
        }

        return bytes;
    }

    [Fact]
    public void WriteTo_RoundTripsBytesIdentically()
    {
        var original = BuildThreeFrameStream();

        using var input = new MemoryStream(original);
        using var walker = new MpaStream();
        Assert.True(walker.ReadStream(input));

        using var output = new MemoryStream();
        walker.WriteTo(output);

        Assert.Equal(original, output.ToArray());
    }

    [Fact]
    public void WriteTo_AfterDispose_ThrowsInvalidOperation()
    {
        var original = BuildThreeFrameStream();

        using var input = new MemoryStream(original);
        var walker = new MpaStream();
        Assert.True(walker.ReadStream(input));
        walker.Dispose();

        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(DetachedSourceMessage, ex.Message);
    }

    [Fact]
    public void WriteTo_BeforeReadStream_ThrowsInvalidOperation()
    {
        using var walker = new MpaStream();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(DetachedSourceMessage, ex.Message);
    }

    [Fact]
    public void ReadStream_PopulatesFrameStartOffsetsAndLengths()
    {
        var original = BuildThreeFrameStream();

        using var input = new MemoryStream(original);
        using var walker = new MpaStream();
        Assert.True(walker.ReadStream(input));

        var frames = walker.Frames.ToList();
        Assert.Equal(3, frames.Count);
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(i * CanonicalFrameLength, frames[i].StartOffset);
            Assert.Equal(CanonicalFrameLength, frames[i].Length);
            Assert.Equal((i + 1) * CanonicalFrameLength, frames[i].EndOffset);
        }
    }

    [Fact]
    public void WriteTo_RoundTripsWhenSourceHasLeadingPaddingBytes()
    {
        var audio = BuildThreeFrameStream();
        var leading = new byte[64]; // simulate an ID3v2 tag the outer scanner already skipped past

        var combined = new byte[leading.Length + audio.Length];
        Buffer.BlockCopy(leading, 0, combined, 0, leading.Length);
        Buffer.BlockCopy(audio, 0, combined, leading.Length, audio.Length);

        using var input = new MemoryStream(combined);
        input.Position = leading.Length;

        using var walker = new MpaStream();
        Assert.True(walker.ReadStream(input));

        using var output = new MemoryStream();
        walker.WriteTo(output);

        Assert.Equal(audio, output.ToArray());
    }
}
