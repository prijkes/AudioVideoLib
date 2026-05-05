/*
 * Phase 6 / Task 28 of the FLAC parser revival
 * (plans/2026-05-05-flac-parser-revival.md).
 *
 * Theory-driven STREAMINFO header assertions over the synthetic corpus.
 * Each row asserts that ReadStream succeeds AND the STREAMINFO block
 * reports the expected sample rate / channel count / bits-per-sample.
 *
 * Reference-corpus rows are not asserted with hard-coded header values
 * here (the IETF cellar files are exotic and not all share trivial header
 * values); instead, the simpler `Reference_ParsesWithoutError` theory
 * just asserts that ReadStream succeeds on each. That's enough to catch
 * a regression that breaks parsing of any spec-compliant subset file.
 *
 * Tests with a missing corpus file fall through silently (PASS) so a
 * partial corpus does not break CI.
 */
namespace AudioVideoLib.Tests.IO;

using System.Collections.Generic;
using System.IO;
using System.Linq;

using AudioVideoLib.IO;

using Xunit;

public sealed class FlacParserComplianceTests
{
    public static IEnumerable<object[]> SyntheticCorpus =>
    [
        ["TestFiles/flac/synthetic/sample-silent-stereo-44100-16.flac",   44100, 2, 16],
        ["TestFiles/flac/synthetic/sample-sine-stereo-44100-16.flac",     44100, 2, 16],
        ["TestFiles/flac/synthetic/sample-sine-mono-48000-24.flac",       48000, 1, 24],
        ["TestFiles/flac/synthetic/sample-sine-stereo-96000-24.flac",     96000, 2, 24],
        ["TestFiles/flac/synthetic/sample-sine-stereo-48000-16-c0.flac",  48000, 2, 16],
        ["TestFiles/flac/synthetic/sample-noise-mono-44100-16.flac",      44100, 1, 16],
        ["TestFiles/flac/synthetic/sample-sine-mono-22050-16.flac",       22050, 1, 16],
    ];

    public static IEnumerable<object[]> ReferenceCorpus =>
    [
        ["TestFiles/flac/reference/11_-_partition_order_8.flac"],
        ["TestFiles/flac/reference/14_-_wasted_bits.flac"],
        ["TestFiles/flac/reference/15_-_only_verbatim_subframes.flac"],
        ["TestFiles/flac/reference/17_-_all_fixed_orders.flac"],
        ["TestFiles/flac/reference/21_-_samplerate_22050Hz.flac"],
    ];

    [Theory]
    [MemberData(nameof(SyntheticCorpus))]
    public void Synthetic_HeaderMatches(string path, int expectedSampleRate, int expectedChannels, int expectedBitsPerSample)
    {
        if (!File.Exists(path))
        {
            return;
        }

        using var fs = File.OpenRead(path);
        using var walker = new FlacStream();
        Assert.True(walker.ReadStream(fs));

        var streamInfo = walker.StreamInfoMetadataBlocks.FirstOrDefault();
        Assert.NotNull(streamInfo);
        Assert.Equal(expectedSampleRate, streamInfo!.SampleRate);
        Assert.Equal(expectedChannels, streamInfo.Channels);
        Assert.Equal(expectedBitsPerSample, streamInfo.BitsPerSample);
    }

    [Theory]
    [MemberData(nameof(ReferenceCorpus))]
    public void Reference_ParsesWithoutError(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        using var fs = File.OpenRead(path);
        using var walker = new FlacStream();
        Assert.True(walker.ReadStream(fs));

        var streamInfo = walker.StreamInfoMetadataBlocks.FirstOrDefault();
        Assert.NotNull(streamInfo);

        // Sanity bounds — StreamInfo fields must be in spec-allowed ranges.
        Assert.InRange(streamInfo!.SampleRate, 1, 655350);
        Assert.InRange(streamInfo.Channels, 1, 8);
        Assert.InRange(streamInfo.BitsPerSample, 4, 32);
    }
}
