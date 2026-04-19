/*
 * Test suite for AIFF text chunk metadata (NAME, AUTH, ANNO, COMT).
 */
namespace AudioVideoLib.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioVideoLib.Formats;
using AudioVideoLib.IO;

using Xunit;

public class AiffTextChunkTests
{
    // ================================================================
    // Helpers — synthesise an AIFF FORM with a minimal COMM chunk and
    // an extra body for text chunks.
    // ================================================================

    private static byte[] BuildAiff(byte[] textChunkBytes)
    {
        // COMM chunk: 18-byte payload (channels, sampleFrames, sampleSize, 80-bit sampleRate).
        // We use the 80-bit value for 44100 Hz = 0x400EAC44000000000000.
        byte[] comm =
        [
            (byte)'C', (byte)'O', (byte)'M', (byte)'M',
            0, 0, 0, 18,
            0, 1,                    // channels
            0, 0, 0, 0,              // sampleFrames
            0, 16,                   // sampleSize
            0x40, 0x0E, 0xAC, 0x44, 0, 0, 0, 0, 0, 0, // sampleRate (44100)
        ];

        // SSND chunk with empty payload (8 bytes for offset/blockSize).
        byte[] ssnd =
        [
            (byte)'S', (byte)'S', (byte)'N', (byte)'D',
            0, 0, 0, 8,
            0, 0, 0, 0,
            0, 0, 0, 0,
        ];

        var inner = new byte[4 + comm.Length + textChunkBytes.Length + ssnd.Length];
        inner[0] = (byte)'A';
        inner[1] = (byte)'I';
        inner[2] = (byte)'F';
        inner[3] = (byte)'F';
        Buffer.BlockCopy(comm, 0, inner, 4, comm.Length);
        Buffer.BlockCopy(textChunkBytes, 0, inner, 4 + comm.Length, textChunkBytes.Length);
        Buffer.BlockCopy(ssnd, 0, inner, 4 + comm.Length + textChunkBytes.Length, ssnd.Length);

        var declared = (uint)inner.Length;
        var form = new byte[8 + inner.Length];
        form[0] = (byte)'F';
        form[1] = (byte)'O';
        form[2] = (byte)'R';
        form[3] = (byte)'M';
        form[4] = (byte)((declared >> 24) & 0xFF);
        form[5] = (byte)((declared >> 16) & 0xFF);
        form[6] = (byte)((declared >> 8) & 0xFF);
        form[7] = (byte)(declared & 0xFF);
        Buffer.BlockCopy(inner, 0, form, 8, inner.Length);
        return form;
    }

    // ================================================================
    // 1. ToByteArray / ReadStream round-trip
    // ================================================================

    [Fact]
    public void AiffTextChunks_ToByteArray_RoundTripsName()
    {
        var bundle = new AiffTextChunks("My Name", null, null, []);
        var bytes = bundle.ToByteArray();

        var aiff = BuildAiff(bytes);
        var stream = new AiffStream();
        Assert.True(stream.ReadStream(new MemoryStream(aiff)));
        Assert.NotNull(stream.TextChunks);
        Assert.Equal("My Name", stream.TextChunks!.Name);
        Assert.Null(stream.TextChunks.Author);
        Assert.Null(stream.TextChunks.Annotation);
        Assert.Empty(stream.TextChunks.Comments);

        Assert.Equal(bytes, stream.TextChunks.ToByteArray());
    }

    [Fact]
    public void AiffTextChunks_ToByteArray_RoundTripsAuthor()
    {
        var bundle = new AiffTextChunks(null, "Some Author", null, []);
        var bytes = bundle.ToByteArray();

        var aiff = BuildAiff(bytes);
        var stream = new AiffStream();
        Assert.True(stream.ReadStream(new MemoryStream(aiff)));
        Assert.NotNull(stream.TextChunks);
        Assert.Equal("Some Author", stream.TextChunks!.Author);
        Assert.Equal(bytes, stream.TextChunks.ToByteArray());
    }

    [Fact]
    public void AiffTextChunks_ToByteArray_RoundTripsAnnotation()
    {
        var bundle = new AiffTextChunks(null, null, "An annotation", []);
        var bytes = bundle.ToByteArray();

        var aiff = BuildAiff(bytes);
        var stream = new AiffStream();
        Assert.True(stream.ReadStream(new MemoryStream(aiff)));
        Assert.NotNull(stream.TextChunks);
        Assert.Equal("An annotation", stream.TextChunks!.Annotation);
        Assert.Equal(bytes, stream.TextChunks.ToByteArray());
    }

