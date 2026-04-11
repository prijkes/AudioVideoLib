/*
 * Tests for the MP3 Xing/Info + LAME + VBRI header parsing code.
 * Driven by the "Mp3 Info Tag revision 1" specification.
 */
namespace AudioVideoLib.Tests;

using System;
using System.IO;
using AudioVideoLib.Formats;
using AudioVideoLib.IO;
using Xunit;

public class VbrHeaderTests
{
    // Offsets for a MPEG1 Layer-III Mono frame built with CreateMp3Frame.
    private const int MpaHeaderSize = 4;
    private const int MpegL3MonoSideInfoSize = 17;
    private const int XingOffsetInFrame = MpaHeaderSize + MpegL3MonoSideInfoSize; // 21
    private const int VbriSilenceGap = 32;
    private const int VbriOffsetInFrame = MpaHeaderSize + VbriSilenceGap; // 36
    private const int DefaultFrameLength = 208;

    ////------------------------------------------------------------------------------------------------------------------------------
    // Helpers
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Builds a valid MPEG 1 Layer III, Mono, 64 kbit/s, 44.1 kHz, no-CRC frame
    /// with the given payload injected at <paramref name="payloadOffsetInFrame"/>.
    /// </summary>
    private static MpaFrame CreateMp3Frame(byte[] payload, int payloadOffsetInFrame, int frameLength = DefaultFrameLength)
    {
        var frameBytes = new byte[frameLength];

        // 11 bits sync (0x7FF), v=11 (MPEG1), layer=01 (Layer III), protection=1 (no CRC)
        // bitrate=0101 (64 kbps), samp=00 (44.1kHz), pad=0, priv=0,
        // channel=11 (mono), mode-ext=00, copy=0, orig=0, emphasis=00.
        frameBytes[0] = 0xFF;
        frameBytes[1] = 0xFB;
        frameBytes[2] = 0x50;
        frameBytes[3] = 0xC0;

        if ((payload != null) && (payload.Length > 0))
        {
            Buffer.BlockCopy(payload, 0, frameBytes, payloadOffsetInFrame, payload.Length);
        }

        using var stream = new MemoryStream(frameBytes);
        return MpaFrame.ReadFrame(stream)!;
    }

    private static byte[] BuildXingPayload(
        int flags,
        int frameCount = 0,
        int fileSize = 0,
        byte[]? toc = null,
        int quality = 0,
        bool useInfoMagic = false,
        byte[]? lameTag = null)
    {
        var buffer = new StreamBuffer();
        buffer.WriteString(useInfoMagic ? "Info" : "Xing");
        buffer.WriteBigEndianInt32(flags);

        if ((flags & XingHeaderFlags.FrameCountFlag) != 0)
        {
            buffer.WriteBigEndianInt32(frameCount);
        }

        if ((flags & XingHeaderFlags.FileSizeFlag) != 0)
        {
            buffer.WriteBigEndianInt32(fileSize);
        }

        if ((flags & XingHeaderFlags.TocFlag) != 0)
        {
            var tocBytes = toc ?? new byte[100];
            Assert.Equal(100, tocBytes.Length);
            buffer.Write(tocBytes, 0, 100);
        }

        if ((flags & XingHeaderFlags.VbrScaleFlag) != 0)
        {
            buffer.WriteBigEndianInt32(quality);
        }

        // Pad out to 120 bytes total so the LAME-tag offset (Xing start + 120)
        // lands on either the caller-supplied LAME bytes or zero padding.
        while (buffer.Length < 120)
        {
            buffer.WriteByte(0x00);
        }

        if (lameTag != null)
        {
            buffer.Position = 120;
            buffer.Write(lameTag, 0, lameTag.Length);
        }

        return buffer.ToByteArray();
    }

