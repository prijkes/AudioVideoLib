namespace AudioVideoLib.Tests.Cryptography;

using AudioVideoLib.Cryptography;
using Xunit;

public sealed class Crc16Tests
{
    // FLAC's CRC-16 is polynomial 0x8005, MSB-first, init 0, no reflection, no XOR-out.
    // Spec reference: RFC 9639 §11.1.

    [Fact]
    public void Empty_ReturnsZero()
    {
        Assert.Equal(0, Crc16.Calculate([]));
    }

    [Fact]
    public void SingleZeroByte_ReturnsZero()
    {
        Assert.Equal(0, Crc16.Calculate([0x00]));
    }

    [Fact]
    public void KnownVector_SingleByte0x54_Returns_0x81FB()
    {
        // ASCII 'T' = 0x54. CRC-16/FLAC of single byte 0x54 with poly 0x8005 MSB-first
        // (init 0, no reflection, no XOR-out) = 0x81FB. Verified against the reference
        // bit-serial algorithm; the value 0x4F70 referenced in the original plan was
        // incorrect.
        Assert.Equal(0x81FB, Crc16.Calculate("T"u8));
    }

    [Fact]
    public void KnownVector_String123456789_Returns_0xFEE8()
    {
        // ASCII "123456789". CRC-16/FLAC = 0xFEE8 (poly=0x8005, refin=false, refout=false, init=0, xorout=0).
        // This is also the published check value for CRC-16/UMTS (a.k.a. CRC-16/BUYPASS),
        // which is the same parameter set as FLAC's frame-footer CRC.
        Assert.Equal(0xFEE8, Crc16.Calculate("123456789"u8));
    }
}
