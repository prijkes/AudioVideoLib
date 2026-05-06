# ID3v2 Frame Editors — Wave 1 (9 simple Form editors)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Each task in this plan is one frame editor and is **dispatched to a fresh subagent in parallel**. Subagents touch ONLY their own files (`XxxEditor.cs`, `XxxEditorDialog.xaml(.cs)`, `XxxEditorTests.cs`); never `MainWindow.xaml.cs`, `TagTabs.cs`, foundation files, or other editors.

**Goal:** Add 9 Pattern-1 (Form) editor pairs registered against the framework built in Phase 0. After this wave, `RegistryCompletenessTests` reports 18 missing editors (down from 27).

**Architecture:** Two-class pattern per spec §4.1: `XxxEditor` (non-Window, registered, holds `Load`/`Save`/`Validate` logic) + `XxxEditorDialog.xaml(.cs)` (pure-UI Window, `DataContext = editor`, constructed fresh per `Edit(...)` call). Reference editor: `CommentEditor` (built in Phase 0 Task 8) — copy its shape exactly.

**Reference:** Spec `specs/2026-05-06-id3v2-frame-editors-design.md` §4.1, §5 frame catalog. Phase 0 plan `plans/2026-05-06-id3v2-frame-editors-phase0-foundation.md` Task 8 (CommentEditor) is the canonical Form-pattern example.

---

## Shared per-task contract (every task in this wave)

**Files (per editor — replace `Xxx` with editor name):**
- Create: `AudioVideoLib.Studio/Editors/Id3v2/XxxEditor.cs` (decorated with `[Id3v2FrameEditor(...)]`, implements `ITagItemEditor<Id3v2YyyFrame>`, `INotifyPropertyChanged`)
- Create: `AudioVideoLib.Studio/Editors/Id3v2/XxxEditorDialog.xaml` (Pattern-1 shell from spec §4.1)
- Create: `AudioVideoLib.Studio/Editors/Id3v2/XxxEditorDialog.xaml.cs` (codebehind: `Ok_Click` validates editor, `Cancel_Click` sets `DialogResult = false`)
- Create: `AudioVideoLib.Studio.Tests/Editors/Id3v2/XxxEditorTests.cs` (round-trip + validation + cancel-untouched)

**Forbidden:** modifying `MainWindow.xaml.cs`, `TagTabs.cs`, `App.xaml`, foundation files, any other editor's files.

**Per-task TDD steps (uniform):**

1. **Step 1 — Write failing tests.** Round-trip Load/Save covering all fields. Validate matrix per the editor's rules. Cancel semantics: instantiate editor, do not call ShowDialog, assert untouched frame.
2. **Step 2 — Run tests.** Expected: FAIL (types don't exist).
3. **Step 3 — Write `XxxEditor.cs`.** Use the per-task code skeleton in this plan. Decorate with the exact `[Id3v2FrameEditor]` attribute below.
4. **Step 4 — Write `XxxEditorDialog.xaml`.** Bind to editor's properties. Reference `CommentEditorDialog` for the OK/Cancel + DockPanel/StackPanel layout.
5. **Step 5 — Write `XxxEditorDialog.xaml.cs`.** Trivial codebehind (Ok_Click / Cancel_Click only).
6. **Step 6 — Run tests.** Expected: PASS. If a XAML-inflation `[StaFact]` is included, run with `xunit.runner.json` configured for STA collection — out of scope for this wave; defer to Phase 2.
7. **Step 7 — Commit** with the message specified per task.

**Pattern-1 editor skeleton** (copy and adapt fields):

```csharp
namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2YyyFrame),
    Category = Id3v2FrameCategory.<<CATEGORY>>,
    MenuLabel = "<<USER-VISIBLE LABEL>>",
    Order = <<SPEC-SECTION-NUMBER>>,
    SupportedVersions = <<MASK>>,
    IsUniqueInstance = <<TRUE/FALSE>>)]
public sealed class XxxEditor : ITagItemEditor<Id3v2YyyFrame>, INotifyPropertyChanged
{
    // === Editor state — populated by Load, written back by Save, bound from XAML ===
    private <<type>> _fieldA;
    public <<type>> FieldA { get => _fieldA; set => Set(ref _fieldA, value); }
    // ... per-frame fields ...

    public Id3v2YyyFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2YyyFrame frame)
    {
        Load(frame);
        var dialog = new XxxEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true) return false;
        Save(frame);
        return true;
    }

    public void Load(Id3v2YyyFrame f) { /* per-task */ }
    public void Save(Id3v2YyyFrame f) { /* per-task */ }
    public bool Validate(out string? error) { /* per-task; default true */ error = null; return true; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T storage, T value, [CallerMemberName] string? prop = null)
    {
        if (Equals(storage, value)) return;
        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
```

**Pattern-1 dialog skeleton:**

```xaml
<Window x:Class="AudioVideoLib.Studio.Editors.Id3v2.XxxEditorDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Width="500" SizeToContent="Height" WindowStartupLocation="CenterOwner"
        Title="<<DIALOG TITLE>>">
    <DockPanel Margin="14">
        <StackPanel DockPanel.Dock="Bottom" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,14,0,0">
            <Button Content="OK"     Width="80"                  Click="Ok_Click"     IsDefault="True" />
            <Button Content="Cancel" Width="80" Margin="8,0,0,0" Click="Cancel_Click" IsCancel="True" />
        </StackPanel>
        <StackPanel>
            <!-- per-task labeled fields, one StackPanel per field, label above control -->
        </StackPanel>
    </DockPanel>
</Window>
```

**Pattern-1 codebehind skeleton (identical for every editor — copy verbatim):**

```csharp
namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

public partial class XxxEditorDialog : Window
{
    public XxxEditorDialog() => InitializeComponent();

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var editor = (XxxEditor)DataContext;
        if (!editor.Validate(out var error))
        {
            MessageBox.Show(this, error, "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
```

---

## Task 1.1: `UserEditor` — Terms of use (USER)

**Frame:** `Id3v2TermsOfUseFrame` (v2.3 / v2.4 only).
**Properties (verified):** `TextEncoding` (`Id3v2FrameEncodingType`), `Language` (3-char string), `Text` (string).
**Spec section:** 4.10 (Order = 10). Category: `CommentsAndLyrics`. `IsUniqueInstance = false` (per language).
**Identifier:** `USER` (no v2.2 form).

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2TermsOfUseFrame),
    Category = Id3v2FrameCategory.CommentsAndLyrics,
    MenuLabel = "Terms of use (USER)",
    Order = 10,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
