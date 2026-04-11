/*
 * Date: 2010-02-12
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://www.datavoyage.com/mpgscript/mpeghdr.htm
 */
namespace AudioVideoLib.Formats;

using System;
using System.Collections.Generic;
using System.IO;

using AudioVideoLib.IO;

/// <summary>
/// Public class for MPEG audio frames.
/// An MPEG audio file consists out of frames. Each frame contains of a header followed by the audio data.
/// </summary>
public sealed partial class MpaFrame : IAudioFrame
{
    private MpaFrame()
    {
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public long StartOffset { get; private set; }

    /// <inheritdoc/>
    public long EndOffset { get; private set; }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads a <see cref="MpaFrame"/> from a <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>
    /// true if found; otherwise, null.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">Thrown if stream is null.</exception>
    public static MpaFrame? ReadFrame(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        var frame = new MpaFrame();
        return frame.ReadFrame(stream as StreamBuffer ?? new StreamBuffer(stream)) ? frame : null;
    }

    /// <summary>
    /// Calculates the CRC of the <see cref="MpaFrame"/>.
    /// </summary>
    /// <returns>
    /// The CRC of the <see cref="MpaFrame"/>
    /// </returns>
    /// <remarks>
    /// The CRC is a CRC16 value.
    /// </remarks>
    public int CalculateCrc()
    {
        if (!IsCrcProtected)
        {
            return 0;
        }

        if ((AudioData == null) || (AudioData.Length < FrameHeaderSize))
        {
            return 0;
        }

        var protectedBits = 0;
        switch (LayerVersion)
        {
            case MpaFrameLayerVersion.Layer1:
            {
                var channels = (ChannelMode == MpaChannelMode.SingleChannel) ? 1 : 2;
                var bound = (ChannelMode == MpaChannelMode.JointStereo) ? 4 + (_channelMode * 4) : 32;
                protectedBits = 4 * ((channels * bound) + (32 - bound));
            }
            break;

            case MpaFrameLayerVersion.Layer2:
            {
                var channels = IsMono ? 1 : 2;
                int squaTableIndex;
                if (AudioVersion != MpaAudioVersion.Version10)
                {
                    squaTableIndex = 4;
                }
                else
                {
                    var bitratePerChannel = BitrateInBitsPerSecond / channels;
                    squaTableIndex = bitratePerChannel <= 48000
                        ? (SamplingRate == 32000) ? 3 : 2
                        : bitratePerChannel <= 80000 ? 0 : (SamplingRate == 48000) ? 0 : 1;
                }
                var subbandLimit = SubbandQuantizationTable[squaTableIndex].SubbandLimit;
                var offsets = SubbandQuantizationTable[squaTableIndex].Offsets;

                var bound = 32;
                if (ChannelMode == MpaChannelMode.JointStereo)
                {
                    bound = 4 + (4 * _modeExtension);
                }

                if (bound > subbandLimit)
                {
                    bound = subbandLimit;
                }

                var bitOffset = 0;
                var dataPtr = 2; // skip the CRC value in the audio data.
                var currentChar = 0;
                for (var subband = 0; subband < subbandLimit; subband++)
                {
                    int bitsAllocated = BitAllocationTable[offsets[subband]].BitsAllocated;
                    if (subband < bound)
                    {
                        for (var i = 0; i < channels; i++)
                        {
                            protectedBits += bitsAllocated;
                            var result = ReadBits(ref bitOffset, ref dataPtr, ref currentChar, bitsAllocated);
                            if (result > 0)
                            {
                                protectedBits += 2;
                            }
                        }
                    }
                    else
                    {
                        protectedBits += bitsAllocated;
                        var result = ReadBits(ref bitOffset, ref dataPtr, ref currentChar, bitsAllocated);
                        if (result > 0)
                        {
                            protectedBits += 2 * channels;
                        }
                    }
                }
            }
            break;

            // For Layer III the protected bits are the side information
            case MpaFrameLayerVersion.Layer3:
            {
                protectedBits = SideInfoSize * 8;
            }
            break;

            default:
            {
            }
            return 0;
        }

        // CRC is also calculated from the last 2 bytes of the header
        protectedBits += (FrameHeaderSize * 8) + 16;   // 16 bit for the CRC value itself

        // Size of buffer is the amount of calculated bits / 8
        var byteSize = Convert.ToInt32(Math.Ceiling(protectedBits / 8.0));

        // If the required amount of bytes is bigger than the total frame size, there must be something wrong.
        if (byteSize > FrameLength)
        {
            return 0;
        }

        // Read the required amount of bytes to be able to calculate the CRC
        var buffer = new byte[byteSize];
        Buffer.BlockCopy(_header, 0, buffer, 0, FrameHeaderSize);

        if (AudioData.Length < (byteSize - FrameHeaderSize))
        {
            return 0;
        }

        Buffer.BlockCopy(AudioData, 0, buffer, FrameHeaderSize, byteSize - FrameHeaderSize);

        // Calculate the CRC on the buffer with the amount of bits (yes bits not bytes)
        return Crc16(buffer, protectedBits);
    }

    /// <summary>
    /// Returns the frame in a byte array.
    /// </summary>
    /// <returns>The frame in a byte array.</returns>
    public byte[] ToByteArray()
    {
        var buffer = new StreamBuffer();
        buffer.Write(_header);
        buffer.Write(AudioData);
        return buffer.ToByteArray();
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private static int Crc16(IList<byte> buffer, int bitSize)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        // start with inverted value of 0
        int crc = 0xFFFF, tmpChar = 0, crcMask = 0;

        // start with byte 2 of header
        for (var n = 16; n < bitSize; n++)
        {
            // skip the 2 bytes of the crc itself
            if (n is >= 32 and < 48)
            {
                continue;
            }

            if ((n % 8) == 0)
            {
                crcMask = 1 << 8;
                tmpChar = buffer[n / 8];
            }
            crcMask >>= 1;
            var tmpI = crc & 0x8000;
            crc <<= 1;

            if ((tmpI == 0) ^ ((tmpChar & crcMask) == 0))
            {
                crc ^= 0x8005;
            }
        }

        // invert the result
        crc &= 0xFFFF;
        return crc;
    }

    /// <summary>
    /// Reads a <see cref="MpaFrame"/> from a <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>
    /// true if found; otherwise, null.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">Thrown if stream is null.</exception>
    private bool ReadFrame(StreamBuffer stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        StartOffset = stream.Position;

        if (!ParseHeader(stream))
        {
            return false;
        }

        int frameLength = FrameLength, audioDataLength = 0;
        if (frameLength > FrameHeaderSize)
        {
            audioDataLength = frameLength - FrameHeaderSize;
        }

        if (audioDataLength == 0)
        {
            return true;
        }

        if ((stream.Length - stream.Position) < audioDataLength)
        {
            // Truncated audio data.
            audioDataLength = (int)(stream.Length - stream.Position);
        }
        AudioData = new byte[audioDataLength];
        var bytesDataRead = stream.Read(AudioData, 0, audioDataLength);

        // Read the CRC
        // Create a buffer to read the CRC stored after the frame (2 bytes)
        if (bytesDataRead >= 2)
        {
            var crc = new byte[2];
            Buffer.BlockCopy(AudioData, 0, crc, 0, 2);
            Crc = StreamBuffer.SwitchEndianness(BitConverter.ToInt16(crc, 0));
        }
        EndOffset = stream.Position;
        return true;
    }

    private bool ParseHeader(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        _header = new byte[FrameHeaderSize];
        var bytesRead = stream.Read(_header, 0, _header.Length);
        if ((bytesRead != _header.Length) || (_header.Length != FrameHeaderSize))
        {
            return false;
        }

        var hdr = ((_header[0] & 0xFF) << 24) | ((_header[1] & 0xFF) << 16) | ((_header[2] & 0xFF) << 8) | (_header[3] & 0xFF);
        _frameSync = (short)((hdr >> 21) & ValidFrameSync);
        _audioVersion = (byte)((hdr >> 19) & 0x03);
        _layerVersion = (byte)((hdr >> 17) & 0x03);
        IsCrcProtected = ((hdr >> 16) & 0x01) == 0x00;
        _bitrateIndex = (byte)((hdr >> 12) & 0x0F);
        _samplingRateFrequency = (byte)((hdr >> 10) & 0x03);
        IsPadded = ((hdr >> 9) & 0x01) == 0x01;
        IsPrivateBitSet = ((hdr >> 8) & 0x01) == 0x01;
        _channelMode = (byte)((hdr >> 6) & 0x03);
        _modeExtension = (byte)((hdr >> 4) & 0x03);
        IsCopyrighted = ((hdr >> 3) & 0x01) == 0x01;
        IsOriginalMedia = ((hdr >> 2) & 0x01) == 0x01;
        _emphasis = (byte)(hdr & 0x03);

        return IsValidHeader();
    }

    private int ReadBits(ref int bitOffset, ref int audioDataIndex, ref int currentChar, int length)
    {
        if (audioDataIndex >= AudioDataLength)
        {
            return 0;
        }

        var result = 0;
        do
        {
            if (bitOffset == 0)
            {
                currentChar = AudioData[audioDataIndex++];
            }

            var mask = (1 << length) - 1;
            var left = 8 - bitOffset - length;
            int chr;
            if (left > 0)
            {
                chr = currentChar >> left;
                bitOffset = (bitOffset + length) % 8;
                length = 0;
            }
            else
            {
                left = Math.Abs(left);
                chr = currentChar << left;
                bitOffset = (bitOffset + length - left) % 8;
                length = left;
            }
            result |= chr & mask;
        }
        while (length > 0);

        return result;
    }
}
