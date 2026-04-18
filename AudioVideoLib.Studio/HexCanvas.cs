namespace AudioVideoLib.Studio;

using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media;

public sealed record HexSubRange(long Start, long End, Brush Tint, string Label);

public sealed class HexCanvas : FrameworkElement
{
    private const int AddressChars = 10;
    private const int GapChars = 3;
    private const double Padding = 6;

    private static readonly Typeface Mono = new("Consolas");

    private int _bytesPerRow = 16;
    private int _hexChars = 49;
    private int _asciiChars = 16;
    private int _totalChars = 10 + 49 + 3 + 16;
    private double _fontSize = 11;

    public HexCanvas()
    {
        ApplySettings();
        AppSettings.Changed += (_, _) =>
        {
            ApplySettings();
            MeasureChar();
            InvalidateMeasure();
            InvalidateVisual();
        };
    }

    private void ApplySettings()
    {
        var s = AppSettings.Current;
        _bytesPerRow = s.HexBytesPerRow;
        _fontSize = s.HexFontSize;
        // Hex column: two chars per byte + one space between, plus one extra space at the midpoint (after byte n/2).
        _hexChars = (_bytesPerRow * 3) + 1;
        _asciiChars = _bytesPerRow;
        _totalChars = AddressChars + _hexChars + GapChars + _asciiChars;
    }

