# ID3v2 Frame Editor Surface — Design Spec

**Date:** 2026-05-06
**Status:** Draft (pending user approval)
**Scope:** AudioVideoLib.Studio — full Add/Edit dialog surface for every ID3v2 frame type, with a tag-format-agnostic foundation that future tag formats (APE, Lyrics3v2, Vorbis, ID3v1) can plug into.

---

## 1. Goal

Bring AudioVideoLib.Studio to feature-parity with full-featured ID3v2 tag editors: every frame class supported by the library can be **added**, **viewed**, and **edited** through a structured dialog, filtered by the active tag version, and reachable through a clean cascading menu plus a power-user "Manage frames" search dialog.

## 2. Decisions (anchor for subsequent design)

The following choices were made during brainstorming and are load-bearing for everything below:

| # | Decision | Rationale |
|---|---|---|
| Q1 | Cover **all 39** ID3v2 frame classes including deprecated and container frames | Studio is meant to be authoritative; users editing real-world files encounter all of these |
| Q2 | **Full structured editor** for every frame (no generic-bytes fallback) | "We're a Studio, behave like one" |
| Q3 | **Filter Add menu by active tag version** | Prevents creating frames that can't be saved |
| Q4 | **Hybrid menu**: keep `Text frame >` and `URL frame >` submenus; group the rest by ID3v2 spec section | Best of cascade + categorisation |
| Q5 | **Inline editable DataGrid** for collection frames (SYLT, ETCO, MLLT, …) | Direct cell editing across all collection editors looks/works the same |
| Q6 | **Two-step "wrap existing"** for CDM/CRM container frames | Mirrors how containers are used in real files |
| Q7 | **Attribute-based registry with auto-discovery** | True parallel work for subagents; zero conflicts on shared files |
| OQ-1 | Order frames within a category by **spec-section number** | Pedagogical, stable, mirrors the spec |
| OQ-2 | Add menu becomes a **smart toggle**: shows "Add ..." for missing frames; for unique-instance frames already in the tag, the entry shows "Edit ..." instead | Cleaner UX than rejection messages |
| OQ-3 | **Keep `BinaryDataDialog` shared** across PRIV/UFID/MCDI; one-dialog-per-frame applies only to *unique* shapes | Don't pay for symmetry that isn't earned |
| OQ-4 | XRVA / RGAD addable under **Experimental** category | Per Q1 — coverage is total |
| OQ-5 | Add **"Manage frames"** toolbar command — flat searchable list dialog as power-user alternative | Discovery aid for the long tail |

## 3. Architecture

```
AudioVideoLib.Studio/
├── Editors/                                  ← Tag-format-agnostic foundation
│   ├── ITagItemEditor.cs
│   ├── TagItemEditorAttribute.cs
│   ├── TagItemEditorRegistry.cs
│   ├── CollectionEditorDialog.cs             Pattern 2 base class
│   ├── WrapperEditorDialog.cs                Pattern 3 base class
│   ├── ManageFramesDialog.xaml(.cs)          Power-user search dialog (OQ-5)
│   └── Id3v2/                                ← ID3v2-specific layer
│       ├── Id3v2FrameEditorAttribute.cs
│       ├── Id3v2FrameCategory.cs
│       ├── Id3v2VersionMask.cs
│       ├── Id3v2AddMenuBuilder.cs
│       ├── TextFrameEditorDialog.xaml(.cs)        family-shared
│       ├── UrlFrameEditorDialog.xaml(.cs)         family-shared
│       ├── UserDefinedTextEditorDialog.xaml(.cs)
│       ├── UserDefinedUrlEditorDialog.xaml(.cs)
│       ├── ApicEditor.cs                          NEW non-Window adapter holding logic + ITagItemEditor
│       ├── ApicEditorDialog.xaml(.cs)             existing dialog — retained, made pure UI
│       ├── UsltEditor.cs / UsltEditorDialog.xaml(.cs)
│       ├── PrivBinaryEditor.cs                    NEW non-Window adapter, wraps shared BinaryDataDialog
│       ├── UfidBinaryEditor.cs                    same shared dialog
│       ├── McdiBinaryEditor.cs                    same shared dialog
│       ├── BinaryDataDialog.xaml(.cs)             existing — retained as shared UI for PRIV/UFID/MCDI
│       └── 30 new editor pairs                    Each frame gets `XxxEditor.cs` (non-Window, registered, holds Load/Save/Validate logic)
│                                                  + `XxxEditorDialog.xaml(.cs)` (pure UI, DataContext=editor)
└── MainWindow.xaml.cs                        Add menu + dispatch reduced to registry-driven calls
```

### 3.1 Three-layer concern split

| Layer | Lives in | Frozen after this project? |
|---|---|---|
| Generic foundation | `Editors/*.cs` (no `Id3v2` prefix) | Yes — APE/Vorbis/etc. plug in via siblings |
| Format-specific layer | `Editors/Id3v2/Id3v2*.cs` | Each future format adds its own siblings |
| Per-item dialogs | `Editors/Id3v2/*EditorDialog.*` | Per-format growth |

### 3.2 Generic contract

```csharp
// Editors/TagItemEditorAttribute.cs
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public abstract class TagItemEditorAttribute : Attribute
{
    public Type ItemType { get; }
    public string MenuLabel { get; init; } = string.Empty;
    public int Order { get; init; }

    protected TagItemEditorAttribute(Type itemType) => ItemType = itemType;
}

// Editors/ITagItemEditor.cs
public interface ITagItemEditor<TItem>
{
    TItem CreateNew(object tag);
    bool Edit(Window owner, TItem item);
}

// Non-generic facade for the registry's reflection layer
internal interface ITagItemEditorAdapter
{
    Type ItemType { get; }
    object CreateNew(object tag);
    bool Edit(Window owner, object item);
}
```

### 3.3 ID3v2 layer

```csharp
// Editors/Id3v2/Id3v2FrameEditorAttribute.cs
public sealed class Id3v2FrameEditorAttribute : TagItemEditorAttribute
{
    public Id3v2FrameCategory Category { get; init; }
    public Id3v2VersionMask SupportedVersions { get; init; } = Id3v2VersionMask.All;
    public bool IsUniqueInstance { get; init; }     // per OQ-2
    public string? KnownIdentifier { get; init; }   // for frames without an (Id3v2Version) ctor (CDM, CRM)
    public Id3v2FrameEditorAttribute(Type frameType) : base(frameType) { }
}

// Editors/Id3v2/Id3v2VersionMask.cs
[Flags]
public enum Id3v2VersionMask
{
    None = 0,
    V220 = 1 << 0,
    V221 = 1 << 1,    // distinct from V220 because Id3v2CompressedDataMetaFrame is hard-coded to v2.2.1
    V230 = 1 << 2,
    V240 = 1 << 3,
    All  = V220 | V221 | V230 | V240,
}

// Editors/Id3v2/Id3v2FrameCategory.cs
public enum Id3v2FrameCategory
{
    TextFrames,
    UrlFrames,
    Identification,
    CommentsAndLyrics,
    TimingAndSync,
    People,
    AudioAdjustment,
    CountersAndRatings,
    Attachments,
    CommerceAndRights,
    EncryptionAndCompression,
    Containers,
    System,
    Experimental,
}
```

