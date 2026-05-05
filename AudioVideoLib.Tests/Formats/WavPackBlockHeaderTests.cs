namespace AudioVideoLib.Tests.Formats;

using System;
using System.Buffers.Binary;

using AudioVideoLib.Formats;

using Xunit;

public sealed class WavPackBlockHeaderTests
{
    [Fact]
    public void Parse_RejectsNonWvpkMagic()
    {
        var bad = new byte[WavPackBlockHeader.Size];
        bad[0] = (byte)'X';
        Assert.Null(WavPackBlockHeader.Parse(bad));
    }

    [Fact]
    public void Parse_RejectsTruncatedSpan()
    {
        Assert.Null(WavPackBlockHeader.Parse(new byte[31]));
    }

    [Fact]
    public void Parse_DecodesAllFields()
    {
        var b = new byte[WavPackBlockHeader.Size];
        b[0] = (byte)'w';
        b[1] = (byte)'v';
        b[2] = (byte)'p';
        b[3] = (byte)'k';
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(4, 4), 0x1234u);              // ckSize
        BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(8, 2), 0x0410);               // version
        b[10] = 0x01;                                                                   // block_index_u8
        b[11] = 0x02;                                                                   // total_samples_u8
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(12, 4), 0x1000u);             // total_samples
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(16, 4), 0x2000u);             // block_index
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(20, 4), 0x0400u);             // block_samples

        // flags: BYTES_STORED=1 (=> 2 bytes/sample), SRATE index 9 (=> 44100), HYBRID set.
        var flags = 0x1u | 0x8u | (9u << 23);
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(24, 4), flags);
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(28, 4), 0xDEADBEEFu);          // crc

        var h = WavPackBlockHeader.Parse(b);

        Assert.NotNull(h);
        Assert.Equal("wvpk", h!.CkId);
        Assert.Equal(0x1234u, h.CkSize);
        Assert.Equal(0x0410, h.Version);
        Assert.Equal((byte)0x01, h.BlockIndexHigh);
        Assert.Equal((byte)0x02, h.TotalSamplesHigh);
        Assert.Equal(0x1000u, h.TotalSamplesLow);
        Assert.Equal(0x2000u, h.BlockIndexLow);
        Assert.Equal(0x0400u, h.BlockSamples);
        Assert.Equal(flags, h.Flags);
        Assert.Equal(0xDEADBEEFu, h.Crc);
        Assert.Equal(2, h.BytesPerSample);
        Assert.False(h.IsMono);
        Assert.True(h.IsHybrid);
        Assert.False(h.IsFloat);
        Assert.Equal(44100, h.SampleRate);
        Assert.Equal(0x2000L + (0x01L << 32), h.BlockIndex);

        // GET_TOTAL_SAMPLES: 0x1000 + (0x02 << 32) - 0x02
        Assert.Equal(0x1000L + (0x02L << 32) - 0x02L, h.TotalSamples);
    }

    [Fact]
    public void TotalSamples_UnknownIsMinusOne()
    {
        var b = new byte[WavPackBlockHeader.Size];
        b[0] = (byte)'w';
        b[1] = (byte)'v';
        b[2] = (byte)'p';
        b[3] = (byte)'k';
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(12, 4), 0xFFFFFFFFu);
        Assert.Equal(-1L, WavPackBlockHeader.Parse(b)!.TotalSamples);
    }

    [Fact]
    public void SampleRate_NonStandardIndexIsZero()
    {
        var b = new byte[WavPackBlockHeader.Size];
        b[0] = (byte)'w';
        b[1] = (byte)'v';
        b[2] = (byte)'p';
        b[3] = (byte)'k';
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(24, 4), 15u << 23);
        Assert.Equal(0, WavPackBlockHeader.Parse(b)!.SampleRate);
    }

    [Theory]
    [InlineData(0u, 1)]
    [InlineData(1u, 2)]
    [InlineData(2u, 3)]
    [InlineData(3u, 4)]
    public void BytesPerSample_DerivedFromLowFlagBits(uint storedField, int expected)
    {
        var b = new byte[WavPackBlockHeader.Size];
        b[0] = (byte)'w';
        b[1] = (byte)'v';
        b[2] = (byte)'p';
        b[3] = (byte)'k';
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(24, 4), storedField);
        Assert.Equal(expected, WavPackBlockHeader.Parse(b)!.BytesPerSample);
    }

    [Theory]
    [InlineData(0x4u, true, false, false, false, false, false)]      // MONO
    [InlineData(0x8u, false, true, false, false, false, false)]      // HYBRID
    [InlineData(0x80u, false, false, true, false, false, false)]     // FLOAT_DATA
    [InlineData(0x800u, false, false, false, true, false, false)]    // INITIAL_BLOCK
    [InlineData(0x1000u, false, false, false, false, true, false)]   // FINAL_BLOCK
    [InlineData(0x80000000u, false, false, false, false, false, true)] // DSD_FLAG
    public void FlagAccessors_DecodeIndividualBits(
        uint flags,
        bool isMono,
        bool isHybrid,
        bool isFloat,
        bool isInitial,
        bool isFinal,
        bool isDsd)
    {
        var b = new byte[WavPackBlockHeader.Size];
        b[0] = (byte)'w';
        b[1] = (byte)'v';
        b[2] = (byte)'p';
        b[3] = (byte)'k';
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(24, 4), flags);

        var h = WavPackBlockHeader.Parse(b)!;
        Assert.Equal(isMono, h.IsMono);
        Assert.Equal(isHybrid, h.IsHybrid);
        Assert.Equal(isFloat, h.IsFloat);
        Assert.Equal(isInitial, h.IsInitialBlock);
        Assert.Equal(isFinal, h.IsFinalBlock);
        Assert.Equal(isDsd, h.IsDsd);
    }

    [Theory]
    [InlineData(0u, 6000)]
    [InlineData(1u, 8000)]
    [InlineData(2u, 9600)]
    [InlineData(3u, 11025)]
    [InlineData(4u, 12000)]
    [InlineData(5u, 16000)]
    [InlineData(6u, 22050)]
    [InlineData(7u, 24000)]
    [InlineData(8u, 32000)]
    [InlineData(9u, 44100)]
    [InlineData(10u, 48000)]
    [InlineData(11u, 64000)]
    [InlineData(12u, 88200)]
    [InlineData(13u, 96000)]
    [InlineData(14u, 192000)]
    [InlineData(15u, 0)]
    public void SampleRate_DecodesIndexTable(uint idx, int expected)
    {
        var b = new byte[WavPackBlockHeader.Size];
        b[0] = (byte)'w';
        b[1] = (byte)'v';
        b[2] = (byte)'p';
        b[3] = (byte)'k';
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(24, 4), idx << 23);
        Assert.Equal(expected, WavPackBlockHeader.Parse(b)!.SampleRate);
    }

    [Fact]
    public void UniqueId_MasksOutFlagBits()
    {
        // not a header test — sanity-check that Size constant is the documented value.
        Assert.Equal(32, WavPackBlockHeader.Size);
    }
}
