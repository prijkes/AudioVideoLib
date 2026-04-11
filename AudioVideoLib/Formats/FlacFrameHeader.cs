/*
 * Date: 2013-02-18
 * Sources used: 
 *  http://flac.sourceforge.net/format.html
 *  http://py.thoulon.free.fr/
 */
using System;
using System.IO;
using System.Linq;

using AudioVideoLib.Cryptography;
using AudioVideoLib.IO;

namespace AudioVideoLib.Formats
{
    public sealed partial class FlacFrame
    {
        /// <summary>
        /// The sample sizes, in bits.
        /// 000 : get from STREAMINFO metadata block
        /// 001 : 8 bits per sample
        /// 010 : 12 bits per sample
        /// 011 : reserved
        /// 100 : 16 bits per sample
        /// 101 : 20 bits per sample
        /// 110 : 24 bits per sample
        /// 111 : reserved
        /// </summary>
        private static readonly int[] SampleSizes = { 0, 8, 12, 0, 16, 20, 24, 0 };

        private static readonly int[] SampleRates = { 0, 88200, 176400, 192000, 8000, 16000, 22050, 24000, 32000, 44100, 48000, 96000, 0, 0, 0, 0 };

        private byte[] _sampleFrameNumberBytes;

        private int _crc8;

        private int _crc16;

        private int _header;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the blocking strategy.
        /// </summary>
        /// <value>
        /// The blocking strategy.
        /// </value>
        public FlacBlockingStrategy BlockingStrategy
        {
            get
            {
                return (FlacBlockingStrategy)((_header >> 16) & 0x01);
            }
        }

        /// <summary>
        /// Gets the size of the block in inter-channel samples.
        /// </summary>
        /// <value>
        /// The size of the block in inter-channel samples.
        /// </value>
        public int BlockSize { get; private set; }

        /// <summary>
        /// Gets the size of the sample, in bits.
        /// </summary>
        /// <value>
        /// The size of the sample, in bits.
        /// </value>
        public int SampleSize { get; private set; }

        /// <summary>
        /// Gets the sampling rate per second in Hz of the audio.
        /// </summary>
        /// <value>
        /// The sampling rate per second, in Hz, of the audio.
        /// </value>
        public int SamplingRate { get; private set; }

        /// <summary>
        /// Gets the channel assignment.
        /// </summary>
        /// <value>
        /// The channel assignment.
        /// </value>
        public FlacChannelAssignment ChannelAssignment { get; private set; }

        /// <summary>
        /// Gets the number of channels.
        /// </summary>
        /// <value>
        /// The number of channels.
        /// </value>
        public int Channels { get; private set; }

        /// <summary>
        /// Gets the number of samples in the frame.
        /// </summary>
        /// <value>
        /// The number of samples in the frame.
        /// </value>
        public long Samples { get; private set; }

        /// <summary>
        /// Gets the frame number of this frame.
        /// </summary>
        /// <value>
        /// The frame number of this frame.
        /// </value>
        public long FrameNumber { get; private set; }

        /// <summary>
        /// Gets the bitrate of the audio, in KBit.
        /// </summary>
        /// <value>
        /// The bitrate of the audio, in KBit.
        /// </value>
        public int Bitrate
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the frame length, in bytes.
        /// </summary>
        /// <value>
        /// The length of the frame, in bytes.
        /// </value>
        /// <remarks>
        /// Length is the length of a frame in bytes when compressed.
        /// </remarks>
        public int FrameLength
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the frame size of the current frame, this is the number of samples contained in the frame.
        /// </summary>
        /// <value>
        /// The number of samples in the frame.
        /// </value>
        /// <remarks>
        /// Frame size is the number of samples contained in a frame.
        /// </remarks>
        public int FrameSize
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the length of audio, in milliseconds.
        /// </summary>
        /// <value>
        /// The length of audio, in milliseconds.
        /// </value>
        public long AudioLength
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        private int Reserved1
        {
            get
            {
                return (_header >> 17) & 0x01;
            }
        }

