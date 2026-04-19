/*
 * Test suite for the ASF Header Object walker (AsfStream).
 */
namespace AudioVideoLib.Tests;

using System;
using System.IO;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

using Xunit;

public class AsfStreamTests
{
    // ================================================================
    // Helpers — synthesise a minimal ASF file in-memory.
    // ================================================================

    private static byte[] BuildHeaderObject(byte[][] children, ulong? overrideHeaderSize = null, uint? overrideChildCount = null)
    {
        var childrenLen = 0;
        foreach (var c in children)
        {
            childrenLen += c.Length;
        }

        var totalLen = 30 + childrenLen;
        var buf = new byte[totalLen];

        Buffer.BlockCopy(AsfMetadataTag.HeaderObjectGuid.ToByteArray(), 0, buf, 0, 16);

        var size = overrideHeaderSize ?? (ulong)totalLen;
        for (var i = 0; i < 8; i++)
        {
            buf[16 + i] = (byte)((size >> (8 * i)) & 0xFF);
        }

        var count = overrideChildCount ?? (uint)children.Length;
        buf[24] = (byte)(count & 0xFF);
        buf[25] = (byte)((count >> 8) & 0xFF);
        buf[26] = (byte)((count >> 16) & 0xFF);
        buf[27] = (byte)((count >> 24) & 0xFF);
        buf[28] = 0x01;
        buf[29] = 0x02;

        var pos = 30;
        foreach (var c in children)
        {
            Buffer.BlockCopy(c, 0, buf, pos, c.Length);
            pos += c.Length;
        }

        return buf;
    }

    private static byte[] BuildFilePropertiesObject(ulong playDuration100Ns)
    {
        // 24-byte header + 80-byte payload (matches spec layout up through Max Bitrate).
        var payload = new byte[80];
        // Play Duration sits at payload offset 40 (16 + 8 + 8 + 8).
        for (var i = 0; i < 8; i++)
        {
            payload[40 + i] = (byte)((playDuration100Ns >> (8 * i)) & 0xFF);
        }

        return AsfMetadataTag.WrapObject(AsfMetadataTag.FilePropertiesObjectGuid, payload);
    }

    private static byte[] BuildHeaderExtensionObject(byte[][] nestedChildren)
    {
        var nestedLen = 0;
        foreach (var c in nestedChildren)
        {
            nestedLen += c.Length;
        }

        // 16 bytes reserved GUID + 2 bytes reserved word + 4 bytes data size + nested data
        var payload = new byte[16 + 2 + 4 + nestedLen];
        // Reserved GUID is fixed: ABD3D211-A9BA-11cf-8EE6-00C00C205365 — but its actual bytes don't
        // matter to our walker since we ignore them. Leave zeros.
        payload[16] = 0x06;
        payload[17] = 0x00;
        // Data size DWORD LE
        var ds = (uint)nestedLen;
        payload[18] = (byte)(ds & 0xFF);
        payload[19] = (byte)((ds >> 8) & 0xFF);
        payload[20] = (byte)((ds >> 16) & 0xFF);
        payload[21] = (byte)((ds >> 24) & 0xFF);

        var pos = 22;
        foreach (var c in nestedChildren)
        {
            Buffer.BlockCopy(c, 0, payload, pos, c.Length);
            pos += c.Length;
        }

        return AsfMetadataTag.WrapObject(AsfMetadataTag.HeaderExtensionObjectGuid, payload);
    }

    // ================================================================
    // 1. Basic walking
    // ================================================================

    [Fact]
    public void AsfStream_RejectsFileWithoutHeaderObjectGuid()
    {
        var bytes = new byte[64];
        var s = new AsfStream();
        Assert.False(s.ReadStream(new MemoryStream(bytes)));
    }

    [Fact]
    public void AsfStream_RejectsTooShortFile()
    {
        var s = new AsfStream();
        Assert.False(s.ReadStream(new MemoryStream(new byte[10])));
    }

    [Fact]
    public void AsfStream_NullStream_Throws()
    {
        var s = new AsfStream();
        Assert.Throws<ArgumentNullException>(() => s.ReadStream(null!));
    }

    [Fact]
    public void AsfStream_EmptyHeaderObject_ParsesWithNoChildren()
    {
        var bytes = BuildHeaderObject([]);
        var s = new AsfStream();
        Assert.True(s.ReadStream(new MemoryStream(bytes)));
        Assert.Single(s.Objects); // Only the Header Object itself
        Assert.Equal(AsfMetadataTag.HeaderObjectGuid, s.Objects[0].Id);
        Assert.Equal((long)bytes.Length, s.HeaderObjectSize);
    }

    // ================================================================
    // 2. CDO only
    // ================================================================

