# ID3v2 Frame Editors — Wave 3 (10 editors: Form+Grid + Wrapper)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Each task is dispatched to a fresh subagent in parallel. Subagents touch ONLY their own four files.

**Goal:** Add 9 Pattern-2 (Form+Grid) editor pairs (8 originally planned for Wave 3 + XRVA relocated from Wave 2) and 1 Pattern-3 (Wrapper) editor pair — **10 total**, completing the 39-frame editor catalog. After this wave, `RegistryCompletenessTests` reports 0 missing editors and Phase 2 can flip the `RegistrationComplete` flag.

**Architecture:** Two reference editors from Phase 0 are the canonical templates: `EtcoEditor` (Pattern 2 — collection editor inheriting `CollectionEditorBase<TFrame, TRow>`) and `CdmEditor` (Pattern 3 — wrapper editor inheriting `WrapperEditorBase<TFrame>`).

**Reference:** Spec §4.2 (Form+Grid), §4.3 (Wrapper), §5 frame catalog. Phase 0 plan Tasks 9 (`EtcoEditor`) and 10 (`CdmEditor`) are the canonical examples.

---

## Shared per-task contract

Same as Waves 1+2:
- 4 files per editor: `XxxEditor.cs`, `XxxEditorDialog.xaml`, `XxxEditorDialog.xaml.cs`, `XxxEditorTests.cs`.
- 7 TDD steps.
- Forbidden file modifications outside the editor's own four files.

**Pattern-2 (Form+Grid) skeleton:**

```csharp
[Id3v2FrameEditor(typeof(Id3v2YyyFrame), Category=..., MenuLabel="...", Order=N, SupportedVersions=..., IsUniqueInstance=...)]
public sealed class XxxEditor : CollectionEditorBase<Id3v2YyyFrame, XxxRowVm>, INotifyPropertyChanged
{
    // Header form fields (per-frame)
    private <<type>> _headerField;
    public <<type>> HeaderField { get => _headerField; set => Set(ref _headerField, value); }

    public override Id3v2YyyFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public override bool Edit(Window owner, Id3v2YyyFrame frame)
    {
        Load(frame);
        var dialog = new XxxEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true) return false;
        Save(frame);
        return true;
    }

    public void Load(Id3v2YyyFrame f)
    {
        // populate header fields from f
        LoadRows(f);   // base helper populates Entries from frame's collection
    }

    public void Save(Id3v2YyyFrame f)
    {
        // write header fields to f
        SaveRows(f);   // base helper writes Entries back to frame's collection
    }

    public override void LoadRows(Id3v2YyyFrame f) { Entries.Clear(); foreach (var item in f.<<Collection>>) Entries.Add(new XxxRowVm { ... }); }
    public override void SaveRows(Id3v2YyyFrame f) { f.<<Collection>>.Clear(); foreach (var r in Entries) f.<<Collection>>.Add(new <<Item>>(r.Field1, r.Field2)); }
    public override bool Validate(out string? error) { error = null; return true; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T storage, T value, [CallerMemberName] string? prop = null) { /* INPC boilerplate */ }
}

public sealed class XxxRowVm
{
    public <<type1>> Field1 { get; set; }
    public <<type2>> Field2 { get; set; }
    // ... per-frame columns ...
}
```

**Pattern-2 dialog skeleton:**

```xaml
<Window x:Class="..." Width="640" Height="480" WindowStartupLocation="CenterOwner" Title="<<TITLE>>">
    <DockPanel Margin="14">
        <StackPanel DockPanel.Dock="Bottom" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,14,0,0">
            <Button Content="OK" Width="80" Click="Ok_Click" IsDefault="True" />
            <Button Content="Cancel" Width="80" Margin="8,0,0,0" Click="Cancel_Click" IsCancel="True" />
        </StackPanel>

        <!-- Header form fields (one StackPanel per labeled field, top-to-bottom) -->
        <StackPanel DockPanel.Dock="Top" Margin="0,0,0,12">
            <!-- per-task -->
        </StackPanel>

        <!-- Collection editor: toolbar + DataGrid -->
        <DockPanel>
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
                      CanUserAddRows="False" CanUserDeleteRows="False"
                      SelectionMode="Single" MinHeight="200">
                <DataGrid.Columns>
                    <!-- subagent declares typed columns matching XxxRowVm -->
                </DataGrid.Columns>
            </DataGrid>
        </DockPanel>
    </DockPanel>
</Window>
```

