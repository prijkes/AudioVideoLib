/*
 * Test suite for MatroskaStream — EBML container walking and Tags round-trip.
 */
namespace AudioVideoLib.Tests;

using System;
using System.IO;

using AudioVideoLib.Formats;
using AudioVideoLib.IO;
using AudioVideoLib.Tags;

using Xunit;

public class MatroskaStreamTests
{
    // ================================================================
    // Helpers — synthesise minimal EBML containers.
    // ================================================================

    private static byte[] BuildElement(long id, byte[] payload)
    {
        var idBytes = EbmlElement.EncodeId(id);
        var sizeBytes = EbmlElement.EncodeVintSize(payload.Length);
        var buf = new byte[idBytes.Length + sizeBytes.Length + payload.Length];
        System.Buffer.BlockCopy(idBytes, 0, buf, 0, idBytes.Length);
        System.Buffer.BlockCopy(sizeBytes, 0, buf, idBytes.Length, sizeBytes.Length);
        System.Buffer.BlockCopy(payload, 0, buf, idBytes.Length + sizeBytes.Length, payload.Length);
        return buf;
    }

    private static byte[] BuildContainerWithTags(string docType, byte[] tagsElementBytes)
    {
        // Segment payload: [optional Info] + Tags element + [optional other].
        // Keep it simple: just the Tags element.
        return MatroskaStream.BuildMinimalContainer(docType, tagsElementBytes);
    }

    private static byte[] BuildContainerNoTags(string docType)
    {
        return MatroskaStream.BuildMinimalContainer(docType, []);
    }

    private static MatroskaTag MakeAlbumTag(string title)
    {
        var entry = new MatroskaTagEntry();
        entry.Targets.TargetTypeValue = 50;
        entry.SimpleTags.Add(new MatroskaSimpleTag { Name = "TITLE", Value = title });
        var t = new MatroskaTag();
        t.Entries.Add(entry);
        return t;
    }

    // ================================================================
    // 1. Magic / DocType.
    // ================================================================

    [Fact]
    public void ReadStream_NonEbmlInput_ReturnsFalse()
    {
        var stream = new MatroskaStream();
        Assert.False(stream.ReadStream(new MemoryStream([0x00, 0x01, 0x02, 0x03, 0x04, 0x05])));
    }

    [Fact]
    public void ReadStream_EmptyStream_ReturnsFalse()
    {
        var stream = new MatroskaStream();
        Assert.False(stream.ReadStream(new MemoryStream([])));
    }

    [Fact]
    public void ReadStream_DocTypeMatroska_IsCaptured()
    {
        var bytes = BuildContainerNoTags("matroska");
        var stream = new MatroskaStream();
        Assert.True(stream.ReadStream(new MemoryStream(bytes)));
        Assert.Equal("matroska", stream.DocType);
    }

    [Fact]
    public void ReadStream_DocTypeWebm_IsCaptured()
    {
        var bytes = BuildContainerNoTags("webm");
        var stream = new MatroskaStream();
        Assert.True(stream.ReadStream(new MemoryStream(bytes)));
        Assert.Equal("webm", stream.DocType);
    }

    // ================================================================
    // 2. Missing Tags element → empty MatroskaTag (not null).
    // ================================================================

    [Fact]
    public void ReadStream_NoTagsElement_TagIsEmpty()
    {
        var bytes = BuildContainerNoTags("matroska");
        var stream = new MatroskaStream();
        Assert.True(stream.ReadStream(new MemoryStream(bytes)));
        Assert.NotNull(stream.Tag);
        Assert.Empty(stream.Tag.Entries);
    }

    // ================================================================
    // 3. Tags element parsing.
    // ================================================================