    [Fact]
    public void AsfStream_CdoOnly_ParsesContentDescriptionFields()
    {
        var srcTag = new AsfMetadataTag
        {
            Title = "T",
            Author = "A",
            Copyright = "C",
            Description = "D",
            Rating = "R",
        };
        var cdo = AsfMetadataTag.WrapObject(
            AsfMetadataTag.ContentDescriptionObjectGuid,
            srcTag.BuildContentDescriptionPayload());
        var bytes = BuildHeaderObject([cdo]);

        var s = new AsfStream();
        Assert.True(s.ReadStream(new MemoryStream(bytes)));
        Assert.Equal("T", s.MetadataTag.Title);
        Assert.Equal("A", s.MetadataTag.Author);
        Assert.Equal("C", s.MetadataTag.Copyright);
        Assert.Equal("D", s.MetadataTag.Description);
        Assert.Equal("R", s.MetadataTag.Rating);
        Assert.Empty(s.MetadataTag.ExtendedItems);
    }

    // ================================================================
    // 3. ECDO only
    // ================================================================

    [Fact]
    public void AsfStream_EcdoOnly_ParsesAllValueTypes()
    {
        var srcTag = new AsfMetadataTag();
        srcTag.AddExtended("WM/AlbumTitle", AsfTypedValue.FromString("Album"));
        srcTag.AddExtended("WM/Cover", AsfTypedValue.FromBytes([1, 2, 3]));
        srcTag.AddExtended("IsCompilation", AsfTypedValue.FromBool(true));
        srcTag.AddExtended("WM/Track", AsfTypedValue.FromDword(7));
        srcTag.AddExtended("WM/EncodingTime", AsfTypedValue.FromQword(0x1122334455667788UL));
        srcTag.AddExtended("WM/Year16", AsfTypedValue.FromWord(2026));

        var ecdo = AsfMetadataTag.WrapObject(
            AsfMetadataTag.ExtendedContentDescriptionObjectGuid,
            srcTag.BuildExtendedContentDescriptionPayload());
        var bytes = BuildHeaderObject([ecdo]);

        var s = new AsfStream();
        Assert.True(s.ReadStream(new MemoryStream(bytes)));
        Assert.Equal(6, s.MetadataTag.ExtendedItems.Count);
        Assert.Equal("Album", s.MetadataTag.Extended["WM/AlbumTitle"].AsString);
        Assert.Equal(new byte[] { 1, 2, 3 }, s.MetadataTag.Extended["WM/Cover"].AsBytes);
        Assert.True(s.MetadataTag.Extended["IsCompilation"].AsBool);
        Assert.Equal(7u, s.MetadataTag.Extended["WM/Track"].AsDword);
        Assert.Equal(0x1122334455667788UL, s.MetadataTag.Extended["WM/EncodingTime"].AsQword);
        Assert.Equal((ushort)2026, s.MetadataTag.Extended["WM/Year16"].AsWord);
    }

    // ================================================================
    // 4. CDO + ECDO together
    // ================================================================

    [Fact]
    public void AsfStream_CdoAndEcdo_BothParsed()
    {
        var srcTag = new AsfMetadataTag { Title = "Hello" };
        srcTag.AddExtended("WM/Year", AsfTypedValue.FromString("2026"));

        var cdo = AsfMetadataTag.WrapObject(
            AsfMetadataTag.ContentDescriptionObjectGuid,
            srcTag.BuildContentDescriptionPayload());
        var ecdo = AsfMetadataTag.WrapObject(
            AsfMetadataTag.ExtendedContentDescriptionObjectGuid,
            srcTag.BuildExtendedContentDescriptionPayload());
        var bytes = BuildHeaderObject([cdo, ecdo]);

        var s = new AsfStream();
        Assert.True(s.ReadStream(new MemoryStream(bytes)));
        Assert.Equal("Hello", s.MetadataTag.Title);
        Assert.Equal("2026", s.MetadataTag.Extended["WM/Year"].AsString);
    }

    // ================================================================
    // 5. MO / MLO inside Header Extension Object
    // ================================================================

    [Fact]
    public void AsfStream_MetadataObjectInsideHeaderExtension_IsParsed()
    {
        var src = new AsfMetadataTag();
        src.AddMetadata(new AsfMetadataItem(0, 5, "WM/Note", AsfTypedValue.FromString("hi")));
        var moPayload = AsfMetadataTag.BuildMetadataPayload(src.MetadataItems);
        var moObj = AsfMetadataTag.WrapObject(AsfMetadataTag.MetadataObjectGuid, moPayload);
        var hext = BuildHeaderExtensionObject([moObj]);
        var bytes = BuildHeaderObject([hext]);

        var s = new AsfStream();
        Assert.True(s.ReadStream(new MemoryStream(bytes)));
        Assert.Single(s.MetadataTag.MetadataItems);
        Assert.Equal((ushort)5, s.MetadataTag.MetadataItems[0].StreamNumber);
        Assert.Equal("WM/Note", s.MetadataTag.MetadataItems[0].Name);
        Assert.Equal("hi", s.MetadataTag.MetadataItems[0].Value.AsString);
    }