**Pattern-2 codebehind skeleton (delegates row mutations to editor):**

```csharp
public partial class XxxEditorDialog : Window
{
    public XxxEditorDialog() => InitializeComponent();

    private XxxEditor Editor => (XxxEditor)DataContext;

    private void Ok_Click(object s, RoutedEventArgs e)
    {
        if (!Editor.Validate(out var error))
        {
            MessageBox.Show(this, error, "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }
    private void Cancel_Click(object s, RoutedEventArgs e) => DialogResult = false;

    private void AddRow_Click(object s, RoutedEventArgs e) => Editor.AddRow(new XxxRowVm());
    private void RemoveRow_Click(object s, RoutedEventArgs e) => Editor.RemoveRow(EntriesGrid.SelectedIndex);
    private void MoveUp_Click(object s, RoutedEventArgs e) => Editor.MoveUp(EntriesGrid.SelectedIndex);
    private void MoveDown_Click(object s, RoutedEventArgs e) => Editor.MoveDown(EntriesGrid.SelectedIndex);
}
```

**Pattern-3 (Wrapper) skeleton:** see Phase 0 Task 10 (`CdmEditor`) for the full reference. Used by Task 3.9 (`CrmEditor`).

---

## Task 3.1: `SyltEditor` — Synchronized lyrics (SYLT / SLT)

**Frame:** `Id3v2SynchronizedLyricsFrame` (all versions).
**Properties (verified):** `TextEncoding`, `Language` (3-char), `TimeStampFormat`, `ContentType` (`Id3v2ContentType` enum), `ContentDescriptor` (string), `LyricSyncs` (`ICollection<Id3v2LyricSync>`).
**`Id3v2LyricSync` shape (verified against `AudioVideoLib/Tags/Id3v2LyricSync.cs`):** `Syllable` (string, private set), `TimeStamp` (int, private set). Constructor: `Id3v2LyricSync(string syllable, int timeStamp)`. Throws on null syllable.

**Spec section:** 4.12 (Order = 12). Category: `CommentsAndLyrics`. `IsUniqueInstance = false` (per language + descriptor).
**Identifier:** `SYLT` (v2.2: `SLT`).

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2SynchronizedLyricsFrame),
    Category = Id3v2FrameCategory.CommentsAndLyrics,
    MenuLabel = "Synchronized lyrics (SYLT)",
    Order = 12,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
```

**Editor header fields:** Encoding (combo), Language (3-char), TimeStampFormat (combo), ContentType (combo bound to `Id3v2ContentTypeValues` ObjectDataProvider — added to App.xaml by coordinator), ContentDescriptor (string).

**Row schema:** `SyltRowVm { Syllable (string), TimeStamp (int) }`. Both settable so DataGrid can edit inline.

**LoadRows / SaveRows:**
```csharp
public override void LoadRows(Id3v2SynchronizedLyricsFrame f)
{
    Entries.Clear();
    foreach (var s in f.LyricSyncs)
        Entries.Add(new SyltRowVm { Syllable = s.Syllable, TimeStamp = s.TimeStamp });
}
public override void SaveRows(Id3v2SynchronizedLyricsFrame f)
{
    f.LyricSyncs.Clear();
    foreach (var r in Entries)
        f.LyricSyncs.Add(new Id3v2LyricSync(r.Syllable ?? string.Empty, r.TimeStamp));
}
```

**Validate:** Language length 3; ContentDescriptor uniqueness key non-empty if frame is intended addable; TimeStamps non-decreasing.

**Tests:** Round-trip header + 3 rows; Validate matrix for language length and timestamp ordering.

**Commit:** `feat(studio): SyltEditor (SYLT, synchronized lyrics)`.

---

## Task 3.2: `MlltEditor` — MPEG location lookup table (MLLT / MLL)

**Frame:** `Id3v2MpegLocationLookupTableFrame` (all versions).
**Properties (verified):** `MpegFramesBetweenReference` (short), `BytesBetweenReference` (int), `MillisecondsBetweenReference` (int), `References` (`IList<Id3v2MpegLookupTableItem>`).
**`Id3v2MpegLookupTableItem` shape (verified against `AudioVideoLib/Tags/Id3v2MpegLookupTableItem.cs`):** `DeviationInBytes` (byte, private set), `DeviationInMilliseconds` (byte, private set). Constructor: `Id3v2MpegLookupTableItem(byte deviationInBytes, byte devationInMilliseconds)` — note typo in the lib's parameter name `devationInMilliseconds`.

**Spec section:** 4.14 (Order = 14). Category: `TimingAndSync`. `IsUniqueInstance = true`.
**Identifier:** `MLLT` (v2.2: `MLL`).

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2MpegLocationLookupTableFrame),
    Category = Id3v2FrameCategory.TimingAndSync,
    MenuLabel = "MPEG location lookup table (MLLT)",
    Order = 14,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = true)]
```

