/*
 * MPEG audio frame lookup tables, extracted from MpaFrameHeader.cs so that
 * MpaFrameHeader stays focused on field parsing and validation.
 *
 * Sources used:
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://www.datavoyage.com/mpgscript/mpeghdr.htm
 *  ISO/IEC 11172-3, ISO/IEC 13818-3
 */
namespace AudioVideoLib.Formats;

public sealed partial class MpaFrame
{
    /// <summary>
    /// The sampling rate specifies how many samples per second are recorded. Each MPEG version can handle different sampling rates.
    /// Sampling rate frequency index (values are in Hz).
    /// </summary>
    //// [_audioVersion][_samplingRateFrequency]
    private static readonly int[][] SamplingRates =
    [
        [11025, 12000, 8000, 0],      // MPEG 2.5 (LSF)
        [0, 0, 0, 0],                 // Reserved MPEG bit
        [22050, 24000, 16000, 0],     // MPEG 2 (LSF)
        [44100, 48000, 32000, 0]      // MPEG 1
    ];

    /// <summary>
    /// Bitrates are always displayed in kilobits per second.
    /// Note that the prefix kilo (abbreviated with the small 'k') doesn't mean 1024 but 1000 bits per second!
    /// The bitrate index 1111 is reserved and should never be used. In the MPEG audio standard there is a free format described.
    /// This free format means that the file is encoded with a constant bitrate, which is not one of the predefined bitrates.
    /// Only very few decoders can handle those files.
    /// </summary>
    //// [_audioVersion][_layerVersion][_bitrateIndex]
    private static readonly int[][][] Bitrates =
    [
        [                                                                               // MPEG 2.5
            [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],                         // Reserved Layer
            [0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0],        // Layer III
            [0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0],        // Layer II
            [0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256, 0]    // Layer I
        ],
        [                                                                               // Reserved MPEG Bit
            [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],                         // Reserved Layer
            [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],                         // Layer III
            [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],                         // Layer II
            [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]                          // Layer I
        ],
        [                                                                               // MPEG 2
            [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],                         // Reserved Layer
            [0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0],        // Layer III
            [0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0],        // Layer II
            [0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256, 0]    // Layer I
        ],
        [                                                                               // MPEG 1
            [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],                         // Reserved Layer
            [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0],    // Layer III
            [0, 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384, 0],   // Layer II
            [0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448, 0] // Layer I
        ]
    ];

    /// <summary>
    /// Frame size is the number of samples contained in a frame.
    /// It is constant and always 384 samples for Layer I, 1152 samples for Layer II and 576 (MPEG 2 and 2.5) or 1152 (MPEG 1) samples for Layer III.
    /// For the calculation of the frame size, you need the number of samples in a MPEG audio frame.
    /// </summary>
    //// [_audioVersion][_layerVersion]
    private static readonly int[][] FrameSizeSamples =
    [
        // Reserved, Layer III, Layer II, Layer I
        [0, 576, 1152, 384],        // MPEG 2.5 (LSF)
        [0, 0, 0, 0],               // Reserved MPEG bit
        [0, 576, 1152, 384],        // MPEG 2 (LSF)
        [0, 1152, 1152, 384]        // MPEG 1
    ];

    /// <summary>
    /// Table used to get the frame length.
    /// </summary>
    //// [_audioVersion][_layerVersion]
    private static readonly int[][] FrameSizeMultipliers =
    [
        // Reserved, Layer III, Layer II, Layer I
        [0, 72, 144, 12],          // MPEG 2.5 (LSF)
        [0, 0, 0, 0],              // Reserved MPEG bit
        [0, 72, 144, 12],          // MPEG 2 (LSF)
        [0, 144, 144, 12]          // MPEG 1
    ];

