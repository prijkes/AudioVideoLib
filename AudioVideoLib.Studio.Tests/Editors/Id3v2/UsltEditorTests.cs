namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class UsltEditorTests
{
    [Fact]
    public void Registered_ForUsltFrame()
    {
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(UsltEditor).Assembly, t => t == typeof(UsltEditor));
        Assert.True(r.TryResolve(typeof(Id3v2UnsynchronizedLyricsFrame), out _));
    }
}
