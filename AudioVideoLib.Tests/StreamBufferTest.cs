namespace AudioVideoLib.Tests;

using System;
using System.IO;
using System.Linq;
using System.Text;
using AudioVideoLib.IO;
using Xunit;

public class StreamBufferTest
{
    [Fact]
    public void StreamBufferConstructorLengthTestUsingEmptyByteArray()
    {
        var buffer = new byte[1024];
        var target = new StreamBuffer(buffer);
        Assert.True(target.Length == 1024);
    }

    [Fact]
    public void StreamBufferConstructorLengthTestUsingInitializedByteArray()
    {
        byte[] msBuffer = [0x90, 0x10, 0xAA, 0x02, 0xFF];
        var target = new StreamBuffer(msBuffer);
        Assert.True(target.Length == msBuffer.Length);
    }

    [Fact]
    public void StreamBufferConstructorLengthTestUsingCapacityValue()
    {
        var target = new StreamBuffer(1024);
        Assert.True(target.Length == 0);
    }

    [Fact]
    public void StreamBufferConstructorLengthTestParameterless()
    {
        var target = new StreamBuffer();
        Assert.True(target.Length == 0);
    }

    [Fact]
    public void StreamBufferConstructorLengthTestUsingMemoryStream()
    {
        var stream = new MemoryStream(1024);
        var target = new StreamBuffer(stream);
        Assert.True(target.Length == 0);
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void FlushTest()
    {
        var target = new StreamBuffer();
        target.Flush();
    }

    [Fact]
    public void ReadTestByteArraysAreEqual()
    {
        byte[] msBuffer = [0x90, 0x10, 0xAA, 0x02, 0xFF];
        var target = new StreamBuffer(msBuffer);
        var buffer = new byte[msBuffer.Length];
        var bytesRead = target.Read(buffer, 0, msBuffer.Length);
        Assert.Equal(msBuffer.Length, bytesRead);
        Assert.True(msBuffer.SequenceEqual(buffer));
    }

    [Fact]
    public void ReadTestByteArraysAreEqualUsingMemoryStream()
    {
        byte[] msBuffer = [0x90, 0x10, 0xAA, 0x02, 0xFF];
        var ms = new MemoryStream(msBuffer);
        var target = new StreamBuffer(ms);
        var buffer = new byte[msBuffer.Length];
        var bytesRead = target.Read(buffer, 0, msBuffer.Length);
        Assert.Equal(msBuffer.Length, bytesRead);
        Assert.True(msBuffer.SequenceEqual(buffer));
    }

    [Fact]
    public void ReadTestAmountOfBytesRead()
    {
        byte[] msBuffer = [0x90, 0x10, 0xAA, 0x02, 0xFF];
        var target = new StreamBuffer(msBuffer);
        var buffer = new byte[msBuffer.Length];
        var expected = msBuffer.Length;
        var actual = target.Read(buffer, 0, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReadTestAmountOfBytesReadUsingMemoryStream()
    {
        byte[] msBuffer = [0x90, 0x10, 0xAA, 0x02, 0xFF];
        var ms = new MemoryStream(msBuffer);
        var target = new StreamBuffer(ms);
        var buffer = new byte[msBuffer.Length];
        var expected = msBuffer.Length;
        var actual = target.Read(buffer, 0, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PeekReadDoesNotAdvancePosition()
    {
        byte[] msBuffer = [0x90, 0x10, 0xAA, 0x02, 0xFF];
        var target = new StreamBuffer(msBuffer);
        var buffer = new byte[msBuffer.Length];
        target.PeekRead(buffer, msBuffer.Length);
        Assert.True(target.Position == 0);
    }

    [Fact]
    public void ReadTestMovePositionTrue()
    {
        byte[] msBuffer = [0x90, 0x10, 0xAA, 0x02, 0xFF];
        var target = new StreamBuffer(msBuffer);
        var buffer = new byte[msBuffer.Length];
        target.Read(buffer, msBuffer.Length);
        Assert.True(target.Position == msBuffer.Length);
    }

    [Fact]
    public void ReadBigEndianIntTest3Bytes()
    {
        const int Value = 0x123456;
        const int Expected = 0x563412;
        var target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
        long actual = target.ReadBigEndianInt(3);
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadBigEndianInt16Test()
    {
        const short Value = 0x5678;
        const short Expected = 0x7856;
        var target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
        long actual = target.ReadBigEndianInt16();
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadBigEndianInt32Test()
    {
        const int Value = 0x12345678;
        const int Expected = 0x78563412;
        var target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
        long actual = target.ReadBigEndianInt(4);
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadBigEndianInt64Test7Bytes()
    {
        const long Value = 0x12345678901234;
        const long Expected = 0x34129078563412;
        var target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
        var actual = target.ReadBigEndianInt64(7);
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadBigEndianInt64Test()
    {
        const long Value = 0x1234567890123456;
        const long Expected = 0x5634129078563412;
        var target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
        var actual = target.ReadBigEndianInt64();
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadByteTest()
    {
        const byte Value = 0xEF;
        const byte Expected = 0xEF;
        var target = new StreamBuffer(BitConverter.GetBytes((short)Value)) { Position = 0 };
        var actual = target.ReadByte();
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void PeekByteDoesNotAdvancePosition()
    {
        const byte Value = 0xEF;
        const byte Expected = 0xEF;
        var target = new StreamBuffer(BitConverter.GetBytes((short)Value)) { Position = 0 };
        var actual = target.PeekByte();
        Assert.Equal(Expected, actual);
        Assert.True(target.Position == 0);
    }

    [Fact]
    public void ReadDoubleTest()
    {
        const double Value = 1.7976931348623157E+256;
        const double Expected = 1.7976931348623157E+256;
        var target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
        var actual = target.ReadDouble();
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadFloatTest()
    {
        const float Value = (float)3.40282346638528859e+25;
        const float Expected = (float)3.40282346638528859e+25;
        var target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
        double actual = target.ReadFloat();
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadIntTest3Bytes()
    {
        const int Value = 0x123456;
        const int Expected = 0x123456;
        var target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
        var actual = target.ReadInt(3);
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadInt16Test()
    {
        const short Value = 0x1234;
        const short Expected = 0x1234;
        var target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
        var actual = target.ReadInt16();
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadInt32Test()
    {
        const int Value = 0x12345678;
        const int Expected = 0x12345678;
        var target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
        var actual = target.ReadInt32();
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadInt64Test()
    {
        const long Value = 0x1234567890123456;
        const long Expected = 0x1234567890123456;
        var target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
        var actual = target.ReadInt64();
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadLittleEndianInt16Test()
    {
        const short Value = 0x1234;
        const short Expected = 0x1234;
        var target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
        var actual = target.ReadLittleEndianInt16();
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadLittleEndianInt32Test()
    {
        const int Value = 0x12345678;
        const int Expected = 0x12345678;
        var target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
        var actual = target.ReadLittleEndianInt32();
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadLittleEndianInt64Test()
    {
        const long Value = 0x1234567812345678;
        const long Expected = 0x1234567812345678;
        var target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
        var actual = target.ReadLittleEndianInt64();
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadStringTestLengthBytesAndEncoding()
    {
        const string Value = "This is a test. 日本語が含まれてます。";
        const string Expected = "This is a test. 日本語が含まれてます。";
        var encoding = Encoding.UTF8;
        var lengthBytes = encoding.GetByteCount(Value);
        var target = new StreamBuffer(encoding.GetBytes(Value)) { Position = 0 };
        var actual = target.ReadString(lengthBytes, encoding);
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadStringTestLengthBytes()
    {
        const string Value = "This is a test.";
        const string Expected = "This is a test.";
        var lengthBytes = Encoding.ASCII.GetByteCount(Value);
        var target = new StreamBuffer(Encoding.ASCII.GetBytes(Value)) { Position = 0 };
        var actual = target.ReadString(lengthBytes);
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadStringTestEncoding()
    {
        const string Value = "This is a test. 日本語が含まれてます。";
        const string Expected = "This is a test. 日本語が含まれてます。";
        var encoding = Encoding.UTF8;
        var target = new StreamBuffer(encoding.GetBytes(Value)) { Position = 0 };
        var actual = target.ReadString(encoding);
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadStringTestEncodingAndCustomStringTerminatorAsString()
    {
        const string Value = "This is a test.||日本語が含まれてます。";
        const string Expected = "This is a test.";
        const string CustomStringTerminator = "||";
        var encoding = Encoding.UTF8;
        var target = new StreamBuffer(encoding.GetBytes(Value)) { Position = 0 };
        var actual = target.ReadString(encoding, CustomStringTerminator);
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ReadStringTestEncodingAndCustomStringTerminatorAsChar()
    {
        const string Value = "日本語が含まれてます。@This is a test.";
        const string Expected = "日本語が含まれてます。";
        const char CustomStringTerminator = '@';
        var encoding = Encoding.UTF8;
        var target = new StreamBuffer(encoding.GetBytes(Value)) { Position = 0 };
        var actual = target.ReadString(encoding, CustomStringTerminator);
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void SeekTest()
    {
        var target = new StreamBuffer();
        target.WritePadding(0x00, 1024);
        const long Offset = 100;
        const SeekOrigin Loc = SeekOrigin.Begin;
        const long Expected = 100;
        var actual = target.Seek(Offset, Loc);
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void SetLengthTest()
    {
        var target = new StreamBuffer();
        const long Value = 1024;
        target.SetLength(Value);
        Assert.Equal(Value, target.Length);
    }

    [Fact]
    public void SwitchEndiannessTest()
    {
        const long Value = 0x1234567890123456;
        const int Bytes = 8;
        const long Expected = 0x5634129078563412;
        var actual = StreamBuffer.SwitchEndianness(Value, Bytes);
        Assert.Equal(Expected, actual);
    }

    [Fact]
    public void ToByteArrayTest()
    {
        byte[] value = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A];
        var target = new StreamBuffer(value);
        byte[] expected = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A];
        var actual = target.ToByteArray();
        Assert.True(actual.SequenceEqual(expected));
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void WriteTest()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void WriteTest1()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void WriteTest2()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void WriteBigEndianBytesTest()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void WriteBigEndianInt16Test()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void WriteBigEndianInt32Test()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void WriteBigEndianInt64Test()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void WriteByteTest()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void WriteBytesTest()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void WriteDoubleTest()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void WriteFloatTest()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void WriteIntTest()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void WritePaddingTest()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void WriteShortTest()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void WriteStringTest()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void WriteStringTest1()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void WriteStringTest2()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void CanReadTest()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void CanSeekTest()
    {
    }

    [Fact(Skip = "placeholder - needs test implementation")]
    public void CanWriteTest()
    {
    }

    [Fact]
    public void LengthTest()
    {
        byte[] value = [0x01, 0x02, 0x10];
        var target = new StreamBuffer(value);
        var actual = target.Length;
        Assert.Equal(value.Length, actual);
    }

    [Fact]
    public void PositionTest()
    {
        var target = new StreamBuffer();
        const long Expected = 1024;
        target.Position = Expected;
        var actual = target.Position;
        Assert.Equal(Expected, actual);
    }
}
