/*
 * Tests for MPEG audio frame header parsing in MpaFrame.
 * Reference: docs/MPEG Audio Frame Header - CodeProject.mht
 */
namespace AudioVideoLib.Tests;

using System;
using System.IO;
using System.Reflection;

using AudioVideoLib.Formats;

using Xunit;

public class MpaFrameHeaderTests
{
    // A canonical "MPEG-1 Layer III, 128 kbps, 44100 Hz, stereo, no padding, no CRC" header.
    //   byte0 = 0xFF                                        all 8 high sync bits.
    //   byte1 = 111_11_01_1 = 0xFB                          3 sync | MPEG-1 (11) | Layer III (01) | CRC-not-protected (1).
    //   byte2 = 1001_00_0_0 = 0x90                          bitrate idx 9 (128) | sample idx 0 (44100) | no pad | priv 0.
    //   byte3 = 00_00_0_0_00 = 0x00                         stereo | ext 0 | copy 0 | orig 0 | emphasis none.
    private static readonly byte[] Mp3Mpeg1Layer3128At44100Stereo = [0xFF, 0xFB, 0x90, 0x00];

    [Fact]
    public void ReadFrameReturnsNullWhenSyncWordIsMissing()
    {
        // Corrupt the top sync byte so the 11-bit sync is 0x7F7 rather than 0x7FF.
        byte[] header = [0xFE, 0xFB, 0x90, 0x00];
        var frame = ReadFrame(header);

        Assert.Null(frame);
    }

    [Fact]
    public void ReadFrameReturnsNullWhenSyncSecondNibbleIsMissing()
    {
        // Break the low 3 sync bits in byte 1 (top 3 bits of byte1 must be 111).
        byte[] header = [0xFF, 0x1B, 0x90, 0x00];
        var frame = ReadFrame(header);

        Assert.Null(frame);
    }

    [Fact]
    public void ReadFrameParsesValidMpeg1Layer3SyncWord()
    {
        var frame = ReadFrame(BuildFrame(Mp3Mpeg1Layer3128At44100Stereo));

        Assert.NotNull(frame);
        Assert.Equal(MpaAudioVersion.Version10, frame!.AudioVersion);
        Assert.Equal(MpaFrameLayerVersion.Layer3, frame.LayerVersion);
    }

    [Fact]
    public void AudioVersionBitsDecodeMpeg2()
    {
        // MPEG-2 Layer III @ 32 kbps, 22050 Hz, padded.
        //   byte1 = 111_10_01_1 = 0xF3                      sync | MPEG-2 (10) | Layer III (01) | no CRC.
        //   byte2 = 0100_00_1_0 = 0x42                      bitrate idx 4 (32) | sample idx 0 (22050) | padding.
        byte[] header = [0xFF, 0xF3, 0x42, 0x00];
        var frame = ReadFrame(BuildFrame(header));

        Assert.NotNull(frame);
        Assert.Equal(MpaAudioVersion.Version20, frame!.AudioVersion);
        Assert.Equal(22050, frame.SamplingRate);
    }

    [Fact]
    public void AudioVersionBitsDecodeMpeg25()
    {
        // MPEG-2.5 Layer III @ 8 kbps, 8000 Hz.
        //   byte1 = 111_00_01_1 = 0xE3
        //   byte2 = 0001_10_0_0 = 0x18                      bitrate idx 1 (8) | sample idx 2 (8000).
        byte[] header = [0xFF, 0xE3, 0x18, 0x00];
        var frame = ReadFrame(BuildFrame(header));

        Assert.NotNull(frame);
        Assert.Equal(MpaAudioVersion.Version25, frame!.AudioVersion);
        Assert.Equal(8000, frame.SamplingRate);
    }

    [Fact]
    public void ReservedAudioVersionBitsAreRejected()
    {
        // byte1 = 111_01_01_1 = 0xEB                        sync | reserved (01) | Layer III | no CRC.
        byte[] header = [0xFF, 0xEB, 0x90, 0x00];
        var frame = ReadFrame(header);

        Assert.Null(frame);
    }