```

**Editor fields:** `Encoding` (Id3v2FrameEncodingType), `Language` (string, default "eng"), `Text` (string).

**Load:**
```csharp
public void Load(Id3v2TermsOfUseFrame f)
{
    Encoding = f.TextEncoding;
    Language = f.Language;
    Text = f.Text;
}
```

**Save:**
```csharp
public void Save(Id3v2TermsOfUseFrame f)
{
    f.TextEncoding = Encoding;
    f.Language = Language;
    f.Text = Text;
}
```

**Validate:** Language must be 3 chars (`error = "Language must be a 3-character ISO-639-2 code (e.g. \"eng\")."`).

**Tests:**
- `LoadSave_RoundTrip` — populate frame at v2.4 with `TextEncoding=UTF16LittleEndian`, `Language="eng"`, `Text="Sample terms text."`, round-trip, assert all three fields equal on the copy.
- `Validate_LanguageMustBe3Chars` — Theory with `("en", false)`, `("eng", true)`, `("ENGL", false)`, `("", false)`.
- `CreateNew_UsesTagVersion` — pass tag at v2.3, assert returned frame `.Version == Id3v2v230`.

**Dialog fields (top-to-bottom):** Encoding combo (ItemsSource={StaticResource Id3v2EncodingValues}), Language textbox (max 3 chars), Text multiline textbox (`AcceptsReturn=True MinHeight=160`).

**Commit:** `feat(studio): UserEditor (USER, terms of use)`.

---

## Task 1.2: `GridEditor` — Group identification registration (GRID)

**Frame:** `Id3v2GroupIdentificationRegistrationFrame` (v2.3 / v2.4).
**Properties (verified):** `OwnerIdentifier` (string), `GroupSymbol` (byte, range 0x80-0xF0 per spec), `GroupDependentData` (byte[], may be empty).
**Spec section:** 4.7 (Order = 7). Category: `Identification`. `IsUniqueInstance = false` (per OwnerIdentifier).
**Identifier:** `GRID` (no v2.2 form).

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2GroupIdentificationRegistrationFrame),
    Category = Id3v2FrameCategory.Identification,
    MenuLabel = "Group identification (GRID)",
    Order = 7,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
```

**Editor fields:** `OwnerIdentifier` (string), `GroupSymbol` (byte), `GroupDependentData` (byte[] — exposed as a `string HexData` for binding, or load via OpenFileDialog like `BinaryDataDialog`).

