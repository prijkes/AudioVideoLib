namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class AencEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2AudioEncryptionFrame(Id3v2Version.Id3v240)
        {
            OwnerIdentifier = "http://example.com",
            PreviewStart = 100,
            PreviewLength = 200,
            EncryptionInfo = [0x01, 0x02, 0x03, 0x04],
        };
        var e = new AencEditor();
        e.Load(frame);
        var copy = new Id3v2AudioEncryptionFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.OwnerIdentifier, copy.OwnerIdentifier);
        Assert.Equal(frame.PreviewStart, copy.PreviewStart);
        Assert.Equal(frame.PreviewLength, copy.PreviewLength);
        Assert.Equal(frame.EncryptionInfo, copy.EncryptionInfo);
    }

    [Fact]
    public void Validate_OwnerIdentifierMustBeNonEmpty()
    {
        var e = new AencEditor { OwnerIdentifier = string.Empty };
        Assert.False(e.Validate(out var err));
        Assert.NotNull(err);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(100, 200, true)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    public void Validate_PreviewStartAndLength(int start, int length, bool expectedValid)
    {
        var e = new AencEditor
        {
            OwnerIdentifier = "http://example.com",
            PreviewStart = (short)start,
            PreviewLength = (short)length,
        };
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new AencEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }

    [Fact]
    public void LoadDataFromFile_AndClear_UpdateState()
    {
        var path = System.IO.Path.GetTempFileName();
        try
        {
            System.IO.File.WriteAllBytes(path, [1, 2, 3]);
            var e = new AencEditor();
            e.LoadDataFromFile(path);
            Assert.Equal("3 bytes", e.DataInfo);
            e.ClearData();
            Assert.Equal("(no data)", e.DataInfo);
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }
}