    [Fact]
    public void LayerBitsDecodeLayer1Layer2Layer3()
    {
        // Layer I: byte1 = 111_11_11_1 = 0xFF; 256 kbps Layer I @ 48000.
        // byte2 = 1000_01_0_0 = 0x84.
        byte[] layer1 = [0xFF, 0xFF, 0x84, 0x00];

        // Layer II: byte1 = 111_11_10_1 = 0xFD; 192 kbps Layer II @ 48000.
        // byte2 = 1010_01_0_0 = 0xA4.
        byte[] layer2 = [0xFF, 0xFD, 0xA4, 0x00];

        var layer3 = Mp3Mpeg1Layer3128At44100Stereo;

        var f1 = ReadFrame(BuildFrame(layer1));
        var f2 = ReadFrame(BuildFrame(layer2));
        var f3 = ReadFrame(BuildFrame(layer3));

        Assert.NotNull(f1);
        Assert.NotNull(f2);
        Assert.NotNull(f3);
        Assert.Equal(MpaFrameLayerVersion.Layer1, f1!.LayerVersion);
        Assert.Equal(MpaFrameLayerVersion.Layer2, f2!.LayerVersion);
        Assert.Equal(MpaFrameLayerVersion.Layer3, f3!.LayerVersion);
    }

    [Fact]
    public void ReservedLayerBitsAreRejected()
    {
        // byte1 = 111_11_00_1 = 0xF9                        sync | MPEG-1 | reserved layer (00) | no CRC.
        byte[] header = [0xFF, 0xF9, 0x90, 0x00];
        var frame = ReadFrame(header);

        Assert.Null(frame);
    }

    [Fact]
    public void BitrateLookupMpeg1Layer3At128kbps()
    {
        var frame = ReadFrame(BuildFrame(Mp3Mpeg1Layer3128At44100Stereo));

        Assert.NotNull(frame);
        Assert.Equal(128, frame!.Bitrate);
        Assert.Equal(128_000, frame.BitrateInBitsPerSecond);
        Assert.Equal(16_000, frame.BitrateInBytesPerSecond);
        Assert.True(frame.IsValidBitrate);
    }

    [Fact]
    public void BitrateLookupMpeg1Layer1At256kbps()
    {
        // bitrate idx 8 for Layer I MPEG-1 = 256 kbps.
        byte[] header = [0xFF, 0xFF, 0x84, 0x00];
        var frame = ReadFrame(BuildFrame(header));

        Assert.NotNull(frame);
        Assert.Equal(256, frame!.Bitrate);
    }

    [Fact]
    public void BitrateLookupMpeg1Layer2At192kbps()
    {
        // bitrate idx 10 for Layer II MPEG-1 = 192 kbps.
        byte[] header = [0xFF, 0xFD, 0xA4, 0x00];
        var frame = ReadFrame(BuildFrame(header));

        Assert.NotNull(frame);
        Assert.Equal(192, frame!.Bitrate);
    }

    [Fact]
    public void ReservedBitrateIndex0x0FIsRejected()
    {
        // byte2 = 1111_00_0_0 = 0xF0                        reserved bitrate idx.
        byte[] header = [0xFF, 0xFB, 0xF0, 0x00];
        var frame = ReadFrame(header);

        Assert.Null(frame);
    }

