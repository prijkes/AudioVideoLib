namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class McdiBinaryEditorTests
{
    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        // MCDI requires a TRCK frame to be present in the tag first per the lib's invariants.
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var trck = new Id3v2TextFrame(Id3v2Version.Id3v240, "TRCK");
        trck.Values.Add("1");
        tag.SetFrame(trck);

        var f = new McdiBinaryEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v240, f.Version);
    }

    [Fact]
    public void Registered_ForMcdiFrame()
    {
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(McdiBinaryEditor).Assembly, t => t == typeof(McdiBinaryEditor));
        Assert.True(r.TryResolve(typeof(Id3v2MusicCdIdentifierFrame), out _));
    }
}
