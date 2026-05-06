namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class EncrEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2EncryptionMethodRegistrationFrame(Id3v2Version.Id3v240)
        {
            OwnerIdentifier = "http://example.com",
            MethodSymbol = 0x90,
            EncryptionData = [0x10, 0x20, 0x30],
        };
        var e = new EncrEditor();
        e.Load(frame);
        var copy = new Id3v2EncryptionMethodRegistrationFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.OwnerIdentifier, copy.OwnerIdentifier);
        Assert.Equal(frame.MethodSymbol, copy.MethodSymbol);
        Assert.Equal(frame.EncryptionData, copy.EncryptionData);
    }

    [Theory]
    [InlineData(0x7F, false)]
    [InlineData(0x80, true)]
    [InlineData(0xF0, true)]
    [InlineData(0xF1, false)]
    public void Validate_MethodSymbolRange(int symbol, bool expectedValid)
    {
        var e = new EncrEditor { OwnerIdentifier = "http://example.com", MethodSymbol = symbol };
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Fact]
    public void Validate_OwnerIdentifierMustBeNonEmpty()
    {
        var e = new EncrEditor { OwnerIdentifier = string.Empty, MethodSymbol = 0x90 };
        Assert.False(e.Validate(out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new EncrEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }
}
