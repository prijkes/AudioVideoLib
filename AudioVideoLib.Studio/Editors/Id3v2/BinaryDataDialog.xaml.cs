namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using System.IO;
using System.Windows;

using AudioVideoLib.Tags;

using Microsoft.Win32;

public partial class BinaryDataDialog : Window
{
    // Runs on OK click before the dialog closes. If it throws InvalidDataException
    // the dialog stays open and the user gets a chance to fix the input. Setters
    // are ordered so the throwing one runs first, avoiding partial mutation on retry.
    private Action? _commit;

    public BinaryDataDialog()
    {
        InitializeComponent();
        // Refresh the size readout whenever the underlying byte buffer changes
        // (typing in hex/ASCII columns mutates HexEditor.Data via the DP).
        var dpd = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
            Controls.HexEditor.DataProperty, typeof(Controls.HexEditor));
        dpd?.AddValueChanged(HexEditor, (_, _) => UpdateInfoText());
    }

    internal static bool EditPriv(Window owner, Id3v2PrivateFrame frame)
    {
        var dlg = new BinaryDataDialog
        {
            Owner = owner,
            Title = "Private frame (PRIV)",
        };
        dlg.OwnerLabel.Text = "Owner identifier (URL)";
        dlg.OwnerBox.Text = frame.OwnerIdentifier ?? string.Empty;
        dlg.HexEditor.Data = frame.PrivateData ?? [];
        dlg.UpdateInfoText();
        dlg._commit = () =>
        {
            frame.OwnerIdentifier = dlg.OwnerBox.Text ?? string.Empty;
            frame.PrivateData = dlg.HexEditor.Data;
        };

        return dlg.ShowDialog() == true;
    }

    internal static bool EditUfid(Window owner, Id3v2UniqueFileIdentifierFrame frame)
    {
        var dlg = new BinaryDataDialog
        {
            Owner = owner,
            Title = "Unique file identifier (UFID)",
        };
        dlg.OwnerLabel.Text = "Owner identifier";
        dlg.OwnerBox.Text = frame.OwnerIdentifier ?? string.Empty;
        dlg.HexEditor.Data = frame.IdentifierData ?? [];
        dlg.HexEditor.MaxLength = 64;     // ID3v2 spec §4.1: max 64 bytes
        dlg.UpdateInfoText();
        dlg._commit = () =>
        {
            frame.OwnerIdentifier = dlg.OwnerBox.Text ?? string.Empty;
            frame.IdentifierData = dlg.HexEditor.Data;
        };

        return dlg.ShowDialog() == true;
    }

    internal static bool EditMcdi(Window owner, Id3v2MusicCdIdentifierFrame frame)
    {
        var dlg = new BinaryDataDialog
        {
            Owner = owner,
            Title = "Music CD identifier (MCDI)",
        };
        dlg.OwnerPanel.Visibility = Visibility.Collapsed;
        dlg.HexEditor.Data = frame.TableOfContents ?? [];
        dlg.HexEditor.MaxLength = 804;    // ID3v2 spec §4.5: 4-byte header + 8 bytes/track, max 804
        dlg.UpdateInfoText();
        dlg._commit = () => frame.TableOfContents = dlg.HexEditor.Data;

        return dlg.ShowDialog() == true;
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
            UpdateInfoText();
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
        UpdateInfoText();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _commit?.Invoke();
        }
        catch (InvalidDataException ex)
        {
            MessageBox.Show(this, ex.Message, "Invalid input",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void UpdateInfoText()
    {
        var len = HexEditor.Data.Length;
        var max = HexEditor.MaxLength;
        InfoText.Text = max is { } m
            ? len == 0 ? $"No data (max {m:N0} bytes)" : $"{len:N0} / {m:N0} bytes"
            : len == 0 ? "No data." : $"{len:N0} bytes";
    }
}
