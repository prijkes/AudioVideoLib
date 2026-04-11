namespace AudioVideoLib.Studio;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Win32;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".flac", ".m4a", ".ogg", ".wav",
    };

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        UpdateStatus("Ready");

        InputBindings.Add(new KeyBinding(new RelayCommand(_ => OpenFiles()),  Key.O,      ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => OpenFolder()), Key.O,      ModifierKeys.Control | ModifierKeys.Shift));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => SaveCurrent()), Key.S,     ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => CloseCurrentFile()), Key.Delete, ModifierKeys.None));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => RefreshCurrent()), Key.F5, ModifierKeys.None));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<FileEntry> Files { get; } = [];

    public FileEntry? Current
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
            OpenDossier(value);
        }
    }

    public FileDossier? CurrentDossier
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
            DossierPanel.DataContext = value;
        }
    }

    private void OpenDossier(FileEntry? entry)
    {
        if (entry == null)
        {
            CurrentDossier = null;
            UpdateStatus("Ready");
            return;
        }

        try
        {
            CurrentDossier = new FileDossier(entry.FilePath);
            UpdateStatus($"Loaded {entry.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to open {entry.FileName}:\n\n{ex.Message}", "Load error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            CurrentDossier = null;
        }
    }

    private void AddTagButton_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentDossier == null)
        {
            UpdateStatus("Open a file first");
            return;
        }

        AddTagMenu.Items.Clear();
        var addable = CurrentDossier.AddableTagKinds;
        if (addable.Count == 0)
        {
            AddTagMenu.Items.Add(new MenuItem { Header = "(all supported tag formats already present)", IsEnabled = false });
        }
        else
        {
            foreach (var kind in addable)
            {
                var item = new MenuItem
                {
                    Header = FileDossier.GetKindLabel(kind),
                    Tag = kind,
                };
                item.Click += AddTagMenuItem_Click;
                AddTagMenu.Items.Add(item);
            }
        }

        AddTagMenu.PlacementTarget = AddTagButton;
        AddTagMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        AddTagMenu.IsOpen = true;
    }

    private void AddTagMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: TagKind kind } && CurrentDossier != null)
        {
            var newTab = CurrentDossier.AddNewTag(kind);
            TagTabControl.SelectedItem = newTab;
            UpdateStatus($"Added {FileDossier.GetKindLabel(kind)}");
        }
    }

    private void OpenFiles_Click(object sender, RoutedEventArgs e) => OpenFiles();
    private void OpenFolder_Click(object sender, RoutedEventArgs e) => OpenFolder();
    private void Save_Click(object sender, RoutedEventArgs e) => SaveCurrent();
    private void CloseFile_Click(object sender, RoutedEventArgs e) => CloseCurrentFile();
    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshCurrent();
    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            this,
            "AudioVideoLib Studio\n\n" +
            "Single-file audio tag editor and technical inspector.\n" +
            "Editable: ID3v1, ID3v2 (2.3/2.4)\n" +
            "Read-only: APE, Lyrics3, MusicMatch, Vorbis/FLAC metadata",
            "About",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Current = FileList.SelectedItem as FileEntry;
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
        var dlg = new OpenFolderDialog { Title = "Open audio folder" };
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
                Files.Add(FileEntry.Load(path));
                loaded++;
            }
            catch
            {
                failed++;
            }
        }

        if (Files.Count > 0)
        {
            FileList.SelectedIndex = 0;
        }

        UpdateStatus(failed == 0
            ? $"Loaded {loaded} file(s)"
            : $"Loaded {loaded} file(s), {failed} failed");
    }

    private void SaveCurrent()
    {
        if (CurrentDossier == null)
        {
            UpdateStatus("Nothing to save");
            return;
        }

        try
        {
            CurrentDossier.Save();
            Current?.Refresh();
            UpdateStatus($"Saved {Current?.FileName ?? "file"}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Failed to save:\n\n{ex.Message}",
                "Save error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void CloseCurrentFile()
    {
        if (Current != null)
        {
            var toRemove = Current;
            Files.Remove(toRemove);
            Current = Files.FirstOrDefault();
            UpdateStatus($"Closed {toRemove.FileName}");
        }
    }

    private void RefreshCurrent()
    {
        if (Current != null)
        {
            OpenDossier(Current);
        }
    }

    private void UpdateStatus(string left)
    {
        StatusLeft.Text = left;
        StatusRight.Text = Files.Count == 0
            ? string.Empty
            : $"{Files.Count} file{(Files.Count == 1 ? string.Empty : "s")} loaded";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class RelayCommand(Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
