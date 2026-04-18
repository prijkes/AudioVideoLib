namespace AudioVideoLib.Tags;

using System;
using System.IO;

using AudioVideoLib.IO;

/// <summary>
/// Class to store a Lyrics3v2 field.
/// </summary>
public partial class Lyrics3v2Field
{
    /// <summary>
    /// Reads an <see cref="Lyrics3v2Field"/> from a <see cref="Stream"/> at the current position.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="maximumFieldSize">Maximum size of the field.</param>
    /// <returns>
    /// An <see cref="Lyrics3v2Field"/> if found; otherwise, null.
    /// </returns>
    public static Lyrics3v2Field? ReadFromStream(Stream stream, long maximumFieldSize)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return ReadField(stream as StreamBuffer ?? new StreamBuffer(stream), maximumFieldSize);
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private static Lyrics3v2Field? ReadField(StreamBuffer sb, long maximumFieldSize)
    {
        ArgumentNullException.ThrowIfNull(sb);

        var identifier = sb.PeekString(FieldIdentifierLength);
        var field = GetField(identifier);
        return field.ReadFieldInstance(sb, maximumFieldSize) ? field : null;
    }

    private bool ReadFieldInstance(StreamBuffer sb, long maximumFieldSize)
    {
        ArgumentNullException.ThrowIfNull(sb);

        var identifier = sb.ReadString(FieldIdentifierLength);
        var strFieldSize = sb.ReadString(FieldSizeLength);

        if (!int.TryParse(strFieldSize, out var fieldSize) || fieldSize > maximumFieldSize)
        {
            return false;
        }
        Identifier = identifier;
        var data = new byte[fieldSize];
        sb.Read(data, fieldSize);

        if (!IsValidData(data))
        {
            return false;
        }

        Data = data;
        return true;
    }
}
