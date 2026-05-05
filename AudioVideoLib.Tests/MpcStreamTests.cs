/*
 * Test suite for MpcStream — walks an SV7 or SV8 Musepack container, exposes
 * the parsed header + per-packet offsets/lengths, and round-trips the byte
 * range verbatim through WriteTo. See spec §7.2.
 */
namespace AudioVideoLib.Tests;

using System;
using System.IO;
using System.Linq;

using AudioVideoLib.Formats;
using AudioVideoLib.IO;

using Xunit;

public class MpcStreamTests
{
    private const string Sv7Sample = "TestFiles/mpc/sample-sv7.mpc";
    private const string Sv8Sample = "TestFiles/mpc/sample-sv8.mpc";
    private const string DetachedMessage =
        "Source stream was detached or never read. WriteTo requires a live source.";
    private const string NoSampleSkip = "no MPC sample available";

    // ================================================================
    // Lifetime — no sample required.
    // ================================================================

    [Fact]
    public void WriteTo_ThrowsWhenSourceIsNull()
    {
        using var walker = new MpcStream();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(DetachedMessage, ex.Message);
    }

    [Fact(Skip = NoSampleSkip)]
    public void WriteTo_ThrowsAfterDispose()
    {
        using var fs = File.OpenRead(Sv8Sample);
        var walker = new MpcStream();
        Assert.True(walker.ReadStream(fs));
        walker.Dispose();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(DetachedMessage, ex.Message);
    }

    // ================================================================
    // Header parse — sample required.
    // ================================================================

    [Fact(Skip = NoSampleSkip)]
    public void ReadStream_Sv7_ParsesHeader()
    {
        using var fs = File.OpenRead(Sv7Sample);
        using var walker = new MpcStream();
        Assert.True(walker.ReadStream(fs));
        Assert.Equal(MpcStreamVersion.Sv7, walker.Version);
        Assert.NotNull(walker.Header);
        Assert.Equal(44100u, walker.Header!.SampleRate);
        Assert.Equal(2u, walker.Header.Channels);
        Assert.True(walker.Header.TotalSamples > 0);
    }

    [Fact(Skip = NoSampleSkip)]
    public void ReadStream_Sv8_ParsesHeader()
    {
        using var fs = File.OpenRead(Sv8Sample);
        using var walker = new MpcStream();
        Assert.True(walker.ReadStream(fs));
        Assert.Equal(MpcStreamVersion.Sv8, walker.Version);
        Assert.NotNull(walker.Header);
        Assert.Equal(44100u, walker.Header!.SampleRate);
        Assert.Equal(2u, walker.Header.Channels);
        Assert.True(walker.Header.TotalSamples > 0);
    }

    // ================================================================
    // Packet enumeration — sample required.
    // ================================================================

    [Fact(Skip = NoSampleSkip)]
    public void ReadStream_Sv7_RecordsSingleAudioPacketWithNullKey()
    {
        using var fs = File.OpenRead(Sv7Sample);
        using var walker = new MpcStream();
        Assert.True(walker.ReadStream(fs));
        Assert.Single(walker.Packets);
        Assert.Null(walker.Packets[0].Key);
        Assert.True(walker.Packets[0].Length > 0);
    }

    [Fact(Skip = NoSampleSkip)]
    public void ReadStream_Sv8_RecordsKeyedPackets()
    {
        using var fs = File.OpenRead(Sv8Sample);
        using var walker = new MpcStream();
        Assert.True(walker.ReadStream(fs));
        Assert.Contains(walker.Packets, p => p.Key == "SH");
        Assert.Contains(walker.Packets, p => p.Key == "AP");
        Assert.Contains(walker.Packets, p => p.Key == "SE");

        var sum = walker.Packets.Sum(p => p.Length);
        var expected = new FileInfo(Sv8Sample).Length - 4; // minus 'MPCK'
        Assert.Equal(expected, sum);
    }

    // ================================================================
    // Round-trip identity — sample required.
    // ================================================================

