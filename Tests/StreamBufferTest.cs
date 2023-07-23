/*
 * Date: 2012-11-16
 * Sources used: 
 */
using AudioVideoLib.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace AudioVideoLibTests
{
    /// <summary>
    /// This is a test class for StreamBufferTest and is intended to contain all StreamBufferTest Unit Tests.
    ///</summary>
    [TestClass]
    public class StreamBufferTest
    {
        /// <summary>
        ///A test for StreamBuffer Constructor
        ///</summary>
        [TestMethod]
        public void StreamBufferConstructorLengthTestUsingEmptyByteArray()
        {
            byte[] buffer = new byte[1024];
            StreamBuffer target = new StreamBuffer(buffer);
            Assert.IsTrue(target.Length == 1024);
        }

        /// <summary>
        ///A test for StreamBuffer Constructor
        ///</summary>
        [TestMethod]
        public void StreamBufferConstructorLengthTestUsingInitializedByteArray()
        {
            byte[] msBuffer = { 0x90, 0x10, 0xAA, 0x02, 0xFF };
            StreamBuffer target = new StreamBuffer(msBuffer);
            Assert.IsTrue(target.Length == msBuffer.Length);
        }

        /// <summary>
        ///A test for StreamBuffer Constructor
        ///</summary>
        [TestMethod]
        public void StreamBufferConstructorLengthTestUsingCapacityValue()
        {
            StreamBuffer target = new StreamBuffer(1024);
            Assert.IsTrue(target.Length == 0);
        }

        /// <summary>
        ///A test for StreamBuffer Constructor
        ///</summary>
        [TestMethod]
        public void StreamBufferConstructorLengthTestParameterless()
        {
            StreamBuffer target = new StreamBuffer();
            Assert.IsTrue(target.Length == 0);
        }

        /// <summary>
        ///A test for StreamBuffer Constructor
        ///</summary>
        [TestMethod]
        public void StreamBufferConstructorLengthTestUsingMemoryStream()
        {
            MemoryStream stream = new MemoryStream(1024);
            StreamBuffer target = new StreamBuffer(stream);
            Assert.IsTrue(target.Length == 0);
        }

        /// <summary>
        ///A test for Flush
        ///</summary>
        [TestMethod]
        public void FlushTest()
        {
            StreamBuffer target = new StreamBuffer();
            target.Flush();
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for Read
        ///</summary>
        [TestMethod]
        public void ReadTestByteArraysAreEqual()
        {
            byte[] msBuffer = { 0x90, 0x10, 0xAA, 0x02, 0xFF };
            StreamBuffer target = new StreamBuffer(msBuffer);
            byte[] buffer = new byte[msBuffer.Length];
            target.Read(buffer, 0, msBuffer.Length);
            Assert.IsTrue(msBuffer.SequenceEqual(buffer));
        }

        /// <summary>
        ///A test for Read
        ///</summary>
        [TestMethod]
        public void ReadTestByteArraysAreEqualUsingMemoryStream()
        {
            byte[] msBuffer = { 0x90, 0x10, 0xAA, 0x02, 0xFF };
            MemoryStream ms = new MemoryStream(msBuffer);
            StreamBuffer target = new StreamBuffer(ms);
            byte[] buffer = new byte[msBuffer.Length];
            target.Read(buffer, 0, msBuffer.Length);
            Assert.IsTrue(msBuffer.SequenceEqual(buffer));
        }

        /// <summary>
        ///A test for Read
        ///</summary>
        [TestMethod]
        public void ReadTestAmountOfBytesRead()
        {
            byte[] msBuffer = { 0x90, 0x10, 0xAA, 0x02, 0xFF };
            StreamBuffer target = new StreamBuffer(msBuffer);
            byte[] buffer = new byte[msBuffer.Length];
            int expected = msBuffer.Length;
            int actual = target.Read(buffer, 0, expected);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for Read
        ///</summary>
        [TestMethod]
        public void ReadTestAmountOfBytesReadUsingMemoryStream()
        {
            byte[] msBuffer = { 0x90, 0x10, 0xAA, 0x02, 0xFF };
            MemoryStream ms = new MemoryStream(msBuffer);
            StreamBuffer target = new StreamBuffer(ms);
            byte[] buffer = new byte[msBuffer.Length];
            int expected = msBuffer.Length;
            int actual = target.Read(buffer, 0, expected);
            Assert.AreEqual(expected, actual);
        }
        
        /// <summary>
        ///A test for Read
        ///</summary>
        [TestMethod]
        public void ReadTestMovePositionFalse()
        {
            byte[] msBuffer = { 0x90, 0x10, 0xAA, 0x02, 0xFF };
            StreamBuffer target = new StreamBuffer(msBuffer);
            byte[] buffer = new byte[msBuffer.Length];
            target.Read(buffer, msBuffer.Length, false);
            Assert.IsTrue(target.Position == 0);
        }

        /// <summary>
        ///A test for Read
        ///</summary>
        [TestMethod]
        public void ReadTestMovePositionTrue()
        {
            byte[] msBuffer = { 0x90, 0x10, 0xAA, 0x02, 0xFF };
            StreamBuffer target = new StreamBuffer(msBuffer);
            byte[] buffer = new byte[msBuffer.Length];
            target.Read(buffer, msBuffer.Length);
            Assert.IsTrue(target.Position == msBuffer.Length);
        }

        /// <summary>
        ///A test for ReadBigEndianInt
        ///</summary>
        [TestMethod]
        public void ReadBigEndianIntTest3Bytes()
        {
            const int Value = 0x123456;
            const int Expected = 0x563412;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            long actual = target.ReadBigEndianInt(3);
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadBigEndianInt16
        ///</summary>
        [TestMethod]
        public void ReadBigEndianInt16Test()
        {
            const short Value = 0x5678;
            const short Expected = 0x7856;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            long actual = target.ReadBigEndianInt16();
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadBigEndianInt32
        ///</summary>
        [TestMethod]
        public void ReadBigEndianInt32Test()
        {
            const int Value = 0x12345678;
            const int Expected = 0x78563412;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            long actual = target.ReadBigEndianInt(4);
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadBigEndianInt64
        ///</summary>
        [TestMethod]
        public void ReadBigEndianInt64Test7Bytes()
        {
            const long Value = 0x12345678901234;
            const long Expected = 0x34129078563412;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            long actual = target.ReadBigEndianInt64(7);
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadBigEndianInt64
        ///</summary>
        [TestMethod]
        public void ReadBigEndianInt64Test()
        {
            const long Value = 0x1234567890123456;
            const long Expected = 0x5634129078563412;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            long actual = target.ReadBigEndianInt64();
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadByte
        ///</summary>
        [TestMethod]
        public void ReadByteTest()
        {
            const byte Value = 0xEF;
            const byte Expected = 0xEF;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            int actual = target.ReadByte();
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadByte
        ///</summary>
        [TestMethod]
        public void ReadByteTestMovePositionFalse()
        {
            const byte Value = 0xEF;
            const byte Expected = 0xEF;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            int actual = target.ReadByte(false);
            Assert.AreEqual(Expected, actual);
            Assert.IsTrue(target.Position == 0);
        }

        /// <summary>
        ///A test for ReadDouble
        ///</summary>
        [TestMethod]
        public void ReadDoubleTest()
        {
            const double Value = 1.7976931348623157E+256;
            const double Expected = 1.7976931348623157E+256;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            double actual = target.ReadDouble();
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadFloat
        ///</summary>
        [TestMethod]
        public void ReadFloatTest()
        {
            const float Value = (float)3.40282346638528859e+25;
            const float Expected = (float)3.40282346638528859e+25;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            double actual = target.ReadFloat();
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadInt
        ///</summary>
        [TestMethod]
        public void ReadIntTest3Bytes()
        {
            const int Value = 0x123456;
            const int Expected = 0x123456;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            int actual = target.ReadInt(3);
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadInt16
        ///</summary>
        [TestMethod]
        public void ReadInt16Test()
        {
            const short Value = 0x1234;
            const short Expected = 0x1234;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            short actual = target.ReadInt16();
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadInt32
        ///</summary>
        [TestMethod]
        public void ReadInt32Test()
        {
            const int Value = 0x12345678;
            const int Expected = 0x12345678;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            int actual = target.ReadInt32();
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadInt64
        ///</summary>
        [TestMethod]
        public void ReadInt64Test()
        {
            const long Value = 0x1234567890123456;
            const long Expected = 0x1234567890123456;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            long actual = target.ReadInt64();
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadLittleEndianInt16
        ///</summary>
        [TestMethod]
        public void ReadLittleEndianInt16Test()
        {
            const short Value = 0x1234;
            const short Expected = 0x1234;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            short actual = target.ReadLittleEndianInt16();
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadLittleEndianInt32
        ///</summary>
        [TestMethod]
        public void ReadLittleEndianInt32Test()
        {
            const int Value = 0x12345678;
            const int Expected = 0x12345678;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            int actual = target.ReadLittleEndianInt32();
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadLittleEndianInt64
        ///</summary>
        [TestMethod]
        public void ReadLittleEndianInt64Test()
        {
            const long Value = 0x1234567812345678;
            const long Expected = 0x1234567812345678;
            StreamBuffer target = new StreamBuffer(BitConverter.GetBytes(Value)) { Position = 0 };
            long actual = target.ReadLittleEndianInt64();
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadString
        ///</summary>
        [TestMethod]
        public void ReadStringTestLengthBytesAndEncoding()
        {
            const string Value = "This is a test. 日本語が含まれてます。";
            const string Expected = "This is a test. 日本語が含まれてます。";
            Encoding encoding = Encoding.UTF8;
            int lengthBytes = encoding.GetByteCount(Value);
            StreamBuffer target = new StreamBuffer(encoding.GetBytes(Value)) { Position = 0 };
            string actual = target.ReadString(lengthBytes, encoding);
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadString
        ///</summary>
        [TestMethod]
        public void ReadStringTestLengthBytes()
        {
            const string Value = "This is a test.";
            const string Expected = "This is a test.";
            int lengthBytes = Encoding.ASCII.GetByteCount(Value);
            StreamBuffer target = new StreamBuffer(Encoding.ASCII.GetBytes(Value)) { Position = 0 };
            string actual = target.ReadString(lengthBytes);
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadString
        ///</summary>
        [TestMethod]
        public void ReadStringTestEncoding()
        {
            const string Value = "This is a test. 日本語が含まれてます。";
            const string Expected = "This is a test. 日本語が含まれてます。";
            Encoding encoding = Encoding.UTF8;
            StreamBuffer target = new StreamBuffer(encoding.GetBytes(Value)) { Position = 0 };
            string actual = target.ReadString(encoding);
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadString
        ///</summary>
        [TestMethod]
        public void ReadStringTestEncodingAndCustomStringTerminatorAsString()
        {
            const string Value = "This is a test.||日本語が含まれてます。";
            const string Expected = "This is a test.";
            const string CustomStringTerminator = "||";
            Encoding encoding = Encoding.UTF8;
            StreamBuffer target = new StreamBuffer(encoding.GetBytes(Value)) { Position = 0 };
            string actual = target.ReadString(encoding, CustomStringTerminator);
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ReadString
        ///</summary>
        [TestMethod]
        public void ReadStringTestEncodingAndCustomStringTerminatorAsChar()
        {
            const string Value = "日本語が含まれてます。@This is a test.";
            const string Expected = "日本語が含まれてます。";
            const char CustomStringTerminator = '@';
            Encoding encoding = Encoding.UTF8;
            StreamBuffer target = new StreamBuffer(encoding.GetBytes(Value)) { Position = 0 };
            string actual = target.ReadString(encoding, CustomStringTerminator);
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for Seek
        ///</summary>
        [TestMethod]
        public void SeekTest()
        {
            StreamBuffer target = new StreamBuffer();
            target.WritePadding(0x00, 1024);
            const long Offset = 100;
            const SeekOrigin Loc = new SeekOrigin();
            const long Expected = 100;
            long actual= target.Seek(Offset, Loc);
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for SetLength
        ///</summary>
        [TestMethod]
        public void SetLengthTest()
        {
            StreamBuffer target = new StreamBuffer();
            const long Value = 1024;
            target.SetLength(Value);
            Assert.AreEqual(target.Length, Value);
        }

        /// <summary>
        ///A test for SwitchEndianness
        ///</summary>
        [TestMethod]
        public void SwitchEndiannessTest()
        {
            const long Value = 0x1234567890123456;
            const int Bytes = 8;
            const long Expected = 0x5634129078563412;
            long actual = StreamBuffer.SwitchEndianness(Value, Bytes);
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        ///A test for ToByteArray
        ///</summary>
        [TestMethod]
        public void ToByteArrayTest()
        {
            byte[] value = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A };
            StreamBuffer target = new StreamBuffer(value);
            byte[] expected = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A };
            byte[] actual = target.ToByteArray();
            Assert.IsTrue(actual.SequenceEqual(expected));
        }

        /// <summary>
        ///A test for Write
        ///</summary>
        [TestMethod]
        public void WriteTest()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            byte[] buffer = null; // TODO: Initialize to an appropriate value
            int offset = 0; // TODO: Initialize to an appropriate value
            int count = 0; // TODO: Initialize to an appropriate value
            target.Write(buffer, offset, count);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for Write
        ///</summary>
        [TestMethod]
        public void WriteTest1()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            byte[] buffer = null; // TODO: Initialize to an appropriate value
            int count = 0; // TODO: Initialize to an appropriate value
            target.Write(buffer, count);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for Write
        ///</summary>
        [TestMethod]
        public void WriteTest2()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            byte[] buffer = null; // TODO: Initialize to an appropriate value
            target.Write(buffer);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for WriteBigEndianBytes
        ///</summary>
        [TestMethod]
        public void WriteBigEndianBytesTest()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            long value = 0; // TODO: Initialize to an appropriate value
            int size = 0; // TODO: Initialize to an appropriate value
            target.WriteBigEndianBytes(value, size);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for WriteBigEndianInt16
        ///</summary>
        [TestMethod]
        public void WriteBigEndianInt16Test()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            short value = 0; // TODO: Initialize to an appropriate value
            target.WriteBigEndianInt16(value);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for WriteBigEndianInt32
        ///</summary>
        [TestMethod]
        public void WriteBigEndianInt32Test()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            int value = 0; // TODO: Initialize to an appropriate value
            target.WriteBigEndianInt32(value);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for WriteBigEndianInt64
        ///</summary>
        [TestMethod]
        public void WriteBigEndianInt64Test()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            long value = 0; // TODO: Initialize to an appropriate value
            target.WriteBigEndianInt64(value);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for WriteByte
        ///</summary>
        [TestMethod]
        public void WriteByteTest()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            byte value = 0; // TODO: Initialize to an appropriate value
            target.WriteByte(value);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for WriteBytes
        ///</summary>
        [TestMethod]
        public void WriteBytesTest()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            long value = 0; // TODO: Initialize to an appropriate value
            int size = 0; // TODO: Initialize to an appropriate value
            target.WriteBytes(value, size);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for WriteDouble
        ///</summary>
        [TestMethod]
        public void WriteDoubleTest()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            double value = 0F; // TODO: Initialize to an appropriate value
            target.WriteDouble(value);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for WriteFloat
        ///</summary>
        [TestMethod]
        public void WriteFloatTest()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            float value = 0F; // TODO: Initialize to an appropriate value
            target.WriteFloat(value);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for WriteInt
        ///</summary>
        [TestMethod]
        public void WriteIntTest()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            int value = 0; // TODO: Initialize to an appropriate value
            target.WriteInt(value);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for WritePadding
        ///</summary>
        [TestMethod]
        public void WritePaddingTest()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            byte paddingByte = 0; // TODO: Initialize to an appropriate value
            int length = 0; // TODO: Initialize to an appropriate value
            target.WritePadding(paddingByte, length);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for WriteShort
        ///</summary>
        [TestMethod]
        public void WriteShortTest()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            short value = 0; // TODO: Initialize to an appropriate value
            target.WriteShort(value);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for WriteString
        ///</summary>
        [TestMethod]
        public void WriteStringTest()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            string str = string.Empty; // TODO: Initialize to an appropriate value
            target.WriteString(str);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for WriteString
        ///</summary>
        [TestMethod]
        public void WriteStringTest1()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            string str = string.Empty; // TODO: Initialize to an appropriate value
            Encoding encoding = null; // TODO: Initialize to an appropriate value
            target.WriteString(str, encoding);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for WriteString
        ///</summary>
        [TestMethod]
        public void WriteStringTest2()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            string str = string.Empty; // TODO: Initialize to an appropriate value
            Encoding encoding = null; // TODO: Initialize to an appropriate value
            int maxBytesToWrite = 0; // TODO: Initialize to an appropriate value
            target.WriteString(str, encoding, maxBytesToWrite);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for CanRead
        ///</summary>
        [TestMethod]
        public void CanReadTest()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            bool actual;
            actual = target.CanRead;
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for CanSeek
        ///</summary>
        [TestMethod]
        public void CanSeekTest()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            bool actual;
            actual = target.CanSeek;
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for CanWrite
        ///</summary>
        [TestMethod]
        public void CanWriteTest()
        {
            StreamBuffer target = new StreamBuffer(); // TODO: Initialize to an appropriate value
            bool actual;
            actual = target.CanWrite;
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for Length
        ///</summary>
        [TestMethod]
        public void LengthTest()
        {
            byte[] value = new byte[] { 0x01, 0x02, 0x10 };
            StreamBuffer target = new StreamBuffer(value);
            long actual= target.Length;
            Assert.AreEqual(actual, value.Length);
        }

        /// <summary>
        ///A test for Position
        ///</summary>
        [TestMethod]
        public void PositionTest()
        {
            StreamBuffer target = new StreamBuffer();
            const long Expected = 1024;
            target.Position = Expected;
            long actual = target.Position;
            Assert.AreEqual(Expected, actual);
        }
    }
}