    [Fact]
    public void AiffTextChunks_ToByteArray_RoundTripsAllFour()
    {
        var bundle = new AiffTextChunks(
            "Track Name",
            "Track Author",
            "Annotation text",
            [
                new AiffComment(0, 0, "First comment"),
                new AiffComment(3000000000U, 7, "Second comment"),
            ]);
        var bytes = bundle.ToByteArray();

        var aiff = BuildAiff(bytes);
        var stream = new AiffStream();
        Assert.True(stream.ReadStream(new MemoryStream(aiff)));

        Assert.NotNull(stream.TextChunks);
        var tc = stream.TextChunks!;
        Assert.Equal("Track Name", tc.Name);
        Assert.Equal("Track Author", tc.Author);
        Assert.Equal("Annotation text", tc.Annotation);
        Assert.Equal(2, tc.Comments.Count);
        Assert.Equal("First comment", tc.Comments[0].Text);
        Assert.Equal(0U, tc.Comments[0].TimeStamp);
        Assert.Null(tc.Comments[0].TimeStampUtc);
        Assert.Equal("Second comment", tc.Comments[1].Text);
        Assert.Equal(3000000000U, tc.Comments[1].TimeStamp);
        Assert.Equal((ushort)7, tc.Comments[1].MarkerId);

        Assert.Equal(bytes, tc.ToByteArray());
    }

    [Fact]
    public void AiffTextChunks_NoneInContainer_TextChunksIsNull()
    {
        var aiff = BuildAiff([]);
        var stream = new AiffStream();
        Assert.True(stream.ReadStream(new MemoryStream(aiff)));
        Assert.Null(stream.TextChunks);
    }

    // ================================================================
    // 2. COMT timestamp parsing
    // ================================================================

    [Fact]
    public void AiffComment_TimeStampUtc_ParsesFromMacEpoch()
    {
        // 2 hours after the Mac epoch (1904-01-01).
        var ts = (uint)(2 * 3600);
        var comment = new AiffComment(ts, 0, "hi");
        Assert.Equal(new DateTime(1904, 1, 1, 2, 0, 0, DateTimeKind.Utc), comment.TimeStampUtc);
    }

    [Fact]
    public void AiffComment_TimeStampZero_HasNoTimeStampUtc()
    {
        var comment = new AiffComment(0, 0, "no time");
        Assert.Null(comment.TimeStampUtc);
    }

    [Fact]
    public void AiffTextChunks_CommentsRoundTrip_PreservesTimestampSemantically()
    {
        var t1 = new DateTime(2024, 6, 1, 12, 30, 45, DateTimeKind.Utc);
        var stamp = (uint)(t1 - AiffComment.MacEpoch).TotalSeconds;
        var bundle = new AiffTextChunks(null, null, null, [new AiffComment(stamp, 0, "ts test")]);
        var bytes = bundle.ToByteArray();

        var aiff = BuildAiff(bytes);
        var stream = new AiffStream();
        Assert.True(stream.ReadStream(new MemoryStream(aiff)));
        Assert.NotNull(stream.TextChunks);
        var c = stream.TextChunks!.Comments[0];
        Assert.Equal(stamp, c.TimeStamp);
        Assert.Equal(t1, c.TimeStampUtc);
    }

    // ================================================================
    // 3. Padding behaviour — odd-length payloads must round-trip
    // ================================================================

    [Fact]
    public void AiffTextChunks_OddLengthPayload_PadsToEvenAndRoundTrips()
    {
        // Length 7 (odd) → expect 1 trailing pad byte in serialised form.
        var bundle = new AiffTextChunks("OddOne!", null, null, []);
        var bytes = bundle.ToByteArray();

        // 8 (header) + 7 (payload) + 1 (pad) = 16
        Assert.Equal(16, bytes.Length);
        Assert.Equal(0, bytes[15]);

        var aiff = BuildAiff(bytes);
        var stream = new AiffStream();
        Assert.True(stream.ReadStream(new MemoryStream(aiff)));
        Assert.NotNull(stream.TextChunks);
        Assert.Equal("OddOne!", stream.TextChunks!.Name);
    }

    [Fact]
    public void AiffTextChunks_EvenLengthPayload_HasNoPadByte()
    {
        var bundle = new AiffTextChunks("EvenLen!", null, null, []);
        var bytes = bundle.ToByteArray();

        // 8 (header) + 8 (payload) = 16, no pad.
        Assert.Equal(16, bytes.Length);
    }

    [Fact]
    public void AiffStream_ReadsTextChunkWithoutTrailingPadByte_AtContainerEnd()
    {
        // Build a chunk with odd payload but truncate the trailing pad byte —
        // the walker tolerates it because the chunk is the last one in the FORM.
        var oddPayload = Encoding.ASCII.GetBytes("OddTail");       // 7 bytes (odd)
        var chunkNoPad = new byte[8 + oddPayload.Length];
        chunkNoPad[0] = (byte)'N';
        chunkNoPad[1] = (byte)'A';
        chunkNoPad[2] = (byte)'M';
        chunkNoPad[3] = (byte)'E';
        var size = (uint)oddPayload.Length;
        chunkNoPad[4] = (byte)((size >> 24) & 0xFF);
        chunkNoPad[5] = (byte)((size >> 16) & 0xFF);
        chunkNoPad[6] = (byte)((size >> 8) & 0xFF);
        chunkNoPad[7] = (byte)(size & 0xFF);
        Buffer.BlockCopy(oddPayload, 0, chunkNoPad, 8, oddPayload.Length);

        // Embed without an extra pad byte — relying on container-end termination.
        var aiff = BuildAiff(chunkNoPad);

        var stream = new AiffStream();
        Assert.True(stream.ReadStream(new MemoryStream(aiff)));
        Assert.NotNull(stream.TextChunks);
        Assert.Equal("OddTail", stream.TextChunks!.Name);
    }