**Editor header fields:** `MpegFramesBetweenReference` (short), `BytesBetweenReference` (int), `MillisecondsBetweenReference` (int).

**Row schema:** `MlltRowVm { DeviationInBytes (byte), DeviationInMilliseconds (byte) }`. UI: bind via int with `IValueConverter` if needed, but underlying VM properties are `byte`.

**LoadRows / SaveRows:**
```csharp
public override void LoadRows(Id3v2MpegLocationLookupTableFrame f)
{
    Entries.Clear();
    foreach (var r in f.References)
        Entries.Add(new MlltRowVm { DeviationInBytes = r.DeviationInBytes, DeviationInMilliseconds = r.DeviationInMilliseconds });
}
public override void SaveRows(Id3v2MpegLocationLookupTableFrame f)
{
    f.References.Clear();
    foreach (var r in Entries)
        f.References.Add(new Id3v2MpegLookupTableItem(r.DeviationInBytes, r.DeviationInMilliseconds));
}
```

**Validate:** Header values non-negative. Each row's bytes can range 0–255 by type.

**Commit:** `feat(studio): MlltEditor (MLLT, MPEG location lookup table)`.

---

## Task 3.3: `SytcEditor` — Synchronized tempo codes (SYTC / STC)

**Frame:** `Id3v2SyncedTempoCodesFrame` (all versions).
**Properties (verified):** `TimeStampFormat`, `TempoData` (`ICollection<Id3v2TempoCode>`).
**`Id3v2TempoCode` shape (verified against `AudioVideoLib/Tags/Id3v2TempoCode.cs`):** `BeatsPerMinute` (int, init-only, valid range 2–510 per spec), `TimeStamp` (int, init-only). Constructor: `Id3v2TempoCode(int beatsPerMinute, int timeStamp)`.

**Spec section:** 4.15 (Order = 15). Category: `TimingAndSync`. `IsUniqueInstance = true`.
**Identifier:** `SYTC` (v2.2: `STC`).

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2SyncedTempoCodesFrame),
    Category = Id3v2FrameCategory.TimingAndSync,
    MenuLabel = "Synced tempo codes (SYTC)",
    Order = 15,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = true)]
```

**Editor header fields:** `TimeStampFormat` (combo).

**Row schema:** `SytcRowVm { BeatsPerMinute (int), TimeStamp (int) }`.

**LoadRows / SaveRows:**
```csharp
public override void LoadRows(Id3v2SyncedTempoCodesFrame f)
{
    Entries.Clear();
    foreach (var t in f.TempoData)
        Entries.Add(new SytcRowVm { BeatsPerMinute = t.BeatsPerMinute, TimeStamp = t.TimeStamp });
}
public override void SaveRows(Id3v2SyncedTempoCodesFrame f)
{
    f.TempoData.Clear();
    foreach (var r in Entries)
        f.TempoData.Add(new Id3v2TempoCode(r.BeatsPerMinute, r.TimeStamp));
}
```

**Validate:** TimeStamps non-decreasing. `BeatsPerMinute` 0, 1, or 2–510 per spec (0 = beat-free, 1 = single beat).

**Commit:** `feat(studio): SytcEditor (SYTC, synced tempo codes)`.

---

## Task 3.4: `AspiEditor` — Audio seek point index (ASPI)

**Frame:** `Id3v2AudioSeekPointIndexFrame` (v2.4 only).
**Properties (verified):** `IndexedDataStart` (int), `IndexedDataLength` (int), `NumberOfIndexPoints` (short), `BitsPerIndexPoint` (byte), `FractionAtIndex` (`IList<short>`).

**Spec section:** 4.17 (Order = 17). Category: `TimingAndSync`. `IsUniqueInstance = true`.
**Identifier:** `ASPI`.

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2AudioSeekPointIndexFrame),
    Category = Id3v2FrameCategory.TimingAndSync,
    MenuLabel = "Audio seek point index (ASPI)",
    Order = 17,
    SupportedVersions = Id3v2VersionMask.V240,
    IsUniqueInstance = true)]
```