    [Fact]
    public void SamplingRateLookupMpeg1()
    {
        // 44100 (idx 0): canonical header.
        var f44 = ReadFrame(BuildFrame(Mp3Mpeg1Layer3128At44100Stereo));

        // 48000 (idx 1): byte2 = 1001_01_0_0 = 0x94.
        byte[] h48 = [0xFF, 0xFB, 0x94, 0x00];
        var f48 = ReadFrame(BuildFrame(h48));

        // 32000 (idx 2): byte2 = 1001_10_0_0 = 0x98.
        byte[] h32 = [0xFF, 0xFB, 0x98, 0x00];
        var f32 = ReadFrame(BuildFrame(h32));

        Assert.NotNull(f44);
        Assert.NotNull(f48);
        Assert.NotNull(f32);
        Assert.Equal(44100, f44!.SamplingRate);
        Assert.Equal(48000, f48!.SamplingRate);
        Assert.Equal(32000, f32!.SamplingRate);
        Assert.True(f44.IsValidSampingRate);
    }

    [Fact]
    public void ReservedSamplingRateIndex0x03IsRejected()
    {
        // byte2 = 1001_11_0_0 = 0x9C                        reserved sample rate idx.
        byte[] header = [0xFF, 0xFB, 0x9C, 0x00];
        var frame = ReadFrame(header);

        Assert.Null(frame);
    }

    [Fact]
    public void FrameLengthMpeg1Layer3At128kbps44100NoPadding()
    {
        var frame = ReadFrame(BuildFrame(Mp3Mpeg1Layer3128At44100Stereo));

        // Spec: 144 * BR / SR + P = 144 * 128000 / 44100 + 0 = 417.
        Assert.NotNull(frame);
        Assert.Equal(417, frame!.FrameLength);
    }

    [Fact]
    public void FrameLengthMpeg1Layer3At128kbps44100WithPadding()
    {
        // byte2 = 1001_00_1_0 = 0x92                        padding bit set.
        byte[] header = [0xFF, 0xFB, 0x92, 0x00];
        var frame = ReadFrame(BuildFrame(header));

        // 417 + 1 slot (1 byte) = 418.
        Assert.NotNull(frame);
        Assert.Equal(418, frame!.FrameLength);
        Assert.True(frame.IsPadded);
    }

    [Fact]
    public void FrameLengthMpeg1Layer1At256kbps48000()
    {
        byte[] header = [0xFF, 0xFF, 0x84, 0x00];
        var frame = ReadFrame(BuildFrame(header));

        // Spec (Layer I): (12 * BR / SR + P) * 4 = (12 * 256000 / 48000 + 0) * 4 = 64 * 4 = 256.
        Assert.NotNull(frame);
        Assert.Equal(256, frame!.FrameLength);
    }

    [Fact]
    public void FrameLengthMpeg1Layer2At192kbps48000()
    {
        byte[] header = [0xFF, 0xFD, 0xA4, 0x00];
        var frame = ReadFrame(BuildFrame(header));

        // Spec (Layer II): 144 * BR / SR + P = 144 * 192000 / 48000 = 576.
        Assert.NotNull(frame);
        Assert.Equal(576, frame!.FrameLength);
    }

    [Fact]
    public void FrameLengthMpeg2Layer3At32kbps22050WithPadding()
    {
        // MPEG-2 Layer III: multiplier is 72, not 144.
        // 72 * 32000 / 22050 + 1 = 104 (truncated) + 1 = 105.
        byte[] header = [0xFF, 0xF3, 0x42, 0x00];
        var frame = ReadFrame(BuildFrame(header));

        Assert.NotNull(frame);
        Assert.Equal(105, frame!.FrameLength);
    }

    [Fact]
    public void PaddingBitAddsExactlyOneSlotOnLayer3()
    {
        var unpadded = ReadFrame(BuildFrame(Mp3Mpeg1Layer3128At44100Stereo));
        byte[] paddedHeader = [0xFF, 0xFB, 0x92, 0x00];
        var padded = ReadFrame(BuildFrame(paddedHeader));

        Assert.NotNull(unpadded);
        Assert.NotNull(padded);
        Assert.False(unpadded!.IsPadded);
        Assert.True(padded!.IsPadded);
        Assert.Equal(unpadded.FrameLength + 1, padded.FrameLength);
        Assert.Equal(1, padded.SlotSize);
    }

