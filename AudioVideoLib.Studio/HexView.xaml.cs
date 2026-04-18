namespace AudioVideoLib.Studio;

using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Win32;

public partial class HexView : UserControl
{
    public HexView()
    {
        InitializeComponent();
        Scroller.ScrollChanged += (_, _) => Canvas.InvalidateVisual();
        Canvas.ByteClicked += (s, offset) => ByteClicked?.Invoke(this, offset);
    }

    public event EventHandler<long>? ByteClicked;

    public void Clear()
    {
        Canvas.ClearContent();
    }

    public void SetContent(byte[] bytes, long viewStart, long viewEnd,
        long? highlightStart = null, int? highlightLength = null,
        System.Collections.Generic.IReadOnlyList<HexSubRange>? subRanges = null)
    {
        if (bytes == null || bytes.Length == 0 || viewEnd <= viewStart)
        {
            Canvas.ClearContent();
            return;
        }

        Canvas.SetContent(bytes, viewStart, viewEnd, highlightStart, highlightLength);
        Canvas.SetSubRanges(subRanges);

        if (highlightStart.HasValue)
        {
            Dispatcher.BeginInvoke(new Action(() => Canvas.ScrollToOffset(highlightStart.Value)),
                System.Windows.Threading.DispatcherPriority.Background);
        }
        else
        {
            Scroller.ScrollToHome();
        }
    }

    public HexCanvas InnerCanvas => Canvas;

    private void HexContextMenu_Opening(object sender, ContextMenuEventArgs e)
    {
        if (Canvas.Bytes == null || Canvas.SelectionLength <= 0)
        {
            e.Handled = true;
            return;
        }

        var selStart = Canvas.SelectionStart;
        var selLen = (int)Canvas.SelectionLength;
        var bytes = new byte[selLen];
        Buffer.BlockCopy(Canvas.Bytes, (int)selStart, bytes, 0, selLen);

        var menu = new ContextMenu();

        var header = new MenuItem
        {
            Header = $"0x{selStart:X8}..0x{selStart + selLen:X8}  ({selLen:N0} bytes)",
            IsEnabled = false,
        };
        menu.Items.Add(header);
        menu.Items.Add(new Separator());

        AddCopy(menu, "Copy as hex", Convert.ToHexString(bytes));
        AddCopy(menu, "Copy as spaced hex", FormatSpacedHex(bytes));
        AddCopy(menu, "Copy as ASCII", FormatAscii(bytes));
        AddCopy(menu, "Copy as base64", Convert.ToBase64String(bytes));
        AddCopy(menu, $"Copy offset (0x{selStart:X8})", $"0x{selStart:X8}");

        menu.Items.Add(new Separator());

        var save = new MenuItem { Header = "Save selection to file..." };
        save.Click += (_, _) => SaveToFile(bytes, selStart);
        menu.Items.Add(save);

        Scroller.ContextMenu = menu;
    }

    private static void AddCopy(ContextMenu menu, string header, string text)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => SetClipboard(text);
        menu.Items.Add(item);
    }

    private static void SetClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch
        {
            // clipboard contention
        }
    }

    private static string FormatSpacedHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 3);
        for (var i = 0; i < bytes.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(' ');
            }

            sb.Append($"{bytes[i]:X2}");
        }

        return sb.ToString();
    }

    private static string FormatAscii(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length);
        foreach (var b in bytes)
        {
            sb.Append(b is >= 0x20 and < 0x7F ? (char)b : '.');
        }

        return sb.ToString();
    }

    private static void SaveToFile(byte[] bytes, long startOffset)
    {
        var dlg = new SaveFileDialog
        {
            Title = "Save selection",
            FileName = $"hex_0x{startOffset:X8}_{bytes.Length}bytes.bin",
            Filter = "Binary|*.bin|All files|*.*",
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                File.WriteAllBytes(dlg.FileName, bytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not write file:\n\n{ex.Message}", "Save",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
