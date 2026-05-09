namespace AudioVideoLib.Studio;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using AudioVideoLib.Tags;

public partial class EditApeItemDialog : Window
{
    private ApeItem? _item;

    public EditApeItemDialog()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ValueBox.Focus();
            ValueBox.SelectAll();
        };
    }

    /// <summary>
    /// Opens the editor for an APEv2 text or locator item and writes user changes
    /// back to the item on OK. Returns true if the user accepted; false on cancel
    /// or if the item type is unsupported (e.g., binary).
    /// </summary>
    public static bool Edit(Window owner, ApeItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item is not (ApeUtf8Item or ApeLocatorItem))
        {
            return false;
        }

        var dlg = new EditApeItemDialog { Owner = owner };
        dlg.LoadItem(item);
        return dlg.ShowDialog() == true;
    }

    private void LoadItem(ApeItem item)
    {
        _item = item;

        var isLocator = item is ApeLocatorItem;
        Title = isLocator ? "Edit APE locator item" : "Edit APE text item";
        ValueLabel.Text = isLocator ? "URIs (one per line)" : "Values (one per line)";
        HintText.Text = isLocator
            ? "Each non-empty line is stored as a separate URI (validated per RFC 2396 / RFC 2732). APEv2 separates multi-values internally with a NUL byte."
            : "Each non-empty line is stored as a separate UTF-8 value (APEv2 separates multi-values internally with a NUL byte).";

        KeyBox.Text = item.Key;

        var values = GetValues(item) ?? [];
        ValueBox.Text = string.Join(Environment.NewLine, values);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (_item is null)
        {
            DialogResult = false;
            return;
        }

        var lines = (ValueBox.Text ?? string.Empty)
            .Split('\n')
            .Select(s => s.TrimEnd('\r'))
            .Where(s => s.Length > 0)
            .ToList();

        if (_item is ApeLocatorItem)
        {
            // Validate up front so we don't half-mutate before failing on the second line.
            foreach (var line in lines)
            {
                if (!Uri.IsWellFormedUriString(line, UriKind.RelativeOrAbsolute))
                {
                    MessageBox.Show(this, $"Invalid URI:\n\n{line}", "Edit APE item",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
        }

        var values = GetValues(_item);
        if (values is null)
        {
            DialogResult = false;
            return;
        }

        if (SequenceEqual(values, lines))
        {
            DialogResult = false;
            return;
        }

        values.Clear();
        foreach (var line in lines)
        {
            values.Add(line);
        }

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static IList<string>? GetValues(ApeItem item) => item switch
    {
        ApeLocatorItem loc => loc.Values,
        ApeUtf8Item utf8 => utf8.Values,
        _ => null,
    };

    private static bool SequenceEqual(IList<string> a, IList<string> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }
        for (var i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }
}
