namespace AudioVideoLib.Studio.Converters;

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Data;

/// <summary>
/// Splits an enum value's PascalCase name into space-separated words and
/// uppercases known acronyms. An acronym immediately followed by digits
/// is joined with a hyphen (e.g. "UTF" + "16" → "UTF-16").
/// </summary>
/// <remarks>
/// Examples: <c>AbsoluteTimeMilliseconds</c> → "Absolute Time Milliseconds";
/// <c>AbsoluteTimeMpegFrames</c> → "Absolute Time MPEG Frames";
/// <c>UTF16LittleEndian</c> → "UTF-16 Little Endian";
/// <c>UTF16BigEndianWithoutBom</c> → "UTF-16 Big Endian Without BOM";
/// <c>UTF8</c> → "UTF-8".
/// </remarks>
public sealed partial class EnumDisplayConverter : IValueConverter
{
    private static readonly string[] Acronyms =
    [
        "UTF", "MPEG", "BOM", "MIME", "URL", "ISO", "ASCII", "ID", "CD", "TOC",
    ];

    // Splits at PascalCase boundaries and at letter↔digit boundaries.
    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])|(?<=[A-Za-z])(?=[0-9])|(?<=[0-9])(?=[A-Za-z])")]
    private static partial Regex Splitter();

    [GeneratedRegex(@"^[0-9]+$")]
    private static partial Regex DigitsOnly();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Enum)
        {
            return value ?? string.Empty;
        }
        var split = Splitter().Replace(value.ToString()!, " ");
        var words = split.Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            foreach (var ac in Acronyms)
            {
                if (string.Equals(words[i], ac, StringComparison.OrdinalIgnoreCase))
                {
                    words[i] = ac;
                    break;
                }
            }
        }
        // Join "<acronym> <digits>" pairs with a hyphen so "UTF 16" → "UTF-16".
        var sb = new StringBuilder();
        for (var i = 0; i < words.Length; i++)
        {
            if (i > 0)
            {
                var prevIsAcronym = Array.IndexOf(Acronyms, words[i - 1]) >= 0;
                var curIsDigits = DigitsOnly().IsMatch(words[i]);
                sb.Append(prevIsAcronym && curIsDigits ? '-' : ' ');
            }
            sb.Append(words[i]);
        }
        return sb.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