    [Fact]
    public void ReadStream_TagsElementWithSingleSimpleTag_IsParsed()
    {
        var sourceTag = MakeAlbumTag("Hello");
        var container = BuildContainerWithTags("matroska", sourceTag.ToByteArray());

        var stream = new MatroskaStream();
        Assert.True(stream.ReadStream(new MemoryStream(container)));
        Assert.Single(stream.Tag.Entries);
        Assert.Equal("Hello", stream.Tag.Title);
    }

    [Fact]
    public void ReadStream_TagsAtTrackLevel_ExposesTrackNumber()
    {
        var entry = new MatroskaTagEntry();
        entry.Targets.TargetTypeValue = 30;
        entry.SimpleTags.Add(new MatroskaSimpleTag { Name = "TITLE", Value = "Track Title" });
        entry.SimpleTags.Add(new MatroskaSimpleTag { Name = "PART_NUMBER", Value = "5" });
        var t = new MatroskaTag();
        t.Entries.Add(entry);

        var container = BuildContainerWithTags("matroska", t.ToByteArray());
        var stream = new MatroskaStream();
        Assert.True(stream.ReadStream(new MemoryStream(container)));
        Assert.Equal("5", stream.Tag.TrackNumber);
        Assert.Equal("Track Title", stream.Tag.Title);
    }

    // ================================================================
    // 4. Cluster / unknown siblings are skipped without throwing.
    // ================================================================

    [Fact]
    public void ReadStream_UnknownSiblingsBetweenSegmentChildren_AreSkipped()
    {
        // Segment payload = [SeekHead-ish unknown] + [Cluster-ish] + [Tags].
        var fakeSeekHead = BuildElement(MatroskaStream.SeekHeadId, [0x01, 0x02, 0x03]);
        var fakeCluster = BuildElement(MatroskaStream.ClusterId, new byte[1024]); // big-ish, must be skipped
        var sourceTag = MakeAlbumTag("Found");
        var tagsBytes = sourceTag.ToByteArray();

        var segmentPayload = new byte[fakeSeekHead.Length + fakeCluster.Length + tagsBytes.Length];
        var pos = 0;
        System.Buffer.BlockCopy(fakeSeekHead, 0, segmentPayload, pos, fakeSeekHead.Length);
        pos += fakeSeekHead.Length;
        System.Buffer.BlockCopy(fakeCluster, 0, segmentPayload, pos, fakeCluster.Length);
        pos += fakeCluster.Length;
        System.Buffer.BlockCopy(tagsBytes, 0, segmentPayload, pos, tagsBytes.Length);

        var container = MatroskaStream.BuildMinimalContainer("matroska", segmentPayload);
        var stream = new MatroskaStream();
        Assert.True(stream.ReadStream(new MemoryStream(container)));
        Assert.Equal("Found", stream.Tag.Title);
    }

    // ================================================================
    // 5. Round-trip — original bytes preserved, Tags reserialised.
    // ================================================================

    [Fact]
    public void ToByteArray_NoChanges_ReproducesOriginalBytes()
    {
        var sourceTag = MakeAlbumTag("Original");
        var container = BuildContainerWithTags("matroska", sourceTag.ToByteArray());

        var stream = new MatroskaStream();
        Assert.True(stream.ReadStream(new MemoryStream(container)));

        var reserialised = stream.ToByteArray();
        Assert.Equal(container, reserialised);
    }

    [Fact]
    public void ToByteArray_NoTagsElement_NoChanges_ReproducesOriginal()
    {
        var container = BuildContainerNoTags("matroska");
        var stream = new MatroskaStream();
        Assert.True(stream.ReadStream(new MemoryStream(container)));
        Assert.Equal(container, stream.ToByteArray());
    }

    [Fact]
    public void ToByteArray_AfterMutatingTitle_ProducesValidContainer_ReadableAgain()
    {
        var sourceTag = MakeAlbumTag("Old Title");
        var container = BuildContainerWithTags("matroska", sourceTag.ToByteArray());

        var stream = new MatroskaStream();
        Assert.True(stream.ReadStream(new MemoryStream(container)));
        Assert.Equal("Old Title", stream.Tag.Title);

        // Mutate.
        stream.Tag.Entries[0].SimpleTags[0].Value = "New Title";
        var rewritten = stream.ToByteArray();

        var stream2 = new MatroskaStream();
        Assert.True(stream2.ReadStream(new MemoryStream(rewritten)));
        Assert.Equal("New Title", stream2.Tag.Title);
    }