**Editor header fields:** `IndexedDataStart`, `IndexedDataLength`, `NumberOfIndexPoints`, `BitsPerIndexPoint` (combo with values 8 / 16 per spec §4.30).

**Row schema:** `AspiRowVm { Fraction (short) }` — single column "Index fraction (0–65535 for 16-bit, 0–255 for 8-bit)".

**LoadRows / SaveRows:** translate `IList<short>` to/from `Entries`.

**Validate:** `BitsPerIndexPoint` is 8 or 16. `NumberOfIndexPoints` matches `Entries.Count` (auto-sync on Save: `NumberOfIndexPoints = (short)Entries.Count`). Each fraction `<= ushort.MaxValue` for 16-bit / `<= byte.MaxValue` for 8-bit.

**Commit:** `feat(studio): AspiEditor (ASPI, audio seek point index)`.

---

## Task 3.5: `IplsEditor` — Involved people list (IPLS / IPL)

**Frame:** `Id3v2InvolvedPeopleListFrame` (v2.2 / v2.3 — deprecated v2.4, replaced by TIPL/TMCL text frames).
**Properties (verified):** `TextEncoding`, `InvolvedPeople` (`IList<Id3v2InvolvedPeople>`).
**`Id3v2InvolvedPeople` shape:** read `AudioVideoLib/Tags/Id3v2InvolvedPeople.cs` — likely `{ Involvement (string), Involvee (string) }`.

**Spec section:** 4.19 (Order = 19). Category: `People`. `IsUniqueInstance = true`.
**Identifier:** `IPLS` (v2.2: `IPL`).

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2InvolvedPeopleListFrame),
    Category = Id3v2FrameCategory.People,
    MenuLabel = "Involved people list (IPLS)",
    Order = 19,
    SupportedVersions = Id3v2VersionMask.V220 | Id3v2VersionMask.V221 | Id3v2VersionMask.V230,
    IsUniqueInstance = true)]
```

**Editor header fields:** `Encoding` (combo).

**Row schema:** `IplsRowVm { Involvement (string), Involvee (string) }`.

**Validate:** No header constraints; row-level: both columns non-empty.

**Commit:** `feat(studio): IplsEditor (IPLS, involved people list)`.

---

## Task 3.6: `Rva2Editor` — Relative volume adjustment 2 (RVA2)

**Frame:** `Id3v2RelativeVolumeAdjustment2Frame` (v2.4 only).
**Properties (verified):** `Identification` (string), `ChannelInformation` (`IList<Id3v2ChannelInformation>`).
**`Id3v2ChannelInformation` shape (verified against `AudioVideoLib/Tags/Id3v2ChannelInformation.cs`):** `ChannelType` (`Id3v2ChannelType`, private set), `VolumeAdjustment` (**float**, private set — fixed-point dB×512 per spec, ±64 dB), `BitsRepresentingPeak` (byte, private set, 0 means no peak field), `PeakVolume` (long, private set). Constructor: `Id3v2ChannelInformation(Id3v2ChannelType channelType, float volumeAdjustment, byte bitsRepresentingPeak, long peakVolume)` — **4 parameters**.

**Spec section:** 4.21 (Order = 21). Category: `AudioAdjustment`. `IsUniqueInstance = false` (per Identification).
**Identifier:** `RVA2`.

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2RelativeVolumeAdjustment2Frame),
    Category = Id3v2FrameCategory.AudioAdjustment,
    MenuLabel = "Relative volume adjustment 2 (RVA2)",
    Order = 21,
    SupportedVersions = Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
```

**Editor header fields:** `Identification` (string).

**Row schema:** `Rva2RowVm { ChannelType (Id3v2ChannelType, combo bound to `Id3v2ChannelTypeValues` resource), VolumeAdjustment (float, displayed in dB), BitsRepresentingPeak (byte, range 0–255), PeakVolume (long) }`.