    [Fact]
    public void PaddingBitAddsExactlyFourBytesOnLayer1()
    {
        // Layer I @ 256 kbps 48000 stereo no padding vs padded.
        byte[] unpaddedHeader = [0xFF, 0xFF, 0x84, 0x00];

        // padding bit: byte2 = 1000_01_1_0 = 0x86.
        byte[] paddedHeader = [0xFF, 0xFF, 0x86, 0x00];

        var unpadded = ReadFrame(BuildFrame(unpaddedHeader));
        var padded = ReadFrame(BuildFrame(paddedHeader));

        Assert.NotNull(unpadded);
        Assert.NotNull(padded);
        Assert.Equal(4, padded!.SlotSize);
        Assert.Equal(unpadded!.FrameLength + 4, padded.FrameLength);
    }

    [Fact]
    public void ChannelModeStereoIsDecoded()
    {
        var frame = ReadFrame(BuildFrame(Mp3Mpeg1Layer3128At44100Stereo));

        Assert.NotNull(frame);
        Assert.Equal(MpaChannelMode.Stereo, frame!.ChannelMode);
        Assert.False(frame.IsMono);
    }

    [Fact]
    public void ChannelModeJointStereoIsDecoded()
    {
        // byte3 = 01_00_0_0_00 = 0x40.
        byte[] header = [0xFF, 0xFB, 0x90, 0x40];
        var frame = ReadFrame(BuildFrame(header));

        Assert.NotNull(frame);
        Assert.Equal(MpaChannelMode.JointStereo, frame!.ChannelMode);
        Assert.False(frame.IsMono);
    }

    [Fact]
    public void ChannelModeDualChannelIsDecoded()
    {
        // byte3 = 10_00_0_0_00 = 0x80.
        byte[] header = [0xFF, 0xFB, 0x90, 0x80];
        var frame = ReadFrame(BuildFrame(header));

        Assert.NotNull(frame);
        Assert.Equal(MpaChannelMode.DualChannel, frame!.ChannelMode);
        Assert.False(frame.IsMono);
    }

    [Fact]
    public void ChannelModeSingleChannelIsDecodedAsMono()
    {
        // byte3 = 11_00_0_0_00 = 0xC0.
        byte[] header = [0xFF, 0xFB, 0x90, 0xC0];
        var frame = ReadFrame(BuildFrame(header));

        Assert.NotNull(frame);
        Assert.Equal(MpaChannelMode.SingleChannel, frame!.ChannelMode);
        Assert.True(frame.IsMono);
    }

    [Fact]
    public void Mpeg1Layer2At32kbpsStereoIsRejectedAsDisallowedCombination()
    {
        // MPEG-1 Layer II @ 32 kbps must be single channel only.
        //   byte1 = 111_11_10_1 = 0xFD                      MPEG-1 Layer II.
        //   byte2 = 0001_00_0_0 = 0x10                      bitrate idx 1 (32) | 44100 | no pad.
        //   byte3 = 0x00                                    stereo.
        byte[] header = [0xFF, 0xFD, 0x10, 0x00];
        var frame = ReadFrame(header);

        Assert.Null(frame);
    }

    [Fact]
    public void Mpeg1Layer2At32kbpsMonoIsAccepted()
    {
        // Same as above but single channel.
        byte[] header = [0xFF, 0xFD, 0x10, 0xC0];
        var frame = ReadFrame(BuildFrame(header));

        Assert.NotNull(frame);
        Assert.Equal(MpaFrameLayerVersion.Layer2, frame!.LayerVersion);
        Assert.Equal(32, frame.Bitrate);
        Assert.True(frame.IsMono);
    }

    [Fact]
    public void EmphasisNoneIsDecoded()
    {
        var frame = ReadFrame(BuildFrame(Mp3Mpeg1Layer3128At44100Stereo));

        Assert.NotNull(frame);
        Assert.Equal(MpaFrameEmphasis.None, frame!.Emphasis);
    }

