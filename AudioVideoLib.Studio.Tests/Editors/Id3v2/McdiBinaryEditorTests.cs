namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class McdiBinaryEditorTests
{
    [Fact]
    public void Registered_ForMcdiFrame()
    {
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(McdiBinaryEditor).Assembly, t => t == typeof(McdiBinaryEditor));
        Assert.True(r.TryResolve(typeof(Id3v2MusicCdIdentifierFrame), out _));
    }
}
