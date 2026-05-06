namespace AudioVideoLib.Studio;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Microsoft.Win32;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        UpdateStatus("Ready");

        InputBindings.Add(new KeyBinding(new RelayCommand(_ => OpenFile()),        Key.O,      ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => SaveCurrent()),     Key.S,      ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => SaveAsFile()),      Key.S,      ModifierKeys.Control | ModifierKeys.Shift));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => CloseCurrentFile()), Key.W,     ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => RefreshCurrent()),   Key.F5,    ModifierKeys.None));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => GoToOffset()),       Key.G,     ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => Find()),             Key.F,     ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => FindNext()),         Key.F3,    ModifierKeys.None));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => FindPrevious()),     Key.F3,    ModifierKeys.Shift));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => ShowShortcuts()),    Key.F1,    ModifierKeys.None));

        Closing += (_, _) => WindowLayout.Save(this);
        Closed += (_, _) =>
        {
            foreach (var f in _openFiles)
            {
                f.Dispose();
            }

            _openFiles.Clear();
            _lastSelectedPerFile.Clear();
        };
        HexViewControl.ByteClicked += HexView_ByteClicked;

        // Populate Open Recent eagerly so the submenu has its items + chevron from
        // first paint. Refreshed after every file open / clear.
        PopulateRecentMenu();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var layout = WindowLayout.Load();
        if (layout == null)
        {
            return;
        }

        var screenW = SystemParameters.VirtualScreenWidth;
        var screenH = SystemParameters.VirtualScreenHeight;
        if (layout.Left + layout.Width > 0 && layout.Left < screenW &&
            layout.Top + layout.Height > 0 && layout.Top < screenH)
        {
            Left = layout.Left;
            Top = layout.Top;
            Width = layout.Width;
            Height = layout.Height;
            WindowState = layout.State;
        }
    }

    private byte[]? _lastFindPattern;
    private string _lastFindDisplay = string.Empty;
    private long _lastFindOffset = -1;

    public FileDossier? CurrentDossier { get; private set; }

    private readonly List<FileDossier> _openFiles = [];
    private readonly Dictionary<FileDossier, InspectorNode?> _lastSelectedPerFile = [];

    public sealed class FileTabItem(FileDossier dossier, bool isActive)
    {
        public FileDossier Dossier { get; } = dossier;

        public string DisplayName { get; } = Path.GetFileName(dossier.FilePath);

        public string FilePath { get; } = dossier.FilePath;

        public System.Windows.Media.Brush TabBackground { get; } = isActive
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x26, 0x4F, 0x78))
            : System.Windows.Media.Brushes.Transparent;
    }

    private void RebuildFileTabsList()
    {
        var items = new List<FileTabItem>();
        foreach (var f in _openFiles)
        {
            items.Add(new FileTabItem(f, ReferenceEquals(f, CurrentDossier)));
        }

        FileTabsList.ItemsSource = items;
        FileTabsStrip.Visibility = _openFiles.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SelectTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: FileDossier d })
        {
            ActivateFile(d);
        }
    }

    private void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: FileDossier d })
        {
            CloseFile(d);
        }
    }

    private void ActivateFile(FileDossier dossier)
    {
        if (ReferenceEquals(dossier, CurrentDossier))
        {
            return;
        }

        // Snapshot the current tab's state so we can restore it later.
        if (CurrentDossier != null)
        {
            _lastSelectedPerFile[CurrentDossier] = _selectedNode;
        }

        CurrentDossier = dossier;
        BindUiToCurrent();
        RebuildFileTabsList();
    }

    private void BindUiToCurrent()
    {
        if (CurrentDossier == null)
        {
            FilePathText.Text = "No file loaded. Use File > Open (Ctrl+O).";
            InspectorTreeView.ItemsSource = null;
            PropertiesGrid.ItemsSource = null;
            TagTabControl.DataContext = null;
            TagTabControl.ItemsSource = null;
            HexViewControl.Clear();
            AnalysisPanel.Load(null);
            PlayBar.Open(null);
            PlayBar.Visibility = Visibility.Collapsed;
            UpdateFormatSpecificMenus();
            UpdateDirtyState();
            Title = "AudioVideoLib Studio";
            return;
        }

        FilePathText.Text = $"{CurrentDossier.FilePath}   ({CurrentDossier.FileSizeText})";
        FilePathText.ToolTip = CurrentDossier.FilePath;

        if (CurrentDossier.InspectorRoot != null)
        {
            InspectorTreeView.ItemsSource = new[] { CurrentDossier.InspectorRoot };
        }

        TagTabControl.DataContext = CurrentDossier;
        TagTabControl.ItemsSource = CurrentDossier.TagTabs;

        AnalysisPanel.Load(CurrentDossier);
        var (frameCount, frameUnit) = CurrentDossier.AudioStream switch
        {
            AudioVideoLib.IO.MpaStream mpa => (mpa.Frames.Count(), "Frame"),
            AudioVideoLib.IO.OggStream ogg => (ogg.PageCount, "Page"),
            _ => (0, "Frame"),
        };
        PlayBar.Open(CurrentDossier.FilePath, frameCount, frameUnit);
        PlayBar.Visibility = Visibility.Visible;

        // Restore last selected node, or render the root.
        _selectedNode = null;
        PropertiesGrid.ItemsSource = null;
        HexViewControl.Clear();
        if (_lastSelectedPerFile.TryGetValue(CurrentDossier, out var lastNode) && lastNode != null)
        {
            _suppressTreeSync = false;
            SelectTreeNode(lastNode);
        }

        UpdateFormatSpecificMenus();
        UpdateDirtyState();
    }

    private void CloseFile(FileDossier dossier)
    {
        var idx = _openFiles.IndexOf(dossier);
        if (idx < 0)
        {
            return;
        }

        _openFiles.RemoveAt(idx);
        _lastSelectedPerFile.Remove(dossier);
        dossier.Dispose();

        if (ReferenceEquals(dossier, CurrentDossier))
        {
            CurrentDossier = _openFiles.Count > 0
                ? _openFiles[Math.Min(idx, _openFiles.Count - 1)]
                : null;
            BindUiToCurrent();
        }

        RebuildFileTabsList();
        UpdateStatus(CurrentDossier != null
            ? $"Closed {Path.GetFileName(dossier.FilePath)}"
            : $"Closed {Path.GetFileName(dossier.FilePath)} — no files open");
    }

    private InspectorNode? _selectedNode;

    internal async void OpenDossierFromPath(string path)
    {
        // If already open, just activate its tab.
        var existing = _openFiles.FirstOrDefault(f =>
            string.Equals(f.FilePath, path, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            ActivateFile(existing);
            UpdateStatus($"Already open: {Path.GetFileName(path)}");
            return;
        }

        UpdateStatus($"Loading {Path.GetFileName(path)}…");
        try
        {
            var dossier = await FileDossier.CreateAsync(path).ConfigureAwait(true);
            RecentFiles.Add(path);
            PopulateRecentMenu();

            // Snapshot previous tab so switching back restores it.
            if (CurrentDossier != null)
            {
                _lastSelectedPerFile[CurrentDossier] = _selectedNode;
            }

            _openFiles.Add(dossier);
            CurrentDossier = dossier;

            BindUiToCurrent();
            RebuildFileTabsList();
            UpdateStatus($"Loaded {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to open {Path.GetFileName(path)}:\n\n{ex.Message}", "Load error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            UpdateStatus("Load failed");
        }
    }

    private void UpdateFormatSpecificMenus()
    {
        var hasFile = CurrentDossier != null;
        ViewMenu.Visibility = hasFile ? Visibility.Visible : Visibility.Collapsed;
        ExportJsonMenuItem.Visibility = hasFile ? Visibility.Visible : Visibility.Collapsed;
        ExportLintMenuItem.Visibility = hasFile ? Visibility.Visible : Visibility.Collapsed;
        CompareMenuItem.Visibility = hasFile ? Visibility.Visible : Visibility.Collapsed;
        FlacIntegrityMenuItem.Visibility = CurrentDossier?.IsFlac == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void InspectorTree_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (InspectorTreeView.SelectedItem is not InspectorNode node || CurrentDossier == null)
        {
            e.Handled = true;
            return;
        }

        var menu = new ContextMenu();

        var copyOffset = new MenuItem { Header = $"Copy offset (0x{node.StartOffset:X8})" };
        copyOffset.Click += (_, _) => CopyToClipboard($"0x{node.StartOffset:X8} ({node.StartOffset:N0})");
        menu.Items.Add(copyOffset);

        var length = node.EndOffset - node.StartOffset;
        var copyRange = new MenuItem { Header = $"Copy range (0x{node.StartOffset:X8} .. 0x{node.EndOffset:X8}, {length:N0} B)" };
        copyRange.Click += (_, _) => CopyToClipboard($"0x{node.StartOffset:X8}..0x{node.EndOffset:X8} ({length:N0} bytes)");
        menu.Items.Add(copyRange);

        menu.Items.Add(new Separator());

        var bytes = ExtractBytes(node);
        var copyHex = new MenuItem { Header = $"Copy as hex ({bytes.Length:N0} B)", IsEnabled = bytes.Length > 0 };
        copyHex.Click += (_, _) => CopyToClipboard(Convert.ToHexString(bytes));
        menu.Items.Add(copyHex);

        var copyBase64 = new MenuItem { Header = $"Copy as base64 ({bytes.Length:N0} B)", IsEnabled = bytes.Length > 0 };
        copyBase64.Click += (_, _) => CopyToClipboard(Convert.ToBase64String(bytes));
        menu.Items.Add(copyBase64);

        menu.Items.Add(new Separator());

        var saveBytes = new MenuItem { Header = $"Save bytes to file…", IsEnabled = bytes.Length > 0 };
        saveBytes.Click += (_, _) => SaveBytesToFile(node, bytes);
        menu.Items.Add(saveBytes);

        if (node.Payload is AudioVideoLib.Formats.FlacPictureMetadataBlock flacPic
            && flacPic.PictureData is { Length: > 0 })
        {
            var export = new MenuItem { Header = "Export picture…" };
            export.Click += (_, _) => ExportFlacPicture(flacPic);
            menu.Items.Add(new Separator());
            menu.Items.Add(export);
        }

        InspectorTreeView.ContextMenu = menu;
    }

    private byte[] ExtractBytes(InspectorNode node)
    {
        if (CurrentDossier?.FileBytes == null)
        {
            return [];
        }

        var length = node.EndOffset - node.StartOffset;
        if (length <= 0 || node.StartOffset < 0 || node.EndOffset > CurrentDossier.FileBytes.Length)
        {
            return [];
        }

        // Cap absurdly large copies at 16 MB to avoid hanging the clipboard.
        const int cap = 16 * 1024 * 1024;
        var take = (int)Math.Min(length, cap);
        var result = new byte[take];
        Buffer.BlockCopy(CurrentDossier.FileBytes, (int)node.StartOffset, result, 0, take);
        return result;
    }

    private void CopyToClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
            UpdateStatus("Copied to clipboard");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Clipboard error: {ex.Message}");
        }
    }

    private void SaveBytesToFile(InspectorNode node, byte[] bytes)
    {
        var dlg = new SaveFileDialog
        {
            Title = "Save bytes",
            FileName = SanitizeFileName($"{node.Label}_0x{node.StartOffset:X8}.bin"),
            Filter = "Binary|*.bin|All files|*.*",
        };
        if (dlg.ShowDialog(this) == true)
        {
            try
            {
                File.WriteAllBytes(dlg.FileName, bytes);
                UpdateStatus($"Saved {bytes.Length:N0} bytes to {Path.GetFileName(dlg.FileName)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not write file:\n\n{ex.Message}", "Save", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void ExportFlacPicture(AudioVideoLib.Formats.FlacPictureMetadataBlock pic)
    {
        var ext = (pic.MimeType ?? string.Empty).ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            "image/webp" => ".webp",
            _ => ".bin",
        };

        var dlg = new SaveFileDialog
        {
            Title = "Export picture",
            FileName = SanitizeFileName($"{pic.PictureType}{ext}"),
            Filter = $"{ext.TrimStart('.').ToUpperInvariant()} file|*{ext}|All files|*.*",
        };
        if (dlg.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            File.WriteAllBytes(dlg.FileName, pic.PictureData ?? []);
            UpdateStatus($"Exported picture to {Path.GetFileName(dlg.FileName)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not write file:\n\n{ex.Message}", "Export picture",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name;
    }

    private void InspectorTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not InspectorNode node || CurrentDossier == null)
        {
            return;
        }

        _selectedNode = node;
        PropertiesGrid.ItemsSource = node.Properties;

        if (_suppressTreeSync)
        {
            // User clicked a byte in hex view; keep hex view untouched so their click target stays visible.
            UpdateSelectionStatus(node.StartOffset, node.EndOffset - node.StartOffset, selection: null);
            return;
        }

        RenderHex(node.StartOffset, node.EndOffset, subRanges: SubRangesFromChildren(node));
        UpdateSelectionStatus(node.StartOffset, node.EndOffset - node.StartOffset, selection: null);
    }

    private void PropertiesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectedNode == null || CurrentDossier == null)
        {
            return;
        }

        if (PropertiesGrid.SelectedItem is InspectorProperty prop && prop.HighlightStart.HasValue && prop.HighlightLength.HasValue)
        {
            RenderHex(_selectedNode.StartOffset, _selectedNode.EndOffset, prop.HighlightStart.Value, prop.HighlightLength.Value);
            UpdateSelectionStatus(_selectedNode.StartOffset, _selectedNode.EndOffset - _selectedNode.StartOffset,
                selection: (prop.HighlightStart.Value, prop.HighlightLength.Value));
        }
        else
        {
            RenderHex(_selectedNode.StartOffset, _selectedNode.EndOffset);
            UpdateSelectionStatus(_selectedNode.StartOffset, _selectedNode.EndOffset - _selectedNode.StartOffset, selection: null);
        }
    }

    private void UpdateSelectionStatus(long nodeStart, long nodeLength, (long Start, int Length)? selection)
    {
        if (selection.HasValue)
        {
            var (s, l) = selection.Value;
            StatusSelection.Text = $"Sel 0x{s:X8}..0x{s + l:X8}  ({l:N0} B)   Node 0x{nodeStart:X8} ({nodeLength:N0} B)";
        }
        else
        {
            StatusSelection.Text = $"0x{nodeStart:X8}..0x{nodeStart + nodeLength:X8}  ({nodeLength:N0} B)";
        }
    }

    private bool _suppressTreeSync;

    private void HexView_ByteClicked(object? sender, long offset)
    {
        if (CurrentDossier?.InspectorRoot == null)
        {
            return;
        }

        var node = FindDeepestNodeContaining(CurrentDossier.InspectorRoot, offset);
        if (node == null || ReferenceEquals(node, _selectedNode))
        {
            return;
        }

        _suppressTreeSync = true;
        try
        {
            SelectTreeNode(node);
        }
        finally
        {
            _suppressTreeSync = false;
        }

        UpdateStatus($"Jumped to 0x{offset:X8} ({node.Label})");
    }

    private void RenderHex(long regionStart, long regionEnd, long? highlightStart = null, int? highlightLength = null,
        IReadOnlyList<HexSubRange>? subRanges = null)
    {
        if (CurrentDossier?.FileBytes == null || CurrentDossier.FileBytes.Length == 0)
        {
            HexViewControl.Clear();
            return;
        }

        HexViewControl.SetContent(CurrentDossier.FileBytes, regionStart, regionEnd, highlightStart, highlightLength, subRanges);
    }

    private static readonly SolidColorBrush[] SubRangeTints =
    [
        Frozen(new SolidColorBrush(Color.FromArgb(0x22, 0x55, 0xB0, 0xF0))),
        Frozen(new SolidColorBrush(Color.FromArgb(0x22, 0xF0, 0x9D, 0x55))),
        Frozen(new SolidColorBrush(Color.FromArgb(0x22, 0x6E, 0xCB, 0x7A))),
        Frozen(new SolidColorBrush(Color.FromArgb(0x22, 0xD6, 0x80, 0xD0))),
        Frozen(new SolidColorBrush(Color.FromArgb(0x22, 0xF0, 0xD6, 0x6A))),
    ];

    private static SolidColorBrush Frozen(SolidColorBrush b)
    {
        b.Freeze();
        return b;
    }

    private static IReadOnlyList<HexSubRange> SubRangesFromChildren(InspectorNode node)
    {
        if (node.Children.Count == 0)
        {
            return [];
        }

        var list = new List<HexSubRange>(node.Children.Count);
        for (var i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            if (child.EndOffset <= child.StartOffset)
            {
                continue;
            }

            list.Add(new HexSubRange(child.StartOffset, child.EndOffset,
                SubRangeTints[i % SubRangeTints.Length], child.Label));
        }

        return list;
    }

    private Id3v2TabViewModel? CurrentId3v2Tab =>
        TagTabControl.SelectedItem as Id3v2TabViewModel;

    private void AddFrameButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.ContextMenu is not { } menu)
        {
            return;
        }
        if (CurrentId3v2Tab is not { Tag: Id3v2Tag id3v2 } tab)
        {
            return;
        }

        var model = Id3v2AddMenuBuilder.BuildModel(TagItemEditorRegistry.Shared, id3v2);
        Id3v2AddMenuBuilder.Populate(menu, model, entry => OnAddOrEditFrame(entry, tab, id3v2));

        menu.PlacementTarget = button;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void ManageFramesButton_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentId3v2Tab is not { Tag: Id3v2Tag id3v2 } tab)
        {
            return;
        }
        ManageFramesDialog.ShowFor(this, id3v2, entry => OnAddOrEditFrame(entry, tab, id3v2));
    }

    private void OnAddOrEditFrame(Id3v2MenuEntry entry, Id3v2TabViewModel tab, Id3v2Tag tag)
    {
        try
        {
            if (entry.IsEditExisting)
            {
                var existing = tag.Frames.FirstOrDefault(f => f.Identifier == entry.FrameIdentifier);
                if (existing is null)
                {
                    return;
                }
                if (DispatchEdit(existing, tag))
                {
                    tab.RefreshFrameRow(existing);
                }
                return;
            }

            var newFrame = ConstructFrameFor(entry.FrameIdentifier, tag);
            if (newFrame is null)
            {
                return;
            }
            if (DispatchEdit(newFrame, tag))
            {
                tab.AddFrame(newFrame);
                UpdateStatus($"Added {entry.FrameIdentifier}");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Could not add {entry.FrameIdentifier}:\n\n{ex.Message}",
                "Add frame",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private bool DispatchEdit(Id3v2Frame frame, Id3v2Tag tag)
    {
        if (!TagItemEditorRegistry.Shared.TryResolve(frame.GetType(), out var editor))
        {
            return false;
        }
        // Wrappers (CDM/CRM) need the tag-frames snapshot before the dialog opens.
        if (editor.Inner is IWrapperEditor wrapper)
        {
            wrapper.OnBeforeEdit(tag, frame);
        }
        return editor.Edit(this, frame);
    }

    private static Id3v2Frame? ConstructFrameFor(string identifier, Id3v2Tag tag)
    {
        if (Id3v2KnownTextFrameIds.All.Any(i => i.Identifier == identifier || i.V220Identifier == identifier))
        {
            return new Id3v2TextFrame(tag.Version, identifier)
            {
                TextEncoding = Id3v2FrameEncodingType.UTF8,
            };
        }
        if (Id3v2KnownUrlFrameIds.All.Any(i => i.Identifier == identifier || i.V220Identifier == identifier))
        {
            return new Id3v2UrlLinkFrame(tag.Version, identifier);
        }
        foreach (var entry in TagItemEditorRegistry.Shared.Entries)
        {
            if (entry.Attribute is not Id3v2FrameEditorAttribute a)
            {
                continue;
            }
            var ident = Id3v2AddMenuBuilder.IdentifierFor(a, tag.Version);
            if (ident == identifier)
            {
                return (Id3v2Frame)entry.Adapter.CreateNew(tag);
            }
        }
        return null;
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

    private void ApeItemRow_PreviewMouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not DataGridRow row || row.Item is not ApeItemRow item || CurrentApeTab is not { } tab)
        {
            return;
        }

        row.IsSelected = true;
        ShowDeleteRowMenu(row, $"Delete {item.Key}", () =>
        {
            tab.RemoveItem(item);
            UpdateStatus($"Removed APE item {item.Key}");
        });
        e.Handled = true;
    }

    private void Lyrics3FieldRow_PreviewMouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not DataGridRow row || row.Item is not Lyrics3v2FieldRow item || CurrentLyrics3v2Tab is not { } tab)
        {
            return;
        }

        row.IsSelected = true;
        ShowDeleteRowMenu(row, $"Delete {item.Identifier}", () =>
        {
            tab.RemoveField(item);
            UpdateStatus($"Removed Lyrics3v2 field {item.Identifier}");
        });
        e.Handled = true;
    }

    private void VorbisCommentRow_PreviewMouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not DataGridRow row || row.Item is not VorbisCommentRow item || CurrentVorbisTab is not { } tab)
        {
            return;
        }

        row.IsSelected = true;
        ShowDeleteRowMenu(row, $"Delete {item.Name}", () =>
        {
            tab.RemoveComment(item);
            UpdateStatus($"Removed Vorbis comment {item.Name}");
        });
        e.Handled = true;
    }

    private static void ShowDeleteRowMenu(DataGridRow row, string header, Action onDelete)
    {
        var menu = new ContextMenu();
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => onDelete();
        menu.Items.Add(item);
        menu.PlacementTarget = row;
        menu.IsOpen = true;
    }

    private ApeTabViewModel? CurrentApeTab =>
        TagTabControl.SelectedItem as ApeTabViewModel;

    private Lyrics3v2TabViewModel? CurrentLyrics3v2Tab =>
        TagTabControl.SelectedItem as Lyrics3v2TabViewModel;

    private VorbisTabViewModel? CurrentVorbisTab =>
        TagTabControl.SelectedItem as VorbisTabViewModel;

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
        if (CurrentId3v2Tab is not { Tag: Id3v2Tag tag } tab)
        {
            return;
        }
        if (!TagItemEditorRegistry.Shared.TryResolve(row.Frame.GetType(), out _))
        {
            return;
        }
        if (DispatchEdit(row.Frame, tag))
        {
            tab.RefreshRow(row);
        }
        e.Handled = true;
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

    private void ValidateTag_Click(object sender, RoutedEventArgs e)
    {
        if (TagTabControl.SelectedItem is not TagTabViewModel tab)
        {
            UpdateStatus("Select a tag tab first");
            return;
        }

        IAudioTag? tag = tab switch
        {
            Id3v2TabViewModel v2 => v2.Tag,
            Id3v1TabViewModel v1 => v1.Tag,
            ApeTabViewModel ape => ape.Tag,
            Lyrics3v1TabViewModel l3v1 => l3v1.Tag,
            Lyrics3v2TabViewModel l3 => l3.Tag,
            MusicMatchTabViewModel mm => mm.Tag,
            _ => null,
        };

        var issues = (IReadOnlyList<ValidationIssue>)(tab switch
        {
            VorbisTabViewModel vt => TagValidator.ValidateVorbisForStudio(vt),
            _ when tag != null => TagValidator.Validate(tag),
            _ => [new ValidationIssue(ValidationSeverity.Info, "No validator for this tab.")],
        });

        var title = $"Validate — {tab.Header}";
        var body = TagValidator.Format(issues);
        var icon = issues.Any(i => i.Severity == ValidationSeverity.Error)
            ? MessageBoxImage.Warning
            : issues.Count == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning;
        MessageBox.Show(this, body, title, MessageBoxButton.OK, icon);
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

    private void OpenFile_Click(object sender, RoutedEventArgs e) => OpenFile();

    private const int RecentMenuMaxItems = 5;

    private void FileMenu_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        // Defensive refresh — covers the case where the recent.txt file was
        // mutated by another process while the app was running.
        PopulateRecentMenu();
    }

    private void PopulateRecentMenu()
    {
        RecentMenu.Items.Clear();

        var recent = RecentFiles.Load();
        if (recent.Count == 0)
        {
            // Disable the parent menu item entirely. No "(no recent files)" stub.
            RecentMenu.IsEnabled = false;
            return;
        }

        RecentMenu.IsEnabled = true;
        var displayCount = Math.Min(recent.Count, RecentMenuMaxItems);

        for (var i = 0; i < displayCount; i++)
        {
            var path = recent[i];
            var item = new MenuItem
            {
                Header = $"_{(i + 1) % 10}  {Path.GetFileName(path)}",
                ToolTip = path,
                Tag = path,
            };
            item.Click += RecentItem_Click;
            RecentMenu.Items.Add(item);
        }

        RecentMenu.Items.Add(new Separator());
        var clear = new MenuItem { Header = "_Clear recent files" };
        clear.Click += (_, _) =>
        {
            RecentFiles.Clear();
            PopulateRecentMenu();
        };
        RecentMenu.Items.Add(clear);
    }

    private void RecentItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string path })
        {
            return;
        }

        if (File.Exists(path))
        {
            OpenDossierFromPath(path);
        }
        else
        {
            MessageBox.Show(this, $"File not found:\n\n{path}", "Open recent", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    private void Save_Click(object sender, RoutedEventArgs e) => SaveCurrent();
    private void SaveAs_Click(object sender, RoutedEventArgs e) => SaveAsFile();
    private void CloseFile_Click(object sender, RoutedEventArgs e) => CloseCurrentFile();
    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshCurrent();
    private void GoToOffset_Click(object sender, RoutedEventArgs e) => GoToOffset();
    private void FlacIntegrity_Click(object sender, RoutedEventArgs e) => FlacIntegrityCheck();
    private void Find_Click(object sender, RoutedEventArgs e) => Find();
    private void FindNext_Click(object sender, RoutedEventArgs e) => FindNext();
    private void FindPrevious_Click(object sender, RoutedEventArgs e) => FindPrevious();
    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void ExportJson_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentDossier == null)
        {
            UpdateStatus("Open a file first");
            return;
        }

        var dlg = new SaveFileDialog
        {
            Title = "Export tags as JSON",
            FileName = Path.GetFileNameWithoutExtension(CurrentDossier.FilePath) + ".tags.json",
            InitialDirectory = Path.GetDirectoryName(CurrentDossier.FilePath) ?? string.Empty,
            Filter = "JSON file|*.json|All files|*.*",
        };
        if (dlg.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var tags = new List<IAudioTag>();
            VorbisComments? vorbis = null;
            foreach (var tab in CurrentDossier.TagTabs)
            {
                IAudioTag? underlying = tab switch
                {
                    Id3v2TabViewModel v2 => v2.Tag,
                    Id3v1TabViewModel v1 => v1.Tag,
                    ApeTabViewModel ape => ape.Tag,
                    Lyrics3v1TabViewModel l3v1 => l3v1.Tag,
                    Lyrics3v2TabViewModel l3 => l3.Tag,
                    MusicMatchTabViewModel mm => mm.Tag,
                    _ => null,
                };

                if (underlying != null)
                {
                    tags.Add(underlying);
                }

                if (tab is VorbisTabViewModel vt)
                {
                    vorbis = vt.Comments;
                }
            }

            var json = TagJsonExporter.Export(CurrentDossier.FilePath, tags, vorbis);
            File.WriteAllText(dlg.FileName, json);
            UpdateStatus($"Exported to {Path.GetFileName(dlg.FileName)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to export:\n\n{ex.Message}", "Export JSON",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CopyFilePath_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentDossier == null)
        {
            UpdateStatus("Open a file first");
            return;
        }

        CopyToClipboard(CurrentDossier.FilePath);
    }

    private void OpenInExplorer_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentDossier == null)
        {
            UpdateStatus("Open a file first");
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{CurrentDossier.FilePath}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            UpdateStatus($"Could not open Explorer: {ex.Message}");
        }
    }

    private void ShowShortcuts()
    {
        var dlg = new ShortcutsDialog { Owner = this };
        dlg.ShowDialog();
    }

    private void Shortcuts_Click(object sender, RoutedEventArgs e) => ShowShortcuts();

    private void Batch_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Batch.BatchDialog { Owner = this };
        dlg.Show();
    }

    private async void Compare_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentDossier == null)
        {
            UpdateStatus("Open a file first");
            return;
        }

        var dlg = new OpenFileDialog
        {
            Title = "Compare with another file",
            Filter = "Audio files|*.mp3;*.flac;*.m4a;*.ogg;*.wav|All files|*.*",
            InitialDirectory = Path.GetDirectoryName(CurrentDossier.FilePath) ?? string.Empty,
            Multiselect = false,
        };

        if (dlg.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            UpdateStatus($"Loading {Path.GetFileName(dlg.FileName)}…");
            var other = await FileDossier.CreateAsync(dlg.FileName).ConfigureAwait(true);
            var diff = new DiffWindow(CurrentDossier, other) { Owner = this };
            diff.Closed += (_, _) => other.Dispose();
            diff.Show();
            UpdateStatus("Compare ready");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not load {Path.GetFileName(dlg.FileName)}:\n\n{ex.Message}", "Compare",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            UpdateStatus("Compare failed");
        }
    }

    private void ValidateAll_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentDossier == null)
        {
            UpdateStatus("Open a file first");
            return;
        }

        var report = LintReport.Build(CurrentDossier, CurrentDossier.AudioStream, CurrentDossier.TagOffsets);
        var body = LintReport.FormatPlainText(report);
        var title = $"Validate — {Path.GetFileName(CurrentDossier.FilePath)}";
        var icon = report.TotalErrors > 0 ? MessageBoxImage.Warning
                 : report.TotalWarnings > 0 ? MessageBoxImage.Warning
                 : MessageBoxImage.Information;
        MessageBox.Show(this, body, title, MessageBoxButton.OK, icon);
    }

    private void ExportLint_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentDossier == null)
        {
            UpdateStatus("Open a file first");
            return;
        }

        var dlg = new SaveFileDialog
        {
            Title = "Export lint report",
            FileName = Path.GetFileNameWithoutExtension(CurrentDossier.FilePath) + ".lint.md",
            InitialDirectory = Path.GetDirectoryName(CurrentDossier.FilePath) ?? string.Empty,
            Filter = "Markdown|*.md|Plain text|*.txt|All files|*.*",
        };
        if (dlg.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var report = LintReport.Build(CurrentDossier, CurrentDossier.AudioStream, CurrentDossier.TagOffsets);
            var content = dlg.FilterIndex == 2
                ? LintReport.FormatPlainText(report)
                : LintReport.FormatMarkdown(report);
            File.WriteAllText(dlg.FileName, content);
            UpdateStatus($"Lint report exported to {Path.GetFileName(dlg.FileName)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to export:\n\n{ex.Message}", "Export lint report",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SettingsDialog { Owner = this };
        dlg.ShowDialog();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        new AboutDialog { Owner = this }.ShowDialog();
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = GetDroppedFile(e) != null ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        var file = GetDroppedFile(e);
        if (file != null)
        {
            OpenDossierFromPath(file);
        }

        e.Handled = true;
    }

    private static string? GetDroppedFile(DragEventArgs e)
    {
        return e.Data.GetDataPresent(DataFormats.FileDrop)
            && e.Data.GetData(DataFormats.FileDrop) is string[] files
            && files.Length > 0
            && File.Exists(files[0])
                ? files[0]
                : null;
    }

    private void OpenFile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Audio files|*.mp3;*.flac;*.wav;*.aif;*.aiff;*.ogg;*.oga;*.m4a|All files|*.*",
            Multiselect = false,
            Title = "Open audio file",
        };
        if (dlg.ShowDialog(this) == true)
        {
            OpenDossierFromPath(dlg.FileName);
        }
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
            UpdateStatus($"Saved {Path.GetFileName(CurrentDossier.FilePath)}");
            UpdateDirtyState();
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

    private void SaveAsFile()
    {
        if (CurrentDossier == null)
        {
            UpdateStatus("Nothing to save");
            return;
        }

        var dlg = new SaveFileDialog
        {
            Title = "Save As",
            Filter = "All files|*.*",
            FileName = Path.GetFileName(CurrentDossier.FilePath),
            InitialDirectory = Path.GetDirectoryName(CurrentDossier.FilePath) ?? string.Empty,
        };
        if (dlg.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            CurrentDossier.SaveAs(dlg.FileName);
            UpdateStatus($"Saved as {Path.GetFileName(dlg.FileName)}");

            // Open the new copy
            OpenDossierFromPath(dlg.FileName);
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
        if (CurrentDossier != null)
        {
            CloseFile(CurrentDossier);
        }
    }

    private void RefreshCurrent()
    {
        if (CurrentDossier != null)
        {
            OpenDossierFromPath(CurrentDossier.FilePath);
        }
    }

    private void GoToOffset()
    {
        if (CurrentDossier?.InspectorRoot == null)
        {
            UpdateStatus("Open a file first");
            return;
        }

        var input = InputDialog.Ask(this, "Go to offset", "Offset (decimal or 0xHEX):", "0x");
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        if (!TryParseOffset(input.Trim(), out var offset))
        {
            MessageBox.Show(this, $"Could not parse '{input}'.\n\nUse decimal (e.g., 94000) or hex (e.g., 0x16F09).",
                "Go to offset", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (offset < 0 || offset >= CurrentDossier.FileBytes.Length)
        {
            MessageBox.Show(this, $"Offset 0x{offset:X} is outside the file (size {CurrentDossier.FileBytes.Length:N0}).",
                "Go to offset", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var node = FindDeepestNodeContaining(CurrentDossier.InspectorRoot, offset);
        if (node == null)
        {
            UpdateStatus($"No structure covers 0x{offset:X8}");
            return;
        }

        SelectTreeNode(node);
        UpdateStatus($"Jumped to 0x{offset:X8} ({node.Label})");
    }

    private static bool TryParseOffset(string input, out long offset)
    {
        offset = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        input = input.Replace("_", string.Empty).Replace(" ", string.Empty);
        if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || input.StartsWith('$'))
        {
            var hex = input.StartsWith('$') ? input[1..] : input[2..];
            return long.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out offset);
        }

        return long.TryParse(input, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out offset);
    }

    private static InspectorNode? FindDeepestNodeContaining(InspectorNode root, long offset)
    {
        if (offset < root.StartOffset || offset >= Math.Max(root.EndOffset, root.StartOffset + 1))
        {
            return null;
        }

        foreach (var child in root.Children)
        {
            var hit = FindDeepestNodeContaining(child, offset);
            if (hit != null)
            {
                return hit;
            }
        }

        return root;
    }

    private void SelectTreeNode(InspectorNode target)
    {
        if (CurrentDossier?.InspectorRoot == null)
        {
            return;
        }

        var path = new List<InspectorNode>();
        if (!BuildPath(CurrentDossier.InspectorRoot, target, path))
        {
            return;
        }

        // Expand each ancestor so the container chain exists, then walk the visual tree.
        ItemsControl container = InspectorTreeView;
        foreach (var node in path)
        {
            var item = container.ItemContainerGenerator.ContainerFromItem(node) as TreeViewItem;
            if (item == null)
            {
                // Containers are generated lazily. Force realization by updating layout.
                container.UpdateLayout();
                item = container.ItemContainerGenerator.ContainerFromItem(node) as TreeViewItem;
                if (item == null)
                {
                    return;
                }
            }

            if (node == target)
            {
                item.IsSelected = true;
                item.BringIntoView();
                item.Focus();
                return;
            }

            item.IsExpanded = true;
            item.UpdateLayout();
            container = item;
        }
    }

    private void FlacIntegrityCheck()
    {
        if (CurrentDossier?.FileBytes == null)
        {
            UpdateStatus("Open a file first");
            return;
        }

        // Look for StreamInfo via the tree payloads (set when BuildFlacTree runs).
        var streamInfo = FindFirstPayload<AudioVideoLib.Formats.FlacStreamInfoMetadataBlock>(CurrentDossier.InspectorRoot);
        if (streamInfo == null)
        {
            MessageBox.Show(this, "No FLAC STREAMINFO block found — is this a FLAC file?",
                "FLAC integrity check", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // audio-frames region = from the first byte after the last metadata block to EOF.
        // Approximate: find the "audio frames" node in the tree.
        var audioNode = FindNodeByLabel(CurrentDossier.InspectorRoot, "audio frames");
        var (regionStart, regionEnd) = audioNode != null
            ? (audioNode.StartOffset, audioNode.EndOffset)
            : (0L, 0L);

        var storedHex = streamInfo.MD5 is { Length: 16 } ? Convert.ToHexString(streamInfo.MD5) : "(none)";
        var computedHex = "(not computed)";
        if (regionEnd > regionStart)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(CurrentDossier.FileBytes, (int)regionStart, (int)(regionEnd - regionStart));
            computedHex = Convert.ToHexString(hash);
        }

        var body =
            $"STREAMINFO MD5 (unencoded PCM):\n  {storedHex}\n\n" +
            $"MD5 of audio frames region on disk:\n  {computedHex}\n\n" +
            $"Region: 0x{regionStart:X8}..0x{regionEnd:X8}  ({regionEnd - regionStart:N0} bytes)\n\n" +
            "Note: the STREAMINFO MD5 is the MD5 of the decoded PCM samples.\n" +
            "Verifying it requires a FLAC decoder, which this library does not provide.\n" +
            "The second value above is the MD5 of the *encoded* audio frames as stored in the file.\n" +
            "It is useful for detecting bit-level corruption but will NOT equal the STREAMINFO value.";

        MessageBox.Show(this, body, "FLAC integrity check", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static T? FindFirstPayload<T>(InspectorNode? root) where T : class
    {
        if (root == null)
        {
            return null;
        }

        if (root.Payload is T t)
        {
            return t;
        }

        foreach (var child in root.Children)
        {
            var hit = FindFirstPayload<T>(child);
            if (hit != null)
            {
                return hit;
            }
        }

        return null;
    }

    private static InspectorNode? FindNodeByLabel(InspectorNode? root, string label)
    {
        if (root == null)
        {
            return null;
        }

        if (string.Equals(root.Label, label, StringComparison.OrdinalIgnoreCase))
        {
            return root;
        }

        foreach (var child in root.Children)
        {
            var hit = FindNodeByLabel(child, label);
            if (hit != null)
            {
                return hit;
            }
        }

        return null;
    }

    private void Find()
    {
        if (CurrentDossier?.FileBytes == null || CurrentDossier.FileBytes.Length == 0)
        {
            UpdateStatus("Open a file first");
            return;
        }

        var result = FindDialog.Ask(this);
        if (result is not (byte[] pattern, string display))
        {
            return;
        }

        _lastFindPattern = pattern;
        _lastFindDisplay = display;
        _lastFindOffset = -1;
        FindAt(0);
    }

    private void FindNext()
    {
        if (_lastFindPattern == null)
        {
            Find();
            return;
        }

        FindAt(_lastFindOffset + 1);
    }

    private void FindPrevious()
    {
        if (_lastFindPattern == null)
        {
            Find();
            return;
        }

        var start = _lastFindOffset < 0
            ? (CurrentDossier?.FileBytes.Length ?? 0) - 1
            : _lastFindOffset - 1;
        FindPreviousAt(start);
    }

    private void FindPreviousAt(long startOffset)
    {
        if (CurrentDossier?.FileBytes == null || _lastFindPattern == null)
        {
            return;
        }

        var haystack = CurrentDossier.FileBytes;
        var needle = _lastFindPattern;

        var hit = IndexOfReverse(haystack, needle, (int)Math.Max(0, startOffset));
        if (hit < 0 && startOffset < haystack.Length - needle.Length)
        {
            hit = IndexOfReverse(haystack, needle, haystack.Length - needle.Length);
            if (hit >= 0)
            {
                UpdateStatus($"Wrapped — found {_lastFindDisplay} at 0x{hit:X8}");
            }
        }

        if (hit < 0)
        {
            MessageBox.Show(this, $"Pattern {_lastFindDisplay} not found.", "Find previous",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _lastFindOffset = hit;

        var node = FindDeepestNodeContaining(CurrentDossier.InspectorRoot!, hit);
        if (node != null)
        {
            SelectTreeNode(node);
        }

        UpdateStatus($"Found {_lastFindDisplay} at 0x{hit:X8} ({hit:N0}). Shift+F3 for previous.");
    }

    private void FindAt(long startOffset)
    {
        if (CurrentDossier?.FileBytes == null || _lastFindPattern == null)
        {
            return;
        }

        var haystack = CurrentDossier.FileBytes;
        var needle = _lastFindPattern;

        var hit = IndexOf(haystack, needle, (int)Math.Max(0, startOffset));
        if (hit < 0 && startOffset > 0)
        {
            // Wrap around
            hit = IndexOf(haystack, needle, 0);
            if (hit >= 0)
            {
                UpdateStatus($"Wrapped — found {_lastFindDisplay} at 0x{hit:X8}");
            }
        }

        if (hit < 0)
        {
            MessageBox.Show(this, $"Pattern {_lastFindDisplay} not found.", "Find", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _lastFindOffset = hit;

        var node = FindDeepestNodeContaining(CurrentDossier.InspectorRoot!, hit);
        if (node != null)
        {
            SelectTreeNode(node);
        }

        UpdateStatus($"Found {_lastFindDisplay} at 0x{hit:X8} ({hit:N0}). F3 for next.");
    }

    private static int IndexOf(byte[] haystack, byte[] needle, int start)
    {
        if (needle.Length == 0 || start < 0 || start > haystack.Length - needle.Length)
        {
            return -1;
        }

        var limit = haystack.Length - needle.Length;
        for (var i = start; i <= limit; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }

    private static int IndexOfReverse(byte[] haystack, byte[] needle, int start)
    {
        if (needle.Length == 0 || start < 0)
        {
            return -1;
        }

        var from = Math.Min(start, haystack.Length - needle.Length);
        for (var i = from; i >= 0; i--)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool BuildPath(InspectorNode current, InspectorNode target, List<InspectorNode> path)
    {
        path.Add(current);
        if (current == target)
        {
            return true;
        }

        foreach (var child in current.Children)
        {
            if (BuildPath(child, target, path))
            {
                return true;
            }
        }

        path.RemoveAt(path.Count - 1);
        return false;
    }

    private void UpdateDirtyState()
    {
        var isDirty = CurrentDossier?.HasUnsavedChanges == true;
        UnsavedBanner.Visibility = isDirty ? Visibility.Visible : Visibility.Collapsed;
        var fileName = CurrentDossier != null ? Path.GetFileName(CurrentDossier.FilePath) : string.Empty;
        Title = isDirty ? $"AudioVideoLib Studio - {fileName} *" : "AudioVideoLib Studio";
    }

    private void UpdateStatus(string left)
    {
        StatusLeft.Text = left;
        StatusRight.Text = CurrentDossier == null ? string.Empty : Path.GetFileName(CurrentDossier.FilePath);
        UpdateDirtyState();
    }

    private sealed class RelayCommand(Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
