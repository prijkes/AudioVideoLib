namespace AudioVideoLib.Formats;

using System;
using System.Text;
using System.Xml.Linq;

/// <summary>
/// A parsed iXML chunk: UTF-8 XML metadata used in film/audio production (ixml.org).
/// </summary>
/// <remarks>
/// The chunk's payload is captured verbatim. Common top-level fields under <c>BWFXML</c> are
/// surfaced as properties; XML parse failures yield <c>null</c> property values rather than throws.
/// </remarks>
public sealed class IxmlChunk
{
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    private readonly XDocument? _doc;

    private IxmlChunk(byte[] rawPayload, string xml)
    {
        RawPayload = rawPayload;
        Xml = xml;
        _doc = TryParse(xml);
    }

    private static XDocument? TryParse(string xml)
    {
        try
        {
            return XDocument.Parse(xml);
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }
    }

    /// <summary>Gets the raw chunk payload bytes (BOM preserved if present).</summary>
    public byte[] RawPayload { get; }

    /// <summary>Gets the decoded UTF-8 XML string (BOM stripped).</summary>
    public string Xml { get; }

    /// <summary>Gets a value indicating whether the XML payload parsed successfully.</summary>
    public bool IsWellFormed => _doc is not null;

    /// <summary>Gets the iXML <c>PROJECT</c> field, or <c>null</c> if absent or unparseable.</summary>
    public string? ProjectName => GetField("PROJECT");

    /// <summary>Gets the iXML <c>SCENE</c> field.</summary>
    public string? SceneName => GetField("SCENE");

    /// <summary>Gets the iXML <c>TAKE</c> field.</summary>
    public string? TakeName => GetField("TAKE");

    /// <summary>Gets the iXML <c>TAPE</c> field.</summary>
    public string? Tape => GetField("TAPE");

    /// <summary>Gets the iXML <c>NOTE</c> field.</summary>
    public string? Note => GetField("NOTE");

    /// <summary>Gets the iXML <c>FILE_UID</c> field.</summary>
    public string? FileUid => GetField("FILE_UID");

    /// <summary>Gets the iXML <c>UBITS</c> field.</summary>
    public string? Ubits => GetField("UBITS");

    /// <summary>
    /// Parses an iXML chunk payload. Returns <c>null</c> for empty input; otherwise always returns a
    /// chunk (well-formedness is reported via <see cref="IsWellFormed"/>).
    /// </summary>
    /// <param name="payload">The raw <c>iXML</c> chunk payload.</param>
    public static IxmlChunk? Parse(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return Parse((ReadOnlySpan<byte>)payload);
    }

    /// <summary>
    /// Parses an iXML chunk payload. Returns <c>null</c> for empty input; otherwise always returns a
    /// chunk (well-formedness is reported via <see cref="IsWellFormed"/>).
    /// </summary>
    /// <param name="payload">The raw <c>iXML</c> chunk payload.</param>
    /// <remarks>
    /// Span-based overload: lets callers pass slices of an existing buffer without allocating
    /// an intermediate <see cref="T:byte[]"/>. The chunk still keeps its own raw-payload copy
    /// for byte-identical round-trip, so internally one allocation occurs.
    /// </remarks>
    public static IxmlChunk? Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length == 0)
        {
            return null;
        }

        var offset = 0;
        if (payload.Length >= 3 && payload[0] == Utf8Bom[0] && payload[1] == Utf8Bom[1] && payload[2] == Utf8Bom[2])
        {
            offset = 3;
        }

        // Strip trailing nulls (some writers pad to even chunk length with NUL inside the payload).
        var len = payload.Length - offset;
        while (len > 0 && payload[offset + len - 1] == 0)
        {
            len--;
        }

        var xml = Encoding.UTF8.GetString(payload.Slice(offset, len));
        return new IxmlChunk(payload.ToArray(), xml);
    }

    /// <summary>
    /// Returns the XML payload, optionally with a leading UTF-8 BOM, as a fresh byte array.
    /// </summary>
    /// <param name="includeBom">When <c>true</c>, prefixes the output with the UTF-8 BOM.</param>
    public byte[] ToByteArray(bool includeBom)
    {
        var xmlBytes = Encoding.UTF8.GetBytes(Xml ?? string.Empty);
        if (!includeBom)
        {
            return xmlBytes;
        }

        var buf = new byte[3 + xmlBytes.Length];
        Array.Copy(Utf8Bom, 0, buf, 0, 3);
        Array.Copy(xmlBytes, 0, buf, 3, xmlBytes.Length);
        return buf;
    }

    /// <summary>Returns the chunk's raw payload bytes (the exact bytes seen at parse time).</summary>
    public byte[] ToByteArray() => (byte[])RawPayload.Clone();

    /// <summary>
    /// Serialises this iXML chunk wrapped in a full <c>iXML</c> RIFF chunk (8-byte header + payload + pad).
    /// </summary>
    public byte[] ToChunkBytes()
    {
        var payload = ToByteArray();
        var pad = (payload.Length & 1) == 1 ? 1 : 0;
        var buf = new byte[8 + payload.Length + pad];
        Encoding.ASCII.GetBytes("iXML", 0, 4, buf, 0);
        var size = (uint)payload.Length;
        buf[4] = (byte)(size & 0xFF);
        buf[5] = (byte)((size >> 8) & 0xFF);
        buf[6] = (byte)((size >> 16) & 0xFF);
        buf[7] = (byte)((size >> 24) & 0xFF);
        Array.Copy(payload, 0, buf, 8, payload.Length);
        return buf;
    }

    private string? GetField(string elementName)
    {
        if (_doc?.Root is null)
        {
            return null;
        }

        foreach (var el in _doc.Root.Elements())
        {
            if (string.Equals(el.Name.LocalName, elementName, StringComparison.OrdinalIgnoreCase))
            {
                return el.Value;
            }
        }

        return null;
    }
}
