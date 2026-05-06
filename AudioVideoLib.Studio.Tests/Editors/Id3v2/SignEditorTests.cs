namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

public class SignEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2SignatureFrame(Id3v2Version.Id3v240)
        {
            GroupSymbol = 0x90,
            SignatureData = [0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80],
        };
        var e = new SignEditor();
        e.Load(frame);
        var copy = new Id3v2SignatureFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.GroupSymbol, copy.GroupSymbol);
        Assert.Equal(frame.SignatureData, copy.SignatureData);
    }

    [Theory]
    [InlineData(0x7F, false)]
    [InlineData(0x80, true)]
    [InlineData(0xF0, true)]
    [InlineData(0xF1, false)]
    public void Validate_GroupSymbolRange(int symbol, bool expectedValid)
    {
        var e = new SignEditor { GroupSymbol = symbol };
        Assert.Equal(expectedValid, e.Validate(out _));
    }

    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var f = new SignEditor().CreateNew(tag);
        Assert.NotNull(f);
        Assert.Equal(Id3v2Version.Id3v240, f.Version);
    }

    [Fact]
    public void LoadDataFromFile_AndClear_UpdateState()
    {
        var path = System.IO.Path.GetTempFileName();
        try
        {
            System.IO.File.WriteAllBytes(path, [9, 8, 7]);
            var e = new SignEditor();
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
