/*
 * Test suite for the ID3v2.4.0 tag-level structure (header, extended header,
 * footer, padding, unsynchronisation, synchsafe integers, CRC, restrictions).
 *
 * Reference: id3v2_4_0-structure - ID3_org.mht (Id3v2.4.0 Informal Standard,
 * sections 3.1-3.4 and 6.1-6.2).
 */
namespace AudioVideoLib.Tests;

using System;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Text;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

using Xunit;

public class Id3v240StructureTests
{
    // Flag bit values for the v2.4 tag header flag byte (%abcd0000).
    private const byte FlagUnsynchronization = 0x80;
    private const byte FlagExtendedHeader = 0x40;
    private const byte FlagExperimental = 0x20;
    private const byte FlagFooter = 0x10;

    // Extended header flags for v2.4 (%0bcd0000 inside the 1 flag byte).
    private const byte ExtFlagTagIsUpdate = 0x40;
    private const byte ExtFlagCrcPresent = 0x20;
    private const byte ExtFlagTagIsRestricted = 0x10;

    ////----------------------------------------------------------------------
    // 1. Header: "ID3" + $04 00 + flag byte + 4-byte synchsafe size
    ////----------------------------------------------------------------------

    [Fact]
    public void Header_MinimalV240Tag_HasCorrectByteLayout()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var bytes = tag.ToByteArray();

        Assert.Equal(Id3v2Tag.HeaderSize, bytes.Length);
        Assert.Equal((byte)'I', bytes[0]);
        Assert.Equal((byte)'D', bytes[1]);
        Assert.Equal((byte)'3', bytes[2]);
        Assert.Equal((byte)0x04, bytes[3]); // major version
        Assert.Equal((byte)0x00, bytes[4]); // revision
        Assert.Equal((byte)0x00, bytes[5]); // flags byte

