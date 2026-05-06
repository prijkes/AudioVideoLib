# ID3v2 Frame Editors — Wave 2 (8 complex Form editors)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Each task is dispatched to a fresh subagent in parallel. Subagents touch ONLY their own four files.

**Goal:** Add **8** Pattern-1 editor pairs covering frames with multi-field forms (often header fields + a binary blob). After this wave, `RegistryCompletenessTests` reports 10 missing editors (down from 18). XRVA was relocated to Wave 3 — see Wave 2 done-definition for details.

**Architecture:** Same two-class Pattern 1 as Wave 1; reference editor `CommentEditor`. Subagents follow the shared per-task contract from Wave 1's plan (skeleton code + dialog + codebehind + commit). Wave 2 differs from Wave 1 only in the per-frame field complexity — most frames here have 4-7 fields with an embedded binary blob (handled via `Load from file…`/`Clear data` buttons backed by a `byte[] _data` field, mirroring `BinaryDataDialog`).

**Reference:** Spec §4.1 (Pattern 1) + §5 frame catalog rows for each frame. Phase 0 plan Task 8 (`CommentEditor`) is the canonical reference.

---

## Shared per-task contract

Identical to Wave 1 (see `plans/2026-05-06-id3v2-frame-editors-wave1-simple-form.md` for the full skeleton). Quick recap:

- 4 files per editor: `XxxEditor.cs`, `XxxEditorDialog.xaml`, `XxxEditorDialog.xaml.cs`, `XxxEditorTests.cs`.
- 7 TDD steps per task: write tests → fail → implement editor → write XAML → write codebehind → tests pass → commit.
- Forbidden: modifying any file outside the editor's own four.
- Pattern-1 dialog skeleton, codebehind, INPC pattern: copy verbatim from Wave 1 plan's "Shared per-task contract" section.

**Binary-blob handling pattern** (used by AENC, GEOB, SIGN, RGAD ChannelInformation):

```csharp
// In XxxEditor.cs, expose:
public string DataInfo => _data.Length == 0 ? "(no data)" : $"{_data.Length:N0} bytes";
private byte[] _data = [];

// Bound buttons in dialog click into helpers:
public void LoadDataFromFile(string path) { _data = File.ReadAllBytes(path); RaisePropertyChanged(nameof(DataInfo)); }
public void ClearData() { _data = []; RaisePropertyChanged(nameof(DataInfo)); }
```

Dialog buttons:
```xaml
<StackPanel Orientation="Horizontal" Margin="0,4,0,0">
    <Button Content="Load from file…" Click="LoadFromFile_Click" Padding="10,3" />
    <Button Content="Clear data"      Click="ClearData_Click"   Padding="10,3" Margin="6,0,0,0" />
    <TextBlock Text="{Binding DataInfo}" Margin="14,0,0,0" VerticalAlignment="Center"
               Foreground="{DynamicResource TextSecondaryBrush}" FontSize="11" />
</StackPanel>
```

`LoadFromFile_Click` uses `Microsoft.Win32.OpenFileDialog` (mirroring existing `BinaryDataDialog.LoadFromFile_Click` shape) and calls `editor.LoadDataFromFile(path)`.

---

## Task 2.1: `OwneEditor` — Ownership (OWNE)

**Frame:** `Id3v2OwnershipFrame` (v2.3 / v2.4).
**Properties (verified):** `TextEncoding`, `PricePaid` (string, format like "USD/$10.99"), `DateOfPurchase` (string, "YYYYMMDD"), `Seller` (string).
**Spec section:** 4.29 (Order = 29). Category: `CommerceAndRights`. `IsUniqueInstance = true`.
**Identifier:** `OWNE`.

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2OwnershipFrame),
    Category = Id3v2FrameCategory.CommerceAndRights,
    MenuLabel = "Ownership (OWNE)",
    Order = 29,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = true)]
