/*
 * Date: 2012-11-16
 * Sources used:
 */
using System;
using System.IO;
using System.Linq;
using System.Text;
using AudioVideoLib.IO;
using Xunit;

namespace AudioVideoLib.Tests
{
    public class StreamBufferTest
    {
        [Fact]
        public void StreamBufferConstructorLengthTestUsingEmptyByteArray()
        {
            byte[] buffer = new byte[1024];
            StreamBuffer target = new StreamBuffer(buffer);
            Assert.True(target.Length == 1024);
        }

        [Fact]
        public void StreamBufferConstructorLengthTestUsingInitializedByteArray()
        {
            byte[] msBuffer = { 0x90, 0x10, 0xAA, 0x02, 0xFF };
            StreamBuffer target = new StreamBuffer(msBuffer);
            Assert.True(target.Length == msBuffer.Length);
        }

        [Fact]
        public void StreamBufferConstructorLengthTestUsingCapacityValue()
        {
            StreamBuffer target = new StreamBuffer(1024);
            Assert.True(target.Length == 0);
        }

        [Fact]
        public void StreamBufferConstructorLengthTestParameterless()
        {
            StreamBuffer target = new StreamBuffer();
            Assert.True(target.Length == 0);
        }

        [Fact]
        public void StreamBufferConstructorLengthTestUsingMemoryStream()
        {
            MemoryStream stream = new MemoryStream(1024);
            StreamBuffer target = new StreamBuffer(stream);
            Assert.True(target.Length == 0);
        }

        [Fact(Skip = "placeholder - needs test implementation")]
        public void FlushTest()
        {
            StreamBuffer target = new StreamBuffer();
            target.Flush();
        }

        [Fact]
        public void ReadTestByteArraysAreEqual()
        {
            byte[] msBuffer = { 0x90, 0x10, 0xAA, 0x02, 0xFF };
            StreamBuffer target = new StreamBuffer(msBuffer);
            byte[] buffer = new byte[msBuffer.Length];
            int bytesRead = target.Read(buffer, 0, msBuffer.Length);
            Assert.Equal(msBuffer.Length, bytesRead);
            Assert.True(msBuffer.SequenceEqual(buffer));
        }

        [Fact]
        public void ReadTestByteArraysAreEqualUsingMemoryStream()
        {
            byte[] msBuffer = { 0x90, 0x10, 0xAA, 0x02, 0xFF };
            MemoryStream ms = new MemoryStream(msBuffer);
            StreamBuffer target = new StreamBuffer(ms);
            byte[] buffer = new byte[msBuffer.Length];
            int bytesRead = target.Read(buffer, 0, msBuffer.Length);
            Assert.Equal(msBuffer.Length, bytesRead);
            Assert.True(msBuffer.SequenceEqual(buffer));
        }