For Wave 1 simplicity: expose two text fields (Owner, GroupSymbol-as-int 128–240) plus a `Load from file…` / `Clear data` button pair backed by a `byte[] _data` field, mirroring `BinaryDataDialog`'s file-load pattern.

**Validate:**
- `OwnerIdentifier` must be non-empty (frame uniqueness key per spec §4.27).
- `GroupSymbol` must be in the inclusive range 0x80–0xF0 (128–240). Error: `"Group symbol must be between 0x80 and 0xF0."`

**Tests:**
- Round-trip with sample owner / symbol / 4-byte data.
- Validate matrix: `(GroupSymbol = 0x7F, false)`, `(0x80, true)`, `(0xF0, true)`, `(0xF1, false)`. `(OwnerIdentifier = "", false)`.

**Commit:** `feat(studio): GridEditor (GRID, group identification)`.

---

## Task 1.3: `EncrEditor` — Encryption method registration (ENCR)

**Frame:** `Id3v2EncryptionMethodRegistrationFrame` (v2.3 / v2.4).
**Properties (verified):** `OwnerIdentifier` (string), `MethodSymbol` (byte, 0x80-0xF0), `EncryptionData` (byte[]).
**Spec section:** 4.8 (Order = 8). Category: `Identification`. `IsUniqueInstance = false`.
**Identifier:** `ENCR`.

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2EncryptionMethodRegistrationFrame),
    Category = Id3v2FrameCategory.Identification,
    MenuLabel = "Encryption method (ENCR)",
    Order = 8,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
```

Same shape as GRID — three fields, byte symbol with the 0x80-0xF0 validation, OwnerIdentifier non-empty validation.

**Tests:** Round-trip, owner-empty validation, symbol-range validation.

**Commit:** `feat(studio): EncrEditor (ENCR, encryption method)`.

---

## Task 1.4: `PossEditor` — Position synchronization (POSS)

**Frame:** `Id3v2PositionSynchronizationFrame` (v2.3 / v2.4).
**Properties (verified):** `TimeStampFormat` (`Id3v2TimeStampFormat`), `Position` (long).
**Spec section:** 4.16 (Order = 16). Category: `TimingAndSync`. `IsUniqueInstance = true`.
**Identifier:** `POSS`.

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2PositionSynchronizationFrame),
    Category = Id3v2FrameCategory.TimingAndSync,
    MenuLabel = "Position synchronization (POSS)",
    Order = 16,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = true)]
```

**Editor fields:** `TimeStampFormat` (combo bound to `Id3v2TimeStampFormatValues` resource from App.xaml), `Position` (long, displayed as decimal).

**Validate:** `Position >= 0`. Error: `"Position must be non-negative."`

**Tests:** Round-trip with `TimeStampFormat=AbsoluteTimeMilliseconds`, `Position=12345`. Validate negative.

**Dialog combo:**
```xaml
<ComboBox ItemsSource="{Binding Source={StaticResource Id3v2TimeStampFormatValues}}"
          SelectedValue="{Binding TimeStampFormat}" />
```

**Commit:** `feat(studio): PossEditor (POSS, position synchronization)`.

---

## Task 1.5: `LinkEditor` — Linked information (LINK / LNK)

**Frame:** `Id3v2LinkedInformationFrame` (all versions).
**Properties (verified):** `FrameIdentifier` (string, 4-char in v2.3+ / 3-char in v2.2), `Url` (string), `AdditionalIdData` (string).
**Spec section:** 4.18 (Order = 18). Category: `TimingAndSync`. `IsUniqueInstance = false` (per URL + frame-id pair).
**Identifier:** `LINK` (v2.2: `LNK`).

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2LinkedInformationFrame),
    Category = Id3v2FrameCategory.TimingAndSync,
    MenuLabel = "Linked information (LINK)",
    Order = 18,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
```

**Editor fields:** `FrameIdentifier` (string), `Url` (string), `AdditionalIdData` (string).

**Validate:**
- `FrameIdentifier`: 3 chars on v2.2, 4 chars on v2.3+. (Inspect tag's version via the `Edit` callsite — pass the version to the editor through the frame's `Version` property after `Load`.)
- `Url`: non-empty + `Uri.TryCreate(Url, UriKind.Absolute, out _)`.

Simpler approach for Wave 1: validate `FrameIdentifier.Length` against the loaded frame's `Version`; cache the version on Load.

**Tests:** Round-trip; identifier-length theory matrix per version; URL validity.

**Commit:** `feat(studio): LinkEditor (LINK, linked information)`.

---

## Task 1.6: `RbufEditor` — Recommended buffer size (RBUF / BUF)

**Frame:** `Id3v2RecommendedBufferSizeFrame` (all versions).
**Properties (verified):** `BufferSize` (int), `UseEmbeddedInfo` (bool), `OffsetToNextTag` (int).
**Spec section:** 4.35 (Order = 35). Category: `System`. `IsUniqueInstance = true`.
**Identifier:** `RBUF` (v2.2: `BUF`).

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2RecommendedBufferSizeFrame),
    Category = Id3v2FrameCategory.System,
    MenuLabel = "Recommended buffer size (RBUF)",
    Order = 35,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = true)]
```

