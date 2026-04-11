namespace AudioVideoLib.Studio;

using System;
using System.Text;

public static class HexDumper
{
    public static string Dump(byte[] data, long baseOffset = 0, int maxBytes = 16 * 1024)
    {
        if (data == null || data.Length == 0)
        {
            return "(empty)";
        }

        var limit = Math.Min(data.Length, maxBytes);
        var sb = new StringBuilder(limit * 4);

        for (var i = 0; i < limit; i += 16)
        {
            sb.Append((baseOffset + i).ToString("X8"));
            sb.Append("  ");

            for (var j = 0; j < 16; j++)
            {
                if (i + j < limit)
                {
                    sb.Append(data[i + j].ToString("X2"));
                    sb.Append(' ');
                }
                else
                {
                    sb.Append("   ");
                }

                if (j == 7)
                {
                    sb.Append(' ');
                }
            }

            sb.Append(' ');
            for (var j = 0; j < 16 && (i + j) < limit; j++)
            {
                var b = data[i + j];
                sb.Append(b is >= 0x20 and < 0x7F ? (char)b : '.');
            }

            sb.AppendLine();
        }

        if (data.Length > maxBytes)
        {
            sb.AppendLine();
            sb.Append($"... truncated; showing {maxBytes:N0} of {data.Length:N0} bytes ...");
        }

        return sb.ToString();
    }
}