**LoadRows / SaveRows:**
```csharp
public override void LoadRows(Id3v2RelativeVolumeAdjustment2Frame f)
{
    Entries.Clear();
    foreach (var c in f.ChannelInformation)
        Entries.Add(new Rva2RowVm
        {
            ChannelType = c.ChannelType,
            VolumeAdjustment = c.VolumeAdjustment,
            BitsRepresentingPeak = c.BitsRepresentingPeak,
            PeakVolume = c.PeakVolume,
        });
}
public override void SaveRows(Id3v2RelativeVolumeAdjustment2Frame f)
{
    f.ChannelInformation.Clear();
    foreach (var r in Entries)
        f.ChannelInformation.Add(new Id3v2ChannelInformation(
            r.ChannelType, r.VolumeAdjustment, r.BitsRepresentingPeak, r.PeakVolume));
}
```

**Validate:** `Identification` non-empty (uniqueness key). `BitsRepresentingPeak` may be 0 (no peak field) or any byte value.

**Commit:** `feat(studio): Rva2Editor (RVA2, relative volume adjustment 2)`.

---

## Task 3.7: `EquaEditor` — Equalisation (EQUA / EQU)

**Frame:** `Id3v2EqualisationFrame` (v2.2 / v2.3 — deprecated v2.4, replaced by EQU2).
**Properties (verified):** `AdjustmentBits` (byte), `EqualisationBands` (`ICollection<Id3v2EqualisationBand>`).
**`Id3v2EqualisationBand` shape (verified against `AudioVideoLib/Tags/Id3v2EqualisationBand.cs`):** `Increment` (bool, private set — true = increase volume, false = decrease), `Frequency` (short, private set, 0–32767 Hz), `Adjustment` (int, private set). Constructor: `Id3v2EqualisationBand(bool incrementDecrement, short frequency, int adjustment)`. **The ctor throws `ArgumentOutOfRangeException` when `adjustment == 0`** — the editor must reject 0-adjustment rows in `Validate()` *before* attempting Save.

**Spec section:** 4.22 (Order = 22). Category: `AudioAdjustment`. `IsUniqueInstance = true`.
**Identifier:** `EQUA` (v2.2: `EQU`).

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2EqualisationFrame),
    Category = Id3v2FrameCategory.AudioAdjustment,
    MenuLabel = "Equalisation (EQUA)",
    Order = 22,
    SupportedVersions = Id3v2VersionMask.V220 | Id3v2VersionMask.V221 | Id3v2VersionMask.V230,
    IsUniqueInstance = true)]
```

**Editor header fields:** `AdjustmentBits` (byte, range 1–16 per spec).

**Row schema:** `EquaRowVm { Increment (bool, CheckBox), Frequency (short, 0–32767 Hz), Adjustment (int, signed dB × 2^(AdjustmentBits-1)) }`.

**LoadRows / SaveRows:**
```csharp
public override void LoadRows(Id3v2EqualisationFrame f)
{
    Entries.Clear();
    foreach (var b in f.EqualisationBands)
        Entries.Add(new EquaRowVm { Increment = b.Increment, Frequency = b.Frequency, Adjustment = b.Adjustment });
}
public override void SaveRows(Id3v2EqualisationFrame f)
{
    f.EqualisationBands.Clear();
    foreach (var r in Entries.OrderBy(x => x.Frequency))
        f.EqualisationBands.Add(new Id3v2EqualisationBand(r.Increment, r.Frequency, r.Adjustment));
}
```

**Validate:** `AdjustmentBits` in `[1, 16]`. **Each row's `Adjustment` must be non-zero** (lib rejects 0; editor surfaces friendly error before Save). All frequencies `>= 0`. Frequencies must be unique within the band list (per spec).

**Commit:** `feat(studio): EquaEditor (EQUA, equalisation)`.

---

## Task 3.8: `Equ2Editor` — Equalisation 2 (EQU2)

**Frame:** `Id3v2Equalisation2Frame` (v2.4 only).
**Properties (verified):** `InterpolationMethod` (`Id3v2InterpolationMethod`), `Identification` (string), `AdjustmentPoints` (`ICollection<Id3v2AdjustmentPoint>`).
**`Id3v2AdjustmentPoint` shape (verified against `AudioVideoLib/Tags/Id3v2AdjustmentPoint.cs`):** `Frequency` (short, private set — units of 1/2 Hz, range 0–32767), `VolumeAdjustment` (short, private set — fixed-point dB×512, ±64 dB). Constructor: `Id3v2AdjustmentPoint(short frequency, short volumeAdjustment)`.

**Spec section:** 4.23 (Order = 23). Category: `AudioAdjustment`. `IsUniqueInstance = false` (per Identification).
**Identifier:** `EQU2`.

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2Equalisation2Frame),
    Category = Id3v2FrameCategory.AudioAdjustment,
    MenuLabel = "Equalisation 2 (EQU2)",
    Order = 23,
    SupportedVersions = Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
```

