namespace AudioVideoLib.Studio;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using Microsoft.Win32;

public partial class MainWindow : Window
{
    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".flac", ".m4a", ".ogg", ".wav",
    };

    public ObservableCollection<AudioFileEntry> Files { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        FileGrid.ItemsSource = Files;
        UpdateStatus("Ready");

        InputBindings.Add(new KeyBinding(new RelayCommand(_ => OpenFiles()), Key.O, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => OpenFolder()), Key.O, ModifierKeys.Control | ModifierKeys.Shift));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => SaveSelected()), Key.S, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => SaveAll()), Key.S, ModifierKeys.Control | ModifierKeys.Shift));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => RemoveSelected()), Key.Delete, ModifierKeys.None));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => FileGrid.SelectAll()), Key.A, ModifierKeys.Control));
    }

    private void OpenFiles_Click(object sender, RoutedEventArgs e) => OpenFiles();
    private void OpenFolder_Click(object sender, RoutedEventArgs e) => OpenFolder();
    private void Save_Click(object sender, RoutedEventArgs e) => SaveSelected();
    private void SaveAll_Click(object sender, RoutedEventArgs e) => SaveAll();
    private void Remove_Click(object sender, RoutedEventArgs e) => RemoveSelected();
    private void SelectAll_Click(object sender, RoutedEventArgs e) => FileGrid.SelectAll();
    private void Revert_Click(object sender, RoutedEventArgs e) => RevertSelected();
    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            this,
            "AudioVideoLib Studio\n\n" +
            "Tag editor built on AudioVideoLib.\n" +
            "Supported formats: MP3 (ID3v1 / ID3v2), FLAC (read-only).\n",
            "About",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OpenFiles()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Audio files|*.mp3;*.flac;*.m4a;*.ogg;*.wav|All files|*.*",
            Multiselect = true,
            Title = "Open audio files",
        };
        if (dlg.ShowDialog(this) == true)
        {
            LoadFiles(dlg.FileNames);
        }
    }

    private void OpenFolder()
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Open audio folder",
        };
        if (dlg.ShowDialog(this) == true)
        {
            var files = Directory
                .EnumerateFiles(dlg.FolderName, "*.*", SearchOption.AllDirectories)
                .Where(p => AudioExtensions.Contains(Path.GetExtension(p)))
                .ToList();
            LoadFiles(files);
        }
    }

    private void LoadFiles(IEnumerable<string> paths)
    {
        Files.Clear();
        var loaded = 0;
        var failed = 0;
        foreach (var path in paths)
        {
            try
            {
                Files.Add(AudioFileEntry.Load(path));
                loaded++;
            }
            catch (Exception ex)
            {
                failed++;
                System.Diagnostics.Debug.WriteLine($"load failed {path}: {ex.Message}");
            }
        }

        if (Files.Count > 0)
        {
            FileGrid.SelectedIndex = 0;
        }

        UpdateStatus(failed == 0
            ? $"Loaded {loaded} file(s)"
            : $"Loaded {loaded} file(s), {failed} failed");
    }

    private void SaveSelected()
    {
        var selected = FileGrid.SelectedItems.OfType<AudioFileEntry>().Where(f => f.IsModified).ToList();
        if (selected.Count == 0)
        {
            UpdateStatus("Nothing to save");
            return;
        }

        var saved = SaveEntries(selected);
        UpdateStatus($"Saved {saved} file(s)");
    }

    private void SaveAll()
    {
        var pending = Files.Where(f => f.IsModified).ToList();
        if (pending.Count == 0)
        {
            UpdateStatus("Nothing to save");
            return;
        }

        var saved = SaveEntries(pending);
        UpdateStatus($"Saved {saved} of {pending.Count} file(s)");
    }

    private int SaveEntries(IEnumerable<AudioFileEntry> entries)
    {
        var saved = 0;
        var errors = new List<string>();
        foreach (var entry in entries)
        {
            try
            {
                entry.Save();
                saved++;
            }
            catch (Exception ex)
            {
                errors.Add($"{entry.FileName}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            MessageBox.Show(
                this,
                $"Failed to save {errors.Count} file(s):\n\n{string.Join("\n", errors.Take(10))}",
                "Save errors",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        return saved;
    }

    private void RemoveSelected()
    {
        var selected = FileGrid.SelectedItems.OfType<AudioFileEntry>().ToList();
        foreach (var entry in selected)
        {
            Files.Remove(entry);
        }
        UpdateStatus($"Removed {selected.Count} file(s)");
    }

    private void RevertSelected()
    {
        var selected = FileGrid.SelectedItems.OfType<AudioFileEntry>().Where(f => f.IsModified).ToList();
        foreach (var entry in selected)
        {
            entry.Revert();
        }
        UpdateStatus($"Reverted {selected.Count} file(s)");
    }

    private void UpdateStatus(string prefix)
    {
        var modified = Files.Count(f => f.IsModified);
        StatusText.Text = modified > 0
            ? $"{prefix}   •   {Files.Count} file(s), {modified} unsaved"
            : $"{prefix}   •   {Files.Count} file(s)";
    }

    private sealed class RelayCommand(Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
