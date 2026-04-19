namespace AudioVideoLib.Formats;

using System;
using System.Text;

/// <summary>
/// A parsed Broadcast Wave Format <c>bext</c> chunk (EBU Tech 3285).
/// </summary>
/// <remarks>
/// Layout (little-endian): Description[256], Originator[32], OriginatorReference[32],
/// OriginationDate[10], OriginationTime[8], TimeReference (uint64), Version (uint16),
/// UMID[64], (v1+) loudness fields (5 × int16), Reserved[180] (v1) / [180] (v2 same pad),
/// CodingHistory (rest of chunk, ASCII).
/// </remarks>
public sealed class BwfBextChunk
{
    /// <summary>The minimum size of a v0 bext chunk payload, in bytes.</summary>
    public const int MinV0Size = 256 + 32 + 32 + 10 + 8 + 8 + 2 + 64 + 190;

    /// <summary>The minimum size of a v1+ bext chunk payload (adds 10 bytes of loudness fields, removes 10 from reserved).</summary>
    public const int MinV1Size = MinV0Size;

    /// <summary>Gets or sets the description (max 256 ASCII chars).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the originator (max 32 ASCII chars).</summary>
    public string Originator { get; set; } = string.Empty;

    /// <summary>Gets or sets the originator reference (max 32 ASCII chars).</summary>
    public string OriginatorReference { get; set; } = string.Empty;

    /// <summary>Gets or sets the origination date (10 ASCII chars, "YYYY-MM-DD").</summary>
    public string OriginationDate { get; set; } = string.Empty;

    /// <summary>Gets or sets the origination time (8 ASCII chars, "HH:MM:SS").</summary>
    public string OriginationTime { get; set; } = string.Empty;

    /// <summary>Gets or sets the time reference (samples since midnight) as a 64-bit value.</summary>
    public ulong TimeReference { get; set; }

    /// <summary>Gets or sets the bext version (0, 1, or 2).</summary>
    public ushort Version { get; set; }

    /// <summary>Gets or sets the SMPTE UMID (always 64 bytes; padded/truncated on write).</summary>
    public byte[] Umid { get; set; } = new byte[64];

    /// <summary>Gets or sets the integrated loudness value (×100, v1+).</summary>
    public short LoudnessValue { get; set; }

    /// <summary>Gets or sets the loudness range (×100, v1+).</summary>
    public short LoudnessRange { get; set; }

    /// <summary>Gets or sets the maximum true peak level (×100, v1+).</summary>
    public short MaxTruePeakLevel { get; set; }

    /// <summary>Gets or sets the maximum momentary loudness (×100, v1+).</summary>
    public short MaxMomentaryLoudness { get; set; }

    /// <summary>Gets or sets the maximum short-term loudness (×100, v1+).</summary>
    public short MaxShortTermLoudness { get; set; }

    /// <summary>Gets or sets the coding history (CRLF-separated ASCII, terminates the chunk).</summary>
    public string CodingHistory { get; set; } = string.Empty;

    /// <summary>
    /// Parses a <c>bext</c> chunk payload, or returns <c>null</c> for malformed input.
    /// </summary>
    /// <param name="payload">The raw chunk payload (excluding the 8-byte chunk header).</param>
    public static BwfBextChunk? Parse(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        // The fixed-size header is 256+32+32+10+8+8+2+64+10+180 = 602 bytes.
        // Per spec, even v0 reserves 254 bytes (no loudness fields), but the layout offsets
        // for the loudness fields are identical because the reserved region absorbs them.
        const int FixedSize = 602;
        if (payload.Length < FixedSize)
        {
            return null;
        }

        var c = new BwfBextChunk
        {
            Description = ReadFixedAscii(payload, 0, 256),
            Originator = ReadFixedAscii(payload, 256, 32),
            OriginatorReference = ReadFixedAscii(payload, 288, 32),
            OriginationDate = ReadFixedAscii(payload, 320, 10),
            OriginationTime = ReadFixedAscii(payload, 330, 8),
            TimeReference = ReadLeU64(payload, 338),
            Version = (ushort)(payload[346] | (payload[347] << 8)),
        };

        var umid = new byte[64];
        Array.Copy(payload, 348, umid, 0, 64);
        c.Umid = umid;

        c.LoudnessValue = (short)(payload[412] | (payload[413] << 8));
        c.LoudnessRange = (short)(payload[414] | (payload[415] << 8));
        c.MaxTruePeakLevel = (short)(payload[416] | (payload[417] << 8));
        c.MaxMomentaryLoudness = (short)(payload[418] | (payload[419] << 8));
        c.MaxShortTermLoudness = (short)(payload[420] | (payload[421] << 8));

        var historyOffset = FixedSize;
        if (payload.Length > historyOffset)
        {
            var historyLen = payload.Length - historyOffset;
            // Trim trailing nulls.
            while (historyLen > 0 && payload[historyOffset + historyLen - 1] == 0)
            {
                historyLen--;
            }

            c.CodingHistory = Encoding.ASCII.GetString(payload, historyOffset, historyLen);
        }

        return c;
    }