        [Fact]
        public void ReadTestAmountOfBytesRead()
        {
            byte[] msBuffer = { 0x90, 0x10, 0xAA, 0x02, 0xFF };
            StreamBuffer target = new StreamBuffer(msBuffer);
            byte[] buffer = new byte[msBuffer.Length];
            int expected = msBuffer.Length;
            int actual = target.Read(buffer, 0, expected);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ReadTestAmountOfBytesReadUsingMemoryStream()
        {
            byte[] msBuffer = { 0x90, 0x10, 0xAA, 0x02, 0xFF };
            MemoryStream ms = new MemoryStream(msBuffer);
            StreamBuffer target = new StreamBuffer(ms);
            byte[] buffer = new byte[msBuffer.Length];
            int expected = msBuffer.Length;
            int actual = target.Read(buffer, 0, expected);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ReadTestMovePositionFalse()
        {
            byte[] msBuffer = { 0x90, 0x10, 0xAA, 0x02, 0xFF };
            StreamBuffer target = new StreamBuffer(msBuffer);
            byte[] buffer = new byte[msBuffer.Length];
            target.Read(buffer, msBuffer.Length, false);
            Assert.True(target.Position == 0);
        }

        [Fact]
        public void ReadTestMovePositionTrue()
        {
            byte[] msBuffer = { 0x90, 0x10, 0xAA, 0x02, 0xFF };
            StreamBuffer target = new StreamBuffer(msBuffer);
            byte[] buffer = new byte[msBuffer.Length];
            target.Read(buffer, msBuffer.Length);
            Assert.True(target.Position == msBuffer.Length);
        }

        [Fact]
        public void ReadBigEndianIntTest3Bytes()
        {
            const int Value = 0x123456;
            const int Expected = 0x563412;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            long actual = target.ReadBigEndianInt(3);
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadBigEndianInt16Test()
        {
            const short Value = 0x5678;
            const short Expected = 0x7856;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            long actual = target.ReadBigEndianInt16();
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadBigEndianInt32Test()
        {
            const int Value = 0x12345678;
            const int Expected = 0x78563412;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            long actual = target.ReadBigEndianInt(4);
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadBigEndianInt64Test7Bytes()
        {
            const long Value = 0x12345678901234;
            const long Expected = 0x34129078563412;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            long actual = target.ReadBigEndianInt64(7);
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadBigEndianInt64Test()
        {
            const long Value = 0x1234567890123456;
            const long Expected = 0x5634129078563412;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            long actual = target.ReadBigEndianInt64();
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadByteTest()
        {
            const byte Value = 0xEF;
            const byte Expected = 0xEF;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes((short)Value)) { Position = 0 };
            int actual = target.ReadByte();
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadByteTestMovePositionFalse()
        {
            const byte Value = 0xEF;
            const byte Expected = 0xEF;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes((short)Value)) { Position = 0 };
            int actual = target.ReadByte(false);
            Assert.Equal(Expected, actual);
            Assert.True(target.Position == 0);
        }

        [Fact]
        public void ReadDoubleTest()
        {
            const double Value = 1.7976931348623157E+256;
            const double Expected = 1.7976931348623157E+256;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            double actual = target.ReadDouble();
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadFloatTest()
        {
            const float Value = (float)3.40282346638528859e+25;
            const float Expected = (float)3.40282346638528859e+25;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            double actual = target.ReadFloat();
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadIntTest3Bytes()
        {
            const int Value = 0x123456;
            const int Expected = 0x123456;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            int actual = target.ReadInt(3);
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadInt16Test()
        {
            const short Value = 0x1234;
            const short Expected = 0x1234;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            short actual = target.ReadInt16();
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadInt32Test()
        {
            const int Value = 0x12345678;
            const int Expected = 0x12345678;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            int actual = target.ReadInt32();
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadInt64Test()
        {
            const long Value = 0x1234567890123456;
            const long Expected = 0x1234567890123456;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            long actual = target.ReadInt64();
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadLittleEndianInt16Test()
        {
            const short Value = 0x1234;
            const short Expected = 0x1234;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            short actual = target.ReadLittleEndianInt16();
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadLittleEndianInt32Test()
        {
            const int Value = 0x12345678;
            const int Expected = 0x12345678;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            int actual = target.ReadLittleEndianInt32();
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadLittleEndianInt64Test()
        {
            const long Value = 0x1234567812345678;
            const long Expected = 0x1234567812345678;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            long actual = target.ReadLittleEndianInt64();
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadStringTestLengthBytesAndEncoding()
        {
            const string Value = "This is a test. 日本語が含まれてます。";
            const string Expected = "This is a test. 日本語が含まれてます。";
            Encoding encoding = Encoding.UTF8;
            int lengthBytes = encoding.GetByteCount(Value);
            StreamBuffer target = new StreamBuffer(encoding.GetBytes(Value)) { Position = 0 };
            string actual = target.ReadString(lengthBytes, encoding);
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadStringTestLengthBytes()
        {
            const string Value = "This is a test.";
            const string Expected = "This is a test.";
            int lengthBytes = Encoding.ASCII.GetByteCount(Value);
            StreamBuffer target = new StreamBuffer(Encoding.ASCII.GetBytes(Value)) { Position = 0 };
            string actual = target.ReadString(lengthBytes);
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadStringTestEncoding()
        {
            const string Value = "This is a test. 日本語が含まれてます。";
            const string Expected = "This is a test. 日本語が含まれてます。";
            Encoding encoding = Encoding.UTF8;
            StreamBuffer target = new StreamBuffer(encoding.GetBytes(Value)) { Position = 0 };
            string actual = target.ReadString(encoding);
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadStringTestEncodingAndCustomStringTerminatorAsString()
        {
            const string Value = "This is a test.||日本語が含まれてます。";
            const string Expected = "This is a test.";
            const string CustomStringTerminator = "||";
            Encoding encoding = Encoding.UTF8;
            StreamBuffer target = new StreamBuffer(encoding.GetBytes(Value)) { Position = 0 };
            string actual = target.ReadString(encoding, CustomStringTerminator);
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ReadStringTestEncodingAndCustomStringTerminatorAsChar()
        {
            const string Value = "日本語が含まれてます。@This is a test.";
            const string Expected = "日本語が含まれてます。";
            const char CustomStringTerminator = '@';
            Encoding encoding = Encoding.UTF8;
            StreamBuffer target = new StreamBuffer(encoding.GetBytes(Value)) { Position = 0 };
            string actual = target.ReadString(encoding, CustomStringTerminator);
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void SeekTest()
        {
            StreamBuffer target = new StreamBuffer();
            target.WritePadding(0x00, 1024);
            const long Offset = 100;
            const SeekOrigin Loc = SeekOrigin.Begin;
            const long Expected = 100;
            long actual = target.Seek(Offset, Loc);
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void SetLengthTest()
        {
            StreamBuffer target = new StreamBuffer();
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
            long actual = StreamBuffer.SwitchEndianness(Value, Bytes);
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void ToByteArrayTest()
        {
            byte[] value = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A };
            StreamBuffer target = new StreamBuffer(value);
            byte[] expected = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A };
            byte[] actual = target.ToByteArray();
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
            byte[] value = new byte[] { 0x01, 0x02, 0x10 };
            StreamBuffer target = new StreamBuffer(value);
            long actual = target.Length;
            Assert.Equal(value.Length, actual);
        }

        [Fact]
        public void PositionTest()
        {
            StreamBuffer target = new StreamBuffer();
            const long Expected = 1024;
            target.Position = Expected;
            long actual = target.Position;
            Assert.Equal(Expected, actual);
        }
    }
}