    [Fact]
    public void ToByteArray_AppendsTagsWhenAbsent_ReadableAgain()
    {
        var container = BuildContainerNoTags("matroska");
        var stream = new MatroskaStream();
        Assert.True(stream.ReadStream(new MemoryStream(container)));

        var entry = new MatroskaTagEntry();
        entry.SimpleTags.Add(new MatroskaSimpleTag { Name = "TITLE", Value = "Brand New" });
        stream.Tag.Entries.Add(entry);

        var rewritten = stream.ToByteArray();
        Assert.NotEqual(container, rewritten);

        var stream2 = new MatroskaStream();
        Assert.True(stream2.ReadStream(new MemoryStream(rewritten)));
        Assert.Equal("Brand New", stream2.Tag.Title);
    }

    // ================================================================
    // 6. Info / Duration parsing.
    // ================================================================

    [Fact]
    public void ReadStream_DurationAndTimecodeScale_ProduceTotalAudioLength()
    {
        // Build Info element with TimecodeScale=1_000_000 (1ms) and Duration=2500.0 → 2500 ms.
        using var infoMs = new MemoryStream();
        var ts = BuildElement(MatroskaStream.TimecodeScaleId, EbmlElement.EncodeUInt(1_000_000));
        infoMs.Write(ts, 0, ts.Length);

        // Duration as float64: 2500.0
        var durBytes = new byte[8];
        var bits = System.BitConverter.DoubleToInt64Bits(2500.0);
        for (var i = 0; i < 8; i++)
        {
            durBytes[i] = (byte)((bits >> ((7 - i) * 8)) & 0xFF);
        }

        var dur = BuildElement(MatroskaStream.DurationId, durBytes);
        infoMs.Write(dur, 0, dur.Length);

        var info = BuildElement(MatroskaStream.InfoId, infoMs.ToArray());
        var container = MatroskaStream.BuildMinimalContainer("matroska", info);

        var stream = new MatroskaStream();
        Assert.True(stream.ReadStream(new MemoryStream(container)));
        Assert.Equal(1_000_000, stream.TimecodeScale);
        Assert.Equal(2500.0, stream.Duration, 6);
        Assert.Equal(2500, stream.TotalDuration);
    }

    [Fact]
    public void ReadStream_NoInfoElement_TotalAudioLengthIsZero()
    {
        var container = BuildContainerNoTags("matroska");
        var stream = new MatroskaStream();
        Assert.True(stream.ReadStream(new MemoryStream(container)));
        Assert.Equal(0, stream.TotalDuration);
    }

    // ================================================================
    // 7. IMediaContainer interface basics.
    // ================================================================

    [Fact]
    public void StartAndEndOffsets_AreSet()
    {
        var container = BuildContainerNoTags("matroska");
        var stream = new MatroskaStream();
        Assert.True(stream.ReadStream(new MemoryStream(container)));
        Assert.Equal(0, stream.StartOffset);
        Assert.Equal(container.Length, stream.EndOffset);
        Assert.Equal(container.Length, stream.TotalMediaSize);
    }

    [Fact]
    public void EmptyMatroskaStream_ToByteArray_ThrowsWhenSourceIsNull()
    {
        // Phase 0 contract: ToByteArray must surface the detached-source state
        // rather than silently returning an empty array.
        var stream = new MatroskaStream();
        var ex = Assert.Throws<InvalidOperationException>(stream.ToByteArray);
        Assert.Equal(
            "Source stream was detached or never read. WriteTo requires a live source.",
            ex.Message);
    }
}
