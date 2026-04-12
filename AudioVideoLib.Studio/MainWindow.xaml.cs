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

using System.Windows.Media;

using AudioVideoLib.Tags;

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

    private static readonly (string Id, string Label)[] CommonTextFrames =
    [
        ("TIT2", "Title"),
        ("TPE1", "Lead artist"),
        ("TPE2", "Band / album artist"),
        ("TPE3", "Conductor"),
        ("TPE4", "Remixer"),
        ("TALB", "Album"),
        ("TCOM", "Composer"),
        ("TCON", "Genre (TCON)"),
        ("TRCK", "Track"),
        ("TPOS", "Disc"),
        ("TYER", "Year (v2.3)"),
        ("TDRC", "Recording time (v2.4)"),
        ("TLAN", "Language"),
        ("TBPM", "BPM"),
        ("TSRC", "ISRC"),
        ("TKEY", "Initial key"),
        ("TPUB", "Publisher"),
        ("TCOP", "Copyright"),
    ];

    private static readonly (string Id, string Label)[] CommonUrlFrames =
    [
        ("WCOM", "Commercial information"),
        ("WCOP", "Copyright / legal information"),
        ("WOAF", "Official audio file webpage"),
        ("WOAR", "Official artist webpage"),
        ("WOAS", "Official source webpage"),
        ("WORS", "Official internet radio station"),
        ("WPAY", "Payment"),
        ("WPUB", "Publisher's webpage"),
    ];

    private Id3v2TabViewModel? CurrentId3v2Tab =>
        TagTabControl.SelectedItem as Id3v2TabViewModel;

    private void AddFrameButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        var menu = button.ContextMenu;
        if (menu == null)
        {
            return;
        }

        menu.Items.Clear();

        var textMenu = new MenuItem { Header = "Text frame" };
        foreach (var (id, label) in CommonTextFrames)
        {
            var mi = new MenuItem
            {
                Header = $"{id} — {label}",
                Tag = ("TEXT", id),
            };
            mi.Click += AddFrameMenuItem_Click;
            textMenu.Items.Add(mi);
        }

        textMenu.Items.Add(new Separator());
        var customText = new MenuItem
        {
            Header = "Custom identifier…",
            Tag = ("TEXT", string.Empty),
        };
        customText.Click += AddFrameMenuItem_Click;
        textMenu.Items.Add(customText);

        var urlMenu = new MenuItem { Header = "URL frame" };
        foreach (var (id, label) in CommonUrlFrames)
        {
            var mi = new MenuItem
            {
                Header = $"{id} — {label}",
                Tag = ("URL", id),
            };
            mi.Click += AddFrameMenuItem_Click;
            urlMenu.Items.Add(mi);
        }

        urlMenu.Items.Add(new Separator());
        var customUrl = new MenuItem
        {
            Header = "Custom identifier…",
            Tag = ("URL", string.Empty),
        };
        customUrl.Click += AddFrameMenuItem_Click;
        urlMenu.Items.Add(customUrl);

        menu.Items.Add(textMenu);
        menu.Items.Add(urlMenu);

        var pictureItem = new MenuItem
        {
            Header = "Picture (APIC)…",
            Tag = ("PICTURE", "APIC"),
        };
        pictureItem.Click += AddFrameMenuItem_Click;
        menu.Items.Add(pictureItem);

        var lyricsItem = new MenuItem
        {
            Header = "Unsynchronized lyrics (USLT)…",
            Tag = ("LYRICS", "USLT"),
        };
        lyricsItem.Click += AddFrameMenuItem_Click;
        menu.Items.Add(lyricsItem);

        var privItem = new MenuItem
        {
            Header = "Private (PRIV)…",
            Tag = ("PRIVATE", "PRIV"),
        };
        privItem.Click += AddFrameMenuItem_Click;
        menu.Items.Add(privItem);

        var ufidItem = new MenuItem
        {
            Header = "Unique file identifier (UFID)…",
            Tag = ("UFID", "UFID"),
        };
        ufidItem.Click += AddFrameMenuItem_Click;
        menu.Items.Add(ufidItem);

        menu.PlacementTarget = button;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void AddFrameMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: ValueTuple<string, string> tag })
        {
            return;
        }

        var tab = CurrentId3v2Tab;
        if (tab == null)
        {
            return;
        }

        var (kind, identifier) = tag;

        if (string.IsNullOrEmpty(identifier))
        {
            var kindLabel = kind == "TEXT" ? "text" : "URL";
            var prompt = $"Enter a 4-character {kindLabel} frame identifier (e.g., {(kind == "TEXT" ? "TSST" : "WFED")}):";
            var input = InputDialog.Ask(this, $"Custom {kindLabel} frame", prompt);
            if (string.IsNullOrWhiteSpace(input))
            {
                return;
            }

            identifier = input.Trim().ToUpperInvariant();
            if (identifier.Length != 4)
            {
                MessageBox.Show(this, "Frame identifier must be exactly 4 characters.", "Add frame",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        try
        {
            var row = kind switch
            {
                "TEXT" => tab.AddTextFrame(identifier),
                "URL" => tab.AddUrlFrame(identifier),
                "PICTURE" => tab.AddPictureFrame(),
                "LYRICS" => tab.AddLyricsFrame(),
                "PRIVATE" => tab.AddPrivateFrame(),
                "UFID" => tab.AddUniqueFileIdentifierFrame(),
                _ => throw new InvalidOperationException($"Unknown frame kind {kind}"),
            };

            var edited = row.Frame switch
            {
                Id3v2AttachedPictureFrame apic => ApicEditorDialog.Edit(this, apic),
                Id3v2UnsynchronizedLyricsFrame uslt => UsltEditorDialog.Edit(this, uslt),
                Id3v2PrivateFrame priv => BinaryDataDialog.Edit(this, priv),
                Id3v2UniqueFileIdentifierFrame ufid => BinaryDataDialog.Edit(this, ufid),
                _ => false,
            };

            if (edited)
            {
                tab.RefreshRow(row);
            }

            UpdateStatus($"Added {identifier}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Could not add {identifier}:\n\n{ex.Message}",
                "Add frame",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void TabItem_PreviewMouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not TabItem tabItem || tabItem.DataContext is not TagTabViewModel tabVm)
        {
            return;
        }

        if (CurrentDossier == null)
        {
            return;
        }

        var menu = new ContextMenu();
        var remove = new MenuItem { Header = $"Remove {tabVm.Header}" };
        remove.Click += (_, _) =>
        {
            var confirm = MessageBox.Show(
                this,
                $"Remove {tabVm.Header} from this file on next save?",
                "Remove tag",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.OK)
            {
                return;
            }

            CurrentDossier.RemoveTag(tabVm);
            UpdateStatus($"Marked {tabVm.Header} for removal on save");
        };

        menu.Items.Add(remove);
        menu.PlacementTarget = tabItem;
        menu.IsOpen = true;
        e.Handled = true;
    }

    private void AddApeItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ApeTabViewModel tab })
        {
            return;
        }

        var key = InputDialog.Ask(this, "Add APE item", "Item key (e.g., Title, Artist, Album, BPM):");
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        try
        {
            tab.AddTextItem(key.Trim());
            UpdateStatus($"Added APE item {key}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not add APE item:\n\n{ex.Message}", "Add item",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RemoveApeItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ApeTabViewModel tab })
        {
            return;
        }

        var grid = FindFirstDescendantDataGrid(sender as DependencyObject);
        if (grid?.SelectedItem is ApeItemRow row)
        {
            tab.RemoveItem(row);
            UpdateStatus($"Removed APE item {row.Key}");
        }
    }

    private void AddLyrics3Field_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Lyrics3v2TabViewModel tab })
        {
            return;
        }

        var id = InputDialog.Ask(this, "Add Lyrics3v2 field", "3-character field identifier (e.g., IND, LYR, INF, AUT, EAL, EAR, ETT):");
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        try
        {
            tab.AddTextField(id.Trim().ToUpperInvariant());
            UpdateStatus($"Added Lyrics3v2 field {id}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not add field:\n\n{ex.Message}", "Add field",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RemoveLyrics3Field_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Lyrics3v2TabViewModel tab })
        {
            return;
        }

        var grid = FindFirstDescendantDataGrid(sender as DependencyObject);
        if (grid?.SelectedItem is Lyrics3v2FieldRow row)
        {
            tab.RemoveField(row);
            UpdateStatus($"Removed Lyrics3v2 field {row.Identifier}");
        }
    }

    private void AddVorbisComment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: VorbisTabViewModel tab })
        {
            return;
        }

        var name = InputDialog.Ask(this, "Add Vorbis comment", "Comment name (e.g., TITLE, ARTIST, ALBUM):");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            tab.AddComment(name.Trim());
            UpdateStatus($"Added Vorbis comment {name}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not add Vorbis comment:\n\n{ex.Message}", "Add comment",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RemoveVorbisComment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: VorbisTabViewModel tab })
        {
            return;
        }

        var grid = FindFirstDescendantDataGrid(sender as DependencyObject);
        if (grid?.SelectedItem is VorbisCommentRow row)
        {
            tab.RemoveComment(row);
            UpdateStatus($"Removed Vorbis comment {row.Name}");
        }
    }

    private static DataGrid? FindFirstDescendantDataGrid(DependencyObject? start)
    {
        if (start == null)
        {
            return null;
        }

        // Walk up to the closest DockPanel/Grid holding both the button and the grid,
        // then walk down into it to find the first DataGrid child.
        var parent = VisualTreeHelper.GetParent(start);
        while (parent is not null and not Panel)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }

        return parent == null ? null : FindFirstDataGrid(parent);
    }

    private static DataGrid? FindFirstDataGrid(DependencyObject root)
    {
        if (root is DataGrid dg)
        {
            return dg;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var found = FindFirstDataGrid(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void AdvancedGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid || grid.SelectedItem is not Id3v2FrameRow row)
        {
            return;
        }

        var tab = CurrentId3v2Tab;
        if (tab == null)
        {
            return;
        }

        switch (row.Frame)
        {
            case Id3v2AttachedPictureFrame apic:
                if (ApicEditorDialog.Edit(this, apic))
                {
                    tab.RefreshRow(row);
                }

                e.Handled = true;
                break;

            case Id3v2UnsynchronizedLyricsFrame uslt:
                if (UsltEditorDialog.Edit(this, uslt))
                {
                    tab.RefreshRow(row);
                }

                e.Handled = true;
                break;

            case Id3v2PrivateFrame priv:
                if (BinaryDataDialog.Edit(this, priv))
                {
                    tab.RefreshRow(row);
                }

                e.Handled = true;
                break;

            case Id3v2UniqueFileIdentifierFrame ufid:
                if (BinaryDataDialog.Edit(this, ufid))
                {
                    tab.RefreshRow(row);
                }

                e.Handled = true;
                break;
        }
    }

    private void AdvancedFrameRow_PreviewMouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not DataGridRow row || row.Item is not Id3v2FrameRow frameRow)
        {
            return;
        }

        var tab = CurrentId3v2Tab;
        if (tab == null)
        {
            return;
        }

        row.IsSelected = true;

        var menu = new ContextMenu();
        var delete = new MenuItem { Header = $"Delete {frameRow.Identifier}" };
        delete.Click += (_, _) =>
        {
            tab.RemoveFrameRow(frameRow);
            UpdateStatus($"Removed {frameRow.Identifier}");
        };

        menu.Items.Add(delete);
        menu.PlacementTarget = row;
        menu.IsOpen = true;
        e.Handled = true;
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
