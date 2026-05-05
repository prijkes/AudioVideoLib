/*
 * Phase 6 / Task 29 of the FLAC parser revival
 * (plans/2026-05-05-flac-parser-revival.md).
 *
 * Pathological-input rejection tests. Each case feeds a deliberately
 * malformed byte sequence to FlacStream.ReadStream and asserts the
 * walker returns `false` cleanly (no exception leaks past the
 * documented contract).
 */
namespace AudioVideoLib.Tests.IO;

using System.IO;

using AudioVideoLib.IO;

using Xunit;

public sealed class FlacRejectsMalformedTests
{
    [Fact]
    public void RejectsTruncatedMetadataBlock()
    {
        // 'fLaC' magic + STREAMINFO header claiming 34 bytes (0x22) of body,
        // but only 5 bytes of body actually follow.
        byte[] bytes =
        [
            0x66, 0x4C, 0x61, 0x43, // "fLaC"
            0x80,                   // last-block flag | type 0 (STREAMINFO)
            0x00, 0x00, 0x22,       // 24-bit length = 34
            0x00, 0x01, 0x02, 0x03, 0x04, // only 5 of 34 body bytes follow
        ];
        using var ms = new MemoryStream(bytes);
        using var walker = new FlacStream();
        Assert.False(walker.ReadStream(ms));
    }

    [Fact]
    public void RejectsAllOnesInput()
    {
        // EOF sentinel / all-ones must not be accepted as a valid FLAC stream:
        // it has no 'fLaC' magic and the frame-sync check would otherwise
        // accidentally accept 0xFFFFFFFF if the sync mask were too loose.
        byte[] bytes = [0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00];
        using var ms = new MemoryStream(bytes);
        using var walker = new FlacStream();
        Assert.False(walker.ReadStream(ms));
    }

    [Fact]
    public void RejectsLengthPastEofMetadataBlock()
    {
        // Metadata header claims length = 0xFFFFFF (16 MiB) but only 2 body
        // bytes remain. The fixed length check must reject without reading.
        byte[] bytes =
        [
            0x66, 0x4C, 0x61, 0x43, // "fLaC"
            0x80,                   // last-block flag | type 0 (STREAMINFO)
            0xFF, 0xFF, 0xFF,       // 24-bit length = 16777215
            0x00, 0x00,             // only 2 body bytes
        ];
        using var ms = new MemoryStream(bytes);
        using var walker = new FlacStream();
        Assert.False(walker.ReadStream(ms));
    }

    [Fact]
    public void RejectsEmptyStream()
    {
        using var ms = new MemoryStream([]);
        using var walker = new FlacStream();
        Assert.False(walker.ReadStream(ms));
    }

    [Fact]
    public void RejectsMissingMagic()
    {
        // Looks plausible but the magic bytes are wrong ('fLac' lowercase 'a').
        byte[] bytes =
        [
            0x66, 0x4C, 0x61, 0x63, // "fLac" — wrong case
            0x80, 0x00, 0x00, 0x22,
        ];
        using var ms = new MemoryStream(bytes);
        using var walker = new FlacStream();
        Assert.False(walker.ReadStream(ms));
    }
}