### 3.4 Registry

The registry is an *instance* class with a process-wide `Shared` singleton populated at Studio startup. Instance-shaped so tests can build their own scoped registries with controlled editor sets.

```csharp
public sealed class TagItemEditorRegistry
{
    public static TagItemEditorRegistry Shared { get; } = new();

    /// Reflection scan over an assembly. `editorTypeFilter` constrains which classes are
    /// considered (used by tests to register only a small subset).
    public void RegisterFromAssembly(Assembly assembly, Func<Type, bool>? editorTypeFilter = null);

    /// Lookup walks the inheritance chain so a runtime subclass falls back to the editor
    /// registered for its base class. Load-bearing for family editors (`Id3v2TextFrame`
    /// is the registered type; concrete frames returned by readers may be exact-typed).
    public bool TryResolve(Type itemRuntimeType, out ITagItemEditorAdapter editor);

    public IReadOnlyList<RegistrationEntry> Entries { get; }

    public readonly record struct RegistrationEntry(
        Type EditorType, TagItemEditorAttribute Attribute, ITagItemEditorAdapter Adapter);
}
```

**Editor instantiation strategy — two-class pattern.** Every editor consists of:

1. **`XxxEditor`** — a *non-Window* class implementing `ITagItemEditor<TFrame>`. Holds editor state (form-field values, row collection for Pattern 2). Public `Load(frame)`, `Save(frame)`, `Validate(out string? error)` methods carry all the testable logic. `Edit(owner, frame)` constructs an `XxxEditorDialog` Window inside, sets `DataContext = this`, calls `ShowDialog()`, applies state on OK. Decorated with `[Id3v2FrameEditor(...)]`. Registered in the registry.

2. **`XxxEditorDialog`** — the WPF `Window` with XAML + codebehind. Pure UI. Bindings target `XxxEditor` properties via `DataContext`. Constructed fresh by the editor's `Edit(...)` method on each call.