**Editor fields:** `BufferSize` (int), `UseEmbeddedInfo` (bool, CheckBox), `OffsetToNextTag` (int).

**Validate:** `BufferSize >= 0`, `OffsetToNextTag >= 0`. Errors are field-specific.

**Tests:** Round-trip; non-negative validations.

**Commit:** `feat(studio): RbufEditor (RBUF, recommended buffer size)`.

---

## Task 1.7: `SeekEditor` — Seek frame (SEEK)

**Frame:** `Id3v2SeekFrame` (v2.4 only).
**Properties (verified):** `MinimumOffsetToNextTag` (int).
**Spec section:** 4.36 (Order = 36). Category: `System`. `IsUniqueInstance = true`.
**Identifier:** `SEEK`.

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2SeekFrame),
    Category = Id3v2FrameCategory.System,
    MenuLabel = "Seek (SEEK)",
    Order = 36,
    SupportedVersions = Id3v2VersionMask.V240,
    IsUniqueInstance = true)]
```

**Editor fields:** `MinimumOffsetToNextTag` (int).

**Validate:** `>= 0`.

**Tests:** Round-trip; non-negative validation.

**Commit:** `feat(studio): SeekEditor (SEEK)`.

---

## Task 1.8: `PcntEditor` — Play counter (PCNT / CNT)

**Frame:** `Id3v2PlayCounterFrame` (all versions).
**Properties (verified):** `Counter` (long).
**Spec section:** 4.25 (Order = 25). Category: `CountersAndRatings`. `IsUniqueInstance = true`.
**Identifier:** `PCNT` (v2.2: `CNT`).

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2PlayCounterFrame),
    Category = Id3v2FrameCategory.CountersAndRatings,
    MenuLabel = "Play counter (PCNT)",
    Order = 25,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = true)]
```

**Editor fields:** `Counter` (long).

**Validate:** `Counter >= 0`.

**Tests:** Round-trip with `Counter = 12345L`; non-negative validation.

**Commit:** `feat(studio): PcntEditor (PCNT, play counter)`.

---

## Task 1.9: `PopmEditor` — Popularimeter (POPM / POP)

**Frame:** `Id3v2PopularimeterFrame` (all versions).
**Properties (verified):** `EmailToUser` (string), `Rating` (byte 0–255), `Counter` (long).
**Spec section:** 4.26 (Order = 26). Category: `CountersAndRatings`. `IsUniqueInstance = false` (per email).
**Identifier:** `POPM` (v2.2: `POP`).

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2PopularimeterFrame),
    Category = Id3v2FrameCategory.CountersAndRatings,
    MenuLabel = "Popularimeter (POPM)",
    Order = 26,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
```

**Editor fields:** `EmailToUser` (string), `Rating` (byte 0–255 — UI: TextBox or NumericUpDown bound to int 0..255 with conversion to byte on Save), `Counter` (long).

**Validate:**
- `EmailToUser` non-empty (uniqueness key).
- `Rating` 0-255 (already constrained by byte type; surface error if user types non-numeric).
- `Counter >= 0`.

**Tests:** Round-trip with `EmailToUser="user@example.com"`, `Rating=200`, `Counter=42L`. Empty-email validation.

**Commit:** `feat(studio): PopmEditor (POPM, popularimeter)`.

---

## Wave 1 done definition

- 9 editor pairs committed (one commit per editor).
- All 9 `XxxEditorTests.cs` test classes pass under default xUnit threading (no STA needed — editors are non-Window).
- `RegistryCompletenessTests` lists the 18 remaining missing editors as informational output (not failure).
- `dotnet build -c Debug` clean.
- No file outside the editor's own four files modified per task.

After this wave, the coordinator runs `dotnet test AudioVideoLib.Studio.Tests/` — all green; reviews each editor via `superpowers:subagent-driven-development` two-stage pattern; merges. Then proceeds to Wave 2.
