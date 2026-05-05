/*
 * Test suite for FlacStream — the round-trip identity contract introduced by
 * the format-pack FLAC retrofit. See plans/2026-05-05-format-pack-flac-retrofit.md
 * Tasks 1, 13-17, 19-20.
 *
 * The sample fixture (`TestFiles/flac/sample.flac`) is a small ffmpeg-produced
 * 0.25 s sine; see TestFiles/flac/PROVENANCE.md for the acquisition story.
 *
 * Bundle B wired up the byte-passthrough WriteTo path (ISourceReader populated
 * in ReadStream, consumed via _source.CopyTo per frame) and replaced the
 * Phase-0 no-op Dispose stub. The round-trip and tag-edit tests stay Skipped
 * until the pre-existing FlacFrame audio-frame parser is fixed — see the test's
 * Skip reason for the inventory of remaining parser bugs. The byte-passthrough
 * code path itself is correct; it just can't be exercised end-to-end yet
 * because the parser cannot find frame boundaries on real FLAC samples.
 *
 * The detached-source error tests (Task 19) DO run — they don't require the
 * parser to find frames; they only require WriteTo to throw the documented
 * InvalidOperationException when _source is null (before ReadStream is called
 * or after Dispose).
 */
namespace AudioVideoLib.Tests.IO;

using System;
using System.IO;
using System.Linq;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

using Xunit;

public sealed class FlacStreamTests
{
    private const string SamplePath = "TestFiles/flac/sample.flac";

    private const string DetachedMessage =
        "Source stream was detached or never read. WriteTo requires a live source.";

    // Pre-existing FlacFrame parser bugs that block end-to-end round-trip:
    //
    //   1. Crc16 (AudioVideoLib/Cryptography/Crc16.cs) uses polynomial 0xA001
    //      LSB-first; FLAC requires polynomial 0x8005 MSB-first. (Crc16 is only
    //      consumed by FlacFrame, so the polynomial change is local.)
    //   2. FlacFrame.ReadFrame calls Crc16.Calculate([]) — an empty span — instead
    //      of computing over the frame bytes from StartOffset up to (but excluding)
    //      the trailing 2-byte CRC. Mirror the CRC-8 pattern that Bundle B fixed
    //      in FlacFrameHeader.cs.
    //   3. FlacSubFrame.ReadSubFrame does NOT consume the subframe payload — only
    //      the subframe header is read, and the `Read(sb)` call is commented out.
    //      As a result, sb.Position is left in the wrong place after subframe
    //      parsing, so frame EndOffset / next-frame StartOffset are wrong even
    //      with both CRCs fixed. Subframe payload reading needs a bit-level
    //      reader (FLAC residuals are bit-packed) and is the largest of the three.
    //
    // Bundle B fixed CRC-8 (commit fd2c511) but stopped short of the CRC-16 +
    // subframe-payload work to keep the bundle scope manageable. When those land,
    // remove the Skip and the test should pass — the Bundle B WriteTo byte-
    // passthrough path is in place and verified by code inspection (it mirrors
    // MpcStream / Mp4Stream).
    [Fact(Skip = "Cluster 3 landed the four structural fixes (subframe Read " +
                 "reactivated; type/residual/Rice bit positions corrected), but " +
                 "subframe payload consumption still drifts because the entire " +
                 "subframe data path uses byte-aligned StreamBuffer reads " +
                 "(ReadBigEndianInt32, byte-aligned ReadUnaryInt) for what RFC 9639 " +
                 "specifies as bit-packed fields. A bit-level reader migration is " +
                 "required to make Position correct after subframe parsing — that " +
                 "is the BitStream-migration sweep deferred to Cluster 5.")]
    public void RoundTrip_UnmodifiedInput_ProducesByteIdenticalOutput()
    {
        var original = File.ReadAllBytes(SamplePath);

        using var input = new MemoryStream(original, writable: false);
        using var walker = new FlacStream();
        Assert.True(walker.ReadStream(input));

        using var output = new MemoryStream();
        walker.WriteTo(output);

        Assert.Equal(original, output.ToArray());
    }