        // Size field is a 32-bit synchsafe integer; for an empty tag it's 0.
        Assert.Equal((byte)0x00, bytes[6]);
        Assert.Equal((byte)0x00, bytes[7]);
        Assert.Equal((byte)0x00, bytes[8]);
        Assert.Equal((byte)0x00, bytes[9]);
    }

    [Fact]
    public void Header_SizeField_EncodesFrameAndPaddingBytesAsSynchsafe()
    {
        // Construct a v2.4 tag with 257 bytes of padding: the spec's canonical
        // "257 -> 00 00 02 01" synchsafe example.
        var tag = new Id3v2Tag(Id3v2Version.Id3v240)
        {
            PaddingSize = 257
        };
        var bytes = tag.ToByteArray();

        Assert.Equal(Id3v2Tag.HeaderSize + 257, bytes.Length);
        Assert.Equal((byte)0x00, bytes[6]);
        Assert.Equal((byte)0x00, bytes[7]);
        Assert.Equal((byte)0x02, bytes[8]);
        Assert.Equal((byte)0x01, bytes[9]);
    }

    [Fact]
    public void Header_VersionByte_IsFour()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var bytes = tag.ToByteArray();

        Assert.Equal((byte)0x04, bytes[3]);
        Assert.Equal((byte)0x00, bytes[4]);
    }

    ////----------------------------------------------------------------------
    // 2. Header flags: %abcd0000 (a=unsync, b=ext hdr, c=exp, d=footer)
    ////----------------------------------------------------------------------

    [Fact]
    public void HeaderFlags_UnsynchronizationBit_WrittenCorrectly()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240) { UseUnsynchronization = true };
        var bytes = tag.ToByteArray();
        Assert.Equal(FlagUnsynchronization, (byte)(bytes[5] & FlagUnsynchronization));
    }

    [Fact]
    public void HeaderFlags_ExtendedHeaderBit_WrittenCorrectly()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240)
        {
            ExtendedHeader = new Id3v2ExtendedHeader()
        };
        var bytes = tag.ToByteArray();
        Assert.Equal(FlagExtendedHeader, (byte)(bytes[5] & FlagExtendedHeader));
    }

    [Fact]
    public void HeaderFlags_ExperimentalBit_WrittenCorrectly()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240) { TagIsExperimental = true };
        var bytes = tag.ToByteArray();
        Assert.Equal(FlagExperimental, (byte)(bytes[5] & FlagExperimental));
    }

    [Fact]
    public void HeaderFlags_FooterBit_WrittenCorrectly()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240) { UseFooter = true };
        var bytes = tag.ToByteArray();
        Assert.Equal(FlagFooter, (byte)(bytes[5] & FlagFooter));
    }

    [Fact]
    public void HeaderFlags_AllFourSet_BitPatternIsF0()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240)
        {
            UseUnsynchronization = true,
            TagIsExperimental = true,
            UseFooter = true,
            ExtendedHeader = new Id3v2ExtendedHeader()
        };
        var bytes = tag.ToByteArray();
        Assert.Equal((byte)0xF0, bytes[5]);
    }

    [Fact]
    public void HeaderFlags_RoundTripThroughReader_PreservesAllFlags()
    {
        var original = new Id3v2Tag(Id3v2Version.Id3v240)
        {
            UseUnsynchronization = true,
            TagIsExperimental = true,
            UseFooter = true,
            ExtendedHeader = new Id3v2ExtendedHeader()
        };
        var parsed = RoundTrip(original);

        Assert.NotNull(parsed);
        Assert.True(parsed!.UseUnsynchronization);
        Assert.True(parsed.TagIsExperimental);
        Assert.True(parsed.UseFooter);
        Assert.True(parsed.UseExtendedHeader);
    }

    ////----------------------------------------------------------------------
    // 3. Synchsafe encoding / decoding
    ////----------------------------------------------------------------------

    [Fact]
    public void Synchsafe_Zero_EncodesToZero()
    {
        Assert.Equal(0, Id3v2Tag.GetSynchsafeValue(0));
        Assert.Equal(0, Id3v2Tag.GetUnsynchedValue(0));
    }

    [Fact]
    public void Synchsafe_TwoHundredFiftySeven_MatchesSpecExample()
    {
        // Spec: "a 257 bytes long tag is represented as $00 00 02 01"
        var synchsafe = Id3v2Tag.GetSynchsafeValue(257);

        // Written big-endian as 4 bytes, this must equal 00 00 02 01.
        var sb = new StreamBuffer();
        sb.WriteBigEndianInt32(synchsafe);
        var bytes = sb.ToByteArray();

        Assert.Equal(new byte[] { 0x00, 0x00, 0x02, 0x01 }, bytes);
        Assert.Equal(257, Id3v2Tag.GetUnsynchedValue(synchsafe));
    }

    [Fact]
    public void Synchsafe_MaxValue_EncodesToAllSeven0x7F()
    {
        // 0x0FFFFFFF = 268435455 is the largest value representable in 28 bits.
        const int Max = 0x0FFFFFFF;
        var synchsafe = Id3v2Tag.GetSynchsafeValue(Max);

        var sb = new StreamBuffer();
        sb.WriteBigEndianInt32(synchsafe);
        var bytes = sb.ToByteArray();

        Assert.Equal(new byte[] { 0x7F, 0x7F, 0x7F, 0x7F }, bytes);
        Assert.Equal(Max, Id3v2Tag.GetUnsynchedValue(synchsafe));
    }

    [Fact]
    public void Synchsafe_Encoding_NeverSetsBit7OfAnyByte()
    {
        // Every byte of any correctly-encoded synchsafe integer has bit 7 clear.
        foreach (var v in new[] { 0, 1, 127, 128, 255, 257, 1024, 0x1FFFFF, 0x0FFFFFFF })
        {
            var encoded = Id3v2Tag.GetSynchsafeValue(v);
            Assert.Equal(0, encoded & unchecked((int)0x80808080));
        }
    }

    [Fact]
    public void Synchsafe_RoundTrip_RecoversValue()
    {
        foreach (var v in new[] { 0, 1, 127, 128, 255, 256, 257, 16383, 16384, 0x1FFFFF, 0x0FFFFFFF })
        {
            var encoded = Id3v2Tag.GetSynchsafeValue(v);
            Assert.Equal(v, Id3v2Tag.GetUnsynchedValue(encoded));
        }
    }

    [Fact]
    public void Synchsafe_InvalidFFFFFFFF_DecodesToMaxByStrippingBit7()
    {
        // 0xFFFFFFFF is not a valid synchsafe integer (every bit-7 is set).
        // The library's unsync routine strips bit 7 of each byte, so the
        // decoded value cannot exceed 0x0FFFFFFF (the legal max).
        var sb = new StreamBuffer([0xFF, 0xFF, 0xFF, 0xFF]);
        var asInt = sb.ReadBigEndianInt32();
        var unsynched = Id3v2Tag.GetUnsynchedValue(asInt);

        Assert.Equal(0x0FFFFFFF, unsynched);
        Assert.True(unsynched <= Id3v2Tag.MaxAllowedSize);
    }

    ////----------------------------------------------------------------------
    // 4. Extended header v2.4: synchsafe size, flags-field-length byte,
    //    flag bytes, optional TagIsUpdate / CRC / Restrictions.
    ////----------------------------------------------------------------------

    [Fact]
    public void ExtendedHeader_Empty_SixBytesOfSynchsafeSizeLengthAndFlags()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240)
        {
            ExtendedHeader = new Id3v2ExtendedHeader()
        };
        var bytes = tag.ToByteArray();

        // After the 10-byte header, the extended header layout is:
        //   0..3  size (synchsafe) = 6
        //   4     flag field length = 1
        //   5     flag byte = 0x00 (nothing set)
        var ext = bytes.Skip(Id3v2Tag.HeaderSize).Take(6).ToArray();
        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x06, 0x01, 0x00 }, ext);
    }

    [Fact]
    public void ExtendedHeader_TagIsUpdate_EmitsFlagAndSingleZeroLengthByte()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240)
        {
            ExtendedHeader = new Id3v2ExtendedHeader { TagIsUpdate = true }
        };
        var bytes = tag.ToByteArray();

        // size = 6 (base) + 1 (update data) = 7
        var ext = bytes.Skip(Id3v2Tag.HeaderSize).Take(7).ToArray();
        Assert.Equal((byte)0x07, ext[3]);              // synchsafe size low byte
        Assert.Equal((byte)0x01, ext[4]);              // flag field length
        Assert.Equal(ExtFlagTagIsUpdate, ext[5]);      // flag byte
        Assert.Equal((byte)0x00, ext[6]);              // spec: flag data length $00
    }

    [Fact]
    public void ExtendedHeader_CrcPresent_EmitsFlagAnd5ByteSynchsafeCrc()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240)
        {
            ExtendedHeader = new Id3v2ExtendedHeader { CrcDataPresent = true }
        };
        var bytes = tag.ToByteArray();

        // size = 6 (base) + 5 (CRC) = 11
        var ext = bytes.Skip(Id3v2Tag.HeaderSize).Take(11).ToArray();
        Assert.Equal((byte)0x0B, ext[3]);
        Assert.Equal((byte)0x01, ext[4]);
        Assert.Equal(ExtFlagCrcPresent, ext[5]);

        // Each of the 5 CRC bytes must have bit 7 clear (spec: stored as a
        // 35-bit synchsafe integer with the upper 4 bits always zero).
        for (var i = 6; i < 11; i++)
        {
            Assert.Equal(0, ext[i] & 0x80);
        }
    }

    [Fact]
    public void ExtendedHeader_TagIsRestricted_EmitsFlagAndSingleRestrictionByte()
    {
        var restrictions = new Id3v2TagRestrictions
        {
            TagSizeRestriction = Id3v2TagSizeRestriction.Max64FramesAnd128KbTotalSize,
            TextEncodingRestriction = Id3v2TextEncodingRestriction.EncodingRestricted,
            TextFieldsSizeRestriction = Id3v2TextFieldsSizeRestriction.Max128Characters,
            ImageEncodingRestriction = Id3v2ImageEncodingRestriction.ImageRestricted,
            ImageSizeRestriction = Id3v2ImageSizeRestriction.Max64X64Pixels
        };
        var tag = new Id3v2Tag(Id3v2Version.Id3v240)
        {
            ExtendedHeader = new Id3v2ExtendedHeader { TagRestrictions = restrictions }
        };
        var bytes = tag.ToByteArray();

        // size = 6 (base) + 1 (restrictions) = 7
        var ext = bytes.Skip(Id3v2Tag.HeaderSize).Take(7).ToArray();
        Assert.Equal((byte)0x07, ext[3]);
        Assert.Equal((byte)0x01, ext[4]);
        Assert.Equal(ExtFlagTagIsRestricted, ext[5]);
        Assert.Equal(restrictions.ToByte()[0], ext[6]);
    }

    [Fact]
    public void ExtendedHeader_AllOptionalFields_EmitsInSpecifiedOrder()
    {
        var restrictions = new Id3v2TagRestrictions
        {
            TagSizeRestriction = Id3v2TagSizeRestriction.Max32FramesAnd4KbTotalSize
        };
        var tag = new Id3v2Tag(Id3v2Version.Id3v240)
        {
            ExtendedHeader = new Id3v2ExtendedHeader
            {
                TagIsUpdate = true,
                CrcDataPresent = true,
                TagRestrictions = restrictions
            }
        };
        var bytes = tag.ToByteArray();

        // size = 6 + 1 (update) + 5 (crc) + 1 (restrictions) = 13
        var ext = bytes.Skip(Id3v2Tag.HeaderSize).Take(13).ToArray();
        Assert.Equal((byte)0x0D, ext[3]);
        Assert.Equal((byte)0x01, ext[4]);
        Assert.Equal((byte)(ExtFlagTagIsUpdate | ExtFlagCrcPresent | ExtFlagTagIsRestricted), ext[5]);

        // Order: update zero-byte, CRC (5 bytes), then restrictions byte.
        Assert.Equal((byte)0x00, ext[6]);
        // Bytes 7-11 are CRC; bit 7 clear.
        for (var i = 7; i <= 11; i++)
        {
            Assert.Equal(0, ext[i] & 0x80);
        }
        Assert.Equal(restrictions.ToByte()[0], ext[12]);
    }

    ////----------------------------------------------------------------------
    // 5. Tag restrictions byte: %ppqrrstt
    ////----------------------------------------------------------------------

    [Fact]
    public void Restrictions_AllZero_ProducesZeroByte()
    {
        var r = new Id3v2TagRestrictions();
        Assert.Equal(new byte[] { 0x00 }, r.ToByte());
    }

    [Fact]
    public void Restrictions_TagSize_OccupiesBits7And6()
    {
        foreach (var sz in Enum.GetValues<Id3v2TagSizeRestriction>())
        {
            var r = new Id3v2TagRestrictions { TagSizeRestriction = sz };
            var b = r.ToByte()[0];
            Assert.Equal((int)sz, (b & 0xC0) >> 6);
            Assert.Equal(0, b & 0x3F); // other bits clear
        }
    }

    [Fact]
    public void Restrictions_TextEncoding_OccupiesBit5()
    {
        var r = new Id3v2TagRestrictions { TextEncodingRestriction = Id3v2TextEncodingRestriction.EncodingRestricted };
        Assert.Equal((byte)0x20, r.ToByte()[0]);
    }

    [Fact]
    public void Restrictions_TextFieldsSize_OccupiesBits4And3()
    {
        foreach (var v in Enum.GetValues<Id3v2TextFieldsSizeRestriction>())
        {
            var r = new Id3v2TagRestrictions { TextFieldsSizeRestriction = v };
            var b = r.ToByte()[0];
            Assert.Equal((int)v, (b & 0x18) >> 3);
            Assert.Equal(0, b & 0xE7); // all other bits clear
        }
    }

    [Fact]
    public void Restrictions_ImageEncoding_OccupiesBit2()
    {
        var r = new Id3v2TagRestrictions { ImageEncodingRestriction = Id3v2ImageEncodingRestriction.ImageRestricted };
        Assert.Equal((byte)0x04, r.ToByte()[0]);
    }

    [Fact]
    public void Restrictions_ImageSize_OccupiesBits1And0()
    {
        foreach (var v in Enum.GetValues<Id3v2ImageSizeRestriction>())
        {
            var r = new Id3v2TagRestrictions { ImageSizeRestriction = v };
            var b = r.ToByte()[0];
            Assert.Equal((int)v, b & 0x03);
            Assert.Equal(0, b & 0xFC); // all other bits clear
        }
    }

    [Fact]
    public void Restrictions_AllFieldsSet_ProducesExpectedBytePattern()
    {
        // Pattern: pp=11, q=1, rr=10, s=1, tt=01 -> 11 1 10 1 01 = 0xF5
        var r = new Id3v2TagRestrictions
        {
            TagSizeRestriction = Id3v2TagSizeRestriction.Max32FramesAnd4KbTotalSize,           // 11
            TextEncodingRestriction = Id3v2TextEncodingRestriction.EncodingRestricted,         //  1
            TextFieldsSizeRestriction = Id3v2TextFieldsSizeRestriction.Max128Characters,       // 10
            ImageEncodingRestriction = Id3v2ImageEncodingRestriction.ImageRestricted,          //  1
            ImageSizeRestriction = Id3v2ImageSizeRestriction.Max256X256Pixels                  // 01
        };
        Assert.Equal((byte)0xF5, r.ToByte()[0]);
    }

    [Fact]
    public void Restrictions_RoundTripThroughReader_PreservesAllFields()
    {
        var restrictions = new Id3v2TagRestrictions
        {
            TagSizeRestriction = Id3v2TagSizeRestriction.Max64FramesAnd128KbTotalSize,
            TextEncodingRestriction = Id3v2TextEncodingRestriction.EncodingRestricted,
            TextFieldsSizeRestriction = Id3v2TextFieldsSizeRestriction.Max30Characters,
            ImageEncodingRestriction = Id3v2ImageEncodingRestriction.ImageRestricted,
            ImageSizeRestriction = Id3v2ImageSizeRestriction.Max64X64Pixels
        };
        var original = new Id3v2Tag(Id3v2Version.Id3v240)
        {
            ExtendedHeader = new Id3v2ExtendedHeader { TagRestrictions = restrictions }
        };

        var parsed = RoundTrip(original);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed!.ExtendedHeader);
        Assert.True(parsed.ExtendedHeader.TagIsRestricted);
        var parsedR = parsed.ExtendedHeader.TagRestrictions;
        Assert.NotNull(parsedR);
        Assert.Equal(restrictions.TagSizeRestriction, parsedR.TagSizeRestriction);
        Assert.Equal(restrictions.TextEncodingRestriction, parsedR.TextEncodingRestriction);
        Assert.Equal(restrictions.TextFieldsSizeRestriction, parsedR.TextFieldsSizeRestriction);
        Assert.Equal(restrictions.ImageEncodingRestriction, parsedR.ImageEncodingRestriction);
        Assert.Equal(restrictions.ImageSizeRestriction, parsedR.ImageSizeRestriction);
    }

    ////----------------------------------------------------------------------
    // 6. Footer ("3DI" + $04 00 + flags + synchsafe size)
    ////----------------------------------------------------------------------

    [Fact]
    public void Footer_WhenUseFooterTrue_WrittenAtEndWithReversedIdentifier()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240) { UseFooter = true };
        var bytes = tag.ToByteArray();

        // 10-byte header + 10-byte footer.
        Assert.Equal(Id3v2Tag.HeaderSize + Id3v2Tag.FooterSize, bytes.Length);

        // Header identifier "ID3"
        Assert.Equal(Encoding.ASCII.GetBytes("ID3"), bytes.Take(3).ToArray());

        // Footer identifier "3DI" -- reversed "ID3"
        Assert.Equal(Encoding.ASCII.GetBytes("3DI"), bytes.Skip(Id3v2Tag.HeaderSize).Take(3).ToArray());

        // Footer version / flags / size must match header.
        Assert.Equal(bytes[3], bytes[Id3v2Tag.HeaderSize + 3]); // major
        Assert.Equal(bytes[4], bytes[Id3v2Tag.HeaderSize + 4]); // revision
        Assert.Equal(bytes[5], bytes[Id3v2Tag.HeaderSize + 5]); // flags (incl footer bit)
        Assert.Equal(bytes[6], bytes[Id3v2Tag.HeaderSize + 6]); // size bytes
        Assert.Equal(bytes[7], bytes[Id3v2Tag.HeaderSize + 7]);
        Assert.Equal(bytes[8], bytes[Id3v2Tag.HeaderSize + 8]);
        Assert.Equal(bytes[9], bytes[Id3v2Tag.HeaderSize + 9]);
    }

    [Fact]
    public void Footer_WithFooter_NoPaddingWritten()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240)
        {
            UseFooter = true,
            PaddingSize = 100
        };
        var bytes = tag.ToByteArray();

        // Spec: "MUST NOT have any padding when a tag footer is added".
        Assert.Equal(Id3v2Tag.HeaderSize + Id3v2Tag.FooterSize, bytes.Length);
    }

    [Fact]
    public void Footer_RoundTripThroughReader_Succeeds()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240) { UseFooter = true };
        var parsed = RoundTrip(tag);
        Assert.NotNull(parsed);
        Assert.True(parsed!.UseFooter);
    }

    ////----------------------------------------------------------------------
    // 7. Padding
    ////----------------------------------------------------------------------

    [Fact]
    public void Padding_WithoutFooter_BytesAreAllZero()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240) { PaddingSize = 32 };
        var bytes = tag.ToByteArray();

        Assert.Equal(Id3v2Tag.HeaderSize + 32, bytes.Length);
        Assert.All(bytes.Skip(Id3v2Tag.HeaderSize), b => Assert.Equal(0, b));
    }

    [Fact]
    public void Padding_WithFooter_IsSuppressedEvenWhenPaddingSizeIsSet()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240)
        {
            UseFooter = true,
            PaddingSize = 64
        };
        var bytes = tag.ToByteArray();

        // AppendTagPadding must honour !UseFooter.
        Assert.Equal(Id3v2Tag.HeaderSize + Id3v2Tag.FooterSize, bytes.Length);
    }

    [Fact]
    public void Padding_RoundTripThroughReader_PreservesSize()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240) { PaddingSize = 16 };
        var parsed = RoundTrip(tag);
        Assert.NotNull(parsed);
        Assert.Equal(16, parsed!.PaddingSize);
    }

    ////----------------------------------------------------------------------
    // 8. Unsynchronisation: v2.4 moved unsync to per-frame; the tag-level
    //    path must NOT rewrite the entire post-header payload.
    ////----------------------------------------------------------------------

    [Fact]
    public void Unsynchronization_V240_DoesNotRewriteTagLevelPayload()
    {
        // We put a known byte-pattern into the payload (as padding) that the
        // pre-v2.4 unsync path would have rewritten:
        //   0xFF 0x00 -> 0xFF 0x00 0x00  (an inserted 0x00 after 0xFF)
        // v2.4 must NOT apply this rewrite at the tag level. The only way
        // padding gets into the output is via AppendTagPadding, which writes
        // literal 0x00 bytes -- so the simpler observable invariant is that
        // enabling tag-level unsync on a v2.4 tag does not grow the byte
        // output.
        var a = new Id3v2Tag(Id3v2Version.Id3v240) { PaddingSize = 8 };
        var b = new Id3v2Tag(Id3v2Version.Id3v240) { PaddingSize = 8, UseUnsynchronization = true };

        var aBytes = a.ToByteArray();
        var bBytes = b.ToByteArray();

        // Same total length regardless of the tag-level unsync flag: v2.4
        // unsync is per-frame and must not expand the post-header payload.
        Assert.Equal(aBytes.Length, bBytes.Length);

        // Only the flag byte differs between the two outputs.
        Assert.Equal(FlagUnsynchronization, (byte)(bBytes[5] & FlagUnsynchronization));
        Assert.Equal(0, aBytes[5] & FlagUnsynchronization);
    }

    ////----------------------------------------------------------------------
    // 9. CRC calculation (v2.4: hash includes frame region + padding)
    ////----------------------------------------------------------------------

    [Fact]
    public void CalculateCrc32_EmptyV240Tag_MatchesHashOfEmptyBuffer()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var expected = (int)Crc32.HashToUInt32([]);
        Assert.Equal(expected, tag.CalculateCrc32());
    }

    [Fact]
    public void CalculateCrc32_EmptyFramesWithPadding_HashesZeroBytesOfPadding()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240) { PaddingSize = 10 };
        var expected = (int)Crc32.HashToUInt32(new byte[10]);
        Assert.Equal(expected, tag.CalculateCrc32());
    }

    [Fact]
    public void CalculateCrc32_V220Tag_ReturnsZero()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        Assert.Equal(0, tag.CalculateCrc32());
    }

    [Fact]
    public void CalculateCrc32_WithSingleTextFrame_MatchesHashOfFrameBytes()
    {
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v240, "TIT2");
        frame.Values.Add("Song");
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        tag.SetFrame(frame);

        // Compute the expected hash the same way CalculateCrc32 does: the
        // concatenation of all GetFrames() outputs, followed by PaddingSize
        // zero bytes.
        var expected = (int)Crc32.HashToUInt32(frame.ToByteArray());
        Assert.Equal(expected, tag.CalculateCrc32());
    }

    ////----------------------------------------------------------------------
    // 10. Id3v2Tag.Equals regression: default ExtendedHeader (null!) must
    //     not NullReferenceException.
    ////----------------------------------------------------------------------

    [Fact]
    public void Equals_TwoTagsWithDefaultExtendedHeader_DoesNotThrow()
    {
        var a = new Id3v2Tag(Id3v2Version.Id3v240);
        var b = new Id3v2Tag(Id3v2Version.Id3v240);

        // Both ExtendedHeader backing fields default to null!; comparing
        // them must not throw.
        var equals = a.Equals(b);
        Assert.True(equals);
    }

    [Fact]
    public void Equals_SelfReference_IsTrue()
    {
        var a = new Id3v2Tag(Id3v2Version.Id3v240);
        Assert.True(a.Equals(a));
    }

    [Fact]
    public void Equals_DifferentVersions_IsFalse()
    {
        var a = new Id3v2Tag(Id3v2Version.Id3v240);
        var b = new Id3v2Tag(Id3v2Version.Id3v230);
        Assert.False(a.Equals(b));
    }

    ////----------------------------------------------------------------------
    // 11. Full round trip: header + extended header + frames + padding.
    ////----------------------------------------------------------------------

    [Fact]
    public void RoundTrip_WithFramesPaddingAndExtendedHeader_ProducesEqualTag()
    {
        var titleFrame = new Id3v2TextFrame(Id3v2Version.Id3v240, "TIT2");
        titleFrame.Values.Add("My Song Title");

        var artistFrame = new Id3v2TextFrame(Id3v2Version.Id3v240, "TPE1");
        artistFrame.Values.Add("My Artist");

        var original = new Id3v2Tag(Id3v2Version.Id3v240)
        {
            PaddingSize = 12,
            ExtendedHeader = new Id3v2ExtendedHeader { TagIsUpdate = true }
        };
        original.SetFrame(titleFrame);
        original.SetFrame(artistFrame);

        var parsed = RoundTrip(original);
        Assert.NotNull(parsed);
        Assert.Equal(original.Version, parsed!.Version);
        Assert.Equal(original.PaddingSize, parsed.PaddingSize);
        Assert.True(parsed.UseExtendedHeader);
        Assert.NotNull(parsed.ExtendedHeader);
        Assert.True(parsed.ExtendedHeader.TagIsUpdate);

        // Full Equals check (covers version, flags, extended header, frames).
        Assert.True(original.Equals(parsed));
    }

    ////----------------------------------------------------------------------
    // Helpers
    ////----------------------------------------------------------------------

    private static Id3v2Tag? RoundTrip(Id3v2Tag tag)
    {
        var bytes = tag.ToByteArray();
        using var ms = new MemoryStream(bytes);
        var reader = new Id3v2TagReader();
        var offset = reader.ReadFromStream(ms, TagOrigin.Start);
        return offset?.AudioTag as Id3v2Tag;
    }
}