    [Fact(Skip = NoSampleSkip)]
    public void WriteTo_Sv7_RoundTripsByteIdentical()
    {
        var original = File.ReadAllBytes(Sv7Sample);
        using var src = new MemoryStream(original);
        using var walker = new MpcStream();
        Assert.True(walker.ReadStream(src));
        using var dst = new MemoryStream();
        walker.WriteTo(dst);
        Assert.Equal(original, dst.ToArray());
    }

    [Fact(Skip = NoSampleSkip)]
    public void WriteTo_Sv8_RoundTripsByteIdentical()
    {
        var original = File.ReadAllBytes(Sv8Sample);
        using var src = new MemoryStream(original);
        using var walker = new MpcStream();
        Assert.True(walker.ReadStream(src));
        using var dst = new MemoryStream();
        walker.WriteTo(dst);
        Assert.Equal(original, dst.ToArray());
    }

    // ================================================================
    // Magic-byte dispatch — no sample required for the rejection cases.
    // ================================================================

    [Fact]
    public void ReadStream_RejectsForeignMagic()
    {
        using var src = new MemoryStream([0x66, 0x4C, 0x61, 0x43, 0, 0, 0, 0]); // 'fLaC'
        using var walker = new MpcStream();
        Assert.False(walker.ReadStream(src));
    }

    [Fact]
    public void ReadStream_RejectsBareMpPlusWithWrongVersionNibble()
    {
        // 'M','P','+',0x18 — low nibble 8, NOT 7. Must not be misidentified as SV7.
        using var src = new MemoryStream([0x4D, 0x50, 0x2B, 0x18, 0, 0, 0, 0]);
        using var walker = new MpcStream();
        Assert.False(walker.ReadStream(src));
    }

    [Fact(Skip = NoSampleSkip)]
    public void ReadStream_HonoursNonZeroStartOffset()
    {
        // Simulate ID3v2-prefixed file: caller advanced past the tag.
        var raw = File.ReadAllBytes(Sv8Sample);
        var prefix = new byte[64];
        new Random(0).NextBytes(prefix);
        var combined = new byte[prefix.Length + raw.Length];
        Buffer.BlockCopy(prefix, 0, combined, 0, prefix.Length);
        Buffer.BlockCopy(raw, 0, combined, prefix.Length, raw.Length);

        using var src = new MemoryStream(combined);
        src.Position = prefix.Length;

        using var walker = new MpcStream();
        Assert.True(walker.ReadStream(src));
        Assert.Equal(prefix.Length, walker.StartOffset);
        Assert.Equal(combined.Length, walker.EndOffset);

        using var dst = new MemoryStream();
        walker.WriteTo(dst);
        Assert.Equal(raw, dst.ToArray());
    }

    // ================================================================
    // Tag-edit round-trip — wired-through skip; AudioTags surface for MPC
    // is a Phase 2 concern (see plan task 16). Even with a sample, the
    // walker-level write API would need an additional integration point.
    // ================================================================

    [Fact(Skip = "AudioTags write API exposed in Phase 2")]
    public void TagEdit_PreservesAudioBytesByteForByte()
    {
        var original = File.ReadAllBytes(Sv8Sample);

        byte[] originalAudio;
        using (var src = new MemoryStream(original))
        using (var walker = new MpcStream())
        {
            Assert.True(walker.ReadStream(src));
            using var ms = new MemoryStream();
            foreach (var pkt in walker.Packets.Where(p => p.Key == "AP"))
            {
                ms.Write(original, (int)pkt.StartOffset, (int)pkt.Length);
            }
            originalAudio = ms.ToArray();
        }

        // Phase 2: drive AudioTags here to add/modify an APEv2 field, write the tagged
        // result to a MemoryStream `tagged`, re-parse with MpcStream, and assert the
        // AP-packet payload bytes match `originalAudio`.
        Assert.NotEmpty(originalAudio);
    }
}
