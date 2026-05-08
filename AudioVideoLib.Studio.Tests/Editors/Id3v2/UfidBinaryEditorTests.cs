namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class UfidBinaryEditorTests
{
    [Fact]
    public void Registered_ForUfidFrame()
    {
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(UfidBinaryEditor).Assembly, t => t == typeof(UfidBinaryEditor));
        Assert.True(r.TryResolve(typeof(Id3v2UniqueFileIdentifierFrame), out _));
    }
}
