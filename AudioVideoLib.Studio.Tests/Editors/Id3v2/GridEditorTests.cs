namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class GridEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2GroupIdentificationRegistrationFrame(Id3v2Version.Id3v240)
        {
            OwnerIdentifier = "http://example.com",
            GroupSymbol = 0x90,
            GroupDependentData = [0x01, 0x02, 0x03, 0x04],
        };
        var e = new GridEditor();
        e.Load(frame);
        var copy = new Id3v2GroupIdentificationRegistrationFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.OwnerIdentifier, copy.OwnerIdentifier);
        Assert.Equal(frame.GroupSymbol, copy.GroupSymbol);
        Assert.Equal(frame.GroupDependentData, copy.GroupDependentData);
    }

    [Theory]
    [InlineData(0x7F, false)]
    [InlineData(0x80, true)]
    [InlineData(0xF0, true)]
    [InlineData(0xF1, false)]
    public void Validate_GroupSymbolRange(int symbol, bool expectedValid)
    {
        var e = new GridEditor { OwnerIdentifier = "http://example.com", GroupSymbol = symbol };
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Fact]
    public void Validate_OwnerIdentifierMustBeNonEmpty()
    {
        var e = new GridEditor { OwnerIdentifier = string.Empty, GroupSymbol = 0x90 };
        Assert.False(e.Validate(out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new GridEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }
}
