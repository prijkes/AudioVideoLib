namespace AudioVideoLib.Studio;

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Win32;

public partial class AddApeItemDialog : Window
{
    public AddApeItemDialog()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            KeyBox.Focus();
        };

        var dpd = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
            Controls.HexEditor.DataProperty, typeof(Controls.HexEditor));
        dpd?.AddValueChanged(HexEditor, (_, _) => UpdateBinaryInfoText());
        UpdateBinaryInfoText();
    }

    public enum ApeItemKind
    {
        Text,
        Binary,
        Locator,
    }

    public ApeItemKind Kind { get; private set; } = ApeItemKind.Text;

    public string Key { get; private set; } = string.Empty;

    public string TextValue { get; private set; } = string.Empty;

    public byte[] BinaryData { get; private set; } = [];

    public string LocatorUri { get; private set; } = string.Empty;

    public static AddApeItemDialog? Show(Window owner)
    {
        var dlg = new AddApeItemDialog { Owner = owner };
        return dlg.ShowDialog() == true ? dlg : null;
    }

    private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TextPanel == null || BinaryPanel == null || LocatorPanel == null)
        {
            return;
        }

        var tag = (TypeCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "Text";
        TextPanel.Visibility    = tag == "Text"    ? Visibility.Visible : Visibility.Collapsed;
        BinaryPanel.Visibility  = tag == "Binary"  ? Visibility.Visible : Visibility.Collapsed;
        LocatorPanel.Visibility = tag == "Locator" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LoadFromFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select binary data",
            Filter = "All files|*.*",
        };
        if (dlg.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            HexEditor.Data = File.ReadAllBytes(dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not read file:\n\n{ex.Message}", "Load",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ClearData_Click(object sender, RoutedEventArgs e)
    {
        HexEditor.Data = [];
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var key = (KeyBox.Text ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(key))
        {
            MessageBox.Show(this, "Key is required.", "Add APE item",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            KeyBox.Focus();
            return;
        }

        var tag = (TypeCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "Text";
        Kind = tag switch
        {
            "Binary"  => ApeItemKind.Binary,
            "Locator" => ApeItemKind.Locator,
            _         => ApeItemKind.Text,
        };

        Key         = key;
        TextValue   = TextValueBox.Text ?? string.Empty;
        BinaryData  = HexEditor.Data ?? [];
        LocatorUri  = (LocatorBox.Text ?? string.Empty).Trim();

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void UpdateBinaryInfoText()
    {
        if (BinaryInfoText == null)
        {
            return;
        }

        var len = HexEditor.Data?.Length ?? 0;
        BinaryInfoText.Text = len == 0 ? "No data." : $"{len:N0} bytes";
    }
}
