namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using System.Linq;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class SyltEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2SynchronizedLyricsFrame(Id3v2Version.Id3v240)
        {
            TextEncoding = Id3v2FrameEncodingType.Default,
            Language = "eng",
            TimeStampFormat = Id3v2TimeStampFormat.AbsoluteTimeMilliseconds,
            ContentType = Id3v2ContentType.Lyrics,
            ContentDescriptor = "verse 1",
        };
        frame.LyricSyncs.Add(new Id3v2LyricSync("Hello", 1000));
        frame.LyricSyncs.Add(new Id3v2LyricSync("world", 2000));
        frame.LyricSyncs.Add(new Id3v2LyricSync("again", 3000));

        var editor = new SyltEditor();
        editor.Load(frame);
        Assert.Equal(3, editor.Entries.Count);
        Assert.Equal("Hello", editor.Entries[0].Syllable);

        var copy = new Id3v2SynchronizedLyricsFrame(Id3v2Version.Id3v240);
        editor.Save(copy);
        Assert.Equal(frame.TextEncoding, copy.TextEncoding);
        Assert.Equal(frame.Language, copy.Language);
        Assert.Equal(frame.TimeStampFormat, copy.TimeStampFormat);
        Assert.Equal(frame.ContentType, copy.ContentType);
        Assert.Equal(frame.ContentDescriptor, copy.ContentDescriptor);
        Assert.Equal(3, copy.LyricSyncs.Count);
        var saved = copy.LyricSyncs.ToList();
        Assert.Equal("Hello", saved[0].Syllable);
        Assert.Equal(1000, saved[0].TimeStamp);
        Assert.Equal("again", saved[2].Syllable);
        Assert.Equal(3000, saved[2].TimeStamp);
    }

    [Fact]
    public void Validate_LanguageMustBe3Chars()
    {
        var editor = new SyltEditor { Language = "en", ContentDescriptor = "x" };
        Assert.False(editor.Validate(out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Validate_TimestampsMustBeNonDecreasing()
    {
        var editor = new SyltEditor { Language = "eng", ContentDescriptor = "x" };
        editor.AddRow(new SyltRowVm { Syllable = "a", TimeStamp = 1000 });
        editor.AddRow(new SyltRowVm { Syllable = "b", TimeStamp = 500 });
        Assert.False(editor.Validate(out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Validate_AllValid_Passes()
    {
        var editor = new SyltEditor { Language = "eng", ContentDescriptor = "x" };
        editor.AddRow(new SyltRowVm { Syllable = "a", TimeStamp = 100 });
        editor.AddRow(new SyltRowVm { Syllable = "b", TimeStamp = 200 });
        Assert.True(editor.Validate(out var err));
        Assert.Null(err);
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new SyltEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }

    [Fact]
    public void AddRow_AppendsToEntries()
    {
        var e = new SyltEditor();
        e.AddRow(new SyltRowVm { Syllable = "x", TimeStamp = 0 });
        Assert.Single(e.Entries);
    }
}
