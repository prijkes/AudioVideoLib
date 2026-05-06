namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class GeobEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var blob = new byte[16];
        for (var i = 0; i < blob.Length; i++)
        {
            blob[i] = (byte)i;
        }
        var frame = new Id3v2GeneralEncapsulatedObjectFrame(Id3v2Version.Id3v240)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF16LittleEndian,
            MimeType = "application/octet-stream",
            Filename = "object.bin",
            ContentDescription = "Sample object",
            EncapsulatedObject = blob,
        };
        var e = new GeobEditor();
        e.Load(frame);
        var copy = new Id3v2GeneralEncapsulatedObjectFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.TextEncoding, copy.TextEncoding);
        Assert.Equal(frame.MimeType, copy.MimeType);
        Assert.Equal(frame.Filename, copy.Filename);
        Assert.Equal(frame.ContentDescription, copy.ContentDescription);
        Assert.Equal(frame.EncapsulatedObject, copy.EncapsulatedObject);
    }

    [Fact]
    public void Validate_ContentDescriptionMustBeNonEmpty()
    {
        var e = new GeobEditor { ContentDescription = string.Empty, MimeType = "application/octet-stream" };
        Assert.False(e.Validate(out var err));
        Assert.NotNull(err);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("application/octet-stream", true)]
    [InlineData("invalidmime", false)]
    public void Validate_MimeTypeShape(string mime, bool expectedValid)
    {
        var e = new GeobEditor { ContentDescription = "desc", MimeType = mime };
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v230);
        var f = new GeobEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v230, f.Version);
    }

    [Fact]
    public void LoadDataFromFile_AndClear_UpdateState()
    {
        var path = System.IO.Path.GetTempFileName();
        try
        {
            System.IO.File.WriteAllBytes(path, [1, 2, 3, 4]);
            var e = new GeobEditor();
            e.LoadDataFromFile(path);
            Assert.Equal("4 bytes", e.DataInfo);
            e.ClearData();
            Assert.Equal("(no data)", e.DataInfo);
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }
}