This split sidesteps WPF threading constraints: editor classes can be instantiated by the registry off the UI thread (they're not `Window` subclasses), and tests target the editor class directly without any STA/Dispatcher setup. XAML inflation smoke tests target `XxxEditorDialog` through `[StaFact]` (see §9.5).

The registry instantiates each editor *once* at startup via `Activator.CreateInstance(editorType)`. Editor state is reset on each `Edit(...)` call (via `Load(frame)` overwriting all fields).

`App.xaml.cs.OnStartup` is responsible for calling `TagItemEditorRegistry.Shared.RegisterFromAssembly(typeof(MainWindow).Assembly)` once, after `Application.Current` is constructed.

**Shared-UI editors (BinaryDataDialog).** `BinaryDataDialog` is reused across PRIV, UFID, MCDI (per OQ-3). Three thin editor classes (`PrivBinaryEditor`, `UfidBinaryEditor`, `McdiBinaryEditor`) each register for their frame type, configure the shared dialog appropriately, and forward Load/Save through it. The dialog's existing static `Edit(Window, T)` overloads are renamed to `EditPriv(...)` / `EditUfid(...)` / `EditMcdi(...)` to free the `Edit(...)` name for the `ITagItemEditor` interface (or removed entirely; the editor classes call into the dialog directly).

### 3.5 Menu builder

Split into a pure-data model builder and a thin WPF projection so the model can be tested without WPF.

```csharp
public static class Id3v2AddMenuBuilder
{
    /// Pure function: produces a model tree from the registry filtered against the tag's version
    /// and current frame inventory. Testable without WPF.
    public static Id3v2MenuModel BuildModel(TagItemEditorRegistry registry, Id3v2Tag tag);

    /// Projects the model into a WPF `ContextMenu`, wiring per-item click handlers.
    public static void Populate(ContextMenu menu, Id3v2MenuModel model,
                                Action<Id3v2MenuEntry> onClick);

    /// Pure function: produces the user-visible label for a single registry entry against a tag.
    /// Returns "Add XXX…" for a missing frame, "Edit XXX…" for a unique-instance frame already
    /// in the tag. Extracted so OQ-2's smart toggle is unit-testable in isolation.
    public static string BuildEntryLabel(Id3v2FrameEditorAttribute attribute, Id3v2Tag tag);
}

public sealed record Id3v2MenuEntry(string Label, string FrameIdentifier, bool IsEditExisting);
public sealed record Id3v2MenuCategory(Id3v2FrameCategory Category, string Header,
                                       IReadOnlyList<Id3v2MenuEntry> Entries);
public sealed record Id3v2MenuModel(IReadOnlyList<Id3v2MenuCategory> Categories);
```

Within a registry-backed category, entries are sorted by attribute `Order` (= spec-section number per OQ-1). Within `TextFrames`/`UrlFrames` (family editors, see below), entries come from `Id3v2KnownTextFrameIds[]` / `Id3v2KnownUrlFrameIds[]` and are sorted alphabetically by `FriendlyName`.

**Family-editor special case.** `Id3v2TextFrame` and `Id3v2UrlLinkFrame` each have *one* registry entry (one editor class for double-click dispatch on the base class), but the Add menu must show many entries — one per declared text frame ID (TIT2, TPE1, TALB, ...) and per URL ID (WCOM, WOAR, ...). The menu builder special-cases the `TextFrames` and `UrlFrames` categories: instead of walking the registry attribute, it walks a checked-in static list (`Id3v2KnownTextFrameIds[]` / `Id3v2KnownUrlFrameIds[]`). Each list entry includes a `V220Identifier` field so that on a v2.2 tag the menu emits the 3-char identifier (TT2, TP1, …) rather than the v2.3+ form. Each menu item carries the per-version identifier in its `Id3v2MenuEntry.FrameIdentifier` field.

The family editor exposes a side-channel `CreateNew(tag, identifier)` overload used by the menu special-case path:

```csharp
public sealed class TextFrameEditorDialog : Window, ITagItemEditor<Id3v2TextFrame>
{
    public Id3v2TextFrame CreateNew(object tag)
        => throw new InvalidOperationException(
            "Text frames need an identifier; call CreateNew(tag, identifier) instead.");

    public Id3v2TextFrame CreateNew(object tag, string identifier)
        => new Id3v2TextFrame(((Id3v2Tag)tag).Version, identifier);

    public bool Edit(Window owner, Id3v2TextFrame frame) { ... }
}
```

Double-click dispatch on existing text frames goes through the standard `Edit(owner, frame)` path. UserDefinedText (TXXX) and UserDefinedUrl (WXXX) are *not* family editors — each has a fixed identifier and uses the standard `CreateNew(tag)` contract.

**Family-editor special case.** `Id3v2TextFrame` and `Id3v2UrlLinkFrame` each have *one* registry entry (one editor class for double-click dispatch on the base class), but the Add menu must show many entries — one per declared text frame ID (TIT2, TPE1, TALB, ...) and per URL ID (WCOM, WOAR, ...). The menu builder special-cases the `TextFrames` and `UrlFrames` categories: instead of walking the registry attribute, it walks a checked-in static list `Id3v2KnownTextFrameIds[]` / `Id3v2KnownUrlFrameIds[]` (each entry: identifier + version mask + friendly label). Each menu item carries the identifier as its `Tag` and on click invokes the family editor with that identifier:

```csharp
public sealed class TextFrameEditorDialog : Window, ITagItemEditor<Id3v2TextFrame>
{
    public Id3v2TextFrame CreateNew(object tag)
        => throw new InvalidOperationException(
            "Text frames need an identifier; call CreateNew(tag, identifier) instead.");

    public Id3v2TextFrame CreateNew(object tag, string identifier)
        => new Id3v2TextFrame(((Id3v2Tag)tag).Version, identifier);

    public bool Edit(Window owner, Id3v2TextFrame frame) { ... }
}
```

The custom `CreateNew(tag, identifier)` is invoked by the special-case menu-click handler. Double-click dispatch on existing text frames goes through the standard `Edit(owner, frame)` path. UserDefinedText (TXXX) and UserDefinedUrl (WXXX) are *not* family editors — each has a fixed identifier and uses the standard `CreateNew(tag)` contract.

### 3.6 MainWindow surgery

```csharp
private void AddFrameButton_Click(object sender, RoutedEventArgs e)
{
    var menu = ((Button)sender).ContextMenu!;
    if (CurrentId3v2Tab is not { Tag: Id3v2Tag id3v2 } tab) return;

    var model = Id3v2AddMenuBuilder.BuildModel(TagItemEditorRegistry.Shared, id3v2);
    Id3v2AddMenuBuilder.Populate(menu, model, entry => OnAddOrEditFrame(entry, tab, id3v2));

    menu.PlacementTarget = (UIElement)sender;
    menu.IsOpen = true;
}

private void OnAddOrEditFrame(Id3v2MenuEntry entry, TagTabViewModel tab, Id3v2Tag tag)
{
    Id3v2Frame frame;
    if (entry.IsEditExisting)
    {
        // Edit existing — find by identifier; no commit needed.
        frame = tag.GetFrames().First(f => f.Identifier == entry.FrameIdentifier);
        if (DispatchEdit(frame)) tab.RefreshFrameRow(frame);
        return;
    }

    // Add path: construct frame, open editor, commit to tag *only if user confirms*.
    frame = ConstructFrameFor(entry.FrameIdentifier, tag);
    if (DispatchEdit(frame))
    {
        tab.AddFrame(frame);
    }
}

private bool DispatchEdit(Id3v2Frame frame)
    => TagItemEditorRegistry.Shared.TryResolve(frame.GetType(), out var editor)
       && editor.Edit(this, frame);

private void AdvancedGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    if (sender is not DataGrid grid || grid.SelectedItem is not Id3v2FrameRow row) return;
    if (CurrentId3v2Tab is not { } tab) return;

    if (DispatchEdit(row.Frame)) tab.RefreshRow(row);
    e.Handled = true;
}

private void ManageFramesButton_Click(object sender, RoutedEventArgs e)
{
    if (CurrentId3v2Tab is { Tag: Id3v2Tag id3v2 } tab)
    {
        ManageFramesDialog.ShowFor(this, id3v2, tab);   // OQ-5
    }
}
```

**Cancellation semantics.** For the *Add* path (entry.IsEditExisting == false), the new frame is *only* committed to the tag if the user clicks OK in the editor. A cancelled Add leaves the tag unchanged. This matches user expectations and avoids polluting tags with placeholder frames. For the *Edit* path, the frame already exists in the tag; the editor mutates it in place; on Cancel the editor restores any pre-edit state it captured.

**TagTabs single-add API.** The frame-specific switches in `MainWindow.xaml.cs` (`AddFrameMenuItem_Click`, `AdvancedGrid_MouseDoubleClick`) and the per-frame methods in `TagTabs.cs` are removed. `TagTabs` exposes:
- `void AddFrame(Id3v2Frame frame)` — calls `Tag.SetFrame(frame)` (the library's actual mutation API; `Id3v2Tag.Frames` is a read-only `IEnumerable`), then refreshes the tab's row collection.
- `void RefreshFrameRow(Id3v2Frame frame)` — re-renders the existing row for the given frame.
- `Id3v2FrameRow? FindRow(Id3v2Frame frame)` — used by Manage Frames dialog to scroll to a specific frame.

`ConstructFrameFor(identifier, tag)` is a small helper in MainWindow that maps an identifier to the right concrete frame constructor — handles the family case (text/URL via the `Id3v2KnownTextFrameIds[]` / `Id3v2KnownUrlFrameIds[]` tables) and the registry case (frame type from the matched editor's attribute).

## 4. Editor patterns

Three concrete templates. Each subagent picks one and follows it exactly.

### 4.1 Pattern 1 — Form (most frames)

**Editor class** (non-Window, registered):

```csharp
[Id3v2FrameEditor(typeof(Id3v2XxxFrame),
    Category=Id3v2FrameCategory.X, MenuLabel="...", Order=N,
    SupportedVersions=Id3v2VersionMask.V230|Id3v2VersionMask.V240,
    IsUniqueInstance=true)]
public sealed class XxxEditor : ITagItemEditor<Id3v2XxxFrame>
{
    // Editor state — populated by Load, written back by Save, bound from XAML
    public string FieldA { get; set; } = string.Empty;
    public int FieldB { get; set; }

    public Id3v2XxxFrame CreateNew(object tag) => new Id3v2XxxFrame(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2XxxFrame frame)
    {
        Load(frame);
        var dialog = new XxxEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true) return false;
        Save(frame);
        return true;
    }

    public void Load(Id3v2XxxFrame f) { FieldA = f.A; FieldB = f.B; }
    public void Save(Id3v2XxxFrame f) { f.A = FieldA; f.B = FieldB; }
    public bool Validate(out string? error) { error = null; return true; }
}
```

**Dialog (Window)** — pure UI, bindings against the editor's properties:

```xaml
<Window x:Class="AudioVideoLib.Studio.Editors.Id3v2.XxxEditorDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Width="500" SizeToContent="Height" WindowStartupLocation="CenterOwner">
    <DockPanel Margin="14">
        <StackPanel DockPanel.Dock="Bottom" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,14,0,0">
            <Button Content="OK"     Width="80"                  Click="Ok_Click"     IsDefault="True" />
            <Button Content="Cancel" Width="80" Margin="8,0,0,0" Click="Cancel_Click" IsCancel="True" />
        </StackPanel>
        <StackPanel>
            <TextBlock Text="Field A" />
            <TextBox Text="{Binding FieldA}" />
            <!-- subagent adds remaining fields -->
        </StackPanel>
    </DockPanel>
</Window>
```

```csharp
public partial class XxxEditorDialog : Window
{
    public XxxEditorDialog() => InitializeComponent();

    private void Ok_Click(object s, RoutedEventArgs e)
    {
        var editor = (XxxEditor)DataContext;
        if (!editor.Validate(out var error))
        {
            MessageBox.Show(this, error, "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void Cancel_Click(object s, RoutedEventArgs e) => DialogResult = false;
}
```

### 4.2 Pattern 2 — Form + Grid (collection frames)

Same two-class split as Pattern 1; the editor inherits from `CollectionEditorBase<TFrame, TRow>` for collection-mutation primitives. Dialog has a DataGrid bound to `editor.Entries`.

**Editor base class** (non-Window):

```csharp
public abstract class CollectionEditorBase<TFrame, TRow> : ITagItemEditor<TFrame>
{
    public ObservableCollection<TRow> Entries { get; } = new();

    public void AddRow(TRow row) => Entries.Add(row);
    public void RemoveRow(int index)
    {
        if (index >= 0 && index < Entries.Count) Entries.RemoveAt(index);
    }
    public void MoveUp(int index)
    {
        if (index <= 0 || index >= Entries.Count) return;
        (Entries[index - 1], Entries[index]) = (Entries[index], Entries[index - 1]);
    }
    public void MoveDown(int index)
    {
        if (index < 0 || index >= Entries.Count - 1) return;
        (Entries[index], Entries[index + 1]) = (Entries[index + 1], Entries[index]);
    }

    public abstract TFrame CreateNew(object tag);
    public abstract bool Edit(Window owner, TFrame frame);
    public abstract void LoadRows(TFrame frame);
    public abstract void SaveRows(TFrame frame);
    public abstract bool Validate(out string? error);
}
```

Concrete editor subclasses CollectionEditorBase, sets `Entries` via `LoadRows`, constructs the dialog, calls ShowDialog, then `SaveRows` on OK.

**Dialog XAML** — collection portion below header form:

```xaml
<DockPanel Margin="0,12,0,0">
    <ToolBar DockPanel.Dock="Top">
        <Button Content="Add"    Click="AddRow_Click" />
        <Button Content="Remove" Click="RemoveRow_Click" />
        <Separator />
        <Button Content="Up"     Click="MoveUp_Click" />
        <Button Content="Down"   Click="MoveDown_Click" />
    </ToolBar>
    <DataGrid x:Name="EntriesGrid"
              ItemsSource="{Binding Entries}"
              AutoGenerateColumns="False"
              CanUserAddRows="False"
              CanUserDeleteRows="False"
              SelectionMode="Single"
              MinHeight="200">
        <!-- subagent declares typed columns matching TRow -->
    </DataGrid>
</DockPanel>
```

Click handlers in dialog codebehind delegate to the editor (which is bound as `DataContext`):

```csharp
private void AddRow_Click(object s, RoutedEventArgs e)    { /* subagent calls editor.AddRow(default) or shows a sub-dialog */ }
private void RemoveRow_Click(object s, RoutedEventArgs e) { ((CollectionEditorBase<,>)DataContext).RemoveRow(EntriesGrid.SelectedIndex); }
private void MoveUp_Click(object s, RoutedEventArgs e)    { /* same pattern */ }
private void MoveDown_Click(object s, RoutedEventArgs e)  { /* same pattern */ }
```

### 4.3 Pattern 3 — Wrapper (CDM / CRM, both v2.2-only)

Two-class split. Editor inherits from `WrapperEditorBase<TFrame>` which manages the wrappable-frame snapshot.

**Editor base class** (non-Window):

```csharp
public abstract class WrapperEditorBase<TFrame> : ITagItemEditor<TFrame> where TFrame : Id3v2Frame
{
    public IReadOnlyList<Id3v2Frame> WrappableSnapshot { get; private set; } = Array.Empty<Id3v2Frame>();
    public Id3v2Frame? SelectedChild { get; set; }

    public void TakeSnapshot(Id3v2Tag tag, TFrame self)
    {
        var list = new List<Id3v2Frame>();
        foreach (var f in tag.GetFrames())
        {
            if (ReferenceEquals(f, self)) continue;
            if (f is Id3v2CompressedDataMetaFrame || f is Id3v2EncryptedMetaFrame) continue;
            list.Add(f);
        }
        WrappableSnapshot = list;
    }

    public abstract TFrame CreateNew(object tag);
    public abstract bool Edit(Window owner, TFrame frame);
    public abstract bool Validate(out string? error);
}
```

**Dialog XAML:**

```xaml
<Window x:Class="..." Width="540">
    <StackPanel Margin="14">
        <!-- Wrapper header fields (compression flag / encryption owner+method) — bound to editor properties -->
        <GroupBox Header="Wrapped frame" Margin="0,14,0,0" Padding="10">
            <StackPanel>
                <TextBlock Text="Pick a frame from this tag to wrap:" />
                <ComboBox ItemsSource="{Binding WrappableSnapshot}"
                          SelectedItem="{Binding SelectedChild}"
                          DisplayMemberPath="Identifier" />
                <TextBlock Text="(child frame is removed from the tag and embedded in this wrapper on save)"
                           FontSize="11" Foreground="{DynamicResource TextSecondaryBrush}" />
            </StackPanel>
        </GroupBox>
        <!-- OK / Cancel — Ok_Click validates editor.SelectedChild != null -->
    </StackPanel>
</Window>
```

The editor's `Edit(owner, frame)` calls `TakeSnapshot(tag, self)` to populate the wrappable list, constructs the dialog with `DataContext = this`, and on OK transfers the chosen child into the wrapper's child slot. Removal of the child from the tag is the caller's responsibility (handled in `MainWindow.OnAddOrEditFrame` after a successful save).

## 5. Frame catalog

All 39 frame classes; each gets a row in the registry. ✓ marks existing dialogs that only need an attribute retrofit.

| Spec § | Category | Identifier(s) | Class | Editor file | Pattern | Versions | Unique-per-tag? |
|---|---|---|---|---|---|---|---|
| 4.1 | TextFrames | TIT2/TPE1/TALB/… (~30 IDs)¹ | `Id3v2TextFrame` | `TextFrameEditorDialog` | Form | All | No (per-ID) |
| 4.2 | TextFrames | TXXX (TXX) | `Id3v2UserDefinedTextInformationFrame` | `UserDefinedTextEditorDialog` | Form | All | No |
| 4.3 | UrlFrames | WCOM/WOAR/WPUB/WCOP/WOAF/WORS/WPAY | `Id3v2UrlLinkFrame` | `UrlFrameEditorDialog` | Form | All | No (per-ID) |
| 4.4 | UrlFrames | WXXX (WXX) | `Id3v2UserDefinedUrlLinkFrame` | `UserDefinedUrlEditorDialog` | Form | All | No |
| 4.5 | Identification | UFID (UFI) ✓ | `Id3v2UniqueFileIdentifierFrame` | `BinaryDataDialog` | Form | All | No (one per owner identifier) |
| 4.6 | Identification | MCDI (MCI) ✓ | `Id3v2MusicCdIdentifierFrame` | `BinaryDataDialog` | Form | All | Yes |
| 4.7 | Identification | GRID | `Id3v2GroupIdentificationRegistrationFrame` | `GridEditorDialog` | Form | V230/V240 | No (per owner) |
| 4.8 | Identification | ENCR | `Id3v2EncryptionMethodRegistrationFrame` | `EncrEditorDialog` | Form | V230/V240 | No (per owner) |
| 4.9 | CommentsAndLyrics | COMM (COM) | `Id3v2CommentFrame` | `CommentEditorDialog` | Form | All | No (per lang+desc) |
| 4.10 | CommentsAndLyrics | USER | `Id3v2TermsOfUseFrame` | `UserEditorDialog` | Form | V230/V240 | No (per lang) |
| 4.11 | CommentsAndLyrics | USLT (ULT) ✓ | `Id3v2UnsynchronizedLyricsFrame` | `UsltEditorDialog` | Form | All | No (per lang+desc) |
| 4.12 | CommentsAndLyrics | SYLT (SLT) | `Id3v2SynchronizedLyricsFrame` | `SyltEditorDialog` | Form + Grid | All | No (per lang+desc) |
| 4.13 | TimingAndSync | ETCO (ETC) | `Id3v2EventTimingCodesFrame` | `EtcoEditorDialog` | Form + Grid | All | Yes |
| 4.14 | TimingAndSync | MLLT (MLL) | `Id3v2MpegLocationLookupTableFrame` | `MlltEditorDialog` | Form + Grid | All | Yes |
| 4.15 | TimingAndSync | SYTC (STC) | `Id3v2SyncedTempoCodesFrame` | `SytcEditorDialog` | Form + Grid | All | Yes |
| 4.16 | TimingAndSync | POSS | `Id3v2PositionSynchronizationFrame` | `PossEditorDialog` | Form | V230/V240 | Yes |
| 4.17 | TimingAndSync | ASPI | `Id3v2AudioSeekPointIndexFrame` | `AspiEditorDialog` | Form + Grid | V240 | Yes |
| 4.18 | TimingAndSync | LINK (LNK) | `Id3v2LinkedInformationFrame` | `LinkEditorDialog` | Form | All | No (per URL+id) |
| 4.19 | People | IPLS (IPL) | `Id3v2InvolvedPeopleListFrame` | `IplsEditorDialog` | Form + Grid | V220/V230 | Yes |
| 4.20 | AudioAdjustment | RVAD (RVA) | `Id3v2RelativeVolumeAdjustmentFrame` | `RvadEditorDialog` | Form | V220/V230 | Yes |
| 4.21 | AudioAdjustment | RVA2 | `Id3v2RelativeVolumeAdjustment2Frame` | `Rva2EditorDialog` | Form + Grid | V240 | No (one per identification string) |
| 4.22 | AudioAdjustment | EQUA (EQU) | `Id3v2EqualisationFrame` | `EquaEditorDialog` | Form + Grid | V220/V230 | Yes |
| 4.23 | AudioAdjustment | EQU2 | `Id3v2Equalisation2Frame` | `Equ2EditorDialog` | Form + Grid | V240 | No (one per identification string) |
| 4.24 | AudioAdjustment | REVB (REV)² | `Id3v2ReverbFrame` | `RevbEditorDialog` | Form | All | Yes |
| 4.25 | CountersAndRatings | PCNT (CNT) | `Id3v2PlayCounterFrame` | `PcntEditorDialog` | Form | All | Yes |
| 4.26 | CountersAndRatings | POPM (POP) | `Id3v2PopularimeterFrame` | `PopmEditorDialog` | Form | All | No (per email) |
| 4.27 | Attachments | APIC (PIC) ✓ | `Id3v2AttachedPictureFrame` | `ApicEditorDialog` | Form | All | No (per type+desc) |
| 4.28 | Attachments | GEOB (GEO) | `Id3v2GeneralEncapsulatedObjectFrame` | `GeobEditorDialog` | Form | All | No (per desc) |
| 4.29 | CommerceAndRights | OWNE | `Id3v2OwnershipFrame` | `OwneEditorDialog` | Form | V230/V240 | Yes |
| 4.30 | CommerceAndRights | COMR | `Id3v2CommercialFrame` | `ComrEditorDialog` | Form | V230/V240 | No |
| 4.31 | EncryptionAndCompression | AENC (CRA) | `Id3v2AudioEncryptionFrame` | `AencEditorDialog` | Form | All | No (per owner) |
| 4.32 | Containers | CDM | `Id3v2CompressedDataMetaFrame` | `CdmEditorDialog` | Wrapper | V221 only³ | No |
| 4.33 | Containers | CRM | `Id3v2EncryptedMetaFrame` | `CrmEditorDialog` | Wrapper | V220 only³ | No |
| 4.34 | System | PRIV ✓ | `Id3v2PrivateFrame` | `BinaryDataDialog` | Form | V230/V240 | No (per owner) |
| 4.35 | System | RBUF (BUF) | `Id3v2RecommendedBufferSizeFrame` | `RbufEditorDialog` | Form | All | Yes |
| 4.36 | System | SEEK | `Id3v2SeekFrame` | `SeekEditorDialog` | Form | V240 | Yes |
| 4.37 | System | SIGN | `Id3v2SignatureFrame` | `SignEditorDialog` | Form | V240 | No (per group) |
| 4.38 | Experimental | XRVA | `Id3v2ExperimentalRelativeVolumeAdjustment2Frame` | `XrvaEditorDialog` | Form + Grid | V240 | No |
| 4.39 | Experimental | RGAD | `Id3v2ReplayGainAdjustmentFrame` | `RgadEditorDialog` | Form | V230 | Yes |

¹ Text frame family expands to ~30 specific IDs (TIT2 Title, TPE1 Lead artist, TALB Album, TYER Year, TCON Genre, …). Shared editor; per-ID labels in a static lookup table.
² Lib uses `REVB`; spec says `RVRB`. Out of scope; flagged as separate library-level ticket.
³ The library's `Id3v2CompressedDataMetaFrame` parameterless ctor pins version to `Id3v2v221`; `Id3v2EncryptedMetaFrame` parameterless ctor pins version to `Id3v2v220`. They cannot coexist in a single tag (SetFrame rejects version mismatch). The Containers category therefore shows only CDM on v2.2.1 tags and only CRM on v2.2.0 tags.

**Tally:**
- 5 existing editors (APIC / USLT / PRIV / UFID / MCDI) — attribute retrofit only
- 4 family / one-off editors built in Phase 0 (TextFrame / UserDefinedText / UrlFrame / UserDefinedUrl)
- 3 reference editors built in Phase 0 (one for each pattern: Comment / ETCO / CDM)
- **27 new frame editors** for parallel subagent waves

## 6. Add menu structure

Top-level cascade:

```
[Add Frame ▾]
  ├─ Text frame >       (~30 IDs sorted alphabetically by user-friendly name; per-ID add)
  ├─ URL frame >        (8 IDs, sorted alphabetically)
  ├─ Identification >   UFID, MCDI, GRID, ENCR              (sorted by spec section)
  ├─ Comments & lyrics > COMM, USER, USLT, SYLT
  ├─ Timing & sync >    ETCO, MLLT, SYTC, POSS, ASPI, LINK
  ├─ People >           IPLS
  ├─ Audio adjustment > RVAD, RVA2, EQUA, EQU2, REVB
  ├─ Counters & ratings > PCNT, POPM
  ├─ Attachments >      APIC, GEOB
  ├─ Commerce & rights > OWNE, COMR
  ├─ Encryption & compression > AENC
  ├─ Containers >       CDM, CRM                            (only on v2.2 tags)
  ├─ System >           PRIV, RBUF, SEEK, SIGN
  ├─ Experimental >     XRVA, RGAD
  ├─ ─────────────
  └─ Manage frames…     → opens ManageFramesDialog (OQ-5)
```

Visibility rules:
- Categories with zero valid editors for the active tag version are hidden entirely (e.g. `Containers` is hidden on v2.3 / v2.4)
- Within a non-empty category, individual entries hidden if `(SupportedVersions & activeVersionFlag) == 0`
- Per OQ-2, a unique-instance frame already in the tag shows as `Edit XXX…` instead of `Add XXX…`; click resolves to the same editor and refreshes the existing row

## 7. Manage Frames dialog (OQ-5)

Power-user alternative to the cascading menu. Reachable from a toolbar button next to "Add Frame".

```
┌─ Manage frames ─────────────────────────────────────────────┐
│ Search: [______________________________]                    │
│                                                             │
│ ┌──────┬──────────────────────────┬──────────────┬────────┐ │
│ │ ID   │ Name                     │ Category     │ Status │ │
│ ├──────┼──────────────────────────┼──────────────┼────────┤ │
│ │ APIC │ Attached picture         │ Attachments  │ —      │ │
│ │ COMM │ Comment                  │ Comments…    │ in tag │ │
│ │ ETCO │ Event timing codes       │ Timing & sync│ —      │ │
│ │ ...  │ ...                      │ ...          │ ...    │ │
│ └──────┴──────────────────────────┴──────────────┴────────┘ │
│                                                             │
│         [Add / Edit selected]            [Close]            │
└─────────────────────────────────────────────────────────────┘
```

Behaviour:
- Lists every editor registered for the active tag's version (filter applied at open time)
- Search box matches case-insensitively against ID + Name + Category
- "Status" column shows "in tag" if at least one frame of this type already exists, else "—"
- Action button is "Edit selected" for unique-instance frames already in the tag, otherwise "Add selected"
- Double-click on a row triggers the same action

## 8. Version-mask handling

Three rules, three places:

1. **Add menu filter (build time).** Categories and items hidden if `SupportedVersions & active.VersionFlag == 0`.
2. **Pre-add validation.** `editor.CreateNew(tag)` constructs the frame; the frame's own constructor validates the version. The menu filter normally prevents this from firing; the throw is a safety net.
3. **Edit-time tolerance.** When opening an existing frame, `frame.Version` is already locked. Editor reads it and disables/hides version-incompatible controls (e.g. POPM in v2.2 has slightly different field semantics from v2.3+). The version of an existing frame is never changed by an editor.

V2.2 → V2.3+ tag promotion: existing feature, frames whose IDs were renamed (3-char → 4-char) are handled by frame classes themselves. Editor framework is uninvolved.

## 9. Test strategy

### 9.1 Per-editor (in `AudioVideoLib.Studio.Tests`)

- **Round-trip headless:** instantiate dialog (no `ShowDialog`), `Load(frame)`, `Save(into a fresh frame)`, assert frames are equal. Catches Load/Save asymmetry.
- **Validation:** invalid input → `Validate()` returns false with non-empty error message.
- **Cancel semantics:** dialog closed via Cancel leaves the input frame untouched.
- **Pattern-2 specific:** Add/Remove/Up/Down preserve order invariants on the underlying collection.

### 9.2 Registry-level (one test class)

- Every concrete `Id3v2Frame` subclass in `AudioVideoLib.dll` has exactly one registered editor — fails the build if a frame is added without an editor.
- No duplicate registrations.
- Every `SupportedVersions` mask is non-zero.
- Every `MenuLabel` non-empty and ≤ 60 chars.
- Every editor implements `ITagItemEditor<>` matching its attribute's `ItemType`.

### 9.3 Menu builder

- For each version (v2.2, v2.3, v2.4), build the Add menu → assert expected category/ID set.
- Snapshot test: serialize menu structure to text, compare against a checked-in golden file per version.

### 9.4 Manage Frames dialog

- Search filtering tests: keyword matches ID, Name, Category.
- "Status" column reflects current tag state.

### 9.5 Integration smoke (scripted, requires STA)

WPF `Window` subclasses cannot be instantiated on a non-STA thread, and the default xUnit thread is MTA. Two consequences:

1. **Tests of editor logic** (`Load`, `Save`, `Validate`, row collection semantics) must NOT instantiate the dialog `Window`. Logic is extracted into a non-Window helper class per editor (`XxxEditorLogic`); the dialog codebehind delegates to it. The helper class has no WPF dependencies and tests it as plain C# under default xUnit threading.

2. **XAML inflation smoke tests** (catching `DynamicResource` typos, missing converters, malformed bindings) DO require STA + a constructed `Application` context. These run via `[StaFact]` from the `Xunit.StaFact` package, with a per-class `Application` initialiser. One smoke test per editor: instantiate dialog, call `Measure` / `Arrange`, dispose. Failure here means the XAML doesn't compile or a resource lookup is broken; passing means the dialog *can* render.

The split keeps the bulk of the test suite fast and threading-clean, while still exercising XAML on a separate path.

### 9.5b Library API discovery — frames mutation

The library's actual mutation API for `Id3v2Tag` is `SetFrame(frame)` and `RemoveFrame(frame)`. `Id3v2Tag.Frames` is a read-only `IEnumerable`. All editor / TagTabs code in this design uses `SetFrame` / `RemoveFrame`; tests build tags using the same API.

Likewise, frame classes' constructors vary: most have a `(Id3v2Version)` ctor, but a few (notably `Id3v2CompressedDataMetaFrame`, `Id3v2EncryptedMetaFrame`) only have a parameterless ctor that fixes the version internally. The `Id3v2FrameEditorAttribute` therefore exposes an optional `KnownIdentifier` property that, when present, overrides reflection-based identifier resolution. Editors for frames without a `(Id3v2Version)` ctor must set `KnownIdentifier` explicitly.

### 9.6 Non-UI logic (mandatory, separately testable from any dialog instance)

Every piece of logic listed below must be implemented in a way that allows it to be tested *without* spinning up a Window. Practically: extract Load/Save and validation into testable methods that take frame + plain-data objects, not control references.

**Foundation:**
- `TagItemEditorRegistry`: reflection scan finds all `[TagItemEditor]`-attributed classes; lookup by item runtime type returns the right adapter; duplicate registrations throw at `Initialize()`; missing `ITagItemEditor<>` implementation throws.
- `Id3v2VersionMask`: `Id3v2VersionMask.From(Id3v2Version)` returns the right single-bit flag; intersection logic (`(supported & active) != 0`) covered.

**Menu construction (`Id3v2AddMenuBuilder`):**
- For each of v2.2 / v2.3 / v2.4: `Build(...)` produces the expected category set with the expected entries (golden snapshot).
- Family-editor expansion: `TextFrames` category produces N entries (one per known text frame ID valid for the version) all targeting `TextFrameEditorDialog` with the right identifier in the menu-item tag. Same for `UrlFrames`.
- Smart-toggle decision (per OQ-2): given a tag with an existing unique-instance frame X, `Build` produces "Edit X..." for that entry; given the tag without X, produces "Add X...". Tested as a pure function `BuildEntryLabel(editor, tag) → string`.
- Empty-category collapse: `Containers` category has zero entries on v2.3/v2.4 → category not present in the menu.

**`Id3v2KnownTextFrameIds[]` / `Id3v2KnownUrlFrameIds[]`:**
- Every entry's `(identifier, versionMask)` round-trips through `Id3v2TextFrame` / `Id3v2UrlLinkFrame` constructors without throwing for at least one supported version.
- v2.2 entries' 3-char IDs map to v2.3+ 4-char IDs consistent with the lib's existing identifier-rename table.
- No duplicates; no gaps versus the ID3v2 spec (test enumerates spec IDs and asserts each is in the table).

**Pattern base classes:**
- `CollectionEditorBase<TFrame, TRow>`: `AddRow()`, `RemoveRow(int)`, `MoveUp(int)`, `MoveDown(int)` operate on a public `ObservableCollection<TRow> Entries` and preserve order; no-op when index out of range. Tested as a plain class (no WPF).
- `WrapperEditorBase<TFrame>`: `TakeSnapshot(tag, self)` excludes self and excludes other wrapper frames (CDM/CRM); the wrapper's child slot reflects user selection on OK. Tested with a tag built using `tag.SetFrame(...)`.

**Per-editor (every Pattern-1, Pattern-2, Pattern-3 editor):**
- The editor class itself is non-Window — it implements `ITagItemEditor<TFrame>` and is therefore directly testable under default xUnit threading.
- Tests instantiate the *editor* directly (no `Window`, no `ShowDialog()`, no STA), call `Load(input)` / `Save(output)`, assert equality. This is the *primary* correctness gate per editor; XAML smoke (§9.5) is secondary.
- For collection editors: `LoadRows(frame)` / `SaveRows(frame)` are public; round-trip tested with empty / single-row / many-rows inputs.

**Editor → test-class meta-test:** every concrete editor class (every `[Id3v2FrameEditor]`-attributed class) must have a matching `XxxEditorTests` in the test project. A reflection-based meta-test enumerates editor classes and asserts a corresponding test class exists in the test assembly by naming convention.

**API discovery for tests:** tests build tags via `Id3v2Tag.SetFrame(frame)` (the library's actual mutation API). `tag.GetFrames()` enumerates them.

**Manage Frames dialog (OQ-5):**
- `ManageFramesViewModel.ApplyFilter(query)` returns the right filtered subset for queries matching ID / Name / Category (case-insensitive, partial). Tested without instantiating the dialog.
- `ManageFramesViewModel.GetActionLabel(entry, tag)` returns "Add" / "Edit" / disabled-state per smart-toggle rules.

**Coverage gate:** the test suite must include at least one round-trip Load/Save test per editor in §5 (39 frames, but family editors share one test class with parameterised cases per identifier). CI fails if any editor lacks a round-trip test, enforced by a meta-test that walks the registry and looks up `XxxEditorDialogTests` by naming convention.

## 10. Work split

### 10.1 Phase 0 — Coordinator builds the foundation (sequential)

| # | File(s) / Task | Purpose |
|---|---|---|
| 0.1 | `Editors/ITagItemEditor.cs`, `Editors/TagItemEditorAttribute.cs`, `Editors/TagItemEditorRegistry.cs` | Tag-format-agnostic foundation |
| 0.2 | `Editors/Id3v2/Id3v2FrameEditorAttribute.cs`, `Id3v2FrameCategory.cs`, `Id3v2VersionMask.cs`, `Id3v2AddMenuBuilder.cs` | ID3v2-specific layer |
| 0.3 | `Editors/CollectionEditorDialog.cs`, `Editors/WrapperEditorDialog.cs`, dark-theme DataGrid style | Pattern 2 + 3 base classes |
| 0.4 | `TextFrameEditorDialog`, `UrlFrameEditorDialog`, `UserDefinedTextEditorDialog`, `UserDefinedUrlEditorDialog` | Family editors |
| 0.5 | `CommentEditor` + `CommentEditorDialog` (Pattern 1 reference), `EtcoEditor` + `EtcoEditorDialog` (Pattern 2 reference), `CdmEditor` + `CdmEditorDialog` (Pattern 3 reference) | Reference editors used by Wave 1+ subagents |
| 0.6 | Create `ApicEditor`, `UsltEditor`, `PrivBinaryEditor`, `UfidBinaryEditor`, `McdiBinaryEditor` adapter classes (decorated with `[Id3v2FrameEditor]`); existing dialogs (`ApicEditorDialog`, `UsltEditorDialog`, `BinaryDataDialog`) become pure UI; rename `BinaryDataDialog`'s static `Edit(Window, T)` overloads or remove them entirely | Existing editors join the registry via the two-class adapter pattern |
| 0.7 | Strip per-frame `switch` blocks from `MainWindow.xaml.cs`; replace with registry-driven dispatch + `Id3v2AddMenuBuilder`. Add path constructs frame, opens editor, commits to tag *only on OK* (cancel leaves tag unchanged). Edit path mutates existing frame in place. | One-time MainWindow surgery |
| 0.8 | Replace per-frame `Add*` methods in `TagTabs.cs` with single `AddFrame(Id3v2Frame frame)` method | Uniform add path |
| 0.9 | Registry self-test: (a) DEBUG-only `App.xaml.cs.OnStartup` assertion that runs after `RegisterFromAssembly` and verifies no duplicates / masks non-zero / `MenuLabel` non-empty; (b) xUnit `RegistryCompletenessTests` that lists missing editors as informational output during waves and gates a hard "every concrete frame has an editor" assertion behind a `RegistrationComplete` flag flipped in Phase 2.1 | Catch missing editors / quality gaps |
| 0.10 | `ManageFramesDialog.xaml(.cs)` + toolbar button wiring | Power-user dialog (OQ-5) |

After Phase 0: framework working with 5 retrofit + 4 family + 3 reference = 12 editors live. Add menu populated; missing editors visible as "(no editor)" placeholders that gate-fail the registry self-test (until completed in Wave 3).

### 10.2 Phase 1 — Parallel subagent waves (3 waves: 9 / 8 / 10)

Coordinator dispatches each wave's 9 tasks in parallel, reviews each via `superpowers:subagent-driven-development` two-stage pattern (spec compliance, then code quality), then waits for full-wave green before dispatching the next wave.

**Wave 1 — Pattern 1 (Form), simple:**
| # | Editor | Frame |
|---|---|---|
| 1.1 | `UserEditorDialog` | USER (Terms of use) |
| 1.2 | `GridEditorDialog` | GRID |
| 1.3 | `EncrEditorDialog` | ENCR |
| 1.4 | `PossEditorDialog` | POSS |
| 1.5 | `LinkEditorDialog` | LINK |
| 1.6 | `RbufEditorDialog` | RBUF |
| 1.7 | `SeekEditorDialog` | SEEK |
| 1.8 | `PcntEditorDialog` | PCNT |
| 1.9 | `PopmEditorDialog` | POPM |

**Wave 2 — Pattern 1 (Form), complex / multi-field (8 tasks):**
| # | Editor | Frame |
|---|---|---|
| 2.1 | `OwneEditorDialog` | OWNE |
| 2.2 | `ComrEditorDialog` | COMR |
| 2.3 | `AencEditorDialog` | AENC |
| 2.4 | `GeobEditorDialog` | GEOB |
| 2.5 | `SignEditorDialog` | SIGN |
| 2.6 | `RvadEditorDialog` | RVAD |
| 2.7 | `RevbEditorDialog` | REVB |
| 2.8 | `RgadEditorDialog` | RGAD |

**Wave 3 — Pattern 2 (Form+Grid) + Pattern 3 (Wrapper) (10 tasks):**
| # | Editor | Frame |
|---|---|---|
| 3.1 | `SyltEditorDialog` | SYLT |
| 3.2 | `MlltEditorDialog` | MLLT |
| 3.3 | `SytcEditorDialog` | SYTC |
| 3.4 | `AspiEditorDialog` | ASPI |
| 3.5 | `IplsEditorDialog` | IPLS |
| 3.6 | `Rva2EditorDialog` | RVA2 |
| 3.7 | `EquaEditorDialog` | EQUA |
| 3.8 | `Equ2EditorDialog` | EQU2 |
| 3.9 | `CrmEditorDialog` | CRM (Wrapper) |
| 3.10 | `XrvaEditorDialog` | XRVA (Pattern 2; shares row schema with RVA2 — relocated from Wave 2) |

**Per-subagent contract:**
- **Input:** frame class name; editor pattern (1/2/3); spec section reference; field list; reference editor file from Phase 0.5
- **Output:**
  - `XxxEditorDialog.xaml` + `.xaml.cs` (attribute-decorated)
  - `Load(frame)`, `Save(frame)`, `Validate()` exposed as public methods so they're testable headlessly (no UI thread, no `ShowDialog()`)
  - For Pattern-2 editors: `LoadRows(frame)` and `SaveRows(frame, rows)` also public
  - `XxxEditorDialogTests` with at minimum: round-trip Load/Save (default frame + populated frame + edge cases); `Validate()` with valid + invalid input matrices; for Pattern-2 also Add/Remove/MoveUp/MoveDown semantics on the row collection
- **Forbidden:** modifying `MainWindow.xaml.cs`, `TagTabs.cs`, foundation files, any other editor's files
- **Done = spec reviewer ✅ + code quality reviewer ✅ + all editor tests pass + the meta-test "every editor has a XxxEditorDialogTests class" still passes**

### 10.3 Phase 2 — Coordinator finalisation

| # | Task |
|---|---|
| 2.1 | Re-enable the registry self-test "every frame has an editor" assertion (it failed harmlessly during waves) |
| 2.2 | Update Manage Frames dialog golden files for full registry |
| 2.3 | Run full Release build + full test suite + DocFX clean |
| 2.4 | Update `docs/release-notes.md` "(next release)" entry |
| 2.5 | Final code review across the entire branch |
| 2.6 | Squash-merge to master + push (with explicit user authorisation) |

## 11. Non-goals

- APE / Lyrics3v2 / Vorbis comments / ID3v1 editor surfaces (foundation accommodates them; implementation deferred)
- Editing tag-level header / extended header / footer / padding
- Localised UI (English only)
- Fixing `REVB` vs spec `RVRB` library identifier (separate ticket)
- Changes to the `AudioVideoLib` library itself (Studio-only changes)
- A "raw bytes for any frame" override mode
- Frame deletion / reordering / drag-drop in the tag's frame list
- Changes to v2.2→v2.3 tag promotion logic

## 12. Risks

| Risk | Mitigation |
|---|---|
| 27 dialogs by 27 different subagents drift visually | Strict template adherence in code review; reference editors in Phase 0.5 are canonical |
| Dark-theme DataGrid styling untested | Phase 0.3 builds + smoke-tests dark-theme DataGrid in `EtcoEditorDialog` reference editor before Wave 3 |
| POPM and other v2.2-vs-v2.3+ field-shape variations | Editor Load/Save reads `frame.Version` and disables/hides version-incompatible controls |
| Inconsistent validation across editors | Pattern templates include `Validate()` hook + shared `ValidationFeedback.Show(...)` helper |
| Registry self-test fails during waves because not all editors registered yet | "Every frame has an editor" assertion is gated by a `[FrameEditorComplete]` flag the coordinator sets only at Phase 2.1 |
| Breaking existing tests calling `tab.AddTextFrame(...)` | Phase 0.8 keeps old methods as `[Obsolete]` thin wrappers for one release |

## 13. Open questions resolved during brainstorm (no further answers needed)

OQ-1 through OQ-5 — answered. See §2.
