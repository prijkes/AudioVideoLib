namespace AudioVideoLib.Tags;

using System;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.IO;

/// <summary>
/// A Vorbis comment is the second (of three) header packets that begin a Vorbis bit stream.
/// It is meant for short, text comments, not arbitrary metadata; arbitrary metadata belongs in a separate logical bit stream (usually an XML stream type)
/// that provides greater structure and machine parse ability.
/// </summary>
public class VorbisComment
{
    private const char Delimiter = '=';

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>
    /// The name.
    /// </value>
    /// <remarks>
    /// A case-insensitive field name that may consist of ASCII 0x20 through 0x7D, 0x3D ('=') excluded.
    /// ASCII 0x41 through 0x5A inclusive (A-Z) is to be considered equivalent to ASCII 0x61 through 0x7A inclusive (a-z).
    /// </remarks>
    public string Name
    {
        get;

        set
        {
            if (!string.IsNullOrEmpty(value) && !IsValidName(value))
            {
                throw new InvalidDataException("Value contains one or more invalid characters.");
            }

            field = value;
        }
    } = null!;

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    /// <value>
    /// The value.
    /// </value>
    public string Value { get; set; } = null!;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads the stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>
    /// A <see cref="VorbisComment"/> instance if a vorbis comment was found; otherwise, null.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">Thrown if stream is null.</exception>
    public static VorbisComment? ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var sb = stream as StreamBuffer ?? new StreamBuffer(stream);
        var length = sb.ReadInt32();

        // A negative / absurd length indicates a malformed or malicious Vorbis block;
        // refuse before allocating. Cap at remaining bytes to protect against a length
        // field that exceeds the physical stream.
        if (length <= 0 || length > sb.Length - sb.Position)
        {
            return null;
        }

        var s = sb.ReadString(length, Encoding.UTF8).Split(Delimiter);
        return s.Length >= 2
                   ? new VorbisComment { Name = s[0], Value = string.Join(string.Empty, s, 1, s.Length - 1) }
                   : null;
    }

    /// <summary>
    /// Places the <see cref="VorbisComment"/> into a byte array.
    /// </summary>
    /// <returns>
    /// A byte array that represents the <see cref="VorbisComment"/>.
    /// </returns>
    public byte[] ToByteArray()
    {
        var buf = new StreamBuffer();
        var val = (Name ?? string.Empty) + Delimiter + Value;
        buf.WriteInt(Encoding.UTF8.GetByteCount(val));
        buf.WriteString(val, Encoding.UTF8);
        return buf.ToByteArray();
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private static bool IsValidName(string name)
    {
        return name == null
            ? throw new ArgumentNullException("name")
            : name.All(c => c is >= (char)0x20 and <= (char)0x7D and not (char)0x3D);
    }
}