**Editor header fields:** `InterpolationMethod` (combo bound to `Id3v2InterpolationMethodValues` ObjectDataProvider — added to App.xaml by coordinator), `Identification` (string).

**Row schema:** `Equ2RowVm { Frequency (short), VolumeAdjustment (short) }`.

**LoadRows / SaveRows:**
```csharp
public override void LoadRows(Id3v2Equalisation2Frame f)
{
    Entries.Clear();
    foreach (var p in f.AdjustmentPoints)
        Entries.Add(new Equ2RowVm { Frequency = p.Frequency, VolumeAdjustment = p.VolumeAdjustment });
}
public override void SaveRows(Id3v2Equalisation2Frame f)
{
    f.AdjustmentPoints.Clear();
    foreach (var r in Entries.OrderBy(x => x.Frequency))
        f.AdjustmentPoints.Add(new Id3v2AdjustmentPoint(r.Frequency, r.VolumeAdjustment));
}
```

**Validate:** `Identification` non-empty. Frequencies sorted ascending (auto-sort on Save per spec §4.13).

**Commit:** `feat(studio): Equ2Editor (EQU2, equalisation 2)`.

---

## Task 3.9: `CrmEditor` — Encrypted meta frame (CRM, Pattern 3 Wrapper)

**Frame:** `Id3v2EncryptedMetaFrame` (v2.2.0 only — parameterless ctor pins to v220).
**Properties (verified):** `OwnerIdentifier` (string), `ContentExplanation` (string), `EncryptedDataBlock` (byte[]).

**Spec section:** 4.33 (Order = 33). Category: `Containers`. `IsUniqueInstance = false` (per OwnerIdentifier).
**Identifier:** `CRM`.

**Constructors (verified):** `Id3v2EncryptedMetaFrame()` defaults to v2.2.0; `Id3v2EncryptedMetaFrame(Id3v2Version version)` accepts any version where `version < Id3v2v230` (i.e. v2.2.0 OR v2.2.1). `KnownIdentifier = "CRM"` is still required because `Id3v2AddMenuBuilder.IdentifierFor` defaults to the `(Id3v2Version)` ctor for identifier resolution — for CRM that works (returns "CRM"), but using `KnownIdentifier` avoids constructing an instance during menu build.

**OwnerIdentifier validation:** the lib's `OwnerIdentifier` setter calls `IsValidDefaultTextString(value, false)` and throws `InvalidDataException` on invalid characters. Test data should use a plain ASCII string (e.g. `"plugin@example.com"` or `"my-plugin"`); avoid characters that fail ISO-8859-1 default-text validation.

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2EncryptedMetaFrame),
    Category = Id3v2FrameCategory.Containers,
    MenuLabel = "Encrypted meta (CRM)",
    Order = 33,
    SupportedVersions = Id3v2VersionMask.V220 | Id3v2VersionMask.V221,    // lib accepts v2.2.0 AND v2.2.1
    IsUniqueInstance = false,
    KnownIdentifier = "CRM")]