    private static readonly Brush BgBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)));
    private static readonly Brush AddressBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x6A, 0x99, 0x55)));
    private static readonly Brush HexBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)));
    private static readonly Brush AsciiBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x9C, 0xDC, 0xFE)));
    private static readonly Brush HighlightBg = Freeze(new SolidColorBrush(Color.FromRgb(0xFF, 0xB8, 0x2E)));
    private static readonly Brush HighlightFg = Brushes.Black;
    private static readonly Brush SelectionBg = Freeze(new SolidColorBrush(Color.FromRgb(0x26, 0x4F, 0x78)));
    private static readonly Pen SelectionBorder = FrozenPen(new SolidColorBrush(Color.FromRgb(0x3A, 0x7F, 0xD5)), 1);

    private static Brush Freeze(SolidColorBrush b)
    {
        b.Freeze();
        return b;
    }

    private static Pen FrozenPen(SolidColorBrush b, double thickness)
    {
        b.Freeze();
        var p = new Pen(b, thickness);
        p.Freeze();
        return p;
    }

    private long _viewStart;
    private long _viewEnd;
    private long _hlStart = -1;
    private long _hlEnd = -1;

    private long _selAnchor = -1;
    private long _downOffset = -1;
    private bool _wasDragged;

    private IReadOnlyList<HexSubRange> _subRanges = [];

    private double _charWidth;
    private double _rowHeight;
    private double _dpi = 1;

    public event EventHandler? SelectionChanged;

    public event EventHandler<long>? ByteClicked;

    public byte[]? Bytes { get; private set; }

    public long SelectionStart { get; private set; } = -1;

    public long SelectionEnd { get; private set; } = -1;

    public long SelectionLength => SelectionEnd > SelectionStart ? SelectionEnd - SelectionStart : 0;

    public void SetContent(byte[]? bytes, long viewStart, long viewEnd, long? hlStart, int? hlLength)
    {
        Bytes = bytes;
        _viewStart = viewStart;
        _viewEnd = Math.Min(viewEnd, bytes?.Length ?? 0);
        _hlStart = hlStart ?? -1;
        _hlEnd = _hlStart + (hlLength ?? 0);
        _selAnchor = SelectionStart = SelectionEnd = -1;
        MeasureChar();
        InvalidateMeasure();
        InvalidateVisual();
    }

    public void ClearContent()
    {
        Bytes = null;
        _selAnchor = SelectionStart = SelectionEnd = -1;
        _subRanges = [];
        InvalidateMeasure();
        InvalidateVisual();
    }

    public void SetSubRanges(IReadOnlyList<HexSubRange>? ranges)
    {
        _subRanges = ranges ?? [];
        InvalidateVisual();
    }

    private int RowCount
    {
        get
        {
            if (Bytes == null)
            {
                return 0;
            }

            var length = _viewEnd - _viewStart;
            return length <= 0 ? 0 : (int)((length + _bytesPerRow - 1) / _bytesPerRow);
        }
    }

    private void MeasureChar()
    {
        _dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        if (_dpi == 0)
        {
            _dpi = 1;
        }

        var ft = new FormattedText("M", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            Mono, _fontSize, HexBrush, _dpi);
        _charWidth = ft.Width;
        _rowHeight = Math.Max(ft.Height, 14);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_charWidth == 0)
        {
            MeasureChar();
        }

        var w = (Padding * 2) + (_totalChars * _charWidth);
        var h = (RowCount + 1) * _rowHeight;
        return new Size(w, Math.Max(h, 1));
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (_charWidth == 0)
        {
            MeasureChar();
        }

        var totalW = (Padding * 2) + (_totalChars * _charWidth);
        var totalH = (RowCount + 1) * _rowHeight;
        dc.DrawRectangle(BgBrush, null, new Rect(0, 0, totalW, totalH));

        if (Bytes == null || RowCount == 0)
        {
            return;
        }

        // Determine visible rows from parent ScrollViewer.
        var scrollViewer = FindParent<ScrollViewer>(this);
        var vOffset = 0.0;
        var vpHeight = totalH;
        if (scrollViewer != null)
        {
            vOffset = scrollViewer.VerticalOffset;
            vpHeight = scrollViewer.ViewportHeight;
        }

        var firstVisible = Math.Max(0, (int)((vOffset / _rowHeight) - 1) - 1);
        var lastVisible = Math.Min(RowCount, (int)(((vOffset + vpHeight) / _rowHeight) - 1) + 2);

        // Header row
        if (vOffset < _rowHeight)
        {
            DrawHeaderRow(dc, 0);
        }

        for (var i = firstVisible; i < lastVisible; i++)
        {
            DrawDataRow(dc, i, (i + 1) * _rowHeight);
        }
    }

    private void DrawHeaderRow(DrawingContext dc, double y)
    {
        var sb = new StringBuilder("Offset    ");
        for (var c = 0; c < _bytesPerRow; c++)
        {
            sb.Append($"{c:X2} ");
            if (c == (_bytesPerRow / 2) - 1)
            {
                sb.Append(' ');
            }
        }

        sb.Append("   ASCII");
        dc.DrawText(MakeText(sb.ToString(), AddressBrush), new Point(Padding, y));
    }

    private void DrawDataRow(DrawingContext dc, int rowIndex, double y)
    {
        var rowStart = _viewStart + ((long)rowIndex * _bytesPerRow);
        var rowEnd = Math.Min(rowStart + _bytesPerRow, _viewEnd);
        var rowLen = (int)(rowEnd - rowStart);

        var hexX = Padding + (AddressChars * _charWidth);
        var asciiX = Padding + ((AddressChars + _hexChars + GapChars) * _charWidth);

        // Sub-range tints (child layout under selected parent).
        if (_subRanges.Count > 0)
        {
            for (var col = 0; col < rowLen; col++)
            {
                var absPos = rowStart + col;
                var tint = FindSubRangeTint(absPos);
                if (tint == null)
                {
                    continue;
                }

                var hexCol = (col * 3) + (col >= _bytesPerRow / 2 ? 1 : 0);
                var hx = hexX + (hexCol * _charWidth);
                dc.DrawRectangle(tint, null, new Rect(hx, y, _charWidth * 3, _rowHeight));

                var ax = asciiX + (col * _charWidth);
                dc.DrawRectangle(tint, null, new Rect(ax, y, _charWidth, _rowHeight));
            }
        }

        // Draw selection + highlight backgrounds on top of tints.
        for (var col = 0; col < rowLen; col++)
        {
            var absPos = rowStart + col;
            var isHl = absPos >= _hlStart && absPos < _hlEnd;
            var isSel = absPos >= SelectionStart && absPos < SelectionEnd;

            if (!isHl && !isSel)
            {
                continue;
            }

            var bg = isHl ? HighlightBg : SelectionBg;

            var hexCol = (col * 3) + (col >= _bytesPerRow / 2 ? 1 : 0);
            var hx = hexX + (hexCol * _charWidth);
            dc.DrawRectangle(bg, null, new Rect(hx, y, _charWidth * 2, _rowHeight));

            var ax = asciiX + (col * _charWidth);
            dc.DrawRectangle(bg, null, new Rect(ax, y, _charWidth, _rowHeight));
        }

        // Address
        dc.DrawText(MakeText($"{rowStart:X8}  ", AddressBrush), new Point(Padding, y));

        // Build hex and ASCII strings as one pass, applying per-char foreground overrides.
        var hexSb = new StringBuilder(50);
        var asciiSb = new StringBuilder(_bytesPerRow);
        for (var col = 0; col < _bytesPerRow; col++)
        {
            if (col < rowLen)
            {
                var b = Bytes![rowStart + col];
                hexSb.Append($"{b:X2} ");
                asciiSb.Append(b is >= 0x20 and < 0x7F ? (char)b : '.');
            }
            else
            {
                hexSb.Append("   ");
            }

            if (col == (_bytesPerRow / 2) - 1)
            {
                hexSb.Append(' ');
            }
        }

        var hexFt = MakeText(hexSb.ToString(), HexBrush);
        var asciiFt = MakeText(asciiSb.ToString(), AsciiBrush);

        // Highlight foreground overrides.
        for (var col = 0; col < rowLen; col++)
        {
            var absPos = rowStart + col;
            var isHl = absPos >= _hlStart && absPos < _hlEnd;
            if (!isHl)
            {
                continue;
            }

            var hexChar = (col * 3) + (col >= _bytesPerRow / 2 ? 1 : 0);
            hexFt.SetForegroundBrush(HighlightFg, hexChar, 2);
            if (col < asciiSb.Length)
            {
                asciiFt.SetForegroundBrush(HighlightFg, col, 1);
            }
        }

        dc.DrawText(hexFt, new Point(hexX, y));
        dc.DrawText(asciiFt, new Point(asciiX, y));
    }

    private FormattedText MakeText(string text, Brush fg) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Mono, _fontSize, fg, _dpi);

    private Brush? FindSubRangeTint(long offset)
    {
        foreach (var r in _subRanges)
        {
            if (offset >= r.Start && offset < r.End)
            {
                return r.Tint;
            }
        }

        return null;
    }

    // ── Mouse handling ──────────────────────────────────────────────

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(this);
        var byteOffset = HitTestByte(pos);
        if (byteOffset < 0)
        {
            return;
        }

        // If right-clicking outside current selection, select just that byte.
        if (byteOffset < SelectionStart || byteOffset >= SelectionEnd)
        {
            _selAnchor = byteOffset;
            SelectionStart = byteOffset;
            SelectionEnd = byteOffset + 1;
            InvalidateVisual();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        Focus();
        var pos = e.GetPosition(this);
        var byteOffset = HitTestByte(pos);
        if (byteOffset < 0)
        {
            return;
        }

        _selAnchor = byteOffset;
        SelectionStart = byteOffset;
        SelectionEnd = byteOffset + 1;
        _downOffset = byteOffset;
        _wasDragged = false;
        CaptureMouse();
        InvalidateVisual();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!IsMouseCaptured || _selAnchor < 0)
        {
            return;
        }

        var pos = e.GetPosition(this);
        var byteOffset = HitTestByte(pos);
        if (byteOffset < 0)
        {
            return;
        }

        if (byteOffset != _downOffset)
        {
            _wasDragged = true;
        }

        if (byteOffset >= _selAnchor)
        {
            SelectionStart = _selAnchor;
            SelectionEnd = byteOffset + 1;
        }
        else
        {
            SelectionStart = byteOffset;
            SelectionEnd = _selAnchor + 1;
        }

        InvalidateVisual();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
            e.Handled = true;
        }

        if (!_wasDragged && _downOffset >= 0)
        {
            ByteClicked?.Invoke(this, _downOffset);
        }

        _downOffset = -1;
        _wasDragged = false;
    }

    private long HitTestByte(Point pos)
    {
        if (Bytes == null || _charWidth == 0)
        {
            return -1;
        }

        var row = (int)((pos.Y / _rowHeight) - 1);
        if (row < 0 || row >= RowCount)
        {
            return -1;
        }

        var x = pos.X - Padding;
        var hexStart = AddressChars * _charWidth;
        var asciiStart = (AddressChars + _hexChars + GapChars) * _charWidth;
        var asciiEnd = asciiStart + (_asciiChars * _charWidth);

        int col;
        if (x >= hexStart && x < hexStart + (_hexChars * _charWidth))
        {
            var relX = x - hexStart;
            var charPos = (int)(relX / _charWidth);
            // Account for the gap space at the midrow separator (after byte _bytesPerRow/2 - 1).
            var midGap = _bytesPerRow / 2 * 3;
            col = charPos < midGap ? charPos / 3 : (charPos - 1) / 3;
        }
        else if (x >= asciiStart && x < asciiEnd)
        {
            col = (int)((x - asciiStart) / _charWidth);
        }
        else
        {
            return -1;
        }

        if (col < 0 || col >= _bytesPerRow)
        {
            return -1;
        }

        var rowStart = _viewStart + ((long)row * _bytesPerRow);
        var absPos = rowStart + col;
        return absPos < _viewEnd ? absPos : -1;
    }

    public void ScrollToOffset(long offset)
    {
        if (_charWidth == 0 || Bytes == null)
        {
            return;
        }

        var row = (int)((offset - _viewStart) / _bytesPerRow);
        var scrollViewer = FindParent<ScrollViewer>(this);
        scrollViewer?.ScrollToVerticalOffset(row * _rowHeight);
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is T t)
            {
                return t;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }
}
