/*
 * Cross-format dispatch tests for MediaContainers — verifies that the magic-byte
 * fast path in MediaContainers.ReadMediaContainer routes to the correct walker
 * for the four new format-pack containers (MPC, WavPack, TTA, MAC), and that
 * the ID3v2-aware fast path correctly skips a leading ID3v2 tag before probing.
 *
 * Test data mixes real on-disk samples (where the format-pack Phase-1 plans
 * dropped fixtures) with small synthetic byte buffers (for formats whose
 * fixtures haven't landed yet — see TestFiles/mpc/PROVENANCE.md and
 * TestFiles/mac/PROVENANCE.md). Rows whose backing real-sample file is
 * missing on disk are filtered out at MemberData time so the suite stays
 * green on machines that haven't fetched the optional corpus.
 */
namespace AudioVideoLib.Tests.IO;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.IO;

using Xunit;

public sealed class MediaContainersTests
{
    private const string WavPackSamplePath = "TestFiles/wavpack/sample-stereo-44100-16.wv";
    private const string TtaSamplePath = "TestFiles/tta/sample-stereo-16bit.tta";
    private const string MacIntegerSamplePath = "TestFiles/mac/sample.ape";
    private const string MpcSv7SamplePath = "TestFiles/mpc/sample-sv7.mpc";
    private const string MpcSv8SamplePath = "TestFiles/mpc/sample-sv8.mpc";
    private const string MacFloatSamplePath = "TestFiles/mac/sample-float.ape";

    /// <summary>
    /// Returns the row-set for the cross-format dispatch theories. Each row carries:
    /// <list type="bullet">
    ///   <item>A label (used in the test display name to identify which row failed).</item>
    ///   <item>A byte[] payload — the raw container input the dispatcher will probe.</item>
    ///   <item>The expected walker <see cref="Type"/>.</item>
    /// </list>
    /// Rows that depend on a real on-disk sample are skipped at MemberData time when
    /// the file isn't present (the optional sample corpus isn't checked into git for
    /// every format yet — see the per-format <c>PROVENANCE.md</c> fragments).
    /// </summary>
    public static IEnumerable<object[]> NewFormatDispatchData()
    {
        // Real-sample rows. Filtered out if the file isn't on disk.
        if (File.Exists(WavPackSamplePath))
        {
            yield return ["wavpack", File.ReadAllBytes(WavPackSamplePath), typeof(WavPackStream)];
        }

        if (File.Exists(TtaSamplePath))
        {
            yield return ["tta", File.ReadAllBytes(TtaSamplePath), typeof(TtaStream)];
        }

        if (File.Exists(MacIntegerSamplePath))
        {
            yield return ["mac-integer", File.ReadAllBytes(MacIntegerSamplePath), typeof(MacStream)];
        }

        // Optional real-sample rows that are usually missing — pulled in if/when
        // a future bundle drops them into TestFiles/.
        if (File.Exists(MpcSv7SamplePath))
        {
            yield return ["mpc-sv7-real", File.ReadAllBytes(MpcSv7SamplePath), typeof(MpcStream)];
        }

        if (File.Exists(MpcSv8SamplePath))
        {
            yield return ["mpc-sv8-real", File.ReadAllBytes(MpcSv8SamplePath), typeof(MpcStream)];
        }

        if (File.Exists(MacFloatSamplePath))
        {
            yield return ["mac-float-real", File.ReadAllBytes(MacFloatSamplePath), typeof(MacStream)];
        }

        // Synthetic-buffer rows. These cover dispatch for formats whose real
        // samples aren't checked in (per the bundle-D notes in the format-pack
        // integration plan). The buffers are crafted minimal enough that the
        // walker's ReadStream succeeds, which is what MediaContainers requires
        // before it commits to the chosen walker.
        yield return ["mpc-sv7-synthetic", BuildSyntheticMpcSv7(), typeof(MpcStream)];
        yield return ["mac-float-synthetic", BuildSyntheticMacDescriptor("MACF"), typeof(MacStream)];
    }

    /// <summary>
    /// Verifies that <see cref="MediaContainers.ReadStream(Stream)"/> dispatches to the
    /// expected walker on a raw (non-tagged) container input.
    /// </summary>
    /// <param name="label">Display label identifying which row this is (parameter unused at runtime).</param>
    /// <param name="bytes">The container payload.</param>
    /// <param name="expectedWalker">The walker type expected to be selected.</param>
    [Theory]
    [MemberData(nameof(NewFormatDispatchData))]
    public void Dispatch_NewFormats_ReturnsExpectedWalker(string label, byte[] bytes, Type expectedWalker)
    {
        _ = label; // surfaced in the test display name; not consumed by the assertion
        using var fs = new MemoryStream(bytes);
        using var streams = MediaContainers.ReadStream(fs);
        var walker = streams.FirstOrDefault();
        Assert.NotNull(walker);
        Assert.IsType(expectedWalker, walker);
    }