    /// <summary>
    /// Determines whether the bitrate/Channel mode is an allowed combination or not.
    /// In MPEG 1 Layer II, there are only some combinations of bitrates and modes allowed. In MPEG 2/2.5, there is no such restriction.
    /// </summary>
    //// [_bitrateIndex][_channelMode]
    private static readonly bool[][] AllowedBitrateChannelModes =
    [
        // Stereo, Joint Stereo, Dual Channel, Single Channel
        [true, true, true, true],          // free - all
        [false, false, false, true],       // 32 - single channel
        [false, false, false, true],       // 48 - single channel
        [false, false, false, true],       // 56 - single channel
        [true, true, true, true],          // 64 - all
        [false, false, false, true],       // 80 - single channel
        [true, true, true, true],          // 96 - all
        [true, true, true, true],          // 112 - all
        [true, true, true, true],          // 128 - all
        [true, true, true, true],          // 160 - all
        [true, true, true, true],          // 192 - all
        [true, true, true, false],         // 224 - stereo, intensity stereo, dual channel
        [true, true, true, false],         // 256 - stereo, intensity stereo, dual channel
        [true, true, true, false],         // 320 - stereo, intensity stereo, dual channel
        [true, true, true, false],         // 384 - stereo, intensity stereo, dual channel
        [false, false, false, false]       // bad - none
    ];

    /// <summary>
    /// The frame itself consists of slots.
    /// For Layer I a slot is always 32 bits (4 bytes) long, for Layer II and Layer III a slot is 8 bits (1 byte) long.
    /// </summary>
    //// [_layerVersion]
    private static readonly int[] FrameSlotSizes = [0, 1, 1, 4];       // Reserved, Layer III, Layer II, Layer I

    /// <summary>
    /// The side information follows the header or the CRC in Layer III files.
    /// It contains information about the general decoding of the frame, but doesn't contain the actual encoded audio samples.
    /// The following table shows the size of the side information for all Layer III files.
    /// </summary>
    //// [_audioVersion][_channelMode]
    private static readonly int[][] SideInfoSizes =
    [
        // Stereo, Joint Stereo, Dual Channel, Single Channel
        [17, 17, 17, 9],          // MPEG 2.5 (LSF)
        [0, 0, 0, 0],             // Reserved
        [17, 17, 17, 9],          // MPEG 2 (LSF)
        [32, 32, 32, 17]         // MPEG 1
    ];

    // Possible quantization per sub band table
    private static readonly SubbandQuantization[] SubbandQuantizationTable =
        [
            //// ISO/IEC 11172-3 Table B.2a
            new SubbandQuantization
                {
                    SubbandLimit = 27,
                    Offsets = [7, 7, 7, 6, 6, 6, 6, 6, 6, 6, 6, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 0, 0, 0, 0]
                },
            //// ISO/IEC 11172-3 Table B.2b
            new SubbandQuantization
                {
                    SubbandLimit = 30,
                    Offsets =
                        [7, 7, 7, 6, 6, 6, 6, 6, 6, 6, 6, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 0, 0, 0, 0, 0, 0, 0]
                },
            //// ISO/IEC 11172-3 Table B.2c
            new SubbandQuantization { SubbandLimit = 8, Offsets = [5, 5, 2, 2, 2, 2, 2, 2] },
            //// ISO/IEC 11172-3 Table B.2d
            new SubbandQuantization { SubbandLimit = 12, Offsets = [5, 5, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2] },
            //// ISO/IEC 13818-3 Table B.1
            new SubbandQuantization
                {
                    SubbandLimit = 30,
                    Offsets =
                        [4, 4, 4, 4, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]
                }
        ];

    private static readonly BitAllocation[] BitAllocationTable =
        [
            new BitAllocation { BitsAllocated = 2, Offset = 0 }, new BitAllocation { BitsAllocated = 2, Offset = 3 },
            new BitAllocation { BitsAllocated = 3, Offset = 3 }, new BitAllocation { BitsAllocated = 3, Offset = 1 },
            new BitAllocation { BitsAllocated = 4, Offset = 2 }, new BitAllocation { BitsAllocated = 4, Offset = 3 },
            new BitAllocation { BitsAllocated = 4, Offset = 4 }, new BitAllocation { BitsAllocated = 4, Offset = 5 }
        ];
}
