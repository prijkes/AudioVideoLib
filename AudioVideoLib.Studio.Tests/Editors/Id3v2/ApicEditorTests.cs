namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class ApicEditorTests
{
    [Fact]
    public void Registered_ForApicFrame()
    {
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(ApicEditor).Assembly, t => t == typeof(ApicEditor));
        Assert.True(r.TryResolve(typeof(Id3v2AttachedPictureFrame), out _));
    }
}