        private int Reserved2
        {
            get
            {
                return _header & 0x01;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static long ReadBigEndianUtf8Int64(StreamBuffer sb, out byte[] utf8Bytes)
        {
            //// Decoded long range              Coded value
            //// 0000 0000 ・00 0000 007F   0xxxxxxx
            //// 0000 0080 ・00 0000 07FF   110xxxxx 10xxxxxx
            //// 0000 0800 ・00 0000 FFFF    1110xxxx 10xxxxxx 10xxxxxx
            //// 0001 0000 ・00 001F FFFF    11110xxx 10xxxxxx 10xxxxxx 10xxxxxx
            //// 0020 0000 ・00 03FF FFFF     111110xx 10xxxxxx 10xxxxxx 10xxxxxx 10xxxxxx
            //// 0400 0000 ・00 7FFF FFFF     1111110x 10xxxxxx 10xxxxxx 10xxxxxx 10xxxxxx 10xxxxxx
            //// 8000 0000 ・0F FFFF FFFF     11111110 10xxxxxx 10xxxxxx 10xxxxxx 10xxxxxx 10xxxxxx 10xxxxxx
            // Where xxx represent the bits in the uncoded value, in the same order.
            const int ByteCountMask = 0x80;     // 10000000
            const int ByteMask = 0x3F;               // 00111111
            int byteCount = 1;
            long value = sb.ReadByte();
            if (value > ByteCountMask)
            {
                int bytes = 0;
                while (((value << bytes) & ByteCountMask) == ByteCountMask)
                    bytes++;

                value &= ByteMask >> (bytes - 1);
                for (int i = bytes - 1; i > 0; i--)
                    value = (value << (6 * i)) | ((long)sb.ReadByte() & ByteMask);

                byteCount += bytes - 1;
            }
            sb.Position -= byteCount;
            utf8Bytes = new byte[byteCount];
            sb.Read(utf8Bytes, byteCount);
            return value;
        }

        /// <summary>
        /// Reads the header.
        /// </summary>
        /// <param name="sb">The <see cref="StreamBuffer"/>.</param>
        /// <returns>
        /// true if the header is read and valid; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="sb"/> is null.</exception>
        private bool ReadHeader(StreamBuffer sb)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            long startPosition = sb.Position;

            // Sync code '11111111111110'
            _header = sb.ReadBigEndianInt32();
            if (((_header >> 18) & FrameSync) != FrameSync)
                return false;

            /*
                Blocking strategy:
                    •0 : fixed-block size stream; frame header encodes the frame number 
                    •1 : variable-block size stream; frame header encodes the sample number 
            */
            long num = ReadBigEndianUtf8Int64(sb, out _sampleFrameNumberBytes);
            switch (BlockingStrategy)
            {
                case FlacBlockingStrategy.FixedBlocksize:
                    Samples = num;
                    FrameNumber = num
                                  *
                                  (FlacStream.StreamInfoMetadataBlocks.Any()
                                       ? FlacStream.StreamInfoMetadataBlocks.First().MinimumBlockSize
                                       : 1);
                    break;

                case FlacBlockingStrategy.VariableBlocksize:
                    Samples = num;
                    FrameNumber = -1;
                break;
            }

            /*
                Block size in inter-channel samples:
                    •0000 : reserved
                    •0001 : 192 samples
                    •0010-0101 : 576 * (2 ^ (n - 2)) samples, i.e. 576/1152/2304/4608
                    •0110 : get 8 bit (block size - 1) from end of header
                    •0111 : get 16 bit (block size - 1) from end of header
                    •1000-1111 : 256 * (2 ^ (n - 8)) samples, i.e. 256/512/1024/2048/4096/8192/16384/3276
            */
            int blockSize = (_header >> 12) & 0xF;
            switch (blockSize)
            {
                case 0x01:
                    BlockSize = 192;
                    break;

                case 0x02:
                case 0x03:
                case 0x04:
                case 0x05:
                    BlockSize = 576 << (blockSize - 2);
                    break;

                case 0x06:
                    BlockSize = sb.ReadByte() + 1;
                    break;

                case 0x07:
                    BlockSize = sb.ReadBigEndianInt16() + 1;
                    break;

                default:
                    BlockSize = 256 << (blockSize - 8);
                    break;
            }

            /*
                Sample rate:
                    •0000 : get from STREAMINFO metadata block 
                    •0001 : 88.2kHz 
                    •0010 : 176.4kHz 
                    •0011 : 192kHz 
                    •0100 : 8kHz 
                    •0101 : 16kHz 
                    •0110 : 22.05kHz 
                    •0111 : 24kHz 
                    •1000 : 32kHz 
                    •1001 : 44.1kHz 
                    •1010 : 48kHz 
                    •1011 : 96kHz 
                    •1100 : get 8 bit sample rate (in kHz) from end of header 
                    •1101 : get 16 bit sample rate (in Hz) from end of header 
                    •1110 : get 16 bit sample rate (in tens of Hz) from end of header 
                    •1111 : invalid, to prevent sync-fooling string of 1s 
            */
            int samplingRate = (_header >> 8) & 0xF;
            if (samplingRate == 0x00)
            {
                SamplingRate = FlacStream.StreamInfoMetadataBlocks.Any()
                                   ? FlacStream.StreamInfoMetadataBlocks.First().SampleRate
                                   : 0;
            }
            else if (samplingRate <= 0x0B)
            {
                SamplingRate = SampleRates[samplingRate];
            }
            else
            {
                switch (samplingRate)
                {
                    case 0x0C:
                        // Sample rate in kHz
                        SamplingRate = sb.ReadByte() * 1000;
                        break;

                    case 0x0D:
                        // Sample rate in Hz
                        SamplingRate = sb.ReadBigEndianInt16();
                        break;

                    case 0x0E:
                        // Sample rate in 10s of Hz
                        SamplingRate = sb.ReadBigEndianInt16() * 10;
                        break;
                }
            }

            /*
                Channel assignment
                    •0000-0111 : (number of independent channels)-1. Where defined, the channel order follows SMPTE/ITU-R recommendations.
                    The assignments are as follows:
                        ◦1 channel: mono
                        ◦2 channels: left, right
                        ◦3 channels: left, right, center
                        ◦4 channels: front left, front right, back left, back right
                        ◦5 channels: front left, front right, front center, back/surround left, back/surround right
                        ◦6 channels: front left, front right, front center, LFE, back/surround left, back/surround right
                        ◦7 channels: front left, front right, front center, LFE, back center, side left, side right
                        ◦8 channels: front left, front right, front center, LFE, back left, back right, side left, side right
                    •1000 : left/side stereo: channel 0 is the left channel, channel 1 is the side(difference) channel
                    •1001 : right/side stereo: channel 0 is the side(difference) channel, channel 1 is the right channel
                    •1010 : mid/side stereo: channel 0 is the mid(average) channel, channel 1 is the side(difference) channel
                    •1011-1111 : reserved 
            */
            int channelAssignment = (_header >> 4) & 0xF;
            if (channelAssignment < 0x08)
            {
                ChannelAssignment = FlacChannelAssignment.Independent;
                Channels = channelAssignment + 1;
            }
            else
            {
                Channels = 2;
                switch (channelAssignment)
                {
                    case 0x08:
                        ChannelAssignment = FlacChannelAssignment.LeftSide;
                        break;

                    case 0x09:
                        ChannelAssignment = FlacChannelAssignment.RightSide;
                        break;

                    case 0x0A:
                        ChannelAssignment = FlacChannelAssignment.MidSide;
                        break;
                }
            }
            
            /*
                Sample size in bits:
                    •000 : get from STREAMINFO metadata block
                    •001 : 8 bits per sample
                    •010 : 12 bits per sample
                    •011 : reserved
                    •100 : 16 bits per sample
                    •101 : 20 bits per sample
                    •110 : 24 bits per sample
                    •111 : reserved
            */
            int sampleSize = (_header >> 0x01) & 0x07;
            SampleSize = (sampleSize == 0x00)
                             ? (FlacStream.StreamInfoMetadataBlocks.Any()
                                    ? FlacStream.StreamInfoMetadataBlocks.First().BitsPerSample
                                    : 0)
                             : SampleSizes[sampleSize];

            _crc8 = sb.ReadByte();

            long endPosition = sb.Position;
            long length = endPosition - startPosition;
            byte[] crcBytes = new byte[length];
            sb.Position -= length;
            sb.Read(crcBytes, (int)length);
            byte crc8 = Crc8.Calculate(crcBytes);
            if (_crc8 != crc8)
                throw new InvalidDataException("Corrupt CRC8.");

            return true;
        }
    }
}