```

**Editor fields:** `Encoding`, `PricePaid`, `DateOfPurchase`, `Seller`.

**Validate:**
- `DateOfPurchase` exactly 8 digits if non-empty (format "YYYYMMDD"). Error: `"Date of purchase must be 8 digits in YYYYMMDD format."`
- `PricePaid` non-empty. Error: `"Price paid is required."`

**Tests:** Round-trip with sample values. Date format theory: `("20260506", true)`, `("2026-05-06", false)`, `("ABCDEFGH", false)`, `("", true)` (empty allowed).

**Commit:** `feat(studio): OwneEditor (OWNE, ownership)`.

---

## Task 2.2: `ComrEditor` — Commercial (COMR)

**Frame:** `Id3v2CommercialFrame` (v2.3 / v2.4).
**Properties (verified):** `TextEncoding`, `PriceString` (string), `ValidUntil` (string "YYYYMMDD"), `ContactUrl` (string), `ReceivedAs` (`Id3v2AudioDeliveryType` enum), `NameOfSeller` (string), `ShortDescription` (string), `PictureMimeType` (string), `SellerLogo` (byte[]).
**Spec section:** 4.30 (Order = 30). Category: `CommerceAndRights`. `IsUniqueInstance = false`.
**Identifier:** `COMR`.

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2CommercialFrame),
    Category = Id3v2FrameCategory.CommerceAndRights,
    MenuLabel = "Commercial (COMR)",
    Order = 30,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
```

**Editor fields (9):** `Encoding` (combo), `PriceString`, `ValidUntil`, `ContactUrl`, `ReceivedAs` (combo bound to ObjectDataProvider for `Id3v2AudioDeliveryType` — declare in App.xaml as `Id3v2AudioDeliveryTypeValues`), `NameOfSeller`, `ShortDescription`, `PictureMimeType`, plus the `SellerLogo` binary blob (Load/Clear/info-text trio).

**Note:** `Id3v2AudioDeliveryTypeValues` ObjectDataProvider must be added to App.xaml. Coordinator handles this as a cross-task addition during the merge step (see Wave 2 done definition).

**Validate:** `ValidUntil` 8-digit format if non-empty (same rule as OWNE). `ContactUrl` URI-validity check if non-empty.

**Tests:** Round-trip with all 9 fields populated; date-format theory; URL validation.

**Commit:** `feat(studio): ComrEditor (COMR, commercial)`.

---

## Task 2.3: `AencEditor` — Audio encryption (AENC / CRA)

**Frame:** `Id3v2AudioEncryptionFrame` (all versions).
**Properties (verified):** `OwnerIdentifier` (string), `PreviewStart` (short), `PreviewLength` (short), `EncryptionInfo` (byte[]).
**Spec section:** 4.31 (Order = 31). Category: `EncryptionAndCompression`. `IsUniqueInstance = false` (per OwnerIdentifier).
**Identifier:** `AENC` (v2.2: `CRA`).

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2AudioEncryptionFrame),
    Category = Id3v2FrameCategory.EncryptionAndCompression,
    MenuLabel = "Audio encryption (AENC)",
    Order = 31,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
```

**Editor fields:** `OwnerIdentifier` (string), `PreviewStart` (short), `PreviewLength` (short), `EncryptionInfo` (binary blob).

**Validate:**
- `OwnerIdentifier` non-empty (uniqueness key).
- `PreviewStart >= 0` and `PreviewLength >= 0`.

**Tests:** Round-trip with 4-byte sample blob, verify all four fields. Validate matrix.

**Commit:** `feat(studio): AencEditor (AENC, audio encryption)`.

---

## Task 2.4: `GeobEditor` — General encapsulated object (GEOB / GEO)

**Frame:** `Id3v2GeneralEncapsulatedObjectFrame` (all versions).
**Properties (verified):** `TextEncoding`, `MimeType` (string), `Filename` (string), `ContentDescription` (string), `EncapsulatedObject` (byte[]).
**Spec section:** 4.28 (Order = 28). Category: `Attachments`. `IsUniqueInstance = false` (per ContentDescription).
**Identifier:** `GEOB` (v2.2: `GEO`).

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2GeneralEncapsulatedObjectFrame),
    Category = Id3v2FrameCategory.Attachments,
    MenuLabel = "General encapsulated object (GEOB)",
    Order = 28,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
```

