namespace AudioVideoLib.Studio;

using System;
using System.IO;
using System.Windows;

using AudioVideoLib.Tags;

using Microsoft.Win32;

public partial class BinaryDataDialog : Window
{
    private byte[] _data = [];

    public BinaryDataDialog()
    {
        InitializeComponent();
    }

    public static bool Edit(Window owner, Id3v2PrivateFrame frame)
    {
        var dlg = new BinaryDataDialog
        {
            Owner = owner,
            Title = "Private frame (PRIV)",
        };
        dlg.OwnerLabel.Text = "Owner identifier (URL or email)";
        dlg.OwnerBox.Text = frame.OwnerIdentifier ?? string.Empty;
        dlg._data = frame.PrivateData ?? [];
        dlg.RefreshHex();

        if (dlg.ShowDialog() != true)
        {
            return false;
        }

        frame.OwnerIdentifier = dlg.OwnerBox.Text ?? string.Empty;
        frame.PrivateData = dlg._data;
        return true;
    }

    public static bool Edit(Window owner, Id3v2UniqueFileIdentifierFrame frame)
    {
        var dlg = new BinaryDataDialog
        {
            Owner = owner,
            Title = "Unique file identifier (UFID)",
        };
        dlg.OwnerLabel.Text = "Owner identifier";
        dlg.OwnerBox.Text = frame.OwnerIdentifier ?? string.Empty;
        dlg._data = frame.IdentifierData ?? [];
        dlg.RefreshHex();

        if (dlg.ShowDialog() != true)
        {
            return false;
        }

        frame.OwnerIdentifier = dlg.OwnerBox.Text ?? string.Empty;
        frame.IdentifierData = dlg._data;
        return true;
    }

    public static bool Edit(Window owner, Id3v2MusicCdIdentifierFrame frame)
    {
        var dlg = new BinaryDataDialog
        {
            Owner = owner,
            Title = "Music CD identifier (MCDI)",
        };
        dlg.OwnerPanel.Visibility = Visibility.Collapsed;
        dlg._data = frame.TableOfContents ?? [];
        dlg.RefreshHex();

        if (dlg.ShowDialog() != true)
        {
            return false;
        }

        frame.TableOfContents = dlg._data;
        return true;
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
            _data = File.ReadAllBytes(dlg.FileName);
            RefreshHex();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not read file:\n\n{ex.Message}", "Load",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ClearData_Click(object sender, RoutedEventArgs e)
    {
        _data = [];
        RefreshHex();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void RefreshHex()
    {
        HexBox.Text = HexDumper.Dump(_data);
        InfoText.Text = _data.Length == 0 ? "No data." : $"{_data.Length:N0} bytes";
    }
}
