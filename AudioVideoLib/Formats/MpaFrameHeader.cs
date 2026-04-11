/*
 * Date: 2010-02-12
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://www.datavoyage.com/mpgscript/mpeghdr.htm
 *  http://sourceforge.net/tracker/index.php?func=detail&aid=3534143&group_id=979&atid=100979
 *  https://github.com/Sjord/checkmate/blob/master/mpck/layer2.c
 */
using System;

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// Public class for MPEG audio frames.
    /// An MPEG audio file consists out of frames. Each frame contains of a header followed by the audio data.
    /// </summary>
    public sealed partial class MpaFrame
    {
        /// <summary>
        /// A MPEGFrame header is always 4 bytes.
        /// </summary>
        public const int FrameHeaderSize = 4;

        /// <summary>
        /// Frame sync to find the header (all bits are always set).
        /// </summary>
        private const short ValidFrameSync = 0x7FF;

        /// <summary>
        /// Reserved value for bitrate index.
        /// </summary>
        private const byte InvalidBitrateIndex = 0x0F;

        /// <summary>
        /// Reserved value for sampling rate frequency index.
        /// </summary>
        private const byte InvalidSamplingRate = 0x03;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// The sampling rate specifies how many samples per second are recorded. Each MPEG version can handle different sampling rates.
        /// Sampling rate frequency index (values are in Hz).
        /// </summary>
        //// [_audioVersion][_samplingRateFrequency]
        private static readonly int[][] SamplingRates = 
        {
            new[] { 11025, 12000, 8000, 0 },      // MPEG 2.5 (LSF)
            new[] { 0, 0, 0, 0 },                 // Reserved MPEG bit
            new[] { 22050, 24000, 16000, 0 },     // MPEG 2 (LSF)
            new[] { 44100, 48000, 32000, 0 }      // MPEG 1
        };

        /// <summary>
        /// Bitrates are always displayed in kilobits per second.
        /// Note that the prefix kilo (abbreviated with the small 'k') doesn't mean 1024 but 1000 bits per second!
        /// The bitrate index 1111 is reserved and should never be used. In the MPEG audio standard there is a free format described.
        /// This free format means that the file is encoded with a constant bitrate, which is not one of the predefined bitrates.
        /// Only very few decoders can handle those files.
        /// </summary>
        //// [_audioVersion][_layerVersion][_bitrateIndex]
        private static readonly int[][][] Bitrates =
        {
            new[] {                                                                               // MPEG 2.5
                new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },                         // Reserved Layer
                new[] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 },        // Layer III
                new[] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 },        // Layer II
                new[] { 0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256, 0 }    // Layer I
            }, 
            new[] {                                                                               // Reserved MPEG Bit
                new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },                         // Reserved Layer
                new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },                         // Layer III
                new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },                         // Layer II
                new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }                          // Layer I
            },
            new[] {                                                                               // MPEG 2
                new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },                         // Reserved Layer
                new[] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 },        // Layer III
                new[] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 },        // Layer II
                new[] { 0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256, 0 }    // Layer I
            }, 
            new[] {                                                                               // MPEG 1
                new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },                         // Reserved Layer
                new[] { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 },    // Layer III
                new[] { 0, 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384, 0 },   // Layer II
                new[] { 0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448, 0 } // Layer I
            }
        };

        /// <summary>
        /// Frame size is the number of samples contained in a frame.
        /// It is constant and always 384 samples for Layer I, 1152 samples for Layer II and 576 (MPEG 2 and 2.5) or 1152 (MPEG 1) samples for Layer III.
        /// For the calculation of the frame size, you need the number of samples in a MPEG audio frame.
        /// </summary>
        //// [_audioVersion][_layerVersion]
        private static readonly int[][] FrameSizeSamples = 
        {
            // Reserved, Layer III, Layer II, Layer I
            new[] { 0, 576, 1152, 384 },        // MPEG 2.5 (LSF)
            new[] { 0, 0, 0, 0 },               // Reserved MPEG bit
            new[] { 0, 576, 1152, 384 },        // MPEG 2 (LSF)
            new[] { 0, 1152, 1152, 384 }        // MPEG 1
        };

        /// <summary>
        /// Table used to get the frame length.
        /// </summary>
        //// [_audioVersion][_layerVersion]
        private static readonly int[][] FrameSizeMultipliers = 
        {
            // Reserved, Layer III, Layer II, Layer I
            new[] { 0, 72, 144, 12 },          // MPEG 2.5 (LSF)
            new[] { 0, 0, 0, 0 },              // Reserved MPEG bit
            new[] { 0, 72, 144, 12 },          // MPEG 2 (LSF)
            new[] { 0, 144, 144, 12 }          // MPEG 1
        };

        /// <summary>
        /// Determines whether the bitrate/Channel mode is an allowed combination or not.
        /// In MPEG 1 Layer II, there are only some combinations of bitrates and modes allowed. In MPEG 2/2.5, there is no such restriction.
        /// </summary>
        //// [_bitrateIndex][_channelMode]
        private static readonly bool[][] AllowedBitrateChannelModes = 
        {
            // Stereo, Joint Stereo, Dual Channel, Single Channel
            new[] { true, true, true, true },          // free - all
            new[] { false, false, false, true },       // 32 - single channel
            new[] { false, false, false, true },       // 48 - single channel
            new[] { false, false, false, true },       // 56 - single channel
            new[] { true, true, true, true },          // 64 - all
            new[] { false, false, false, true },       // 80 - single channel
            new[] { true, true, true, true },          // 96 - all
            new[] { true, true, true, true },          // 112 - all
            new[] { true, true, true, true },          // 128 - all
            new[] { true, true, true, true },          // 160 - all
            new[] { true, true, true, true },          // 192 - all
            new[] { true, true, true, false },         // 224 - stereo, intensity stereo, dual channel
            new[] { true, true, true, false },         // 256 - stereo, intensity stereo, dual channel
            new[] { true, true, true, false },         // 320 - stereo, intensity stereo, dual channel
            new[] { true, true, true, false },         // 384 - stereo, intensity stereo, dual channel
            new[] { false, false, false, false }       // bad - none
        };

        /// <summary>
        /// The frame itself consists of slots.
        /// For Layer I a slot is always 32 bits (4 bytes) long, for Layer II and Layer III a slot is 8 bits (1 byte) long.
        /// </summary>
        //// [_layerVersion]
        private static readonly int[] FrameSlotSizes = { 0, 1, 1, 4 };       // Reserved, Layer III, Layer II, Layer I

        /// <summary>
        /// The side information follows the header or the CRC in Layer III files. 
        /// It contains information about the general decoding of the frame, but doesn't contain the actual encoded audio samples. 
        /// The following table shows the size of the side information for all Layer III files.
        /// </summary>
        //// [_audioVersion][_channelMode]
        private static readonly int[][] SideInfoSizes = 
        {
            // Stereo, Joint Stereo, Dual Channel, Single Channel
            new[] { 17, 17, 17, 9 },          // MPEG 2.5 (LSF)
            new[] { 0, 0, 0, 0 },             // Reserved
            new[] { 17, 17, 17, 9 },          // MPEG 2 (LSF)
            new[] { 32, 32, 32, 17 }         // MPEG 1
        };

        // Possible quantization per sub band table
        private static readonly SubbandQuantization[] SubbandQuantizationTable = 
            {
                //// ISO/IEC 11172-3 Table B.2a
                new SubbandQuantization
                    {
                        SubbandLimit = 27,
                        Offsets = new[] { 7, 7, 7, 6, 6, 6, 6, 6, 6, 6, 6, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 0, 0, 0, 0 }
                    },
                //// ISO/IEC 11172-3 Table B.2b
                new SubbandQuantization
                    {
                        SubbandLimit = 30,
                        Offsets =
                            new[]
                                { 7, 7, 7, 6, 6, 6, 6, 6, 6, 6, 6, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 0, 0, 0, 0, 0, 0, 0 }
                    },
                //// ISO/IEC 11172-3 Table B.2c
                new SubbandQuantization { SubbandLimit = 8, Offsets = new[] { 5, 5, 2, 2, 2, 2, 2, 2 } },
                //// ISO/IEC 11172-3 Table B.2d
                new SubbandQuantization { SubbandLimit = 12, Offsets = new[] { 5, 5, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 } },
                //// ISO/IEC 13818-3 Table B.1
                new SubbandQuantization
                    {
                        SubbandLimit = 30,
                        Offsets =
                            new[]
                                { 4, 4, 4, 4, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
                    }
            };

        private static readonly BitAllocation[] BitAllocationTable = 
            {
                new BitAllocation { BitsAllocated = 2, Offset = 0 }, new BitAllocation { BitsAllocated = 2, Offset = 3 },
                new BitAllocation { BitsAllocated = 3, Offset = 3 }, new BitAllocation { BitsAllocated = 3, Offset = 1 },
                new BitAllocation { BitsAllocated = 4, Offset = 2 }, new BitAllocation { BitsAllocated = 4, Offset = 3 },
                new BitAllocation { BitsAllocated = 4, Offset = 4 }, new BitAllocation { BitsAllocated = 4, Offset = 5 }
            };

        ////------------------------------------------------------------------------------------------------------------------------------

        private byte _emphasis;

        private byte _modeExtension;

        private byte _channelMode;

        // Sampling rate frequency index 
        private byte _samplingRateFrequency;

        private byte _bitrateIndex;

        // Layer description  {0, III, II, I}
        private byte _layerVersion;

        // MPEG Audio version ID {2.5, 0, 2, 1}
        private byte _audioVersion;

        // Frame sync (all bits must be set)
        private short _frameSync;

        private byte[] _header;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the sampling rate per second in Hz of the audio.
        /// </summary>
        /// <value>
        /// The sampling rate per second, in Hz, of the audio.
        /// </value>
        public int SamplingRate
        {
            get { return SamplingRates[_audioVersion][_samplingRateFrequency]; }
        }

        /// <summary>
        /// Gets the size of a sample, in bits.
        /// </summary>
        /// <value>
        /// The size of a sample, in bits.
        /// </value>
        public int SampleSize
        {
            get
            {
                return (FrameLength / SamplingRate) * 8;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the valid sampling rate is valid.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is valid sampling rate; otherwise, <c>false</c>.
        /// </value>
        public bool IsValidSampingRate
        {
            get { return _samplingRateFrequency != InvalidSamplingRate; }
        }

        /// <summary>
        /// Gets the bitrate of the audio, in KBit.
        /// </summary>
        /// <value>
        /// The bitrate of the audio, in KBit.
        /// </value>
        //// TODO: add calc of free bitrate
        public int Bitrate
        {
            // I don't know how to calculate the bitrate yet if the free bitrate is used.
            // The bitrate should be calculated from the frame data and that bitrate should be used to calculate the frame length.
            get { return Bitrates[_audioVersion][_layerVersion][_bitrateIndex]; }
        }

        /// <summary>
        /// Gets a value indicating whether the bitrate is valid.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is valid bitrate; otherwise, <c>false</c>.
        /// </value>
        public bool IsValidBitrate
        {
            get { return _bitrateIndex != InvalidBitrateIndex; }
        }

        /// <summary>
        /// Gets the bitrate in bits per second of the audio.
        /// </summary>
        /// <value>
        /// The bitrate in bits per second, of the audio.
        /// </value>
        //// TODO: Take note for free bitrate when implemented.
        public int BitrateInBitsPerSecond
        {
            // bitrate is in KBit per second * 1000 => Bit per second.
            get { return (_bitrateIndex == 0) ? 0 : (Bitrate * 1000); }
        }

        /// <summary>
        /// Gets the bitrate in bytes per second of the audio.
        /// </summary>
        /// <value>
        /// The bitrate in bytes per second, of the audio.
        /// </value>
        //// TODO: Take note for free bitrate when implemented.
        public int BitrateInBytesPerSecond
        {
            // bitrate is in KBit per second * 1000 => Bit per second / 8 => bytes per second.
            get { return (_bitrateIndex == 0) ? 0 : (Bitrate * 1000) / 8; }
        }

        /// <summary>
        /// Gets the frame size of the current frame, this is the number of samples contained in the frame.
        /// </summary>
        /// <value>
        /// The number of samples in the frame.
        /// </value>
        /// <remarks>
        /// Frame size is the number of samples contained in a frame.
        /// It is constant and always 384 samples for Layer I, 1152 samples for Layer II and 576 (MPEG 2 and 2.5) or 1152 (MPEG 1) samples for Layer III.
        /// This is NOT the size of the frame itself. See <see cref="FrameLength"/> to retrieve the size of the full frame.
        /// </remarks>
        public int FrameSize
        {
            get { return FrameSizeSamples[_audioVersion][_layerVersion]; }
        }

        /// <summary>
        /// Gets the frame multiplier size of the current frame.
        /// </summary>
        /// <value>
        /// The multiplier for the current frame based on the <see cref="AudioVersion"/> and the <see cref="LayerVersion"/>.
        /// </value>
        public int FrameSizeMultiplier
        {
            get { return FrameSizeMultipliers[_audioVersion][_layerVersion]; }
        }

        /// <summary>
        /// Gets the size of the slot of the current frame.
        /// </summary>
        /// <value>
        /// The slot size for this MPEG frame.
        /// </value>
        /// <remarks>
        /// For Layer I a slot is 32 bits (4 bytes) long, for Layer II and Layer III a slot is 8 bit (1 byte) long.
        /// </remarks>
        public int SlotSize
        {
            get { return FrameSlotSizes[_layerVersion]; }
        }

        /// <summary>
        /// Gets a value indicating whether this frame is in mono.
        /// </summary>
        /// <value>
        /// <c>true</c> if this frame is in mono; otherwise, <c>false</c>.
        /// </value>
        public bool IsMono
        {
            get { return ChannelMode == MpaChannelMode.SingleChannel; }
        }

        /// <summary>
        /// Gets the size of the side info.
        /// </summary>
        /// <value>
        /// The size of the side info, in bytes.
        /// </value>
        /// <remarks>
        /// The side information follows the header or the CRC in Layer III files.
        /// It contains information about the general decoding of the frame, but doesn't contain the actual encoded audio samples.
        /// </remarks>
        public int SideInfoSize
        {
            get { return LayerVersion == MpaFrameLayerVersion.Layer3 ? SideInfoSizes[_audioVersion][_channelMode] : 0; }
        }

        /// <summary>
        /// Gets the frame length, in bytes.
        /// </summary>
        /// <value>
        /// The length of the frame, in bytes.
        /// </value>
        /// <remarks>
        /// Frame length is the length of a frame in bytes when compressed. This includes the size of the header and the size of the audio data.
        /// </remarks>
        //// TODO: add calc of free bitrate.
        public int FrameLength
        {
            // According to the ISO standards, you have to calculate the frame size in slots, 
            // then truncate this number to an integer, and after that multiply it with the slot size.
            // (Bitrate * 1000) is used to convert from kbit to bit
            get
            {
                // I don't know how to calculate the bitrate yet if the free bitrate is used.
                // The bitrate should be calculated from the frame data and that bitrate should be used to calculate the frame length.
                return (Bitrate == 0)
                           ? FrameHeaderSize
                           : Convert.ToInt32(((FrameSizeMultiplier * (Bitrate * 1000)) / SamplingRate) + Convert.ToByte(IsPadded)) * SlotSize;
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
            get { return Convert.ToInt64(((double)FrameSize / SamplingRate) * 1000); }
        }

        /// <summary>
        /// Gets the audio version used in this frame.
        /// </summary>
        /// <value>
        /// The MPEG Audio Version, this can be one of the following: MPEG-1, MPEG-2 or MPEG-2.5.
        /// </value>
        public MpaAudioVersion AudioVersion
        {
            get { return (MpaAudioVersion)_audioVersion; }
        }

        /// <summary>
        /// Gets the layer version used in this frame.
        /// </summary>
        /// <value>
        /// The Layer Version, this can be one of the following: Layer I, Layer II or Layer III.
        /// </value>
        public MpaFrameLayerVersion LayerVersion
        {
            get { return (MpaFrameLayerVersion)_layerVersion; }
        }

        /// <summary>
        /// Gets the channel mode this frame is encoded in.
        /// </summary>
        /// <value>
        /// The Channel Mode, this can be one of the following: Stereo, Join Stereo, Dual Channel (Stereo) or Single Channel (Mono).
        /// </value>
        public MpaChannelMode ChannelMode
        {
            get { return (MpaChannelMode)_channelMode; }
        }

        /// <summary>
        /// Gets the Emphasis used for this MPEG frame.
        /// </summary>
        /// <value>
        /// The Emphasis for this frame, if used; otherwise 0. Possible values are Half (50/15 milliseconds) or CCIT J.17.
        /// </value>
        public MpaFrameEmphasis Emphasis
        {
            get { return (MpaFrameEmphasis)_emphasis; }
        }

        /// <summary>
        /// Gets the Mode extension used for this MPEG frame.
        /// </summary>
        /// <value>
        /// The mode extension used for this MPEG frame, this is different depending on the <see cref="LayerVersion"/>used, see remarks.
        /// </value>
        /// <remarks>
        /// Mode extension is used to join information that are of no use for stereo effect, thus reducing needed resources.
        /// These bits are dynamically determined by an encoder in Joint stereo mode.
        /// </remarks>
        public int ModeExtension
        {
            get { return _modeExtension; }
        }

        /// <summary>
        /// Gets a value indicating whether this MPEG frame is protected by a CRC that's appended after the header.
        /// </summary>
        /// <value>
        /// <c>true</c> if this frame is CRC protected; otherwise, <c>false</c>.
        /// </value>
        public bool IsCrcProtected { get; private set; }

        /// <summary>
        /// Gets the CRC value of the frame.
        /// </summary>
        /// <value>
        /// The CRC value of the frame.
        /// </value>
        /// <remarks>
        /// The CRC field will be set with the CRC when <see cref="IsCrcProtected"/> is true.
        /// </remarks>
        /// 16 bit CRC if the protection bit is set to protected.
        public int Crc { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the MPEG frame is from the original media.
        /// </summary>
        /// <value>
        /// <c>true</c> if this frame is from the original media; otherwise, <c>false</c> if it's from a copy.
        /// </value>
        public bool IsOriginalMedia { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the MPEG frame is padded with an extra slot or not.
        /// </summary>
        /// <value>
        /// <c>true</c> if this frame is padded; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// See <see cref="SlotSize"/> to retrieve the slot size, in bytes.
        /// </remarks>
        public bool IsPadded { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the private bit is set for this frame.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is private; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// This bit is only informative; it may be freely used for specific needs of an application, 
        /// i.e. if it has to trigger some application specific events.
        /// </remarks>
        public bool IsPrivateBitSet { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this frame is has the copyright flag set.
        /// </summary>
        /// <value>
        /// <c>true</c> if this frame is copyrighted; otherwise, <c>false</c>.
        /// </value>
        public bool IsCopyrighted { get; private set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets a value indicating whether the <see cref="Bitrate"/>/<see cref="ChannelMode"/> is an allowed combination or not.
        /// </summary>
        /// <value>
        /// <c>true</c> if it's allowed; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// In MPEG 1 Layer II, there are only some combinations of bitrates and modes allowed. In MPEG 2/2.5, there is no such restriction.
        /// </remarks>
        private bool IsAllowedBitrateChannelMode
        {
            get
            {
                return (AudioVersion != MpaAudioVersion.Version10) || (LayerVersion != MpaFrameLayerVersion.Layer2) || AllowedBitrateChannelModes[_bitrateIndex][_channelMode];
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="MpaFrame" /> is valid or not.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the header of this frame is valid; otherwise, <c>false</c>.
        /// </returns>
        private bool IsValidHeader()
        {
            return (_frameSync == ValidFrameSync) && (AudioVersion != MpaAudioVersion.Reserved)
                   && (LayerVersion != MpaFrameLayerVersion.Reserved) && (_bitrateIndex != InvalidBitrateIndex)
                   && (_samplingRateFrequency != InvalidSamplingRate) && (_emphasis != InvalidSamplingRate)
                   && IsAllowedBitrateChannelMode;
        }
    }
}
