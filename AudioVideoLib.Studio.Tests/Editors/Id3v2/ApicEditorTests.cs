namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class ApicEditorTests
{
    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var f = new ApicEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v240, f.Version);
    }

    [Fact]
    public void Registered_ForApicFrame()
    {
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(ApicEditor).Assembly, t => t == typeof(ApicEditor));
        Assert.True(r.TryResolve(typeof(Id3v2AttachedPictureFrame), out _));
    }
}