**Editor fields:** `Encoding` (combo), `MimeType`, `Filename`, `ContentDescription`, `EncapsulatedObject` (blob).

**Validate:**
- `ContentDescription` non-empty (uniqueness key).
- `MimeType` either empty or matches a coarse pattern (`<type>/<subtype>`). Lenient check: must contain `/` if non-empty.

**Tests:** Round-trip with sample values + 16-byte blob. Validate matrix.

**Commit:** `feat(studio): GeobEditor (GEOB, general encapsulated object)`.

---

## Task 2.5: `SignEditor` — Signature (SIGN)

**Frame:** `Id3v2SignatureFrame` (v2.4 only).
**Properties (verified):** `GroupSymbol` (byte 0x80-0xF0), `SignatureData` (byte[]).
**Spec section:** 4.37 (Order = 37). Category: `System`. `IsUniqueInstance = false` (per GroupSymbol).
**Identifier:** `SIGN`.

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2SignatureFrame),
    Category = Id3v2FrameCategory.System,
    MenuLabel = "Signature (SIGN)",
    Order = 37,
    SupportedVersions = Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
```

**Editor fields:** `GroupSymbol` (byte UI as int 0x80–0xF0), `SignatureData` (blob).

**Validate:** `GroupSymbol` in `[0x80, 0xF0]`.

**Tests:** Round-trip with sample symbol + 8-byte signature. Symbol-range theory.

**Commit:** `feat(studio): SignEditor (SIGN, signature)`.

---

## Task 2.6: `RvadEditor` — Relative volume adjustment (RVAD / RVA)

**Frame:** `Id3v2RelativeVolumeAdjustmentFrame` (v2.2 / v2.3 — deprecated in v2.4, replaced by RVA2).
**Properties (verified, partial — full list at `AudioVideoLib/Tags/Id3v2RelativeVolumeAdjustmentFrame.cs`):**
- `IncrementDecrement` (byte)
- `VolumeDescriptionBits` (byte)
- `RelativeVolumeChangeRightChannel` (int) / Left / Right-Back / Left-Back
- `PeakVolumeRightChannel` (int) / Left / Right-Back / Left-Back
- `RelativeVolumeChangeCenterChannel` (int)
- `PeakVolumeCenterChannel` (int)
- `RelativeVolumeChangeBassChannel` (int)
- `PeakVolumeBassChannel` (int)

**Spec section:** 4.20 (Order = 20). Category: `AudioAdjustment`. `IsUniqueInstance = true`.
**Identifier:** `RVAD` (v2.2: `RVA`).

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2RelativeVolumeAdjustmentFrame),
    Category = Id3v2FrameCategory.AudioAdjustment,
    MenuLabel = "Relative volume adjustment (RVAD)",
    Order = 20,
    SupportedVersions = Id3v2VersionMask.V220 | Id3v2VersionMask.V221 | Id3v2VersionMask.V230,
    IsUniqueInstance = true)]
```

**Editor fields:** ~14 numeric fields. Group by channel via `<GroupBox>` per channel (Right / Left / Right-Back / Left-Back / Center / Bass), each with two TextBoxes (Adjustment / Peak). Plus `IncrementDecrement` (combo / bit-flags) and `VolumeDescriptionBits` (int).

Subagent reads `AudioVideoLib/Tags/Id3v2RelativeVolumeAdjustmentFrame.cs` to enumerate the exact property list and bit-flag enum for `IncrementDecrement` (the byte's 6 bits each toggle a channel's increment-vs-decrement direction).

**Validate:** Adjustment + Peak values are unsigned (per spec). All `>= 0`. `VolumeDescriptionBits` in `[0, 64]`.

**Tests:** Round-trip with all channel values populated. Validate matrix for non-negative numbers.

