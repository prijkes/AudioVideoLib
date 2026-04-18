namespace AudioVideoLib.Studio.Analysis;

using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

public sealed class BitrateChart : FrameworkElement
{
    private static readonly Brush BgBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)));
    private static readonly Brush GridBrush = Freeze(new SolidColorBrush(Color.FromArgb(0x33, 0x88, 0x88, 0x88)));
    private static readonly Pen LinePen = FrozenPen(new SolidColorBrush(Color.FromRgb(0x37, 0x94, 0xFF)), 1.5);
    private static readonly Pen AxisPen = FrozenPen(new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), 1);
    private static readonly Brush LabelBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)));

    private IReadOnlyList<int> _bitrates = [];
    private int _min;
    private int _max;

    public void SetBitrates(IReadOnlyList<int> bitrates)
    {
        _bitrates = bitrates ?? [];
        _min = int.MaxValue;
        _max = 0;
        foreach (var b in _bitrates)
        {
            if (b < _min)
            {
                _min = b;
            }

            if (b > _max)
            {
                _max = b;
            }
        }

        if (_bitrates.Count == 0)
        {
            _min = 0;
        }

        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var w = ActualWidth;
        var h = ActualHeight;
        dc.DrawRectangle(BgBrush, null, new Rect(0, 0, w, h));

        if (_bitrates.Count == 0 || _max == 0)
        {
            dc.DrawText(MakeText("(no frame data)"), new Point(12, h / 2));
            return;
        }

        const double marginLeft = 56;
        const double marginTop = 10;
        const double marginRight = 10;
        const double marginBottom = 22;
        var plotW = w - marginLeft - marginRight;
        var plotH = h - marginTop - marginBottom;
        if (plotW < 10 || plotH < 10)
        {
            return;
        }

        // Y axis labels (min, max, mid)
        dc.DrawText(MakeText($"{_max} kbps"), new Point(4, marginTop - 4));
        dc.DrawText(MakeText($"{_min} kbps"), new Point(4, marginTop + plotH - 14));
        if (_max != _min)
        {
            dc.DrawText(MakeText($"{(_min + _max) / 2} kbps"), new Point(4, marginTop + (plotH / 2) - 7));
        }

        // Axis lines
        dc.DrawLine(AxisPen, new Point(marginLeft, marginTop), new Point(marginLeft, marginTop + plotH));
        dc.DrawLine(AxisPen, new Point(marginLeft, marginTop + plotH), new Point(marginLeft + plotW, marginTop + plotH));

        // Grid: horizontal lines at 25/50/75%
        for (var i = 1; i < 4; i++)
        {
            var y = marginTop + (plotH * i / 4.0);
            dc.DrawLine(new Pen(GridBrush, 1), new Point(marginLeft, y), new Point(marginLeft + plotW, y));
        }

        // X axis range label
        dc.DrawText(MakeText($"0"), new Point(marginLeft - 4, marginTop + plotH + 4));
        dc.DrawText(MakeText($"{_bitrates.Count:N0} frames"), new Point(marginLeft + plotW - 70, marginTop + plotH + 4));

        // Data line
        var range = (double)(_max - _min);
        if (range < 1)
        {
            range = 1;
        }

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            for (var i = 0; i < _bitrates.Count; i++)
            {
                var x = marginLeft + (plotW * i / (double)System.Math.Max(1, _bitrates.Count - 1));
                var normalized = (_bitrates[i] - _min) / range;
                var y = marginTop + plotH - (plotH * normalized);
                if (i == 0)
                {
                    ctx.BeginFigure(new Point(x, y), false, false);
                }
                else
                {
                    ctx.LineTo(new Point(x, y), true, false);
                }
            }
        }

        geometry.Freeze();
        dc.DrawGeometry(null, LinePen, geometry);
    }

    private static FormattedText MakeText(string s) =>
        new(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Consolas"), 11, LabelBrush,
            VisualTreeHelper.GetDpi(new Canvas()).PixelsPerDip);

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
}

// Dummy canvas used only to query DPI — the actual FrameworkElement is drawn in OnRender above.
file sealed class Canvas : System.Windows.Controls.Canvas;
