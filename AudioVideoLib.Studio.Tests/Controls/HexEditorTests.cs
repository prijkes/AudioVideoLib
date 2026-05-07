namespace AudioVideoLib.Studio.Tests.Controls;

using AudioVideoLib.Studio.Controls;

using Xunit;

public class HexEditorTests
{
    [Fact]
    public void RenderHex_Empty_ReturnsEmpty()
        => Assert.Equal(string.Empty, HexEditor.RenderHex([]));

    [Fact]
    public void RenderHex_OneByte()
        => Assert.Equal("AB", HexEditor.RenderHex([0xAB]));

    [Fact]
    public void RenderHex_SixteenBytes_NoNewline()
    {
        var bytes = new byte[16];
        for (var i = 0; i < 16; i++)
        {
            bytes[i] = (byte)i;
        }
        Assert.Equal("00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F", HexEditor.RenderHex(bytes));
    }

    [Fact]
    public void RenderHex_SeventeenBytes_TwoLines()
    {
        var bytes = new byte[17];
        bytes[16] = 0xFF;
        var rendered = HexEditor.RenderHex(bytes);
        Assert.Equal(
            "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00\nFF",
            rendered);
    }

    [Fact]
    public void RenderAscii_PrintableAndNonPrintable()
    {
        var bytes = new byte[] { (byte)'A', 0x00, (byte)'b', 0x7F };
        Assert.Equal("A.b.", HexEditor.RenderAscii(bytes));
    }

    [Fact]
    public void RenderAscii_LineBreakEvery16()
    {
        var bytes = new byte[17];
        for (var i = 0; i < 17; i++)
        {
            bytes[i] = (byte)('A' + i);
        }
        // 16 chars, then \n, then last char
        Assert.Equal("ABCDEFGHIJKLMNOP\nQ", HexEditor.RenderAscii(bytes));
    }

    [Fact]
    public void RenderOffsets_Empty_ReturnsZero()
        => Assert.Equal("00000000", HexEditor.RenderOffsets(0));

    [Fact]
    public void RenderOffsets_TwoLines()
        => Assert.Equal("00000000\n00000010", HexEditor.RenderOffsets(17));

    [Theory]
    [InlineData(0, 4, 0)]    // before first byte
    [InlineData(1, 4, 0)]    // between high+low nibble of byte 0
    [InlineData(2, 4, 1)]    // space after byte 0 = caret between bytes = start of byte 1
    [InlineData(3, 4, 1)]    // high nibble of byte 1
    [InlineData(10, 4, 3)]   // low nibble of byte 3 (last)
    [InlineData(11, 4, 4)]   // past end
    public void HexCaretToByteIndex_KnownPositions(int caret, int totalBytes, int expectedByte)
        => Assert.Equal(expectedByte, HexEditor.HexCaretToByteIndex(caret, totalBytes));

    [Theory]
    [InlineData(0, 4, 0)]
    [InlineData(1, 4, 3)]
    [InlineData(2, 4, 6)]
    [InlineData(3, 4, 9)]
    public void ByteIndexToHexCaret_KnownPositions(int byteIdx, int totalBytes, int expectedCaret)
        => Assert.Equal(expectedCaret, HexEditor.ByteIndexToHexCaret(byteIdx, totalBytes));

    [Theory]
    [InlineData(0, 0, 0)]      // empty buffer, caret at 0
    [InlineData(0, 16, 0)]     // byte 0 = caret 0
    [InlineData(15, 16, 15)]   // byte 15 = caret 15 (last char of line 1)
    [InlineData(16, 32, 17)]   // byte 16 = past line-1 newline = caret 17
    [InlineData(17, 32, 18)]
    public void ByteIndexToAsciiCaret_KnownPositions(int byteIdx, int totalBytes, int expectedCaret)
        => Assert.Equal(expectedCaret, HexEditor.ByteIndexToAsciiCaret(byteIdx, totalBytes));

    [Theory]
    [InlineData(0, 4, 0)]
    [InlineData(1, 4, 1)]
    [InlineData(4, 4, 4)]    // past end
    public void AsciiCaretToByteIndex_KnownPositions(int caret, int totalBytes, int expectedByte)
        => Assert.Equal(expectedByte, HexEditor.AsciiCaretToByteIndex(caret, totalBytes));
}