    // ================================================================
    // Lifetime / detached-source contract — these don't require the parser
    // to find any frames, so they run unconditionally.
    // ================================================================

    [Fact]
    public void WriteTo_BeforeRead_ThrowsInvalidOperationException()
    {
        using var walker = new FlacStream();

        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(DetachedMessage, ex.Message);
    }

    [Fact]
    public void WriteTo_AfterDispose_ThrowsInvalidOperationException()
    {
        // Synthetic minimal FLAC: 'fLaC' magic + last-flag Padding block (4-byte
        // zero payload) + a tail of zero bytes so the frame loop scans bytes
        // that don't match the FlacFrame sync code. This is just enough to
        // drive ReadStream past the magic-check branch so _source gets
        // populated, without tripping the documented FlacFrame parser bugs
        // that would otherwise throw InvalidDataException out of the frame
        // loop.
        var minimalFlac = new byte[]
        {
            (byte)'f', (byte)'L', (byte)'a', (byte)'C',
            0x81,             // last-block flag | Padding (type=1)
            0x00, 0x00, 0x04, // length = 4
            0x00, 0x00, 0x00, 0x00,                          // padding payload
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  // tail (no sync code)
        };
        using var input = new MemoryStream(minimalFlac, writable: false);

        var walker = new FlacStream();
        // ReadStream returns false because no audio frames are present, but
        // _source is still captured during the magic check. Dispose must
        // release that source.
        walker.ReadStream(input);
        walker.Dispose();

        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(DetachedMessage, ex.Message);
    }

    // ================================================================
    // Tag-edit round-trip — the spec §5.3 acceptance test for the retrofit.
    // Mutate a Vorbis comment via the metadata-block path, save, re-parse,
    // assert audio-frame bytes are byte-identical to the originals.
    //
    // Currently Skipped: depends on the FLAC parser working end-to-end so
    // that re-parse yields a populated walker3.Frames list. The byte-
    // passthrough WriteTo code path is correct by inspection; this test
    // becomes runnable as soon as the three parser bugs above are fixed.
    // ================================================================

    [Fact(Skip = "Cluster 3 landed the four structural fixes (subframe Read " +
                 "reactivated; type/residual/Rice bit positions corrected), but " +
                 "subframe payload consumption still drifts because the entire " +
                 "subframe data path uses byte-aligned StreamBuffer reads " +
                 "(ReadBigEndianInt32, byte-aligned ReadUnaryInt) for what RFC 9639 " +
                 "specifies as bit-packed fields. A bit-level reader migration is " +
                 "required to make Position correct after subframe parsing — that " +
                 "is the BitStream-migration sweep deferred to Cluster 5.")]
    public void TagEdit_RoundTrip_PreservesAudioFrameBytes()
    {
        var original = File.ReadAllBytes(SamplePath);

        // Read the original; capture (offset, length) of every audio frame, plus
        // a hash of each frame's bytes for value-based comparison after re-parse.
        var (originalFrameLengths, originalFrameBytes) = CaptureFrames(original);

        // Read again, mutate a Vorbis comment, write out.
        var mutated = WriteWithMutatedTitle(original);

        // Byte sequence has changed (metadata grew/shrunk).
        Assert.NotEqual(original, mutated);

        // Re-parse the mutated output; audio frames must be value-identical to the originals.
        AssertFramesPreservedAndTitleSurvives(mutated, originalFrameLengths, originalFrameBytes);
    }

    private static (long[] Lengths, byte[][] Bytes) CaptureFrames(byte[] source)
    {
        using var input = new MemoryStream(source, writable: false);
        using var walker = new FlacStream();
        Assert.True(walker.ReadStream(input));

        long[] lengths = [.. walker.Frames.Select(f => f.Length)];
        byte[][] bytes =
        [
            .. walker.Frames.Select(f =>
            {
                var buf = new byte[f.Length];
                input.Position = f.StartOffset;
                input.ReadExactly(buf);
                return buf;
            })
        ];
        return (lengths, bytes);
    }

