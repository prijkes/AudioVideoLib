namespace AudioVideoLib.Studio.Batch;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.Win32;

public partial class BatchDialog : Window
{
    private List<BatchRow> _rows = [];

    public BatchDialog()
    {
        InitializeComponent();
    }

    private void Pick_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Choose a folder to scan",
        };
        if (dlg.ShowDialog(this) == true)
        {
            FolderBox.Text = dlg.FolderName;
        }
    }

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        var folder = FolderBox.Text;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            MessageBox.Show(this, "Pick a valid folder first.", "Batch scan",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SummaryText.Text = "Scanning...";
        Grid.ItemsSource = null;
        _rows = [];

        var recursive = RecursiveBox.IsChecked == true;
        var files = BatchScanner.EnumerateFiles(folder, recursive).ToList();
        var rows = new List<BatchRow>();

        await Task.Run(() =>
        {
            var i = 0;
            foreach (var file in files)
            {
                var row = BatchScanner.ScanFile(file);
                rows.Add(row);
                i++;
                if (i % 20 == 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        SummaryText.Text = $"Scanning... {i}/{files.Count}";
                    });
                }
            }
        });

        _rows = rows;
        Grid.ItemsSource = _rows;
        var totalErrors = _rows.Sum(r => r.Errors);
        var totalWarnings = _rows.Sum(r => r.Warnings);
        var failed = _rows.Count(r => r.Error != null);
        SummaryText.Text = $"Scanned {_rows.Count} files. {totalErrors} errors, {totalWarnings} warnings, {failed} failed to read.";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_rows.Count == 0)
        {
            MessageBox.Show(this, "Run a scan first.", "Batch scan",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new SaveFileDialog
        {
            Title = "Save batch report",
            FileName = "batch-report",
            Filter = "CSV|*.csv|JSON|*.json|All files|*.*",
        };

        if (dlg.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var content = dlg.FilterIndex == 2
                ? BatchScanner.ToJson(_rows)
                : BatchScanner.ToCsv(_rows);
            File.WriteAllText(dlg.FileName, content);
            SummaryText.Text = $"Saved {_rows.Count} rows to {System.IO.Path.GetFileName(dlg.FileName)}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not save:\n\n{ex.Message}", "Save report",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