**Commit:** `feat(studio): RvadEditor (RVAD, relative volume adjustment)`.

---

## Task 2.7: `RevbEditor` — Reverb (REVB / REV)

**Frame:** `Id3v2ReverbFrame` (all versions). Note: lib uses `REVB` as the v2.3+ identifier (spec calls it `RVRB`; library quirk — out of scope).
**Properties (verified):** `ReverbLeftMilliseconds`, `ReverbRightMilliseconds` (short); `ReverbBouncesLeft`, `ReverbBouncesRight` (byte); `ReverbFeedbackLeftToLeft`, `ReverbFeedbackLeftToRight`, `ReverbFeedbackRightToRight`, `ReverbFeedbackRightToLeft` (byte); `PremixLeftToRight`, `PremixRightToLeft` (byte). 10 fields total per spec §4.13.
**Spec section:** 4.24 (Order = 24). Category: `AudioAdjustment`. `IsUniqueInstance = true`.
**Identifier:** `REVB` (v2.2: `REV`).

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2ReverbFrame),
    Category = Id3v2FrameCategory.AudioAdjustment,
    MenuLabel = "Reverb (REVB)",
    Order = 24,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = true)]
```

**Editor fields:** All 10 numeric properties. Layout via `<GroupBox>` per logical group (Reverb time, Bounces, Feedback, Premix).

**Validate:** All non-negative.

**Tests:** Round-trip with sample values across all 10 fields.

**Commit:** `feat(studio): RevbEditor (REVB, reverb)`.

---

## Task 2.8: `RgadEditor` — Replay gain adjustment (RGAD)

**Frame:** `Id3v2ReplayGainAdjustmentFrame` (v2.3 only — non-standard, Foobar2000/Winamp).
**Properties (verified):** `PeakAmplitude` (int), `RadioAdjustment` (`Id3v2ReplayGain`), `AudiophileAdjustment` (`Id3v2ReplayGain`).

**`Id3v2ReplayGain` shape:** subagent reads `AudioVideoLib/Tags/Id3v2ReplayGain.cs` to enumerate sub-fields (likely Name code, Originator code, Sign bit, Adjustment value).

**Spec section:** 4.39 (Order = 39). Category: `Experimental`. `IsUniqueInstance = true`.
**Identifier:** `RGAD`.

**Attribute:**
```csharp
[Id3v2FrameEditor(typeof(Id3v2ReplayGainAdjustmentFrame),
    Category = Id3v2FrameCategory.Experimental,
    MenuLabel = "Replay gain (RGAD)",
    Order = 39,
    SupportedVersions = Id3v2VersionMask.V230,
    IsUniqueInstance = true)]
```

**Editor fields:** `PeakAmplitude` (int) + Radio gain group (sub-fields of `Id3v2ReplayGain`) + Audiophile gain group (same shape).

**Validate:** `PeakAmplitude >= 0`; sub-field validations per the `Id3v2ReplayGain` type's constraints.

**Tests:** Round-trip with sample peak + both gain entries populated.

**Commit:** `feat(studio): RgadEditor (RGAD, replay gain adjustment)`.

---

## Wave 2 done definition

- **8 editor pairs committed** (Task 2.9 / XRVA was relocated to Wave 3 because it is structurally Pattern 2, shares the `Id3v2ChannelInformation` row schema with `Rva2Editor`, and benefits from the Pattern-2 skeleton in Wave 3's plan).
- `RegistryCompletenessTests` lists 10 remaining missing editors (the 9 in Wave 3 + XRVA).
- `dotnet test AudioVideoLib.Studio.Tests/` green.
- `dotnet build -c Debug` clean.
- Coordinator adds `Id3v2AudioDeliveryTypeValues` ObjectDataProvider to App.xaml (consumed by `ComrEditor`) — this is a cross-task addition, not subagent work.
- No file outside per-editor scope modified.

After Wave 2, coordinator runs all tests, reviews each editor via `superpowers:subagent-driven-development` two-stage pattern, merges, then proceeds to Wave 3.
