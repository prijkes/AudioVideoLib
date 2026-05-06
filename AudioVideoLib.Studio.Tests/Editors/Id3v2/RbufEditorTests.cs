namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class RbufEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2RecommendedBufferSizeFrame(Id3v2Version.Id3v240)
        {
            BufferSize = 4096,
            UseEmbeddedInfo = true,
            OffsetToNextTag = 1024,
        };
        var e = new RbufEditor();
        e.Load(frame);
        var copy = new Id3v2RecommendedBufferSizeFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.BufferSize, copy.BufferSize);
        Assert.Equal(frame.UseEmbeddedInfo, copy.UseEmbeddedInfo);
        Assert.Equal(frame.OffsetToNextTag, copy.OffsetToNextTag);
    }

    [Theory]
    [InlineData(0,  0,  true)]
    [InlineData(-1, 0,  false)]
    [InlineData(0,  -1, false)]
    public void Validate_NonNegative(int buf, int off, bool expectedValid)
    {
        var e = new RbufEditor { BufferSize = buf, OffsetToNextTag = off };
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new RbufEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }
}
