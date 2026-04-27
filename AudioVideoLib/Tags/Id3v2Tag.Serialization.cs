namespace AudioVideoLib.Tags;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Linq;

using AudioVideoLib.IO;

public sealed partial class Id3v2Tag
{
    /// <summary>
    /// Writes the serialised <see cref="Id3v2Tag"/> to the supplied stream.
    /// </summary>
    /// <param name="destination">The stream to write the tag bytes into.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="destination"/> is <c>null</c>.</exception>
    /// <remarks>
    /// Frames which have <see cref="Id3v2Frame.TagAlterPreservation"/> or
    /// <see cref="Id3v2Frame.FileAlterPreservation"/> set to false won't be written.
    /// </remarks>
    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var data = new StreamBuffer();
        WriteExtendedHeader(data);
        WriteFrames(data);
        data = MaybeApplyUnsynchronization(data);
        AppendTagPadding(data);

        var fullBuffer = new StreamBuffer();
        WriteTagBoundary(fullBuffer, HeaderIdentifierBytes, (int)data.Length, UseHeader);
        fullBuffer.Write(data.ToByteArray());
        WriteTagBoundary(fullBuffer, FooterIdentifierBytes, (int)data.Length, UseFooter);
        var bytes = fullBuffer.ToByteArray();
        destination.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Calculates the CRC32 of the <see cref="Id3v2Tag"/>.
    /// </summary>
    /// <returns>The CRC32 of the <see cref="Id3v2Tag"/>.</returns>
    /// <remarks>
    /// This property only applies for version <see cref="Id3v2Version.Id3v230"/> and later.
    /// </remarks>
    public int CalculateCrc32()
    {
        if (Version < Id3v2Version.Id3v230)
        {
            return 0;
        }

        // The CRC is computed over exactly the bytes that ToByteArray() will write
        // for the frame region (and, in v2.4, the padding). ToByteArray() also
        // iterates GetFrames(), so the two match by construction: a tag written by
        // this library will always validate against its own stored CRC when read
        // back, because the read-path CRC in Id3v2TagReader hashes the same byte
        // range that was written here. Tags produced by other writers that include
        // empty-data frames will still validate on read (that path hashes raw bytes)
        // but we deliberately don't reproduce such frames on write, so a subsequent
        // round-trip will re-compute a different (filtered) CRC.
        var stream = new StreamBuffer();
        // Id3v2.3.0
        // The CRC should be calculated before unsynchronisation on the data between the extended header and the padding, i.e. the frames and only the frames.
        foreach (var frame in GetFrames())
        {
            stream.Write(frame.ToByteArray());
        }

        // Id3v2.4.0
        // The CRC is calculated on all the data between the header and footer as indicated by the header's size field, minus the extended header.
        // Note that this includes the padding (if there is any), but excludes the footer.
        if (Version >= Id3v2Version.Id3v240)
        {
            stream.WritePadding(0, PaddingSize);
        }

        return (int)Crc32.HashToUInt32(stream.ToByteArray());
    }

    private void WriteExtendedHeader(StreamBuffer data)
    {
        if (ExtendedHeader == null)
        {
            return;
        }

        if (Version is >= Id3v2Version.Id3v230 and < Id3v2Version.Id3v240)
        {
            data.WriteBigEndianInt32(ExtendedHeader.GetHeaderSize(Version));
            data.WriteBigEndianInt16((short)ExtendedHeader.GetFlags(Version));
            data.WriteBigEndianInt32(ExtendedHeader.PaddingSize + PaddingSize);
            if (ExtendedHeader.CrcDataPresent)
            {
                data.WriteBigEndianInt32(CalculateCrc32());
            }

            return;
        }

        if (Version < Id3v2Version.Id3v240)
        {
            return;
        }

        var flagsFieldLength = ExtendedHeader.GetFlagsFieldLength(Version);
        data.WriteBigEndianInt32(GetSynchsafeValue(ExtendedHeader.GetHeaderSize(Version)));
        data.WriteByte((byte)flagsFieldLength);
        data.WriteBytes(ExtendedHeader.GetFlags(Version), flagsFieldLength);

        if (ExtendedHeader.TagIsUpdate)
        {
            data.WriteByte(0x00);
        }

        if (ExtendedHeader.CrcDataPresent)
        {
            var crc = GetSynchsafeValue(CalculateCrc32() & 0xFFFFFFFF);
            data.WriteBigEndianBytes(crc, 5);
        }

        if (ExtendedHeader.TagIsRestricted)
        {
            data.Write(ExtendedHeader.TagRestrictions.ToByte());
        }
    }

    private void WriteFrames(StreamBuffer data)
    {
        foreach (var byteField in GetFrames().Select(frame => frame.ToByteArray()))
        {
            data.Write(byteField);
        }
    }

    // v2.4+ unsynchronizes per-frame ([S:6.1]); earlier versions rewrite the whole post-header payload.
    private StreamBuffer MaybeApplyUnsynchronization(StreamBuffer data)
    {
        if (!UseUnsynchronization || Version >= Id3v2Version.Id3v240)
        {
            return data;
        }

        var synchronizedData = data.ToByteArray();
        var unsynchronizedData = GetUnsynchronizedData(synchronizedData, 0, synchronizedData.Length);
        return new StreamBuffer(unsynchronizedData) { Position = unsynchronizedData.Length };
    }

    // Spec: a tag MUST NOT have both padding and a footer.
    private void AppendTagPadding(StreamBuffer data)
    {
        if (UseFooter)
        {
            return;
        }

        data.WritePadding(0, PaddingSize);
        if (ExtendedHeader != null)
        {
            data.WritePadding(0, ExtendedHeader.PaddingSize);
        }
    }

    // Header and footer share the same 10-byte layout, differing only in identifier bytes.
    private void WriteTagBoundary(StreamBuffer buffer, byte[] identifierBytes, int dataSize, bool condition)
    {
        if (!condition)
        {
            return;
        }

        buffer.Write(identifierBytes);
        buffer.WriteShort((short)(Convert.ToInt16(Version) / 10));
        buffer.WriteByte((byte)Flags);
        buffer.WriteBigEndianInt32(GetSynchsafeValue(dataSize));
    }

    // Filters out 0-byte data frames (spec: data must contain at least 1 byte) and, for
    // v2.3+, respects the TagAlterPreservation/FileAlterPreservation flags.
    private IEnumerable<Id3v2Frame> GetFrames()
    {
        return _frames.Where(
            frame =>
            (Version < Id3v2Version.Id3v230)
            || ((PreserveFramesAfterTagAlteration || frame.TagAlterPreservation)
                && (PreserveFramesAfterFileAlteration || frame.FileAlterPreservation))).Where(
                    frame =>
                        {
                            var data = frame.Data;
                            return (data != null) && data.Length > 0;
                        });
    }
}
