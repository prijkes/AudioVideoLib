/*
 * Phase 6 / Task 27 of the FLAC parser revival
 * (plans/2026-05-05-flac-parser-revival.md).
 *
 * Direct CRC-vector tests live in `Cryptography/Crc16Tests.cs`. The tests
 * here are an integration check between the parser and the CRC primitive —
 * they assert that real FLAC frames in the synthetic corpus are accepted
 * by the strict-rejection walker (which validates frame-footer CRC-16 on
 * every frame as a side effect of `ReadStream`).
 *
 * Tests fall through silently (PASS) when the corpus file is missing, so
 * a partial corpus does not break CI.
 */
namespace AudioVideoLib.Tests.IO;

using System.IO;
using System.Linq;

using AudioVideoLib.IO;

using Xunit;

public sealed class FlacFrameCrcTests
{
    [Fact]
    public void SyntheticSineSample_AllFramesValidate()
    {
        const string Path = "TestFiles/flac/synthetic/sample-sine-stereo-44100-16.flac";
        if (!File.Exists(Path))
        {
            return;
        }

        using var fs = File.OpenRead(Path);
        using var walker = new FlacStream();
        Assert.True(walker.ReadStream(fs));
        Assert.NotEmpty(walker.Frames);
    }

    [Fact]
    public void SyntheticSilentSample_AllFramesValidate()
    {
        const string Path = "TestFiles/flac/synthetic/sample-silent-stereo-44100-16.flac";
        if (!File.Exists(Path))
        {
            return;
        }

        using var fs = File.OpenRead(Path);
        using var walker = new FlacStream();
        Assert.True(walker.ReadStream(fs));
        Assert.NotEmpty(walker.Frames);
    }

    [Fact]
    public void SyntheticNoiseSample_AllFramesValidate()
    {
        const string Path = "TestFiles/flac/synthetic/sample-noise-mono-44100-16.flac";
        if (!File.Exists(Path))
        {
            return;
        }

        using var fs = File.OpenRead(Path);
        using var walker = new FlacStream();
        Assert.True(walker.ReadStream(fs));
        Assert.NotEmpty(walker.Frames);
    }

    [Fact]
    public void SyntheticHighRateSample_AllFramesValidate()
    {
        const string Path = "TestFiles/flac/synthetic/sample-sine-stereo-96000-24.flac";
        if (!File.Exists(Path))
        {
            return;
        }

        using var fs = File.OpenRead(Path);
        using var walker = new FlacStream();
        Assert.True(walker.ReadStream(fs));
        Assert.NotEmpty(walker.Frames);

        // Sanity check: at least one frame has a non-empty byte range so a
        // stale parser can't trivially satisfy NotEmpty with zero-length frames.
        Assert.All(walker.Frames, f => Assert.True(f.Length > 0));
    }

    [Fact]
    public void ReferenceWastedBitsSample_AllFramesValidate()
    {
        const string Path = "TestFiles/flac/reference/14_-_wasted_bits.flac";
        if (!File.Exists(Path))
        {
            return;
        }

        using var fs = File.OpenRead(Path);
        using var walker = new FlacStream();
        Assert.True(walker.ReadStream(fs));
        Assert.NotEmpty(walker.Frames);
        _ = walker.Frames.Count();
    }
}