    private static byte[] BuildVbriPayload(
        short version,
        short delay,
        short quality,
        int fileSize,
        int frameCount,
        short tableEntries,
        short tableScale,
        short tableEntrySize,
        short framesPerTableEntry,
        int[]? tocValues = null)
    {
        var buffer = new StreamBuffer();
        buffer.WriteString("VBRI");
        buffer.WriteBigEndianInt16(version);
        buffer.WriteBigEndianInt16(delay);
        buffer.WriteBigEndianInt16(quality);
        buffer.WriteBigEndianInt32(fileSize);
        buffer.WriteBigEndianInt32(frameCount);
        buffer.WriteBigEndianInt16(tableEntries);
        buffer.WriteBigEndianInt16(tableScale);
        buffer.WriteBigEndianInt16(tableEntrySize);
        buffer.WriteBigEndianInt16(framesPerTableEntry);

        var entries = tableEntries + 1;
        for (var i = 0; i < entries; i++)
        {
            var raw = (tocValues != null && i < tocValues.Length) ? tocValues[i] : 0;
            buffer.WriteBigEndianBytes(raw, tableEntrySize);
        }

        return buffer.ToByteArray();
    }

    private static byte[] BuildLameTagPayload(
        string encoderVersion = "LAME3.99",
        int infoTagRevision = InfoTagRevision.Revision1,
        int vbrMethod = 3,
        int lowpassHz = 19500,
        float peakAmplitude = 1.0f,
        short radioGain = 0,
        short audiophileGain = 0,
        int encodingFlags = 0,
        int athType = 3,
        int bitrate = 0,
        int encoderDelays = 0,
        int misc = 0,
        int mp3Gain = 0,
        short presetSurroundInfo = 0,
        int musicLength = 0,
        short musicCrc = 0,
        short infoTagCrc = 0)
    {
        var buffer = new StreamBuffer();

        // 9-byte encoder string, right-padded with spaces to fit the spec slot.
        var version = encoderVersion.Length >= 9 ? encoderVersion[..9] : encoderVersion.PadRight(9, ' ');
        buffer.WriteString(version);

        // 4 MSB = info tag revision, 4 LSB = VBR method.
        buffer.WriteByte((byte)(((infoTagRevision & 0x0F) << 4) | (vbrMethod & 0x0F)));

        // Lowpass filter stored in multiples of 100 Hz.
        buffer.WriteByte((byte)(lowpassHz / 100));

        buffer.WriteFloat(peakAmplitude);
        // RadioReplayGain / AudiophileReplayGain are read with ReadInt16 (native LE) in LameTag.
        buffer.WriteShort(radioGain);
        buffer.WriteShort(audiophileGain);

        // Encoding flags MSB, ATH type LSB.
        buffer.WriteByte((byte)(((encodingFlags & 0x0F) << 4) | (athType & 0x0F)));
        buffer.WriteByte((byte)bitrate);
        // EncoderDelays is read via ReadInt(3) which is native LE. Use WriteBytes(value, 3) to match.
        buffer.WriteBytes(encoderDelays, 3);
        buffer.WriteByte((byte)misc);
        buffer.WriteByte((byte)mp3Gain);
        // PresetSurroundInfo is also read with ReadInt16 (native LE).
        buffer.WriteShort(presetSurroundInfo);
        buffer.WriteBigEndianInt32(musicLength);
        buffer.WriteBigEndianInt16(musicCrc);
        buffer.WriteBigEndianInt16(infoTagCrc);

        return buffer.ToByteArray();
    }