```

**`CreateNew(tag)`:** `new Id3v2EncryptedMetaFrame(((Id3v2Tag)tag).Version)` — pass the tag's version explicitly so a v2.2.1 tag gets a v2.2.1 frame (avoiding the parameterless-ctor-defaults-to-v220 + SetFrame-version-mismatch trap that affected the round-3 review).

**Pattern:** Inherits `WrapperEditorBase<Id3v2EncryptedMetaFrame>` per spec §4.3 + Phase 0 Task 10 reference (`CdmEditor`).

**Editor header fields:** `OwnerIdentifier` (string), `ContentExplanation` (string), encrypted-data blob (Load from file / Clear / size info — same blob pattern as Wave 2 binary frames).

**Wrapped-child handling:** the `EncryptedDataBlock` byte[] *is* the wrapped frame's bytes. The wrapper's `Edit(...)` flow: `TakeSnapshot(tag, self)` populates `WrappableSnapshot`; user picks a child; on OK, `EncryptedDataBlock = SerializeFrame(SelectedChild)` (use `frame.WriteTo(...)` against a `MemoryStream`); `MainWindow.OnAddOrEditFrame` removes the original child via `tag.RemoveFrame(SelectedChild)` (this last step is coordinator-handled in MainWindow surgery; subagent does NOT touch MainWindow).

For the subagent's scope: editor's `Save` writes the chosen child's bytes into `EncryptedDataBlock`, sets `OwnerIdentifier` and `ContentExplanation`. The original-child-removal hook is coordinator territory.

(See above — uses the version-aware ctor.)

**Validate:**
- `OwnerIdentifier` non-empty (uniqueness key per spec §4.20).
- `SelectedChild != null` if `EncryptedDataBlock` is empty (i.e. user must pick a child to wrap, OR provide an external blob via Load from file).

**Tests:** Round-trip with sample owner / explanation / 16-byte blob. Snapshot exclusion (covered by `WrapperEditorBaseTests` already; per-editor test repeats one case to confirm wiring).

**Commit:** `feat(studio): CrmEditor (CRM, encrypted meta)`.

---

## Task 3.10: `XrvaEditor` — Experimental relative volume adjustment 2 (XRVA)

Relocated from Wave 2 because XRVA is structurally Pattern 2 (Form+Grid), shares the row schema with `Rva2Editor` (Task 3.6), and slots cleanly alongside it in this wave.

**Frame:** `Id3v2ExperimentalRelativeVolumeAdjustment2Frame` (v2.4, non-standard).
**Properties (verified):** `Identification` (string), `ChannelInformation` (`IList<Id3v2ChannelInformation>`).
**`Id3v2ChannelInformation` shape:** identical to RVA2 (see Task 3.6 above). 4-arg ctor `(Id3v2ChannelType, float volumeAdjustment, byte bitsRepresentingPeak, long peakVolume)`.

**Spec section:** 4.38 (Order = 38). Category: `Experimental`. `IsUniqueInstance = false` (per Identification).
**Identifier:** `XRVA`.

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2ExperimentalRelativeVolumeAdjustment2Frame),
    Category = Id3v2FrameCategory.Experimental,
    MenuLabel = "Experimental volume adjustment 2 (XRVA)",
    Order = 38,
    SupportedVersions = Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
```

**Editor fields and Row schema:** Define a parallel **`XrvaRowVm`** with the same 4 fields as `Rva2RowVm` (`ChannelType`, `VolumeAdjustment`, `BitsRepresentingPeak`, `PeakVolume`). Do NOT reuse `Rva2RowVm` directly — keeping them separate preserves the "subagents touch ONLY their own four files" invariant for parallel dispatch. Trivial duplication (one record/class) is the right trade-off here.

**LoadRows / SaveRows:** Identical to RVA2 — see Task 3.6.

**Validate:** `Identification` non-empty.

**Tests:** Round-trip with `Identification = "experimental-track"` + 2 channel rows. Identification-empty validation.

**Commit:** `feat(studio): XrvaEditor (XRVA, experimental volume adjustment 2)`.

---

## Wave 3 done definition

- **10 editor pairs committed** (8 Pattern-2 + 1 Pattern-3 + 1 relocated XRVA Pattern-2).
- `RegistryCompletenessTests` lists 0 missing editors — all 39 frames covered.
- `dotnet test AudioVideoLib.Studio.Tests/` green (full editor catalog).
- `dotnet build -c Debug` clean.
- Coordinator adds `Id3v2ContentTypeValues`, `Id3v2InterpolationMethodValues`, `Id3v2ChannelTypeValues` ObjectDataProviders to App.xaml (consumed by Wave 3 editors) — cross-task additions, not subagent work.
- No file outside per-editor scope modified.

After Wave 3, coordinator runs Phase 2 (finalisation):
1. Flip `RegistryCompletenessTests.RegistrationComplete = true`.
2. Run full Release build + test suite + DocFX.
3. Update release notes.
4. Final code review.
5. Squash-merge to master + push (with explicit user authorisation).
