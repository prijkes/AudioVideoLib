namespace AudioVideoLib.Studio;

using System;
using System.Globalization;
using System.Text;
using System.Windows;

public partial class FindDialog : Window
{
    public FindDialog()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            PatternText.Focus();
            PatternText.SelectAll();
        };
    }

    public byte[]? Pattern { get; private set; }

    public string DisplayPattern { get; private set; } = string.Empty;

    public static (byte[] Pattern, string Display)? Ask(Window owner, string initial = "", bool initialHex = false)
    {
        var dlg = new FindDialog
        {
            Owner = owner,
        };
        dlg.PatternText.Text = initial;
        dlg.HexRadio.IsChecked = initialHex;
        dlg.AsciiRadio.IsChecked = !initialHex;

        return dlg.ShowDialog() == true && dlg.Pattern != null
            ? (dlg.Pattern, dlg.DisplayPattern)
            : null;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var raw = PatternText.Text;
        if (string.IsNullOrEmpty(raw))
        {
            MessageBox.Show(this, "Enter a pattern to search for.", "Find", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (HexRadio.IsChecked == true)
        {
            if (!TryParseHex(raw, out var bytes))
            {
                MessageBox.Show(this, "Hex pattern must be an even number of hex digits (spaces allowed).",
                    "Find", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Pattern = bytes;
            DisplayPattern = $"hex {Convert.ToHexString(bytes)}";
        }
        else
        {
            Pattern = Encoding.UTF8.GetBytes(raw);
            DisplayPattern = $"\"{raw}\"";
        }

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static bool TryParseHex(string input, out byte[] bytes)
    {
        bytes = [];
        var stripped = input.Replace(" ", string.Empty).Replace("-", string.Empty).Replace(":", string.Empty);
        if (stripped.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            stripped = stripped[2..];
        }

        if (stripped.Length == 0 || (stripped.Length % 2) != 0)
        {
            return false;
        }

        var result = new byte[stripped.Length / 2];
        for (var i = 0; i < result.Length; i++)
        {
            if (!byte.TryParse(stripped.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result[i]))
            {
                return false;
            }
        }

        bytes = result;
        return true;
    }
}