    private static StreamBuffer WrapForCtor(byte[] payload, long prefixOffset)
    {
        // The public Xing/Vbri ctors expect the StreamBuffer to be positioned at
        // <prefixOffset> where the tag bytes start. We pad with zeros so seeks in
        // LameTag.FindTag(+120) don't run off the end.
        var padded = new byte[prefixOffset + payload.Length + 256];
        Buffer.BlockCopy(payload, 0, padded, (int)prefixOffset, payload.Length);
        return new StreamBuffer(padded) { Position = prefixOffset };
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Xing header detection
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void XingFindHeaderDetectsXingMagic()
    {
        var payload = BuildXingPayload(XingHeaderFlags.FrameCountFlag, frameCount: 100);
        var frame = CreateMp3Frame(payload, XingOffsetInFrame);

        var header = XingHeader.FindHeader(frame);

        Assert.NotNull(header);
        Assert.Equal("Xing", header!.Name);
        Assert.Equal(VbrHeaderType.Xing, header.HeaderType);
        Assert.Equal(100, header.FrameCount);
    }

    [Fact]
    public void XingFindHeaderDetectsInfoMagicForCbr()
    {
        var payload = BuildXingPayload(XingHeaderFlags.FileSizeFlag, fileSize: 2048, useInfoMagic: true);
        var frame = CreateMp3Frame(payload, XingOffsetInFrame);

        var header = XingHeader.FindHeader(frame);

        Assert.NotNull(header);
        Assert.Equal("Info", header!.Name);
        Assert.Equal(2048, header.FileSize);
    }

    [Fact]
    public void XingFindHeaderReturnsNullWhenMagicAbsent()
    {
        // Empty audio data at the Xing slot => no magic string.
        var frame = CreateMp3Frame([], XingOffsetInFrame);

        var header = XingHeader.FindHeader(frame);

        Assert.Null(header);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Xing flags
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void XingFrameCountFlagPopulatesFrameCount()
    {
        var payload = BuildXingPayload(XingHeaderFlags.FrameCountFlag, frameCount: 4242);
        var buffer = WrapForCtor(payload, 0);

        var header = new XingHeader(CreateMp3Frame([], XingOffsetInFrame), buffer, 0);

        Assert.Equal(XingHeaderFlags.FrameCountFlag, header.Flags);
        Assert.Equal(4242, header.FrameCount);
        Assert.Equal(0, header.FileSize);
    }

    [Fact]
    public void XingFileSizeFlagPopulatesFileSize()
    {
        var payload = BuildXingPayload(XingHeaderFlags.FileSizeFlag, fileSize: 123456);
        var buffer = WrapForCtor(payload, 0);

        var header = new XingHeader(CreateMp3Frame([], XingOffsetInFrame), buffer, 0);

        Assert.Equal(XingHeaderFlags.FileSizeFlag, header.Flags);
        Assert.Equal(123456, header.FileSize);
        Assert.Equal(0, header.FrameCount);
    }

    [Fact]
    public void XingTocFlagPopulatesHundredEntries()
    {
        var toc = new byte[100];
        toc[0] = 0;
        for (var i = 1; i < 100; i++)
        {
            toc[i] = (byte)((i * 2) + 1);
        }
        var payload = BuildXingPayload(XingHeaderFlags.TocFlag | XingHeaderFlags.FileSizeFlag, fileSize: 1_000_000, toc: toc);
        var buffer = WrapForCtor(payload, 0);

        var header = new XingHeader(CreateMp3Frame([], XingOffsetInFrame), buffer, 0);

        // Validate TOC was read & in monotonic order by exercising SeekPositionByPercent.
        // At 0% with Toc[0]=0 the interpolation yields 0.
        Assert.Equal(0, header.SeekPositionByPercent(0));
        Assert.True(header.SeekPositionByPercent(50) > 0);
        Assert.True(header.SeekPositionByPercent(99) > header.SeekPositionByPercent(50));
    }

    [Fact]
    public void XingVbrScaleFlagPopulatesQuality()
    {
        var payload = BuildXingPayload(XingHeaderFlags.VbrScaleFlag, quality: 75);
        var buffer = WrapForCtor(payload, 0);

        var header = new XingHeader(CreateMp3Frame([], XingOffsetInFrame), buffer, 0);

        Assert.Equal(XingHeaderFlags.VbrScaleFlag, header.Flags);
        Assert.Equal(75, header.Quality);
    }

    [Fact]
    public void XingAllFlagsSetPopulatesEverything()
    {
        var toc = new byte[100];
        for (var i = 0; i < 100; i++)
        {
            toc[i] = (byte)i;
        }
        const int AllFlags = XingHeaderFlags.FrameCountFlag
                             | XingHeaderFlags.FileSizeFlag
                             | XingHeaderFlags.TocFlag
                             | XingHeaderFlags.VbrScaleFlag;

        var payload = BuildXingPayload(AllFlags, frameCount: 500, fileSize: 9_000_000, toc: toc, quality: 50);
        var buffer = WrapForCtor(payload, 0);

        var header = new XingHeader(CreateMp3Frame([], XingOffsetInFrame), buffer, 0);

        Assert.Equal(AllFlags, header.Flags);
        Assert.Equal(500, header.FrameCount);
        Assert.Equal(9_000_000, header.FileSize);
        Assert.Equal(50, header.Quality);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Xing SeekPositionByPercent regressions
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void XingSeekPositionByPercentWithNullTocReturnsZero()
    {
        // TocFlag NOT set, FileSize is set.
        var payload = BuildXingPayload(XingHeaderFlags.FileSizeFlag, fileSize: 1_000_000);
        var buffer = WrapForCtor(payload, 0);
        var header = new XingHeader(CreateMp3Frame([], XingOffsetInFrame), buffer, 0);

        var result = header.SeekPositionByPercent(50f);

        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(50.5f)]
    [InlineData(99f)]
    [InlineData(100f)]
    [InlineData(150f)]
    [InlineData(-10f)]
    public void XingSeekPositionByPercentClampsAndDoesNotThrow(float percentage)
    {
        var toc = new byte[100];
        for (var i = 0; i < 100; i++)
        {
            toc[i] = (byte)((i * 256 / 100) & 0xFF);
        }
        var payload = BuildXingPayload(XingHeaderFlags.TocFlag | XingHeaderFlags.FileSizeFlag, fileSize: 4_000_000, toc: toc);
        var buffer = WrapForCtor(payload, 0);
        var header = new XingHeader(CreateMp3Frame([], XingOffsetInFrame), buffer, 0);

        var result = header.SeekPositionByPercent(percentage);

        Assert.InRange(result, 0, header.FileSize);
    }

    [Fact]
    public void XingSeekPositionByPercentHundredReturnsFileSize()
    {
        // Last Toc entry = 255 so with fb=256 and pct=100 the result is floor(256/256 * FileSize) = FileSize.
        var toc = new byte[100];
        for (var i = 0; i < 100; i++)
        {
            toc[i] = (byte)((i * 256 / 100) & 0xFF);
        }
        toc[99] = 255;
        var payload = BuildXingPayload(XingHeaderFlags.TocFlag | XingHeaderFlags.FileSizeFlag, fileSize: 4_000_000, toc: toc);
        var buffer = WrapForCtor(payload, 0);
        var header = new XingHeader(CreateMp3Frame([], XingOffsetInFrame), buffer, 0);

        var result = header.SeekPositionByPercent(100f);

        Assert.Equal(4_000_000, result);
    }

    [Fact]
    public void XingSeekPositionByPercentLargeFileSizeKeepsPrecision()
    {
        // 100 MB file, TOC with entries doubling linearly in 0..199 range so 50% should map
        // to ~39% of the file (toc[50] = 100, fx ~ 100+0.5 => 100.5 => floor(100.5/256 * 100M)).
        var toc = new byte[100];
        for (var i = 0; i < 100; i++)
        {
            toc[i] = (byte)(i * 2); // 0, 2, 4, ..., 198
        }
        const int LargeFileSize = 100 * 1024 * 1024; // 100 MB
        var payload = BuildXingPayload(XingHeaderFlags.TocFlag | XingHeaderFlags.FileSizeFlag, fileSize: LargeFileSize, toc: toc);
        var buffer = WrapForCtor(payload, 0);
        var header = new XingHeader(CreateMp3Frame([], XingOffsetInFrame), buffer, 0);

        var result = header.SeekPositionByPercent(50f);

        // toc[50]=100, toc[51]=102, fx = 100, expected = 100/256 * 100MB ~ 40,960,000.
        const long Expected = (long)100 * LargeFileSize / 256;
        Assert.Equal(Expected, result);
        // Sanity: we didn't lose precision by truncating to 16-bit-ish.
        Assert.True(result > 16 * 1024 * 1024);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // VBRI header detection
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void VbriFindHeaderDetectsMagicAt32BytesPastFrameHeader()
    {
        var payload = BuildVbriPayload(
            version: 1,
            delay: 0,
            quality: 70,
            fileSize: 1_000_000,
            frameCount: 1000,
            tableEntries: 10,
            tableScale: 1,
            tableEntrySize: 2,
            framesPerTableEntry: 100,
            tocValues: [10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110]);
        var frame = CreateMp3Frame(payload, VbriOffsetInFrame);

        var header = VbriHeader.FindHeader(frame);

        Assert.NotNull(header);
        Assert.Equal("VBRI", header!.Name);
        Assert.Equal(VbrHeaderType.Vbri, header.HeaderType);
        Assert.Equal(1000, header.FrameCount);
        Assert.Equal(1_000_000, header.FileSize);
    }

    [Fact]
    public void VbriFindHeaderReturnsNullWithoutMagic()
    {
        var frame = CreateMp3Frame([], VbriOffsetInFrame);

        var header = VbriHeader.FindHeader(frame);

        Assert.Null(header);
    }

    [Fact]
    public void VbriFindHeaderReturnsNullWhenMagicIsAtWrongOffset()
    {
        // Place magic one byte off.
        var payload = BuildVbriPayload(1, 0, 70, 1_000_000, 1000, 10, 1, 2, 100);
        var frame = CreateMp3Frame(payload, VbriOffsetInFrame + 1);

        var header = VbriHeader.FindHeader(frame);

        Assert.Null(header);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // VBRI seek regressions
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void VbriSeekPositionByTimeAtZeroDoesNotThrow()
    {
        var payload = BuildVbriPayload(1, 0, 70, 1_000_000, 1000, 10, 1, 2, 100,
            tocValues: [100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100]);
        var buffer = WrapForCtor(payload, 0);
        var header = new VbriHeader(CreateMp3Frame([], VbriOffsetInFrame), buffer, 0);

        var result = header.SeekPositionByTime(0f);

        Assert.True(result >= 0);
    }

    [Fact]
    public void VbriSeekPositionByTimeBeyondTotalDoesNotThrow()
    {
        var payload = BuildVbriPayload(1, 0, 70, 1_000_000, 1000, 10, 1, 2, 100,
            tocValues: [100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100]);
        var buffer = WrapForCtor(payload, 0);
        var header = new VbriHeader(CreateMp3Frame([], VbriOffsetInFrame), buffer, 0);

        // Way beyond total length.
        var result = header.SeekPositionByTime(float.MaxValue / 2);

        Assert.True(result >= 0);
    }

    [Fact]
    public void VbriSeekTimeByPositionEdgeInputsDoNotThrow()
    {
        var payload = BuildVbriPayload(1, 0, 70, 1_000_000, 1000, 10, 1, 2, 100,
            tocValues: [100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100]);
        var buffer = WrapForCtor(payload, 0);
        var header = new VbriHeader(CreateMp3Frame([], VbriOffsetInFrame), buffer, 0);

        var atZero = header.SeekTimeByPosition(0);
        var atEnd = header.SeekTimeByPosition(int.MaxValue);

        Assert.True(atZero >= 0f);
        Assert.True(atEnd >= 0f);
    }

    [Fact]
    public void VbriSeekWithZeroFrameCountReturnsZero()
    {
        var payload = BuildVbriPayload(1, 0, 70, 0, 0, 2, 1, 2, 100,
            tocValues: [0, 0, 0]);
        var buffer = WrapForCtor(payload, 0);
        var header = new VbriHeader(CreateMp3Frame([], VbriOffsetInFrame), buffer, 0);

        Assert.Equal(0, header.SeekPositionByTime(1000f));
        Assert.Equal(0f, header.SeekTimeByPosition(1000));
        Assert.Equal(0L, header.SeekPositionByPercent(50f));
    }

    [Fact]
    public void VbriSeekPositionByPercentClampsOutOfRangeInputs()
    {
        var payload = BuildVbriPayload(1, 0, 70, 1_000_000, 1000, 10, 1, 2, 100,
            tocValues: [100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100]);
        var buffer = WrapForCtor(payload, 0);
        var header = new VbriHeader(CreateMp3Frame([], VbriOffsetInFrame), buffer, 0);

        // Shouldn't throw and should stay in a sane range.
        var lo = header.SeekPositionByPercent(-5f);
        var hi = header.SeekPositionByPercent(150f);

        Assert.True(lo >= 0);
        Assert.True(hi >= 0);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // LAME tag detection
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void LameTagFindTagReturnsNonNullForLamePrefix()
    {
        var lame = BuildLameTagPayload();
        var buffer = new StreamBuffer(lame);

        var tag = LameTag.FindTag(buffer, 0);

        Assert.NotNull(tag);
        Assert.Equal("LAME3.99 ", tag!.EncoderVersion);
    }

    [Fact]
    public void LameTagFindTagReturnsNullWhenMagicAbsent()
    {
        var buffer = new StreamBuffer(new byte[64]);

        var tag = LameTag.FindTag(buffer, 0);

        Assert.Null(tag);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // LAME revision + VBR method decoding
    ////------------------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(InfoTagRevision.Revision0, 1)] // rev0 / CBR
    [InlineData(InfoTagRevision.Revision1, 2)] // rev1 / ABR
    [InlineData(InfoTagRevision.Revision1, 3)] // rev1 / VBR1
    [InlineData(InfoTagRevision.Revision0, 9)] // rev0 / ABR2pass
    public void LameTagDecodesRevisionAndVbrMethod(int revision, int vbrMethod)
    {
        var lame = BuildLameTagPayload(infoTagRevision: revision, vbrMethod: vbrMethod);
        var buffer = new StreamBuffer(lame);

        var tag = LameTag.FindTag(buffer, 0);

        Assert.NotNull(tag);
        Assert.Equal(revision, tag!.InfoTagRevision);
        Assert.Equal(vbrMethod, tag.VbrMethod);
    }

    [Fact]
    public void LameTagVbrMethodFlagsMatchSpec()
    {
        var cbrTag = LameTag.FindTag(new StreamBuffer(BuildLameTagPayload(vbrMethod: 1)), 0);
        var abrTag = LameTag.FindTag(new StreamBuffer(BuildLameTagPayload(vbrMethod: 2)), 0);
        var vbrTag = LameTag.FindTag(new StreamBuffer(BuildLameTagPayload(vbrMethod: 4)), 0);

        Assert.True(cbrTag!.IsCbr);
        Assert.False(cbrTag.IsVbr);
        Assert.False(cbrTag.IsAbr);

        Assert.True(abrTag!.IsAbr);
        Assert.False(abrTag.IsCbr);
        Assert.False(abrTag.IsVbr);

        Assert.True(vbrTag!.IsVbr);
        Assert.False(vbrTag.IsCbr);
        Assert.False(vbrTag.IsAbr);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // LAME CRC fields
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void LameTagMusicCrcAndInfoTagCrcAreReadFromCorrectBytes()
    {
        const short ExpectedMusicCrc = 0x1234;
        const short ExpectedInfoTagCrc = 0x5678;
        var lame = BuildLameTagPayload(musicCrc: ExpectedMusicCrc, infoTagCrc: ExpectedInfoTagCrc);
        var buffer = new StreamBuffer(lame);

        var tag = LameTag.FindTag(buffer, 0);

        Assert.NotNull(tag);
        Assert.Equal(ExpectedMusicCrc, tag!.MusicCrc);
        Assert.Equal(ExpectedInfoTagCrc, tag.InfoTagCrc);
    }

    [Fact]
    public void LameTagMusicLengthIsReadFromCorrectBytes()
    {
        const int ExpectedMusicLength = 0x00ABCDEF;
        var lame = BuildLameTagPayload(musicLength: ExpectedMusicLength);
        var buffer = new StreamBuffer(lame);

        var tag = LameTag.FindTag(buffer, 0);

        Assert.NotNull(tag);
        Assert.Equal(ExpectedMusicLength, tag!.MusicLength);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // LAME reserved revision behaviour
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void LameTagReservedRevisionThrowsArgumentException()
    {
        var lame = BuildLameTagPayload(infoTagRevision: InfoTagRevision.Reserved);
        var buffer = new StreamBuffer(lame);

        Assert.Throws<ArgumentException>(() => LameTag.FindTag(buffer, 0));
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Round-trip tests
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void XingHeaderRoundTripPreservesAllFields()
    {
        var toc = new byte[100];
        for (var i = 0; i < 100; i++)
        {
            toc[i] = (byte)((i * 3) & 0xFF);
        }
        const int AllFlags = XingHeaderFlags.FrameCountFlag
                             | XingHeaderFlags.FileSizeFlag
                             | XingHeaderFlags.TocFlag
                             | XingHeaderFlags.VbrScaleFlag;

        var payload = BuildXingPayload(AllFlags, frameCount: 321, fileSize: 7_654_321, toc: toc, quality: 42);
        var originalBuffer = WrapForCtor(payload, 0);
        var original = new XingHeader(CreateMp3Frame([], XingOffsetInFrame), originalBuffer, 0);

        var serialized = original.ToByteArray();
        var reparsedBuffer = WrapForCtor(serialized, 0);
        var reparsed = new XingHeader(CreateMp3Frame([], XingOffsetInFrame), reparsedBuffer, 0);

        Assert.Equal(original.Name, reparsed.Name);
        Assert.Equal(original.Flags, reparsed.Flags);
        Assert.Equal(original.FrameCount, reparsed.FrameCount);
        Assert.Equal(original.FileSize, reparsed.FileSize);
        Assert.Equal(original.Quality, reparsed.Quality);
        Assert.Equal(original.SeekPositionByPercent(25f), reparsed.SeekPositionByPercent(25f));
        Assert.Equal(original.SeekPositionByPercent(75f), reparsed.SeekPositionByPercent(75f));
    }

    [Fact]
    public void VbriHeaderRoundTripPreservesAllFields()
    {
        int[] tocValues = [10, 20, 30, 40, 50, 60];
        var payload = BuildVbriPayload(
            version: 1,
            delay: 50,
            quality: 80,
            fileSize: 2_000_000,
            frameCount: 2000,
            tableEntries: 5,
            tableScale: 1,
            tableEntrySize: 2,
            framesPerTableEntry: 400,
            tocValues: tocValues);

        var originalBuffer = WrapForCtor(payload, 0);
        var original = new VbriHeader(CreateMp3Frame([], VbriOffsetInFrame), originalBuffer, 0);

        var serialized = original.ToByteArray();
        var reparsedBuffer = WrapForCtor(serialized, 0);
        var reparsed = new VbriHeader(CreateMp3Frame([], VbriOffsetInFrame), reparsedBuffer, 0);

        Assert.Equal(original.Name, reparsed.Name);
        Assert.Equal(original.Version, reparsed.Version);
        Assert.Equal(original.Quality, reparsed.Quality);
        Assert.Equal(original.FileSize, reparsed.FileSize);
        Assert.Equal(original.FrameCount, reparsed.FrameCount);
    }
}