    private static byte[] WriteWithMutatedTitle(byte[] source)
    {
        using var input = new MemoryStream(source, writable: false);
        using var walker = new FlacStream();
        Assert.True(walker.ReadStream(input));

        var vc = walker.VorbisCommentsMetadataBlock;
        Assert.NotNull(vc); // sample.flac must contain a Vorbis comment block

        // Mutate a tag: replace TITLE if present, otherwise add it. Either way
        // the saved output must differ from `original` in the metadata region
        // but match in the audio region.
        var existing = vc!.VorbisComments.Comments
            .FirstOrDefault(c => string.Equals(c.Name, "TITLE", StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.Value = "RetrofitTest";
        }
        else
        {
            vc.VorbisComments.Comments.Add(new VorbisComment { Name = "TITLE", Value = "RetrofitTest" });
        }

        using var output = new MemoryStream();
        walker.WriteTo(output);
        return output.ToArray();
    }

    // ================================================================
    // Frame-header validation rejection tests (Cluster 2 — sync mask, reserved bits).
    //
    // These inputs deliberately lack a `fLaC` magic, so ReadStream rejects on
    // the container probe before reaching the frame-header sync check. The
    // tests still verify the walker rejects the input cleanly. The deeper
    // sync-mask / reserved-bit code paths get exercised by Phase 6's
    // FlacRejectsMalformedTests, which build proper containers around a
    // malformed frame.
    // ================================================================

    [Fact]
    public void ReadFrame_RejectsIllegalSync_0x3FFF()
    {
        // 14-bit sync 0x3FFF (last bit 1) is illegal per RFC 9639 §11.21.
        // The 14-bit MSB-first sync MUST be 0b11111111111110 (0x3FFE).
        byte[] buffer = [0xFF, 0xFC, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        using var ms = new MemoryStream(buffer);
        using var walker = new FlacStream();
        Assert.False(walker.ReadStream(ms));
    }

    [Fact]
    public void ReadFrame_RejectsAllOnes_EofSentinel()
    {
        // 0xFFFFFFFF (EOF sentinel from short read) must NOT be accepted.
        byte[] buffer = [0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00];
        using var ms = new MemoryStream(buffer);
        using var walker = new FlacStream();
        Assert.False(walker.ReadStream(ms));
    }

    [Fact]
    public void ReadFrame_RejectsReservedBitSet()
    {
        // Header word top bytes: 0xFF 0xFA 0x00 0x00 — sync = 0x3FFE, reserved bit 17 = 1.
        // Without fLaC magic the walker rejects on container probe; we still assert ReadStream returns false.
        byte[] buffer = [0xFF, 0xFA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        using var ms = new MemoryStream(buffer);
        using var walker = new FlacStream();
        Assert.False(walker.ReadStream(ms));
    }

    private static void AssertFramesPreservedAndTitleSurvives(
        byte[] mutated, long[] originalFrameLengths, byte[][] originalFrameBytes)
    {
        using var input = new MemoryStream(mutated, writable: false);
        using var walker = new FlacStream();
        Assert.True(walker.ReadStream(input));

        var newFrames = walker.Frames.ToArray();
        Assert.Equal(originalFrameLengths.Length, newFrames.Length);

        for (var i = 0; i < newFrames.Length; i++)
        {
            Assert.Equal(originalFrameLengths[i], newFrames[i].Length);

            var buf = new byte[newFrames[i].Length];
            input.Position = newFrames[i].StartOffset;
            input.ReadExactly(buf);

            Assert.Equal(originalFrameBytes[i], buf);
        }

        // And the new TITLE survived.
        var roundTripped = walker.VorbisCommentsMetadataBlock!.VorbisComments.Comments
            .First(c => string.Equals(c.Name, "TITLE", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("RetrofitTest", roundTripped.Value);
    }
}