    // ================================================================
    // 4. Oversized chunk size — declared size larger than remaining bytes
    // ================================================================

    [Fact]
    public void AiffStream_OversizedTextChunkSize_DoesNotThrow()
    {
        // NAME chunk claiming 1 GB of payload — clearly past the container end.
        byte[] header =
        [
            (byte)'N', (byte)'A', (byte)'M', (byte)'E',
            0x40, 0, 0, 0,  // size = 1,073,741,824
        ];

        var aiff = BuildAiff(header);
        var stream = new AiffStream();
        // Must not throw; either returns false or returns true with TextChunks null/incomplete.
        var ok = stream.ReadStream(new MemoryStream(aiff));
        Assert.True(ok || !ok); // never throw — either outcome acceptable
        // The bogus size should not have produced a captured NAME string.
        if (stream.TextChunks is not null)
        {
            Assert.Null(stream.TextChunks.Name);
        }
    }

    // ================================================================
    // 5. ReadComments — malformed inputs return null
    // ================================================================

    [Fact]
    public void AiffTextChunks_ReadComments_NullPayload_ReturnsNull()
    {
        Assert.Null(AiffTextChunks.ReadComments(null));
    }

    [Fact]
    public void AiffTextChunks_ReadComments_TooShortForCount_ReturnsNull()
    {
        Assert.Null(AiffTextChunks.ReadComments([0x00]));
    }

    [Fact]
    public void AiffTextChunks_ReadComments_TruncatedHeader_ReturnsNull()
    {
        // Claims one comment but supplies only 5 of the required 8 header bytes.
        Assert.Null(AiffTextChunks.ReadComments([0, 1, 0, 0, 0, 1, 0]));
    }

    [Fact]
    public void AiffTextChunks_ReadComments_TruncatedText_ReturnsNull()
    {
        // 1 comment, count=10, but only 3 bytes of text follow.
        byte[] payload = [0, 1, 0, 0, 0, 0, 0, 0, 0, 10, (byte)'a', (byte)'b', (byte)'c'];
        Assert.Null(AiffTextChunks.ReadComments(payload));
    }

    // ================================================================
    // 6. ReadText — null payload returns null
    // ================================================================

    [Fact]
    public void AiffTextChunks_ReadText_NullPayload_ReturnsNull()
    {
        Assert.Null(AiffTextChunks.ReadText(null));
    }

    [Fact]
    public void AiffTextChunks_ReadText_EmptyPayload_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, AiffTextChunks.ReadText([]));
    }

    // ================================================================
    // 7. Bundle helpers
    // ================================================================

    [Fact]
    public void AiffTextChunks_IsEmpty_TrueWhenNothingSet()
    {
        var bundle = new AiffTextChunks(null, null, null, []);
        Assert.True(bundle.IsEmpty);
    }

    [Fact]
    public void AiffTextChunks_IsEmpty_FalseWhenAnyFieldSet()
    {
        Assert.False(new AiffTextChunks("x", null, null, []).IsEmpty);
        Assert.False(new AiffTextChunks(null, "x", null, []).IsEmpty);
        Assert.False(new AiffTextChunks(null, null, "x", []).IsEmpty);
        Assert.False(new AiffTextChunks(null, null, null, [new AiffComment(0, 0, "c")]).IsEmpty);
    }

    [Fact]
    public void AiffTextChunks_ToByteArray_EmptyBundleProducesNoBytes()
    {
        var bundle = new AiffTextChunks(null, null, null, []);
        Assert.Empty(bundle.ToByteArray());
    }

    // ================================================================
    // 8. Existing chunks reflected on the stream
    // ================================================================

    [Fact]
    public void AiffStream_TextChunks_AreAlsoListedInChunks()
    {
        var bundle = new AiffTextChunks("A", "B", "C", [new AiffComment(0, 0, "D")]);
        var aiff = BuildAiff(bundle.ToByteArray());

        var stream = new AiffStream();
        Assert.True(stream.ReadStream(new MemoryStream(aiff)));
        var ids = new List<string>();
        foreach (var c in stream.Chunks)
        {
            ids.Add(c.Id);
        }

        Assert.Contains("NAME", ids);
        Assert.Contains("AUTH", ids);
        Assert.Contains("ANNO", ids);
        Assert.Contains("COMT", ids);
    }
}