    [Fact]
    public void AsfStream_MetadataLibraryObjectInsideHeaderExtension_IsParsed()
    {
        var src = new AsfMetadataTag();
        src.AddMetadataLibrary(new AsfMetadataItem(3, 9, "WM/Lyrics", AsfTypedValue.FromString("la")));
        var mlo = AsfMetadataTag.WrapObject(
            AsfMetadataTag.MetadataLibraryObjectGuid,
            AsfMetadataTag.BuildMetadataPayload(src.MetadataLibraryItems));
        var hext = BuildHeaderExtensionObject([mlo]);
        var bytes = BuildHeaderObject([hext]);

        var s = new AsfStream();
        Assert.True(s.ReadStream(new MemoryStream(bytes)));
        Assert.Single(s.MetadataTag.MetadataLibraryItems);
        var item = s.MetadataTag.MetadataLibraryItems[0];
        Assert.Equal((ushort)3, item.LanguageListIndex);
        Assert.Equal((ushort)9, item.StreamNumber);
        Assert.Equal("la", item.Value.AsString);
    }

    // ================================================================
    // 6. File Properties Object → duration
    // ================================================================

    [Fact]
    public void AsfStream_FilePropertiesObject_PopulatesPlayDurationAndAudioLength()
    {
        // 1 second = 10,000,000 100-ns units.
        const ulong OneSecond100Ns = 10_000_000UL;
        var fpo = BuildFilePropertiesObject(OneSecond100Ns);
        var bytes = BuildHeaderObject([fpo]);

        var s = new AsfStream();
        Assert.True(s.ReadStream(new MemoryStream(bytes)));
        Assert.Equal(OneSecond100Ns, s.PlayDuration100Ns);
        Assert.Equal(1000L, s.TotalAudioLength);
    }

    [Fact]
    public void AsfStream_NoFileProperties_TotalAudioLengthIsZero()
    {
        var bytes = BuildHeaderObject([]);
        var s = new AsfStream();
        Assert.True(s.ReadStream(new MemoryStream(bytes)));
        Assert.Equal(0L, s.TotalAudioLength);
    }

    // ================================================================
    // 7. Unknown objects are recorded but skipped
    // ================================================================

    [Fact]
    public void AsfStream_UnknownChildObject_IsRecordedNotParsed()
    {
        var unknownGuid = new Guid("11111111-2222-3333-4444-555555555555");
        var unknown = AsfMetadataTag.WrapObject(unknownGuid, [0x01, 0x02, 0x03]);
        var bytes = BuildHeaderObject([unknown]);

        var s = new AsfStream();
        Assert.True(s.ReadStream(new MemoryStream(bytes)));
        Assert.Equal(2, s.Objects.Count); // Header + unknown
        Assert.Equal(unknownGuid, s.Objects[1].Id);
        Assert.False(s.MetadataTag.HasContentDescription);
    }

    [Fact]
    public void AsfStream_RejectsObjectWithSizeBelowMinimum()
    {
        // Build a "child" whose size field claims < 24.
        var bogus = new byte[24];
        Buffer.BlockCopy(AsfMetadataTag.ContentDescriptionObjectGuid.ToByteArray(), 0, bogus, 0, 16);
        bogus[16] = 0x10; // size = 16 → invalid

        var bytes = BuildHeaderObject([bogus]);
        var s = new AsfStream();
        // Walk silently terminates — Header Object itself is still recorded as valid.
        Assert.True(s.ReadStream(new MemoryStream(bytes)));
        Assert.Single(s.Objects); // No child was added
    }

    // ================================================================
    // 8. ToByteArray — splice round-trip
    // ================================================================

