/*
 * Test suite for EBML variable-length integer encoding / decoding.
 */
namespace AudioVideoLib.Tests;

using System.IO;

using AudioVideoLib.Formats;

using Xunit;

public class EbmlElementTests
{
    // ================================================================
    // 1. VINT size encode/decode for representative values across all 8 lengths.
    // ================================================================

    [Theory]
    [InlineData(0L, 1)]
    [InlineData(1L, 1)]
    [InlineData(126L, 1)]                       // max length-1 (127 is the unknown sentinel)
    [InlineData(127L, 2)]
    [InlineData(0x3FFEL, 2)]                    // max length-2 (0x3FFF is unknown sentinel)
    [InlineData(0x3FFFL, 3)]
    [InlineData(0x1FFFFEL, 3)]                  // max length-3
    [InlineData(0x1FFFFFL, 4)]
    [InlineData(0x0FFFFFFEL, 4)]                // max length-4
    [InlineData(0x0FFFFFFFL, 5)]
    [InlineData(0x07FFFFFFFEL, 5)]              // max length-5
    [InlineData(0x07FFFFFFFFL, 6)]
    [InlineData(0x03FFFFFFFFFEL, 6)]            // max length-6
    [InlineData(0x03FFFFFFFFFFL, 7)]
    [InlineData(0x01FFFFFFFFFFFEL, 7)]          // max length-7
    [InlineData(0x01FFFFFFFFFFFFL, 8)]
    public void VintSize_Encode_PicksMinimumLength(long value, int expectedLength)
    {
        var bytes = EbmlElement.EncodeVintSize(value);
        Assert.Equal(expectedLength, bytes.Length);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(126L)]
    [InlineData(127L)]
    [InlineData(1000L)]
    [InlineData(0x3FFEL)]
    [InlineData(0x123456L)]
    [InlineData(0x12345678L)]
    [InlineData(0x123456789AL)]
    [InlineData(0x123456789ABCL)]
    public void VintSize_RoundTrips(long value)
    {
        var bytes = EbmlElement.EncodeVintSize(value);
        using var ms = new MemoryStream(bytes);
        var ok = EbmlElement.TryReadVintSize(ms, out var len, out var decoded, out var unknown);
        Assert.True(ok);
        Assert.Equal(bytes.Length, len);
        Assert.Equal(value, decoded);
        Assert.False(unknown);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void VintSize_AllLengths_RoundTripExhaustiveBoundaries(int length)
    {
        // Test 0 and the max non-sentinel value at each length.
        long[] samples = length == 8 ? [0, 1, 0x123456789ABCL] : [0, 1, length == 1 ? 100L : 1L << (7 * (length - 1))];
        foreach (var v in samples)
        {
            var bytes = EbmlElement.EncodeVintSize(v, length);
            Assert.Equal(length, bytes.Length);
            using var ms = new MemoryStream(bytes);
            var ok = EbmlElement.TryReadVintSize(ms, out var len, out var decoded, out var unknown);
            Assert.True(ok);
            Assert.Equal(length, len);
            Assert.Equal(v, decoded);
            Assert.False(unknown);
        }
    }

    // ================================================================
    // 2. ID encode/decode round-trip for the canonical Matroska ids.
    // ================================================================

    [Theory]
    [InlineData(0x1A45DFA3L, 4)]                 // EBML root
    [InlineData(0x18538067L, 4)]                 // Segment
    [InlineData(0x1254C367L, 4)]                 // Tags
    [InlineData(0x7373L, 2)]                     // Tag
    [InlineData(0x67C8L, 2)]                     // SimpleTag
    [InlineData(0x63C0L, 2)]                     // Targets
    [InlineData(0x68CAL, 2)]                     // TargetTypeValue
    [InlineData(0x45A3L, 2)]                     // TagName
    [InlineData(0xA0L, 1)]                       // BlockGroup
    public void Id_EncodeAndReadBack(long id, int expectedLength)
    {
        var bytes = EbmlElement.EncodeId(id);
        Assert.Equal(expectedLength, bytes.Length);

        using var ms = new MemoryStream(bytes);
        var ok = EbmlElement.TryReadVintId(ms, out var len, out var readId);
        Assert.True(ok);
        Assert.Equal(expectedLength, len);
        Assert.Equal(id, readId);
    }

    // ================================================================
    // 3. UInt encode/decode for payload values.
    // ================================================================

    [Theory]
    [InlineData(0L, 1)]
    [InlineData(1L, 1)]
    [InlineData(255L, 1)]
    [InlineData(256L, 2)]
    [InlineData(0xFFFFL, 2)]
    [InlineData(0xFFFFFFL, 3)]
    [InlineData(0xFFFFFFFFL, 4)]
    [InlineData(0x123456789AL, 5)]
    public void UInt_RoundTrips(long value, int expectedLength)
    {
        var bytes = EbmlElement.EncodeUInt(value);
        Assert.Equal(expectedLength, bytes.Length);
        Assert.Equal(value, EbmlElement.DecodeUInt(bytes));
    }

    // ================================================================
    // 4. Float decoding.
    // ================================================================

    [Fact]
    public void DecodeFloat_Float32_DecodesCorrectly()
    {
        // 1.5f in big-endian IEEE 754 = 0x3FC00000.
        byte[] bytes = [0x3F, 0xC0, 0x00, 0x00];
        Assert.Equal(1.5, EbmlElement.DecodeFloat(bytes), 6);
    }

    [Fact]
    public void DecodeFloat_Float64_DecodesCorrectly()
    {
        // 1.5 in big-endian IEEE 754 double = 0x3FF8000000000000.
        byte[] bytes = [0x3F, 0xF8, 0, 0, 0, 0, 0, 0];
        Assert.Equal(1.5, EbmlElement.DecodeFloat(bytes), 9);
    }

    [Fact]
    public void DecodeFloat_BadLength_ReturnsZero()
    {
        Assert.Equal(0, EbmlElement.DecodeFloat([0x01, 0x02]));
    }

    // ================================================================
    // 5. Unknown-size sentinel detection.
    // ================================================================

    [Fact]
    public void VintSize_UnknownSentinel_OneByte_Detected()
    {
        // Length 1, all data bits 1: 0xFF.
        using var ms = new MemoryStream([0xFF]);
        var ok = EbmlElement.TryReadVintSize(ms, out var len, out var value, out var unknown);
        Assert.True(ok);
        Assert.Equal(1, len);
        Assert.Equal(0x7FL, value);
        Assert.True(unknown);
    }

    [Fact]
    public void VintSize_UnknownSentinel_FourByte_Detected()
    {
        // Length 4 unknown size = 0x1FFFFFFF.
        using var ms = new MemoryStream([0x1F, 0xFF, 0xFF, 0xFF]);
        var ok = EbmlElement.TryReadVintSize(ms, out var len, out _, out var unknown);
        Assert.True(ok);
        Assert.Equal(4, len);
        Assert.True(unknown);
    }

    // ================================================================
    // 6. Bounds / error handling.
    // ================================================================

    [Fact]
    public void Vint_FirstByteZero_ReturnsFalse()
    {
        // No marker bit anywhere — invalid.
        using var ms = new MemoryStream([0x00, 0xFF]);
        Assert.False(EbmlElement.TryReadVintId(ms, out _, out _));
    }

    [Fact]
    public void Vint_EofAfterMarker_ReturnsFalse()
    {
        // First byte 0x40 → marker at bit 6 → length 2, but we only supply 1 byte.
        using var ms = new MemoryStream([0x40]);
        Assert.False(EbmlElement.TryReadVintSize(ms, out _, out _, out _));
    }

    [Fact]
    public void Vint_EmptyStream_ReturnsFalse()
    {
        using var ms = new MemoryStream([]);
        Assert.False(EbmlElement.TryReadVintId(ms, out _, out _));
    }
}