    [Fact]
    public void EmphasisHalfIsDecoded()
    {
        // byte3 = 00_00_0_0_01 = 0x01.
        byte[] header = [0xFF, 0xFB, 0x90, 0x01];
        var frame = ReadFrame(BuildFrame(header));

        Assert.NotNull(frame);
        Assert.Equal(MpaFrameEmphasis.Half, frame!.Emphasis);
    }

    [Fact]
    public void EmphasisCcitIsDecoded()
    {
        // byte3 = 00_00_0_0_11 = 0x03.
        byte[] header = [0xFF, 0xFB, 0x90, 0x03];
        var frame = ReadFrame(BuildFrame(header));

        Assert.NotNull(frame);
        Assert.Equal(MpaFrameEmphasis.Ccit, frame!.Emphasis);
    }

    [Fact]
    public void ReservedEmphasisIsRejected()
    {
        // byte3 = 00_00_0_0_10 = 0x02.
        byte[] header = [0xFF, 0xFB, 0x90, 0x02];
        var frame = ReadFrame(header);

        Assert.Null(frame);
    }

    [Fact]
    public void CrcProtectionBitIsDecodedWhenClear()
    {
        // byte1 = 111_11_01_0 = 0xFA                        protection bit = 0 (protected).
        byte[] header = [0xFF, 0xFA, 0x90, 0x00];
        var frame = ReadFrame(BuildFrame(header));

        Assert.NotNull(frame);
        Assert.True(frame!.IsCrcProtected);
    }

    [Fact]
    public void CrcProtectionBitIsDecodedWhenSet()
    {
        var frame = ReadFrame(BuildFrame(Mp3Mpeg1Layer3128At44100Stereo));

        Assert.NotNull(frame);
        Assert.False(frame!.IsCrcProtected);
    }

    [Fact]
    public void FrameSizeSamplesMatchSpecPerLayerAndVersion()
    {
        // Layer I always 384 samples.
        var layer1 = ReadFrame(BuildFrame([0xFF, 0xFF, 0x84, 0x00]));

        // MPEG-1 Layer II always 1152.
        var layer2 = ReadFrame(BuildFrame([0xFF, 0xFD, 0xA4, 0x00]));

        // MPEG-1 Layer III 1152, MPEG-2 Layer III 576.
        var mpeg1Layer3 = ReadFrame(BuildFrame(Mp3Mpeg1Layer3128At44100Stereo));
        var mpeg2Layer3 = ReadFrame(BuildFrame([0xFF, 0xF3, 0x42, 0x00]));

        Assert.NotNull(layer1);
        Assert.NotNull(layer2);
        Assert.NotNull(mpeg1Layer3);
        Assert.NotNull(mpeg2Layer3);
        Assert.Equal(384, layer1!.FrameSize);
        Assert.Equal(1152, layer2!.FrameSize);
        Assert.Equal(1152, mpeg1Layer3!.FrameSize);
        Assert.Equal(576, mpeg2Layer3!.FrameSize);
    }

    [Fact]
    public void ReadFrameCapturesStartOffsetAndLength()
    {
        var original = BuildFrame(Mp3Mpeg1Layer3128At44100Stereo);
        var frame = ReadFrame(original);

        Assert.NotNull(frame);

        // After the byte-passthrough retrofit (Bundle A), MpaFrame no longer encodes
        // itself. It captures (StartOffset, Length); the round-trip identity guarantee
        // moves to MpaStreamTests.WriteTo_RoundTripsBytesIdentically (Red until Bundle B).
        Assert.Equal(0L, frame!.StartOffset);
        Assert.Equal((long)original.Length, frame.Length);
        Assert.Equal(original.Length, frame.FrameLength);
    }

