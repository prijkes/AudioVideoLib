namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class PrivBinaryEditorTests
{
    [Fact]
    public void Registered_ForPrivateFrame()
    {
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(PrivBinaryEditor).Assembly, t => t == typeof(PrivBinaryEditor));
        Assert.True(r.TryResolve(typeof(Id3v2PrivateFrame), out _));
    }
}