    [Fact]
    public void AsfStream_ToByteArray_RoundTrip_PreservesUnchangedTag()
    {
        // Original file: CDO with Title=A, ECDO with one item, plus a trailing "Data Object"
        // simulated as an unknown GUID after the Header Object.
        var srcTag = new AsfMetadataTag { Title = "Original" };
        srcTag.AddExtended("k", AsfTypedValue.FromString("v"));

        var cdo = AsfMetadataTag.WrapObject(
            AsfMetadataTag.ContentDescriptionObjectGuid,
            srcTag.BuildContentDescriptionPayload());
        var ecdo = AsfMetadataTag.WrapObject(
            AsfMetadataTag.ExtendedContentDescriptionObjectGuid,
            srcTag.BuildExtendedContentDescriptionPayload());
        var header = BuildHeaderObject([cdo, ecdo]);

        // Trailing "Data Object" — bytes the walker should preserve verbatim past the header.
        var dataGuid = new Guid("75B22636-668E-11CF-A6D9-00AA0062CE6C");
        var dataObj = AsfMetadataTag.WrapObject(dataGuid, new byte[16]);

        var file = new byte[header.Length + dataObj.Length];
        Buffer.BlockCopy(header, 0, file, 0, header.Length);
        Buffer.BlockCopy(dataObj, 0, file, header.Length, dataObj.Length);

        var s = new AsfStream();
        Assert.True(s.ReadStream(new MemoryStream(file)));
        Assert.Equal("Original", s.MetadataTag.Title);
        Assert.Equal("v", s.MetadataTag.Extended["k"].AsString);

        var rewritten = s.ToByteArray();
        Assert.Equal(file.Length, rewritten.Length);
        Assert.Equal(file, rewritten);
    }

    [Fact]
    public void AsfStream_ToByteArray_ReplacesExistingMetadata_AndPreservesOtherObjects()
    {
        // Original: CDO(Title=Old) + FilePropertiesObject (preserved verbatim).
        var oldTag = new AsfMetadataTag { Title = "Old" };
        var cdo = AsfMetadataTag.WrapObject(
            AsfMetadataTag.ContentDescriptionObjectGuid,
            oldTag.BuildContentDescriptionPayload());
        var fpo = BuildFilePropertiesObject(20_000_000UL); // 2 sec
        var header = BuildHeaderObject([cdo, fpo]);

        var s = new AsfStream();
        Assert.True(s.ReadStream(new MemoryStream(header)));
        Assert.Equal("Old", s.MetadataTag.Title);
        Assert.Equal(2000L, s.TotalAudioLength);

        // Mutate the tag — change title and add an extended item.
        s.MetadataTag.Title = "New";
        s.MetadataTag.AddExtended("WM/Year", AsfTypedValue.FromString("2026"));

        var rewritten = s.ToByteArray();
        Assert.NotEmpty(rewritten);

        // Re-parse the rewritten file.
        var s2 = new AsfStream();
        Assert.True(s2.ReadStream(new MemoryStream(rewritten)));
        Assert.Equal("New", s2.MetadataTag.Title);
        Assert.Equal("2026", s2.MetadataTag.Extended["WM/Year"].AsString);
        // File Properties Object preserved.
        Assert.Equal(2000L, s2.TotalAudioLength);
    }

    [Fact]
    public void AsfStream_ToByteArray_FullRoundTrip_ByteIdentical_ForSynthesisedFile()
    {
        var src = new AsfMetadataTag
        {
            Title = "RT",
            Author = "Auth",
        };
        src.AddExtended("k1", AsfTypedValue.FromString("v1"));
        src.AddExtended("k2", AsfTypedValue.FromDword(123));
        var arrays = src.ToByteArrays();
        var header = BuildHeaderObject(arrays);

        var s1 = new AsfStream();
        Assert.True(s1.ReadStream(new MemoryStream(header)));
        var rewritten = s1.ToByteArray();

        // Re-parse rewritten file and ensure semantic equality + further byte stability.
        var s2 = new AsfStream();
        Assert.True(s2.ReadStream(new MemoryStream(rewritten)));
        Assert.Equal("RT", s2.MetadataTag.Title);
        Assert.Equal("Auth", s2.MetadataTag.Author);
        Assert.Equal("v1", s2.MetadataTag.Extended["k1"].AsString);
        Assert.Equal(123u, s2.MetadataTag.Extended["k2"].AsDword);

        var rewritten2 = s2.ToByteArray();
        Assert.Equal(rewritten, rewritten2);
    }

    // ================================================================
    // 9. IAudioStream surface
    // ================================================================

    [Fact]
    public void AsfStream_StartAndEndOffsets_MatchStreamPosition()
    {
        var bytes = BuildHeaderObject([]);
        var ms = new MemoryStream();
        // Prepend 5 garbage bytes and seek the stream forward.
        byte[] prefix = [0, 1, 2, 3, 4];
        ms.Write(prefix, 0, prefix.Length);
        ms.Write(bytes, 0, bytes.Length);
        ms.Position = 5;

        var s = new AsfStream();
        Assert.True(s.ReadStream(ms));
        Assert.Equal(5, s.StartOffset);
        Assert.Equal(5 + bytes.Length, s.EndOffset);
    }

    [Fact]
    public void AsfStream_TotalAudioSize_EqualsStreamLengthCovered()
    {
        var bytes = BuildHeaderObject([]);
        var s = new AsfStream();
        Assert.True(s.ReadStream(new MemoryStream(bytes)));
        Assert.Equal(bytes.Length, s.TotalAudioSize);
    }
}
