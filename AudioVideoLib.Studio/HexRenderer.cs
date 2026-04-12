namespace AudioVideoLib.Studio;

using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

public static class HexRenderer
{
    private const int MaxBytes = 64 * 1024;
    private const int BytesPerRow = 16;

    private static readonly Brush AddressBrush = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
    private static readonly Brush NormalHexBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
    private static readonly Brush NormalAsciiBrush = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
    private static readonly Brush HighlightFg = Brushes.Black;
    private static readonly Brush HighlightBg = new SolidColorBrush(Color.FromRgb(0xFF, 0xB8, 0x2E));
    private static readonly Brush SeparatorBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
    private static readonly FontFamily Mono = new("Consolas");

    static HexRenderer()
    {
        AddressBrush.Freeze();
        NormalHexBrush.Freeze();
        NormalAsciiBrush.Freeze();
        HighlightBg.Freeze();
        SeparatorBrush.Freeze();
    }

    public static FlowDocument Render(byte[] fileBytes, long regionStart, long regionEnd,
        long? highlightStart = null, int? highlightLength = null)
    {
        var doc = new FlowDocument
        {
            FontFamily = Mono,
            FontSize = 11,
            PagePadding = new Thickness(6),
            Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)),
        };

        var length = Math.Min(regionEnd - regionStart, MaxBytes);
        if (length <= 0 || regionStart < 0 || regionStart >= fileBytes.Length)
        {
            doc.Blocks.Add(new Paragraph(new Run("(no data)") { Foreground = NormalHexBrush }));
            return doc;
        }

        var hlAbsStart = highlightStart ?? -1;
        var hlAbsEnd = hlAbsStart + (highlightLength ?? 0);

        var para = new Paragraph { LineHeight = 16, Margin = new Thickness(0) };

        // Column header
        para.Inlines.Add(new Run("Offset    ") { Foreground = AddressBrush });
        for (var c = 0; c < BytesPerRow; c++)
        {
            para.Inlines.Add(new Run($"{c:X2} ") { Foreground = AddressBrush });
            if (c == 7)
            {
                para.Inlines.Add(new Run(" ") { Foreground = SeparatorBrush });
            }
        }

        para.Inlines.Add(new Run(" ASCII") { Foreground = AddressBrush });
        para.Inlines.Add(new LineBreak());

        for (long row = 0; row < length; row += BytesPerRow)
        {
            var absRowStart = regionStart + row;

            // Address column
            para.Inlines.Add(new Run($"{absRowStart:X8}  ") { Foreground = AddressBrush });

            // Hex columns
            for (var col = 0; col < BytesPerRow; col++)
            {
                var byteIndex = row + col;
                if (byteIndex < length)
                {
                    var absPos = regionStart + byteIndex;
                    var b = fileBytes[absPos];
                    var isHl = absPos >= hlAbsStart && absPos < hlAbsEnd;

                    var run = new Run($"{b:X2} ")
                    {
                        Foreground = isHl ? HighlightFg : NormalHexBrush,
                        Background = isHl ? HighlightBg : null,
                    };
                    para.Inlines.Add(run);
                }
                else
                {
                    para.Inlines.Add(new Run("   ") { Foreground = NormalHexBrush });
                }

                if (col == 7)
                {
                    para.Inlines.Add(new Run(" ") { Foreground = SeparatorBrush });
                }
            }

            para.Inlines.Add(new Run(" ") { Foreground = SeparatorBrush });

            // ASCII column
            for (var col = 0; col < BytesPerRow; col++)
            {
                var byteIndex = row + col;
                if (byteIndex >= length)
                {
                    break;
                }

                var absPos = regionStart + byteIndex;
                var b = fileBytes[absPos];
                var ch = b is >= 0x20 and < 0x7F ? (char)b : '.';
                var isHl = absPos >= hlAbsStart && absPos < hlAbsEnd;

                var run = new Run(ch.ToString())
                {
                    Foreground = isHl ? HighlightFg : NormalAsciiBrush,
                    Background = isHl ? HighlightBg : null,
                };
                para.Inlines.Add(run);
            }

            para.Inlines.Add(new LineBreak());
        }

        if (regionEnd - regionStart > MaxBytes)
        {
            para.Inlines.Add(new LineBreak());
            para.Inlines.Add(new Run($"... showing first {MaxBytes:N0} of {regionEnd - regionStart:N0} bytes ...")
            {
                Foreground = AddressBrush,
                FontStyle = FontStyles.Italic,
            });
        }

        doc.Blocks.Add(para);
        return doc;
    }
}