    /// <summary>
    /// Verifies the ID3v2-aware fast path: prefixes the same payload with a minimal
    /// ID3v2.4 header (10-byte header, 0-byte body, no footer) and asserts the
    /// dispatcher still selects the correct walker. The fast path computes the
    /// post-tag offset from the syncsafe size field and re-runs the magic probe
    /// at that offset — without it, the brute-force scan would still find the
    /// container but only after a per-byte rescan.
    /// </summary>
    /// <param name="label">Display label identifying which row this is (parameter unused at runtime).</param>
    /// <param name="bytes">The container payload that will be ID3v2-prefixed.</param>
    /// <param name="expectedWalker">The walker type expected to be selected.</param>
    [Theory]
    [MemberData(nameof(NewFormatDispatchData))]
    public void Dispatch_NewFormatsWithId3v2Prefix_ReturnsExpectedWalker(string label, byte[] bytes, Type expectedWalker)
    {
        _ = label;
        var prefixed = PrefixWithMinimalId3v2(bytes);

        using var fs = new MemoryStream(prefixed);
        using var streams = MediaContainers.ReadStream(fs);
        var walker = streams.FirstOrDefault();
        Assert.NotNull(walker);
        Assert.IsType(expectedWalker, walker);
    }

    /// <summary>
    /// Splices a 10-byte ID3v2.4 header (zero-length body, no footer) onto the front
    /// of <paramref name="payload"/>. The resulting buffer is what the ID3v2-aware
    /// fast path in <see cref="MediaContainers"/> is expected to skip past.
    /// </summary>
    private static byte[] PrefixWithMinimalId3v2(byte[] payload)
    {
        var prefixed = new byte[10 + payload.Length];
        // "ID3" magic.
        prefixed[0] = 0x49;
        prefixed[1] = 0x44;
        prefixed[2] = 0x33;
        // Version 2.4.0.
        prefixed[3] = 0x04;
        prefixed[4] = 0x00;
        // Flags: no footer, no extended header.
        prefixed[5] = 0x00;
        // Syncsafe size = 0 (zero-length tag body).
        prefixed[6] = 0x00;
        prefixed[7] = 0x00;
        prefixed[8] = 0x00;
        prefixed[9] = 0x00;
        Buffer.BlockCopy(payload, 0, prefixed, 10, payload.Length);
        return prefixed;
    }

    /// <summary>
    /// Builds a minimal SV7 Musepack container that <see cref="MpcStream.ReadStream"/>
    /// will accept. Layout: 4-byte <c>'M','P','+',0x17</c> magic + 24 zero-bytes for the
    /// SV7 stream-info bit-packed header. With all zeros the parsed fields collapse to
    /// the safe defaults (<c>frames=0</c>, <c>lastFrameSamples=0 → 1152</c>) and the
    /// audio-span packet has zero length — exactly what the dispatch test needs.
    /// </summary>
    private static byte[] BuildSyntheticMpcSv7()
    {
        var buf = new byte[4 + 24];
        buf[0] = 0x4D; // 'M'
        buf[1] = 0x50; // 'P'
        buf[2] = 0x2B; // '+'
        buf[3] = 0x17; // low nibble = 7 (SV7 marker)
        // Remaining 24 bytes are zero.
        return buf;
    }

    /// <summary>
    /// Builds the smallest input that <see cref="MacStream.ReadStream"/> will accept:
    /// a 52-byte APE_DESCRIPTOR (with <c>nVersion = 3990</c> and <c>nDescriptorBytes = 52</c>)
    /// followed by a 24-byte APE_HEADER (with <c>nHeaderBytes = 24</c>). All other fields
    /// default to zero so the seek-table and audio regions are empty. Mirrors the helper in
    /// <c>MacStreamTests</c> — kept as a private duplicate to avoid coupling the dispatch
    /// suite to that test class's internals.
    /// </summary>
    private static byte[] BuildSyntheticMacDescriptor(string magic, ushort version = 3990)
    {
        var bytes = new byte[52 + 24];
        Encoding.ASCII.GetBytes(magic, bytes.AsSpan(0, 4));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(4, 2), version);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), 52);  // nDescriptorBytes
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12, 4), 24); // nHeaderBytes
        return bytes;
    }
}