    [Fact]
    public void PrivateBitAndCopyrightAndOriginalBitsAreDecoded()
    {
        // byte2 bit 0 = private bit, byte3 bit 3 = copyright, byte3 bit 2 = original.
        //   byte2 = 1001_00_0_1 = 0x91                      private bit set.
        //   byte3 = 00_00_1_1_00 = 0x0C                     copyright + original.
        byte[] header = [0xFF, 0xFB, 0x91, 0x0C];
        var frame = ReadFrame(BuildFrame(header));

        Assert.NotNull(frame);
        Assert.True(frame!.IsPrivateBitSet);
        Assert.True(frame.IsCopyrighted);
        Assert.True(frame.IsOriginalMedia);
    }

    [Fact]
    public void SampleSizeReturnsZeroWhenSamplingRateIsZero()
    {
        // Defensive guard regression: force samplingRate == 0 via reflection on a bare frame.
        // (IsValidHeader would block this path in normal ReadFrame usage.)
        var frame = CreateBareFrame();

        SetPrivateField(frame, "_audioVersion", (byte)3);          // MPEG-1.
        SetPrivateField(frame, "_layerVersion", (byte)1);          // Layer III.
        SetPrivateField(frame, "_samplingRateFrequency", (byte)3); // Reserved => 0 Hz.
        SetPrivateField(frame, "_bitrateIndex", (byte)9);          // 128 kbps.
        SetPrivateField(frame, "_channelMode", (byte)0);           // Stereo.

        Assert.Equal(0, frame.SamplingRate);
        Assert.Equal(0, frame.SampleSize);
        Assert.Equal(0L, frame.AudioLength);
    }

    [Fact]
    public void FrameLengthReturnsHeaderSizeWhenSamplingRateIsZero()
    {
        var frame = CreateBareFrame();

        SetPrivateField(frame, "_audioVersion", (byte)3);
        SetPrivateField(frame, "_layerVersion", (byte)1);
        SetPrivateField(frame, "_samplingRateFrequency", (byte)3); // 0 Hz.
        SetPrivateField(frame, "_bitrateIndex", (byte)9);          // 128 kbps.
        SetPrivateField(frame, "_channelMode", (byte)0);

        // The guard returns FrameHeaderSize (4) rather than dividing by zero.
        Assert.Equal(MpaFrame.FrameHeaderSize, frame.FrameLength);
    }

    [Fact]
    public void AudioLengthForMpeg1Layer3At128kbps44100IsAround26Milliseconds()
    {
        var frame = ReadFrame(BuildFrame(Mp3Mpeg1Layer3128At44100Stereo));

        // 1152 samples / 44100 Hz * 1000 ≈ 26.12 ms.
        Assert.NotNull(frame);
        Assert.Equal(26L, frame!.AudioLength);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Helpers
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Pads a 4-byte header into a full-length frame buffer filled with zero audio data so <see cref="MpaFrame.ReadFrame"/>
    /// can consume it without truncation. Requires the header to already describe a valid frame.
    /// </summary>
    private static byte[] BuildFrame(byte[] header)
    {
        // Peek the length by running a dry parse first; if that fails the test will surface the error
        // when it calls ReadFrame(header) directly, so we fall back to a generously sized buffer.
        var dry = ReadFrame(header);
        var length = dry?.FrameLength ?? 2048;
        if (length < header.Length)
        {
            length = header.Length;
        }
        var buffer = new byte[length];
        Buffer.BlockCopy(header, 0, buffer, 0, header.Length);
        return buffer;
    }

    private static MpaFrame? ReadFrame(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return MpaFrame.ReadFrame(stream);
    }

    private static MpaFrame CreateBareFrame()
    {
        var ctor = typeof(MpaFrame).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        Assert.NotNull(ctor);
        return (MpaFrame)ctor!.Invoke(parameters: null);
    }

    private static void SetPrivateField(MpaFrame frame, string name, object value)
    {
        var field = typeof(MpaFrame).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(frame, value);
    }
}
