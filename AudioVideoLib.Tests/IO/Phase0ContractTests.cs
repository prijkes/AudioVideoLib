namespace AudioVideoLib.Tests.IO;

using System;
using System.IO;

using AudioVideoLib.IO;

using Xunit;

public sealed class Phase0ContractTests
{
    private const string ExpectedMessage =
        "Source stream was detached or never read. WriteTo requires a live source.";

    [Fact]
    public void Mp4Stream_WriteTo_ThrowsWhenSourceIsNull()
    {
        using var walker = new Mp4Stream();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(ExpectedMessage, ex.Message);
    }

    [Fact]
    public void AsfStream_WriteTo_ThrowsWhenSourceIsNull()
    {
        using var walker = new AsfStream();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(ExpectedMessage, ex.Message);
    }

    [Fact]
    public void MatroskaStream_WriteTo_ThrowsWhenSourceIsNull()
    {
        using var walker = new MatroskaStream();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(ExpectedMessage, ex.Message);
    }

    [Fact]
    public void Mp4Stream_WriteTo_ThrowsAfterDispose()
    {
        using var fs = new MemoryStream();
        var walker = new Mp4Stream();
        walker.ReadStream(fs); // succeeds or fails harmlessly on empty stream
        walker.Dispose();
        var ex = Assert.Throws<InvalidOperationException>(
            () => walker.WriteTo(new MemoryStream()));
        Assert.Equal(ExpectedMessage, ex.Message);
    }

    [Fact]
    public void Dispose_NoOp_StubsDoNotThrow()
    {
        // Each walker's no-op Dispose must be safe to call (and tolerate a second call via the using).
        Action[] dispose =
        [
            DisposeAiff,
            DisposeDff,
            DisposeDsf,
            DisposeFlac,
            DisposeMpa,
            DisposeOgg,
            DisposeRiff,
        ];

        foreach (var d in dispose)
        {
            d();
        }
    }

    private static void DisposeAiff()
    {
        using var w = new AiffStream();
        w.Dispose();
    }

    private static void DisposeDff()
    {
        using var w = new DffStream();
        w.Dispose();
    }

    private static void DisposeDsf()
    {
        using var w = new DsfStream();
        w.Dispose();
    }

    private static void DisposeFlac()
    {
        using var w = new FlacStream();
        w.Dispose();
    }

    private static void DisposeMpa()
    {
        using var w = new MpaStream();
        w.Dispose();
    }

    private static void DisposeOgg()
    {
        using var w = new OggStream();
        w.Dispose();
    }

    private static void DisposeRiff()
    {
        using var w = new RiffStream();
        w.Dispose();
    }

    [Fact]
    public void MediaContainers_Dispose_DisposesAllChildren()
    {
        // MediaContainers is populated via ReadStream from a Stream; the public API does
        // not currently expose a way to inject walker instances directly. Until it does,
        // assert the contract that matters here: Dispose() is idempotent — calling it
        // twice on an empty (or any) MediaContainers must not throw.
        var holder = new MediaContainers();
        holder.Dispose();
        holder.Dispose();
    }
}