    /// <summary>
    /// Serialises the bext chunk into its on-disk payload bytes (excluding the 8-byte chunk header).
    /// </summary>
    public byte[] ToByteArray()
    {
        var historyBytes = Encoding.ASCII.GetBytes(CodingHistory ?? string.Empty);
        var size = 602 + historyBytes.Length;
        var buf = new byte[size];

        WriteFixedAscii(buf, 0, 256, Description);
        WriteFixedAscii(buf, 256, 32, Originator);
        WriteFixedAscii(buf, 288, 32, OriginatorReference);
        WriteFixedAscii(buf, 320, 10, OriginationDate);
        WriteFixedAscii(buf, 330, 8, OriginationTime);
        WriteLeU64(buf, 338, TimeReference);
        buf[346] = (byte)(Version & 0xFF);
        buf[347] = (byte)((Version >> 8) & 0xFF);

        var umid = Umid ?? [];
        Array.Copy(umid, 0, buf, 348, Math.Min(64, umid.Length));

        WriteLeI16(buf, 412, LoudnessValue);
        WriteLeI16(buf, 414, LoudnessRange);
        WriteLeI16(buf, 416, MaxTruePeakLevel);
        WriteLeI16(buf, 418, MaxMomentaryLoudness);
        WriteLeI16(buf, 420, MaxShortTermLoudness);

        Array.Copy(historyBytes, 0, buf, 602, historyBytes.Length);
        return buf;
    }

    /// <summary>
    /// Serialises this bext chunk wrapped in a full <c>bext</c> RIFF chunk (8-byte header + payload + pad).
    /// </summary>
    public byte[] ToChunkBytes()
    {
        var payload = ToByteArray();
        var pad = (payload.Length & 1) == 1 ? 1 : 0;
        var buf = new byte[8 + payload.Length + pad];
        Encoding.ASCII.GetBytes("bext", 0, 4, buf, 0);
        var size = (uint)payload.Length;
        buf[4] = (byte)(size & 0xFF);
        buf[5] = (byte)((size >> 8) & 0xFF);
        buf[6] = (byte)((size >> 16) & 0xFF);
        buf[7] = (byte)((size >> 24) & 0xFF);
        Array.Copy(payload, 0, buf, 8, payload.Length);
        return buf;
    }

    private static string ReadFixedAscii(byte[] b, int off, int len)
    {
        var n = 0;
        while (n < len && b[off + n] != 0)
        {
            n++;
        }

        return Encoding.ASCII.GetString(b, off, n);
    }

    private static void WriteFixedAscii(byte[] b, int off, int len, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);
        var copy = Math.Min(bytes.Length, len);
        Array.Copy(bytes, 0, b, off, copy);
    }

    private static ulong ReadLeU64(byte[] b, int off)
    {
        ulong v = 0;
        for (var i = 0; i < 8; i++)
        {
            v |= (ulong)b[off + i] << (8 * i);
        }

        return v;
    }

    private static void WriteLeU64(byte[] b, int off, ulong v)
    {
        for (var i = 0; i < 8; i++)
        {
            b[off + i] = (byte)((v >> (8 * i)) & 0xFF);
        }
    }

    private static void WriteLeI16(byte[] b, int off, short v)
    {
        b[off] = (byte)(v & 0xFF);
        b[off + 1] = (byte)((v >> 8) & 0xFF);
    }
}
