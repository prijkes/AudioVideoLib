namespace AudioVideoLib.Studio.Converters;

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

/// <summary>
/// Splits an enum value's PascalCase name into space-separated words and
/// uppercases known acronyms.
/// </summary>
/// <remarks>
/// Examples: <c>AbsoluteTimeMilliseconds</c> → "Absolute Time Milliseconds";
/// <c>AbsoluteTimeMpegFrames</c> → "Absolute Time MPEG Frames";
/// <c>UTF16LittleEndian</c> → "UTF16 Little Endian";
/// <c>UTF16BigEndianWithoutBom</c> → "UTF16 Big Endian Without BOM".
/// </remarks>
public sealed partial class EnumDisplayConverter : IValueConverter
{
    private static readonly string[] Acronyms =
    [
        "UTF", "MPEG", "BOM", "MIME", "URL", "ISO", "ASCII", "ID", "CD", "TOC",
    ];

    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex Splitter();

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
        return string.Join(' ', words);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
