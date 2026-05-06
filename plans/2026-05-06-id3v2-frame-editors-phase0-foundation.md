# ID3v2 Frame Editors — Phase 0: Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the tag-format-agnostic editor framework, the ID3v2-specific layer, four family editors (text + URL + their user-defined variants), three reference editors (one per pattern: Form / Form+Grid / Wrapper), retrofit the five existing editors (APIC / USLT / PRIV / UFID / MCDI) onto the registry via the two-class adapter pattern, replace the per-frame switches in `MainWindow.xaml.cs` and `TagTabs.cs` with registry-driven dispatch, and add a "Manage frames" power-user dialog. After this phase, the Studio's frame-editor surface is fully working with 12 editors live; the remaining 27 are filled by Wave 1–3 plans.

**Architecture:** Reflection-based editor registry with the **two-class pattern**: each editor consists of `XxxEditor` (non-Window class implementing `ITagItemEditor<TFrame>`, registered, holds `Load`/`Save`/`Validate` logic) plus `XxxEditorDialog` (pure-UI `Window` subclass, `DataContext = editor`, constructed fresh per `Edit(...)` call). This split keeps editor logic testable under default xUnit threading (no STA required) and avoids the `static Edit(...)` vs instance `ITagItemEditor.Edit(...)` collision in retrofit dialogs.

**Tech Stack:** C# 13 / .NET 10 / WPF / xUnit. New test project `AudioVideoLib.Studio.Tests` introduced.

**Library API discovery (verified):**
- `Id3v2Tag.SetFrame(Id3v2Frame)` — adds or replaces. (`Id3v2Tag.Frames` is read-only `IEnumerable`; `Add` doesn't exist.)
- `Id3v2Tag.RemoveFrame(Id3v2Frame)` — removes.
- `Id3v2Tag.GetFrame<T>()` / `GetFrames<T>()` — typed lookup.
- Version enum values: `Id3v2Version.Id3v220`, `Id3v221`, `Id3v230`, `Id3v240`.
- Most frame classes have an `(Id3v2Version)` ctor. Exceptions: `Id3v2CompressedDataMetaFrame` and `Id3v2EncryptedMetaFrame` only have parameterless ctors that fix version internally.
- Existing `DarkTheme.xaml` already styles `DataGrid`, `DataGridRow`, `DataGridColumnHeader`, `DataGridCell`. No new style file needed.

**Working-tree note:** This project lives at `E:\Projects\AudioVideoLib\src\` — the `.git` directory is at the parent `E:\Projects\AudioVideoLib\`. Bash commands resolve git automatically as long as cwd is the project root or below; no special handling needed.

**Scope boundary:** Phase 0 creates the framework + 12 editors. It does NOT add the 27 remaining frame editors — those are Wave 1, 2, 3 plans. After this phase, the registry-completeness meta-test reports 27 missing editors as informational output (not a CI failure); the hard "every frame must have an editor" assertion is gated by a `RegistrationComplete` flag flipped in Phase 2.1.

**Reference:** Spec `specs/2026-05-06-id3v2-frame-editors-design.md`, especially §3 (architecture), §4 (patterns), §6 (menu structure), §7 (Manage Frames dialog), §9 (test strategy), §10.1 (Phase 0 work split).

---

## File Structure

| File | Change |
|---|---|
| `AudioVideoLib.Studio.Tests/AudioVideoLib.Studio.Tests.csproj` | **CREATE** |
| `AudioVideoLib.slnx` | **MODIFY** — add the new test project |
| `AudioVideoLib.Studio/Editors/ITagItemEditor.cs` | **CREATE** |
| `AudioVideoLib.Studio/Editors/TagItemEditorAttribute.cs` | **CREATE** |
| `AudioVideoLib.Studio/Editors/TagItemEditorRegistry.cs` | **CREATE** |
| `AudioVideoLib.Studio/Editors/CollectionEditorBase.cs` | **CREATE** |
| `AudioVideoLib.Studio/Editors/WrapperEditorBase.cs` | **CREATE** |
| `AudioVideoLib.Studio/Editors/Id3v2/Id3v2VersionMask.cs` | **CREATE** |
| `AudioVideoLib.Studio/Editors/Id3v2/Id3v2FrameCategory.cs` | **CREATE** |
| `AudioVideoLib.Studio/Editors/Id3v2/Id3v2FrameEditorAttribute.cs` | **CREATE** |
| `AudioVideoLib.Studio/Editors/Id3v2/Id3v2KnownTextFrameIds.cs` | **CREATE** |
| `AudioVideoLib.Studio/Editors/Id3v2/Id3v2KnownUrlFrameIds.cs` | **CREATE** |
| `AudioVideoLib.Studio/Editors/Id3v2/Id3v2MenuModel.cs` | **CREATE** |
| `AudioVideoLib.Studio/Editors/Id3v2/Id3v2AddMenuBuilder.cs` | **CREATE** |
| `AudioVideoLib.Studio/Editors/Id3v2/TextFrameEditor.cs` + `TextFrameEditorDialog.xaml(.cs)` | **CREATE** — family editor |
| `AudioVideoLib.Studio/Editors/Id3v2/UserDefinedTextEditor.cs` + `UserDefinedTextEditorDialog.xaml(.cs)` | **CREATE** |
| `AudioVideoLib.Studio/Editors/Id3v2/UrlFrameEditor.cs` + `UrlFrameEditorDialog.xaml(.cs)` | **CREATE** — family editor |
| `AudioVideoLib.Studio/Editors/Id3v2/UserDefinedUrlEditor.cs` + `UserDefinedUrlEditorDialog.xaml(.cs)` | **CREATE** |
| `AudioVideoLib.Studio/Editors/Id3v2/CommentEditor.cs` + `CommentEditorDialog.xaml(.cs)` | **CREATE** — Pattern 1 reference |
| `AudioVideoLib.Studio/Editors/Id3v2/EtcoEditor.cs` + `EtcoEditorDialog.xaml(.cs)` | **CREATE** — Pattern 2 reference |
| `AudioVideoLib.Studio/Editors/Id3v2/CdmEditor.cs` + `CdmEditorDialog.xaml(.cs)` | **CREATE** — Pattern 3 reference |
| `AudioVideoLib.Studio/Editors/Id3v2/ApicEditor.cs` | **CREATE** — adapter wrapping existing `ApicEditorDialog` |
| `AudioVideoLib.Studio/ApicEditorDialog.xaml(.cs)` → `Editors/Id3v2/` | **MOVE + EDIT** — drop static Edit(), keep XAML; expose Load/Save methods bound from new ApicEditor |
| `AudioVideoLib.Studio/Editors/Id3v2/UsltEditor.cs` | **CREATE** — adapter |
| `AudioVideoLib.Studio/UsltEditorDialog.xaml(.cs)` → `Editors/Id3v2/` | **MOVE + EDIT** — drop static Edit() |
| `AudioVideoLib.Studio/Editors/Id3v2/PrivBinaryEditor.cs` | **CREATE** — adapter for `Id3v2PrivateFrame` over shared BinaryDataDialog |
| `AudioVideoLib.Studio/Editors/Id3v2/UfidBinaryEditor.cs` | **CREATE** |
| `AudioVideoLib.Studio/Editors/Id3v2/McdiBinaryEditor.cs` | **CREATE** |
| `AudioVideoLib.Studio/BinaryDataDialog.xaml(.cs)` → `Editors/Id3v2/` | **MOVE + EDIT** — keep as shared UI; rename static `Edit(Window, T)` overloads to `EditPriv` / `EditUfid` / `EditMcdi` (consumed by adapter classes only) |
| `AudioVideoLib.Studio/MainWindow.xaml.cs` | **MODIFY** — strip per-frame switches, add registry init + ManageFrames wiring |
| `AudioVideoLib.Studio/MainWindow.xaml` | **MODIFY** — add "Manage frames…" toolbar button |
| `AudioVideoLib.Studio/TagTabs.cs` | **MODIFY** — add uniform `AddFrame(Id3v2Frame)`, `RefreshFrameRow(Id3v2Frame)`, `FindRow(Id3v2Frame)`; mark per-frame methods `[Obsolete]` |
| `AudioVideoLib.Studio/App.xaml.cs` | **MODIFY** — call `TagItemEditorRegistry.Shared.RegisterFromAssembly(...)` in `OnStartup` |
| `AudioVideoLib.Studio/Editors/ManageFramesViewModel.cs` | **CREATE** |
| `AudioVideoLib.Studio/Editors/ManageFramesDialog.xaml(.cs)` | **CREATE** |
| `AudioVideoLib.Studio/Converters/BoolToInTagConverter.cs` | **CREATE** |
| `AudioVideoLib.Studio.Tests/Editors/StudioFixture.cs` | **CREATE** — `[CollectionDefinition("Studio")]` fixture that populates `TagItemEditorRegistry.Shared` once for tests needing the live registry |
| `AudioVideoLib.Studio.Tests/Editors/...` (multiple test files) | **CREATE** — see per-task |

---

## Tasks

### Task 1: Create the Studio test project

**Files:**
- Create: `AudioVideoLib.Studio.Tests/AudioVideoLib.Studio.Tests.csproj`
- Create: `AudioVideoLib.Studio.Tests/Smoke/SanityTests.cs`
- Modify: `AudioVideoLib.slnx`

- [ ] **Step 1: Create the project file**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <RootNamespace>AudioVideoLib.Studio.Tests</RootNamespace>
    <AssemblyName>AudioVideoLib.Studio.Tests</AssemblyName>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.4.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Xunit.StaFact" Version="1.1.11" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\AudioVideoLib.Studio\AudioVideoLib.Studio.csproj" />
    <ProjectReference Include="..\AudioVideoLib\AudioVideoLib.csproj" />
  </ItemGroup>
</Project>
```

`Xunit.StaFact` adds `[StaFact]` for the small number of XAML-inflation smoke tests in §9.5.

- [ ] **Step 2: Add a sanity test**

```csharp
namespace AudioVideoLib.Studio.Tests.Smoke;
using Xunit;

public class SanityTests
{
    [Fact]
    public void StudioAssembly_IsReferenceable()
        => Assert.Equal("AudioVideoLib.Studio", typeof(MainWindow).Assembly.GetName().Name);
}
```

- [ ] **Step 3: Add to `AudioVideoLib.slnx`** — append `<Project Path="AudioVideoLib.Studio.Tests/AudioVideoLib.Studio.Tests.csproj" />` after the existing `AudioVideoLib.Tests` line.

- [ ] **Step 4: Build and run**

```bash
dotnet test AudioVideoLib.Studio.Tests/AudioVideoLib.Studio.Tests.csproj
```

Expected: 1/1 passing.

- [ ] **Step 5: Commit** — `chore(studio): add Studio test project`.

---

### Task 2: Build the tag-format-agnostic foundation

**Files:**
- Create: `AudioVideoLib.Studio/Editors/ITagItemEditor.cs`
- Create: `AudioVideoLib.Studio/Editors/TagItemEditorAttribute.cs`
- Create: `AudioVideoLib.Studio/Editors/TagItemEditorRegistry.cs`
- Create: `AudioVideoLib.Studio.Tests/Editors/TagItemEditorRegistryTests.cs`

- [ ] **Step 1: Write `ITagItemEditor.cs`**

```csharp
namespace AudioVideoLib.Studio.Editors;

using System;
using System.Windows;

public interface ITagItemEditor<TItem>
{
    TItem CreateNew(object tag);
    bool Edit(Window owner, TItem item);
}

public interface ITagItemEditorAdapter
{
    Type ItemType { get; }
    object CreateNew(object tag);
    bool Edit(Window owner, object item);
}

internal sealed class TagItemEditorAdapter<TItem> : ITagItemEditorAdapter
{
    private readonly ITagItemEditor<TItem> _inner;
    public TagItemEditorAdapter(ITagItemEditor<TItem> inner) => _inner = inner;
    public Type ItemType => typeof(TItem);
    public object CreateNew(object tag) => _inner.CreateNew(tag)!;
    public bool Edit(Window owner, object item) => _inner.Edit(owner, (TItem)item);
}
```

- [ ] **Step 2: Write `TagItemEditorAttribute.cs`**

```csharp
namespace AudioVideoLib.Studio.Editors;

using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public abstract class TagItemEditorAttribute : Attribute
{
    public Type ItemType { get; }
    public string MenuLabel { get; init; } = string.Empty;
    public int Order { get; init; }

    protected TagItemEditorAttribute(Type itemType)
    {
        ArgumentNullException.ThrowIfNull(itemType);
        ItemType = itemType;
    }
}
```

- [ ] **Step 3: Write `TagItemEditorRegistry.cs`**

```csharp
namespace AudioVideoLib.Studio.Editors;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public sealed class TagItemEditorRegistry
{
    public static TagItemEditorRegistry Shared { get; } = new();

    private readonly Dictionary<Type, RegistrationEntry> _byItemType = new();
    private readonly List<RegistrationEntry> _entries = new();

    public IReadOnlyList<RegistrationEntry> Entries => _entries;

    public void RegisterFromAssembly(Assembly assembly, Func<Type, bool>? editorTypeFilter = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        Type[] candidates;
        try { candidates = assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { candidates = ex.Types.Where(t => t is not null).Cast<Type>().ToArray(); }

        foreach (var type in candidates
                     .Where(t => !t.IsAbstract && !t.IsInterface)
                     .Where(t => editorTypeFilter is null || editorTypeFilter(t)))
        {
            foreach (var attr in type.GetCustomAttributes<TagItemEditorAttribute>(inherit: false))
            {
                var adapter = CreateAdapter(type, attr.ItemType);
                if (_byItemType.ContainsKey(attr.ItemType))
                {
                    var existing = _byItemType[attr.ItemType].EditorType.Name;
                    throw new InvalidOperationException(
                        $"Duplicate editor for item type {attr.ItemType.FullName}: {existing} and {type.Name}.");
                }
                var entry = new RegistrationEntry(type, attr, adapter);
                _byItemType.Add(attr.ItemType, entry);
                _entries.Add(entry);
            }
        }
    }

    public bool TryResolve(Type itemRuntimeType, out ITagItemEditorAdapter editor)
    {
        ArgumentNullException.ThrowIfNull(itemRuntimeType);
        for (var t = itemRuntimeType; t is not null && t != typeof(object); t = t.BaseType)
        {
            if (_byItemType.TryGetValue(t, out var entry))
            {
                editor = entry.Adapter;
                return true;
            }
        }
        editor = null!;
        return false;
    }

    private static ITagItemEditorAdapter CreateAdapter(Type editorType, Type itemType)
    {
        var iface = typeof(ITagItemEditor<>).MakeGenericType(itemType);
        if (!iface.IsAssignableFrom(editorType))
        {
            throw new InvalidOperationException(
                $"Editor {editorType.FullName} must implement ITagItemEditor<{itemType.Name}>.");
        }
        var instance = Activator.CreateInstance(editorType)!;
        var adapterType = typeof(TagItemEditorAdapter<>).MakeGenericType(itemType);
        return (ITagItemEditorAdapter)Activator.CreateInstance(adapterType, instance)!;
    }

    public readonly record struct RegistrationEntry(
        Type EditorType, TagItemEditorAttribute Attribute, ITagItemEditorAdapter Adapter);
}
```

Note: `Activator.CreateInstance(editorType)` works off the UI thread because every editor is a *non-Window* class per the two-class pattern. Tests validating registry behaviour with sample editors thus run under default xUnit threading.

- [ ] **Step 4: Write tests**

```csharp
namespace AudioVideoLib.Studio.Tests.Editors;

using System;
using System.Linq;
using AudioVideoLib.Studio.Editors;
using Xunit;

public class TagItemEditorRegistryTests
{
    public sealed class SampleItem { }
    public sealed class OtherItem { }

    [SampleItemEditor(typeof(SampleItem), MenuLabel = "Sample")]
    public sealed class SampleEditor : ITagItemEditor<SampleItem>
    {
        public SampleItem CreateNew(object tag) => new();
        public bool Edit(System.Windows.Window owner, SampleItem item) => true;
    }

    public sealed class SampleItemEditorAttribute : TagItemEditorAttribute
    {
        public SampleItemEditorAttribute(Type itemType) : base(itemType) { }
    }

    [Fact]
    public void RegisterFromAssembly_ScopedFilter_FindsExpectedEditor()
    {
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(SampleEditor).Assembly, t => t == typeof(SampleEditor));
        Assert.True(r.TryResolve(typeof(SampleItem), out var editor));
        Assert.Equal(typeof(SampleItem), editor.ItemType);
    }

    [Fact]
    public void TryResolve_UnknownType_ReturnsFalse()
    {
        var r = new TagItemEditorRegistry();
        Assert.False(r.TryResolve(typeof(string), out _));
    }

    [Fact]
    public void TryResolve_DerivedType_FallsBackToBase()
    {
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(SampleEditor).Assembly, t => t == typeof(SampleEditor));

        // Derive a runtime type from SampleItem
        var derivedItem = new SampleItemDerived();
        Assert.True(r.TryResolve(derivedItem.GetType(), out var editor));
        Assert.Equal(typeof(SampleItem), editor.ItemType);
    }

    public sealed class SampleItemDerived : SampleItem { }
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test AudioVideoLib.Studio.Tests/ --filter TagItemEditorRegistryTests
```

Expected: 3/3 passing.

- [ ] **Step 6: Commit** — `feat(studio): tag-item editor registry foundation`.

---

### Task 3: Build the ID3v2-specific layer (version mask, category, attribute)

**Files:**
- Create: `AudioVideoLib.Studio/Editors/Id3v2/Id3v2VersionMask.cs`
- Create: `AudioVideoLib.Studio/Editors/Id3v2/Id3v2FrameCategory.cs`
- Create: `AudioVideoLib.Studio/Editors/Id3v2/Id3v2FrameEditorAttribute.cs`
- Create: `AudioVideoLib.Studio.Tests/Editors/Id3v2/Id3v2VersionMaskTests.cs`

- [ ] **Step 1: `Id3v2VersionMask.cs`**

```csharp
namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using AudioVideoLib.Tags;

[Flags]
public enum Id3v2VersionMask
{
    None = 0,
    V220 = 1 << 0,
    V221 = 1 << 1,    // distinct from V220 because Id3v2CompressedDataMetaFrame (CDM) is hard-coded to v2.2.1
    V230 = 1 << 2,
    V240 = 1 << 3,
    All  = V220 | V221 | V230 | V240,
}

public static class Id3v2VersionMaskExtensions
{
    public static Id3v2VersionMask ToMask(this Id3v2Version version) => version switch
    {
        Id3v2Version.Id3v220 => Id3v2VersionMask.V220,
        Id3v2Version.Id3v221 => Id3v2VersionMask.V221,
        Id3v2Version.Id3v230 => Id3v2VersionMask.V230,
        Id3v2Version.Id3v240 => Id3v2VersionMask.V240,
        _ => Id3v2VersionMask.None,
    };

    public static bool Contains(this Id3v2VersionMask mask, Id3v2Version version)
        => (mask & version.ToMask()) != Id3v2VersionMask.None;
}
```

- [ ] **Step 2: `Id3v2FrameCategory.cs`**

```csharp
namespace AudioVideoLib.Studio.Editors.Id3v2;

public enum Id3v2FrameCategory
{
    TextFrames, UrlFrames, Identification, CommentsAndLyrics, TimingAndSync, People,
    AudioAdjustment, CountersAndRatings, Attachments, CommerceAndRights,
    EncryptionAndCompression, Containers, System, Experimental,
}

internal static class Id3v2FrameCategoryDisplay
{
    public static string ToDisplay(this Id3v2FrameCategory c) => c switch
    {
        Id3v2FrameCategory.TextFrames                 => "Text frame",
        Id3v2FrameCategory.UrlFrames                  => "URL frame",
        Id3v2FrameCategory.Identification             => "Identification",
        Id3v2FrameCategory.CommentsAndLyrics          => "Comments & lyrics",
        Id3v2FrameCategory.TimingAndSync              => "Timing & sync",
        Id3v2FrameCategory.People                     => "People",
        Id3v2FrameCategory.AudioAdjustment            => "Audio adjustment",
        Id3v2FrameCategory.CountersAndRatings         => "Counters & ratings",
        Id3v2FrameCategory.Attachments                => "Attachments",
        Id3v2FrameCategory.CommerceAndRights          => "Commerce & rights",
        Id3v2FrameCategory.EncryptionAndCompression   => "Encryption & compression",
        Id3v2FrameCategory.Containers                 => "Containers",
        Id3v2FrameCategory.System                     => "System",
        Id3v2FrameCategory.Experimental               => "Experimental",
        _ => c.ToString(),
    };
}
```

- [ ] **Step 3: `Id3v2FrameEditorAttribute.cs`**

```csharp
namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using AudioVideoLib.Studio.Editors;

public sealed class Id3v2FrameEditorAttribute : TagItemEditorAttribute
{
    public Id3v2FrameCategory Category { get; init; }
    public Id3v2VersionMask SupportedVersions { get; init; } = Id3v2VersionMask.All;
    public bool IsUniqueInstance { get; init; }
    /// Used when the frame class lacks an `(Id3v2Version)` ctor and reflection-based
    /// identifier resolution would fail. Required for `Id3v2CompressedDataMetaFrame` (CDM)
    /// and `Id3v2EncryptedMetaFrame` (CRM); optional for everything else.
    public string? KnownIdentifier { get; init; }

    public Id3v2FrameEditorAttribute(Type frameType) : base(frameType) { }
}
```

- [ ] **Step 4: Tests**

```csharp
namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;
using Xunit;

public class Id3v2VersionMaskTests
{
    [Theory]
    [InlineData(Id3v2Version.Id3v220, Id3v2VersionMask.V220)]
    [InlineData(Id3v2Version.Id3v221, Id3v2VersionMask.V221)]
    [InlineData(Id3v2Version.Id3v230, Id3v2VersionMask.V230)]
    [InlineData(Id3v2Version.Id3v240, Id3v2VersionMask.V240)]
    public void ToMask_RoundTrip(Id3v2Version version, Id3v2VersionMask expected)
        => Assert.Equal(expected, version.ToMask());

    [Theory]
    [InlineData(Id3v2VersionMask.All, Id3v2Version.Id3v230, true)]
    [InlineData(Id3v2VersionMask.V230 | Id3v2VersionMask.V240, Id3v2Version.Id3v220, false)]
    [InlineData(Id3v2VersionMask.V240, Id3v2Version.Id3v240, true)]
    [InlineData(Id3v2VersionMask.None, Id3v2Version.Id3v230, false)]
    public void Contains_Matches(Id3v2VersionMask mask, Id3v2Version version, bool expected)
        => Assert.Equal(expected, mask.Contains(version));
}
```

- [ ] **Step 5: Run tests** — `dotnet test --filter Id3v2VersionMaskTests`. Expected 8/8 passing.
- [ ] **Step 6: Commit** — `feat(studio): ID3v2 editor framework — version mask, category, attribute`.

---

### Task 4: Known frame ID tables (text + URL families)

**Files:**
- Create: `AudioVideoLib.Studio/Editors/Id3v2/Id3v2KnownTextFrameIds.cs`
- Create: `AudioVideoLib.Studio/Editors/Id3v2/Id3v2KnownUrlFrameIds.cs`
- Create: `AudioVideoLib.Studio.Tests/Editors/Id3v2/Id3v2KnownFrameIdsTests.cs`

- [ ] **Step 1: `Id3v2KnownTextFrameIds.cs`** — full table per ID3v2.4 §4.2 + back-fill of v2.3-only and v2.2 IDs. Each entry: `(Identifier4, V220Identifier3?, FriendlyName, SupportedVersions)`.

```csharp
namespace AudioVideoLib.Studio.Editors.Id3v2;

public sealed record Id3v2KnownTextFrameId(
    string Identifier,
    string? V220Identifier,
    string FriendlyName,
    Id3v2VersionMask SupportedVersions);

public static class Id3v2KnownTextFrameIds
{
    public static readonly Id3v2KnownTextFrameId[] All =
    [
        new("TALB", "TAL", "Album",                          Id3v2VersionMask.All),
        new("TBPM", "TBP", "Beats per minute",               Id3v2VersionMask.All),
        new("TCOM", "TCM", "Composer",                       Id3v2VersionMask.All),
        new("TCON", "TCO", "Genre",                          Id3v2VersionMask.All),
        new("TCOP", "TCR", "Copyright message",              Id3v2VersionMask.All),
        new("TDEN", null,  "Encoding time",                  Id3v2VersionMask.V240),
        new("TDLY", "TDY", "Playlist delay",                 Id3v2VersionMask.All),
        new("TDOR", null,  "Original release time",          Id3v2VersionMask.V240),
        new("TDRC", null,  "Recording time",                 Id3v2VersionMask.V240),
        new("TDRL", null,  "Release time",                   Id3v2VersionMask.V240),
        new("TDTG", null,  "Tagging time",                   Id3v2VersionMask.V240),
        new("TENC", "TEN", "Encoded by",                     Id3v2VersionMask.All),
        new("TEXT", "TXT", "Lyricist / Text writer",         Id3v2VersionMask.All),
        new("TFLT", "TFT", "File type",                      Id3v2VersionMask.All),
        new("TIPL", null,  "Involved people list",           Id3v2VersionMask.V240),
        new("TIT1", "TT1", "Content group description",      Id3v2VersionMask.All),
        new("TIT2", "TT2", "Title",                          Id3v2VersionMask.All),
        new("TIT3", "TT3", "Subtitle / refinement",          Id3v2VersionMask.All),
        new("TKEY", "TKE", "Initial key",                    Id3v2VersionMask.All),
        new("TLAN", "TLA", "Language(s)",                    Id3v2VersionMask.All),
        new("TLEN", "TLE", "Length",                         Id3v2VersionMask.All),
        new("TMCL", null,  "Musician credits list",          Id3v2VersionMask.V240),
        new("TMED", "TMT", "Media type",                     Id3v2VersionMask.All),
        new("TMOO", null,  "Mood",                           Id3v2VersionMask.V240),
        new("TOAL", "TOT", "Original album",                 Id3v2VersionMask.All),
        new("TOFN", "TOF", "Original filename",              Id3v2VersionMask.All),
        new("TOLY", "TOL", "Original lyricist",              Id3v2VersionMask.All),
        new("TOPE", "TOA", "Original artist",                Id3v2VersionMask.All),
        new("TOWN", null,  "File owner / licensee",          Id3v2VersionMask.V230 | Id3v2VersionMask.V240),
        new("TPE1", "TP1", "Lead artist",                    Id3v2VersionMask.All),
        new("TPE2", "TP2", "Band / orchestra",               Id3v2VersionMask.All),
        new("TPE3", "TP3", "Conductor",                      Id3v2VersionMask.All),
        new("TPE4", "TP4", "Interpreted / remixed by",       Id3v2VersionMask.All),
        new("TPOS", "TPA", "Part of a set",                  Id3v2VersionMask.All),
        new("TPRO", null,  "Produced notice",                Id3v2VersionMask.V240),
        new("TPUB", "TPB", "Publisher",                      Id3v2VersionMask.All),
        new("TRCK", "TRK", "Track number",                   Id3v2VersionMask.All),
        new("TRSN", null,  "Internet radio station name",    Id3v2VersionMask.V230 | Id3v2VersionMask.V240),
        new("TRSO", null,  "Internet radio station owner",   Id3v2VersionMask.V230 | Id3v2VersionMask.V240),
        new("TSOA", null,  "Album sort order",               Id3v2VersionMask.V240),
        new("TSOP", null,  "Performer sort order",           Id3v2VersionMask.V240),
        new("TSOT", null,  "Title sort order",               Id3v2VersionMask.V240),
        new("TSRC", "TRC", "ISRC",                           Id3v2VersionMask.All),
        new("TSSE", "TSS", "Encoding software / hardware",   Id3v2VersionMask.All),
        new("TSST", null,  "Set subtitle",                   Id3v2VersionMask.V240),
        // v2.4-deprecated date/year frames (still readable in v2.2/v2.3)
        new("TDAT", "TDA", "Date (DDMM, deprecated v2.4)",   Id3v2VersionMask.V220 | Id3v2VersionMask.V230),
        new("TIME", "TIM", "Time (HHMM, deprecated v2.4)",   Id3v2VersionMask.V220 | Id3v2VersionMask.V230),
        new("TORY", "TOR", "Original release year",          Id3v2VersionMask.V220 | Id3v2VersionMask.V230),
        new("TRDA", "TRD", "Recording dates",                Id3v2VersionMask.V220 | Id3v2VersionMask.V230),
        new("TSIZ", "TSI", "Size",                           Id3v2VersionMask.V220 | Id3v2VersionMask.V230),
        new("TYER", "TYE", "Year",                           Id3v2VersionMask.V220 | Id3v2VersionMask.V230),
    ];

    public static string IdentifierFor(Id3v2KnownTextFrameId entry, Id3v2VersionMask versionMask)
    {
        if (versionMask == Id3v2VersionMask.V220 && entry.V220Identifier is { } v220) return v220;
        return entry.Identifier;
    }
}
```

- [ ] **Step 2: `Id3v2KnownUrlFrameIds.cs`**

```csharp
namespace AudioVideoLib.Studio.Editors.Id3v2;

public sealed record Id3v2KnownUrlFrameId(
    string Identifier,
    string? V220Identifier,
    string FriendlyName,
    Id3v2VersionMask SupportedVersions);

public static class Id3v2KnownUrlFrameIds
{
    public static readonly Id3v2KnownUrlFrameId[] All =
    [
        new("WCOM", "WCM", "Commercial information",         Id3v2VersionMask.All),
        new("WCOP", "WCP", "Copyright / legal information",  Id3v2VersionMask.All),
        new("WOAF", "WAF", "Official audio file webpage",    Id3v2VersionMask.All),
        new("WOAR", "WAR", "Official artist webpage",        Id3v2VersionMask.All),
        new("WOAS", "WAS", "Official audio source webpage",  Id3v2VersionMask.All),
        new("WORS", null,  "Official internet radio webpage",Id3v2VersionMask.V230 | Id3v2VersionMask.V240),
        new("WPAY", null,  "Payment URL",                    Id3v2VersionMask.V230 | Id3v2VersionMask.V240),
        new("WPUB", "WPB", "Publishers official webpage",    Id3v2VersionMask.All),
    ];

    public static string IdentifierFor(Id3v2KnownUrlFrameId entry, Id3v2VersionMask versionMask)
    {
        if (versionMask == Id3v2VersionMask.V220 && entry.V220Identifier is { } v220) return v220;
        return entry.Identifier;
    }
}
```

- [ ] **Step 3: Tests**

```csharp
namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using System.Linq;
using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;
using Xunit;

public class Id3v2KnownFrameIdsTests
{
    [Fact]
    public void TextIds_NoDuplicateIdentifiers()
        => Assert.Equal(Id3v2KnownTextFrameIds.All.Length,
                        Id3v2KnownTextFrameIds.All.Select(i => i.Identifier).Distinct().Count());

    [Fact]
    public void TextIds_NoDuplicateV220Identifiers()
    {
        var v220 = Id3v2KnownTextFrameIds.All.Where(i => i.V220Identifier is not null)
                                             .Select(i => i.V220Identifier!).ToArray();
        Assert.Equal(v220.Length, v220.Distinct().Count());
    }

    [Fact]
    public void TextIds_AllStartWithT()
        => Assert.All(Id3v2KnownTextFrameIds.All, i => Assert.StartsWith("T", i.Identifier));

    [Theory]
    [InlineData("TIT2")] [InlineData("TPE1")] [InlineData("TALB")] [InlineData("TCON")]
    public void TextId_ConstructsAtV240(string identifier)
    {
        var entry = Id3v2KnownTextFrameIds.All.Single(i => i.Identifier == identifier);
        if (!entry.SupportedVersions.Contains(Id3v2Version.Id3v240)) return;
        var f = new Id3v2TextFrame(Id3v2Version.Id3v240, identifier);
        Assert.Equal(identifier, f.Identifier);
    }

    [Theory]
    [InlineData("TYER", "TYE")] [InlineData("TIT2", "TT2")] [InlineData("TALB", "TAL")]
    public void TextId_V220_ConstructsAndIdentifierMatches(string v240Id, string v220Id)
    {
        var entry = Id3v2KnownTextFrameIds.All.Single(i => i.Identifier == v240Id);
        Assert.Equal(v220Id, entry.V220Identifier);
        if (!entry.SupportedVersions.Contains(Id3v2Version.Id3v220)) return;
        var f = new Id3v2TextFrame(Id3v2Version.Id3v220, v220Id);
        Assert.Equal(v220Id, f.Identifier);
    }

    [Fact]
    public void UrlIds_NoDuplicates()
        => Assert.Equal(Id3v2KnownUrlFrameIds.All.Length,
                        Id3v2KnownUrlFrameIds.All.Select(i => i.Identifier).Distinct().Count());

    [Fact]
    public void UrlIds_AllStartWithW()
        => Assert.All(Id3v2KnownUrlFrameIds.All, i => Assert.StartsWith("W", i.Identifier));

    [Theory]
    [InlineData("WCOM")] [InlineData("WOAR")] [InlineData("WPUB")]
    public void UrlId_ConstructsAtV240(string identifier)
    {
        var entry = Id3v2KnownUrlFrameIds.All.Single(i => i.Identifier == identifier);
        if (!entry.SupportedVersions.Contains(Id3v2Version.Id3v240)) return;
        var f = new Id3v2UrlLinkFrame(Id3v2Version.Id3v240, identifier);
        Assert.Equal(identifier, f.Identifier);
    }
}
```

- [ ] **Step 4: Run tests** — expected: all passing.
- [ ] **Step 5: Commit** — `feat(studio): known text + URL frame ID tables`.

---

### Task 5: Pattern base classes (CollectionEditorBase + WrapperEditorBase)

**Files:**
- Create: `AudioVideoLib.Studio/Editors/CollectionEditorBase.cs`
- Create: `AudioVideoLib.Studio/Editors/WrapperEditorBase.cs`
- Create: `AudioVideoLib.Studio.Tests/Editors/CollectionEditorBaseTests.cs`
- Create: `AudioVideoLib.Studio.Tests/Editors/WrapperEditorBaseTests.cs`

- [ ] **Step 1: `CollectionEditorBase.cs`**

```csharp
namespace AudioVideoLib.Studio.Editors;

using System.Collections.ObjectModel;
using System.Windows;

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

- [ ] **Step 2: `WrapperEditorBase.cs`**

```csharp
namespace AudioVideoLib.Studio.Editors;

using System;
using System.Collections.Generic;
using System.Windows;
using AudioVideoLib.Tags;

public abstract class WrapperEditorBase<TFrame> : ITagItemEditor<TFrame> where TFrame : Id3v2Frame
{
    public IReadOnlyList<Id3v2Frame> WrappableSnapshot { get; private set; } = Array.Empty<Id3v2Frame>();
    public Id3v2Frame? SelectedChild { get; set; }

    public void TakeSnapshot(Id3v2Tag tag, TFrame self)
    {
        ArgumentNullException.ThrowIfNull(tag);
        var list = new List<Id3v2Frame>();
        foreach (var f in tag.Frames)
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

- [ ] **Step 3: Tests**

```csharp
namespace AudioVideoLib.Studio.Tests.Editors;

using AudioVideoLib.Studio.Editors;
using System.Windows;
using Xunit;

public class CollectionEditorBaseTests
{
    private sealed class Dialog : CollectionEditorBase<object, int>
    {
        public override object CreateNew(object tag) => new();
        public override bool Edit(Window owner, object frame) => false;
        public override void LoadRows(object frame) { }
        public override void SaveRows(object frame) { }
        public override bool Validate(out string? error) { error = null; return true; }
    }

    [Fact]
    public void AddRow_Appends()
    {
        var d = new Dialog();
        d.AddRow(1); d.AddRow(2);
        Assert.Equal(new[] { 1, 2 }, d.Entries);
    }

    [Fact]
    public void RemoveRow_OutOfRange_NoOp()
    {
        var d = new Dialog();
        d.RemoveRow(0);            // empty
        d.AddRow(7); d.RemoveRow(5); // out of range
        Assert.Single(d.Entries);
    }

    [Fact]
    public void MoveUp_FirstRow_NoOp()
    {
        var d = new Dialog();
        d.AddRow(1); d.AddRow(2);
        d.MoveUp(0);
        Assert.Equal(new[] { 1, 2 }, d.Entries);
    }

    [Fact]
    public void MoveDown_LastRow_NoOp()
    {
        var d = new Dialog();
        d.AddRow(1); d.AddRow(2);
        d.MoveDown(1);
        Assert.Equal(new[] { 1, 2 }, d.Entries);
    }

    [Fact]
    public void MoveUp_Swaps()
    {
        var d = new Dialog();
        d.AddRow(1); d.AddRow(2); d.AddRow(3);
        d.MoveUp(2);
        Assert.Equal(new[] { 1, 3, 2 }, d.Entries);
    }

    [Fact]
    public void MoveDown_Swaps()
    {
        var d = new Dialog();
        d.AddRow(1); d.AddRow(2); d.AddRow(3);
        d.MoveDown(0);
        Assert.Equal(new[] { 2, 1, 3 }, d.Entries);
    }
}
```

```csharp
namespace AudioVideoLib.Studio.Tests.Editors;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;
using System.Linq;
using System.Windows;
using Xunit;

public class WrapperEditorBaseTests
{
    private sealed class Dialog : WrapperEditorBase<Id3v2CompressedDataMetaFrame>
    {
        public override Id3v2CompressedDataMetaFrame CreateNew(object tag) => new();
        public override bool Edit(Window owner, Id3v2CompressedDataMetaFrame frame) => false;
        public override bool Validate(out string? error) { error = null; return true; }
    }

    // Note: Id3v2CompressedDataMetaFrame()'s parameterless ctor fixes version to Id3v2v221.
    // SetFrame validates frame.Version == tag.Version, so the tag and any text-frame siblings
    // must also be at Id3v2v221. (Id3v2EncryptedMetaFrame is v220; can't mix the two in one tag.)
    [Fact]
    public void TakeSnapshot_ExcludesSelf()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v221);
        var self = new Id3v2CompressedDataMetaFrame();           // v221
        var other = new Id3v2TextFrame(Id3v2Version.Id3v221, "TT2");
        tag.SetFrame(self);
        tag.SetFrame(other);

        var d = new Dialog();
        d.TakeSnapshot(tag, self);
        Assert.Single(d.WrappableSnapshot);
        Assert.Same(other, d.WrappableSnapshot[0]);
    }

    [Fact]
    public void TakeSnapshot_ExcludesOtherCdmWrappers_AtV221()
    {
        // CRM (Id3v2EncryptedMetaFrame) is v220; CDM (Id3v2CompressedDataMetaFrame) is v221.
        // They cannot coexist in one tag. This test verifies CDM exclusion via two CDM siblings.
        var tag = new Id3v2Tag(Id3v2Version.Id3v221);
        var self = new Id3v2CompressedDataMetaFrame();
        var siblingCdm = new Id3v2CompressedDataMetaFrame();
        var plain = new Id3v2TextFrame(Id3v2Version.Id3v221, "TT2");
        tag.SetFrame(self); tag.SetFrame(siblingCdm); tag.SetFrame(plain);

        var d = new Dialog();
        d.TakeSnapshot(tag, self);
        Assert.Single(d.WrappableSnapshot);
        Assert.Same(plain, d.WrappableSnapshot[0]);
    }
}
```

- [ ] **Step 4: Run tests** — expected: 8/8 passing.
- [ ] **Step 5: Commit** — `feat(studio): pattern base classes (collection + wrapper)`.

---

### Task 6: Add menu builder (model + WPF projection + smart-toggle)

**Files:**
- Create: `AudioVideoLib.Studio/Editors/Id3v2/Id3v2MenuModel.cs`
- Create: `AudioVideoLib.Studio/Editors/Id3v2/Id3v2AddMenuBuilder.cs`
- Create: `AudioVideoLib.Studio.Tests/Editors/Id3v2/Id3v2AddMenuBuilderTests.cs`

The builder is structured so:
- `BuildEntryLabel(attribute, tag)` — pure function, smart-toggle Add/Edit decision (testable in isolation, OQ-2).
- `BuildModel(registry, tag)` — pure function, returns the cascading menu as a tree of `Id3v2MenuModel`.
- `Populate(menu, model, onClick)` — thin WPF projection.

- [ ] **Step 1: `Id3v2MenuModel.cs`**

```csharp
namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Collections.Generic;

public sealed record Id3v2MenuEntry(string Label, string FrameIdentifier, bool IsEditExisting);
public sealed record Id3v2MenuCategory(Id3v2FrameCategory Category, string Header,
                                       IReadOnlyList<Id3v2MenuEntry> Entries);
public sealed record Id3v2MenuModel(IReadOnlyList<Id3v2MenuCategory> Categories);
```

- [ ] **Step 2: `Id3v2AddMenuBuilder.cs`**

```csharp
namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

public static class Id3v2AddMenuBuilder
{
    public static string BuildEntryLabel(Id3v2FrameEditorAttribute attribute, Id3v2Tag tag)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        ArgumentNullException.ThrowIfNull(tag);
        var label = string.IsNullOrEmpty(attribute.MenuLabel)
            ? IdentifierFor(attribute, tag.Version) ?? "?"
            : attribute.MenuLabel;
        var existing = attribute.IsUniqueInstance && tag.Frames.Any(f => f.GetType() == attribute.ItemType);
        return $"{(existing ? "Edit" : "Add")} {label}…";
    }

    public static Id3v2MenuModel BuildModel(TagItemEditorRegistry registry, Id3v2Tag tag)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(tag);
        var version = tag.Version;
        var versionMask = version.ToMask();
        var categories = new List<Id3v2MenuCategory>();

        foreach (var category in CategoriesInDisplayOrder())
        {
            IReadOnlyList<Id3v2MenuEntry> entries = category switch
            {
                Id3v2FrameCategory.TextFrames => BuildTextFamilyEntries(versionMask),
                Id3v2FrameCategory.UrlFrames  => BuildUrlFamilyEntries(versionMask),
                _                             => BuildRegistryEntries(registry, category, version, tag),
            };

            if (entries.Count > 0)
                categories.Add(new Id3v2MenuCategory(category, category.ToDisplay(), entries));
        }

        return new Id3v2MenuModel(categories);
    }

    public static void Populate(ContextMenu menu, Id3v2MenuModel model,
                                Action<Id3v2MenuEntry> onClick)
    {
        ArgumentNullException.ThrowIfNull(menu);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(onClick);
        menu.Items.Clear();
        foreach (var cat in model.Categories)
        {
            var sub = new MenuItem { Header = cat.Header };
            foreach (var entry in cat.Entries)
            {
                var mi = new MenuItem { Header = entry.Label, Tag = entry };
                mi.Click += (_, _) => onClick(entry);
                sub.Items.Add(mi);
            }
            menu.Items.Add(sub);
        }
    }

    private static IEnumerable<Id3v2FrameCategory> CategoriesInDisplayOrder() => new[]
    {
        Id3v2FrameCategory.TextFrames, Id3v2FrameCategory.UrlFrames,
        Id3v2FrameCategory.Identification, Id3v2FrameCategory.CommentsAndLyrics,
        Id3v2FrameCategory.TimingAndSync, Id3v2FrameCategory.People,
        Id3v2FrameCategory.AudioAdjustment, Id3v2FrameCategory.CountersAndRatings,
        Id3v2FrameCategory.Attachments, Id3v2FrameCategory.CommerceAndRights,
        Id3v2FrameCategory.EncryptionAndCompression, Id3v2FrameCategory.Containers,
        Id3v2FrameCategory.System, Id3v2FrameCategory.Experimental,
    };

    private static IReadOnlyList<Id3v2MenuEntry> BuildTextFamilyEntries(Id3v2VersionMask versionMask)
        => Id3v2KnownTextFrameIds.All
            .Where(i => (i.SupportedVersions & versionMask) != 0)
            .OrderBy(i => i.FriendlyName, StringComparer.Ordinal)
            .Select(i =>
            {
                var ident = Id3v2KnownTextFrameIds.IdentifierFor(i, versionMask);
                return new Id3v2MenuEntry($"{ident} — {i.FriendlyName}", ident, IsEditExisting: false);
            })
            .ToArray();

    private static IReadOnlyList<Id3v2MenuEntry> BuildUrlFamilyEntries(Id3v2VersionMask versionMask)
        => Id3v2KnownUrlFrameIds.All
            .Where(i => (i.SupportedVersions & versionMask) != 0)
            .OrderBy(i => i.FriendlyName, StringComparer.Ordinal)
            .Select(i =>
            {
                var ident = Id3v2KnownUrlFrameIds.IdentifierFor(i, versionMask);
                return new Id3v2MenuEntry($"{ident} — {i.FriendlyName}", ident, IsEditExisting: false);
            })
            .ToArray();

    private static IReadOnlyList<Id3v2MenuEntry> BuildRegistryEntries(
        TagItemEditorRegistry registry, Id3v2FrameCategory category,
        Id3v2Version version, Id3v2Tag tag)
        => registry.Entries
            .Where(e => e.Attribute is Id3v2FrameEditorAttribute a
                        && a.Category == category
                        && a.SupportedVersions.Contains(version))
            .OrderBy(e => e.Attribute.Order)
            .Select(e =>
            {
                var attr = (Id3v2FrameEditorAttribute)e.Attribute;
                var label = BuildEntryLabel(attr, tag);
                var ident = IdentifierFor(attr, version) ?? string.Empty;
                var existing = attr.IsUniqueInstance && tag.Frames.Any(f => f.GetType() == attr.ItemType);
                return new Id3v2MenuEntry(label, ident, existing);
            })
            .ToArray();

    internal static string? IdentifierFor(Id3v2FrameEditorAttribute attr, Id3v2Version version)
    {
        if (!string.IsNullOrEmpty(attr.KnownIdentifier)) return attr.KnownIdentifier;
        try
        {
            var ctor = attr.ItemType.GetConstructor(new[] { typeof(Id3v2Version) });
            if (ctor is null) return null;
            return ((Id3v2Frame)ctor.Invoke(new object[] { version })).Identifier;
        }
        catch { return null; }
    }
}
```

- [ ] **Step 3: Tests for `BuildEntryLabel`** — pure function, can be tested without a populated registry.

```csharp
namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using System.Linq;
using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;
using Xunit;

[Collection("Studio")]   // pulls in StudioFixture so live-registry tests in Task 13 see populated Shared
public class Id3v2AddMenuBuilderTests
{
    [Fact]
    public void BuildEntryLabel_NotInTag_ReturnsAdd()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var attr = new Id3v2FrameEditorAttribute(typeof(Id3v2MusicCdIdentifierFrame))
        {
            MenuLabel = "Music CD identifier (MCDI)", IsUniqueInstance = true,
            SupportedVersions = Id3v2VersionMask.All,
        };
        Assert.StartsWith("Add", Id3v2AddMenuBuilder.BuildEntryLabel(attr, tag));
    }

    [Fact]
    public void BuildEntryLabel_InTagAndUnique_ReturnsEdit()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        tag.SetFrame(new Id3v2MusicCdIdentifierFrame(Id3v2Version.Id3v240) { TableOfContents = new byte[8] });
        var attr = new Id3v2FrameEditorAttribute(typeof(Id3v2MusicCdIdentifierFrame))
        {
            MenuLabel = "Music CD identifier (MCDI)", IsUniqueInstance = true,
            SupportedVersions = Id3v2VersionMask.All,
        };
        Assert.StartsWith("Edit", Id3v2AddMenuBuilder.BuildEntryLabel(attr, tag));
    }

    [Fact]
    public void BuildEntryLabel_InTagButNotUnique_ReturnsAdd()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        tag.SetFrame(new Id3v2PrivateFrame(Id3v2Version.Id3v240) { OwnerIdentifier = "x", PrivateData = [1] });
        var attr = new Id3v2FrameEditorAttribute(typeof(Id3v2PrivateFrame))
        {
            MenuLabel = "Private (PRIV)", IsUniqueInstance = false,
            SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
        };
        Assert.StartsWith("Add", Id3v2AddMenuBuilder.BuildEntryLabel(attr, tag));
    }
}
```

Note: tests for `BuildModel(...)` against the full Studio registry are gated and unskipped in Task 13 (after retrofit + reference editors are registered). Family-entry expansion tests (no registry needed) live here:

```csharp
[Fact]
public void BuildModel_V240_TextFrameEntries_UseV240Identifier()
{
    var tag = new Id3v2Tag(Id3v2Version.Id3v240);
    var model = Id3v2AddMenuBuilder.BuildModel(new TagItemEditorRegistry(), tag);
    var text = model.Categories.Single(c => c.Category == Id3v2FrameCategory.TextFrames);
    Assert.Contains(text.Entries, e => e.FrameIdentifier == "TIT2");
}

[Fact]
public void BuildModel_V220_TextFrameEntries_UseV220Identifier()
{
    var tag = new Id3v2Tag(Id3v2Version.Id3v220);
    var model = Id3v2AddMenuBuilder.BuildModel(new TagItemEditorRegistry(), tag);
    var text = model.Categories.Single(c => c.Category == Id3v2FrameCategory.TextFrames);
    Assert.Contains(text.Entries, e => e.FrameIdentifier == "TT2");   // v2.2 form of TIT2
}

[Fact]
public void BuildModel_V240_HidesContainersCategory()
{
    var tag = new Id3v2Tag(Id3v2Version.Id3v240);
    var model = Id3v2AddMenuBuilder.BuildModel(new TagItemEditorRegistry(), tag);
    Assert.DoesNotContain(model.Categories, c => c.Category == Id3v2FrameCategory.Containers);
}
```

- [ ] **Step 4: Run tests** — expected: 6/6 passing.
- [ ] **Step 5: Commit** — `feat(studio): ID3v2 add-menu builder (model + smart toggle + WPF projection)`.

---

### Task 7: Family editors — TextFrameEditor + UrlFrameEditor (and user-defined variants)

The four family editors share the same shape: non-Window editor class + `XxxEditorDialog.xaml(.cs)` Window subclass with `DataContext = editor`. Each editor exposes public `Load(frame)` / `Save(frame)` / `Validate(out string? error)` for testing, plus the family editors expose a `CreateNew(tag, identifier)` overload used by the menu special-case path.

For each of these four editors (TextFrameEditor, UserDefinedTextEditor, UrlFrameEditor, UserDefinedUrlEditor), follow this exact sequence:

#### Task 7a: TextFrameEditor

- [ ] **Step 1: `TextFrameEditor.cs`**

```csharp
namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2TextFrame),
    Category = Id3v2FrameCategory.TextFrames,
    MenuLabel = "Text frame",
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class TextFrameEditor : ITagItemEditor<Id3v2TextFrame>, INotifyPropertyChanged
{
    private string _identifier = "TIT2";
    private string _value = string.Empty;
    private Id3v2FrameEncodingType _encoding = Id3v2FrameEncodingType.Default;

    public string Identifier { get => _identifier; set => Set(ref _identifier, value); }
    public string Value      { get => _value;      set => Set(ref _value,      value); }
    public Id3v2FrameEncodingType Encoding { get => _encoding; set => Set(ref _encoding, value); }

    public Id3v2TextFrame CreateNew(object tag)
        => throw new InvalidOperationException(
            "TextFrameEditor needs an identifier. Use CreateNew(tag, identifier) instead.");

    public Id3v2TextFrame CreateNew(object tag, string identifier)
        => new Id3v2TextFrame(((Id3v2Tag)tag).Version, identifier);

    public bool Edit(Window owner, Id3v2TextFrame frame)
    {
        Load(frame);
        var dialog = new TextFrameEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true) return false;
        Save(frame);
        return true;
    }

    public void Load(Id3v2TextFrame frame)
    {
        Identifier = frame.Identifier;
        Encoding = frame.TextEncoding;
        Value = string.Join("\n", frame.Values);
    }

    public void Save(Id3v2TextFrame frame)
    {
        frame.TextEncoding = Encoding;
        frame.Values.Clear();
        // Split on '\n', strip a trailing '\r' (Windows TextBox produces \r\n), drop empty entries.
        foreach (var raw in Value.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0) continue;
            frame.Values.Add(line);
        }
    }

    public bool Validate(out string? error)
    {
        error = null;
        return true;     // TIT2 etc. allow empty values; validation is per-frame in detail editors
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T storage, T value, [CallerMemberName] string? prop = null)
    {
        if (Equals(storage, value)) return;
        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
```

- [ ] **Step 2: `TextFrameEditorDialog.xaml`**

```xaml
<Window x:Class="AudioVideoLib.Studio.Editors.Id3v2.TextFrameEditorDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Width="500" SizeToContent="Height" WindowStartupLocation="CenterOwner"
        Title="Text frame">
    <DockPanel Margin="14">
        <StackPanel DockPanel.Dock="Bottom" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,14,0,0">
            <Button Content="OK"     Width="80"                  Click="Ok_Click"     IsDefault="True" />
            <Button Content="Cancel" Width="80" Margin="8,0,0,0" Click="Cancel_Click" IsCancel="True" />
        </StackPanel>
        <StackPanel>
            <TextBlock Text="Identifier" Foreground="{DynamicResource TextSecondaryBrush}" FontSize="11" />
            <TextBox Text="{Binding Identifier}" IsReadOnly="True" Padding="4,3" Margin="0,0,0,8" />
            <TextBlock Text="Encoding" Foreground="{DynamicResource TextSecondaryBrush}" FontSize="11" />
            <!-- The Id3v2EncodingValues resource is declared in App.xaml (Task 15 Step 1) as an
                 ObjectDataProvider over Id3v2FrameEncodingType. Same pattern is used across editors
                 (CommentEditor, GeobEditor, etc.) so it lives in App.xaml, not per-dialog. -->
            <ComboBox ItemsSource="{Binding Source={StaticResource Id3v2EncodingValues}}"
                      SelectedValue="{Binding Encoding}" Margin="0,0,0,8" />
            <TextBlock Text="Value (newline = additional value)"
                       Foreground="{DynamicResource TextSecondaryBrush}" FontSize="11" />
            <TextBox Text="{Binding Value, UpdateSourceTrigger=PropertyChanged}"
                     AcceptsReturn="True" MinHeight="120" TextWrapping="Wrap"
                     VerticalScrollBarVisibility="Auto" />
        </StackPanel>
    </DockPanel>
</Window>
```

- [ ] **Step 3: `TextFrameEditorDialog.xaml.cs`**

```csharp
namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

public partial class TextFrameEditorDialog : Window
{
    public TextFrameEditorDialog() => InitializeComponent();

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var editor = (TextFrameEditor)DataContext;
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

- [ ] **Step 4: `TextFrameEditorTests.cs`** — round-trip test on the editor logic without instantiating the Window.

```csharp
namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;
using Xunit;

public class TextFrameEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2TextFrame(Id3v2Version.Id3v240, "TIT2")
        {
            TextEncoding = Id3v2FrameEncodingType.UTF16LittleEndian,
        };
        frame.Values.Add("Test title");

        var editor = new TextFrameEditor();
        editor.Load(frame);
        var copy = new Id3v2TextFrame(Id3v2Version.Id3v240, "TIT2");
        editor.Save(copy);
        Assert.Equal(frame.TextEncoding, copy.TextEncoding);
        Assert.Equal(frame.Values.ToArray(), copy.Values.ToArray());
    }

    [Fact]
    public void CreateNew_NoIdentifier_Throws()
        => Assert.Throws<System.InvalidOperationException>(() =>
                new TextFrameEditor().CreateNew(new Id3v2Tag(Id3v2Version.Id3v240)));

    [Fact]
    public void CreateNew_WithIdentifier_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var f = new TextFrameEditor().CreateNew(tag, "TPE1");
        Assert.Equal("TPE1", f.Identifier);
        Assert.Equal(Id3v2Version.Id3v240, f.Version);
    }

    [Fact]
    public void Validate_AlwaysTrue_ForBaseTextFrame() // detail validation lives in per-frame editors
    {
        var e = new TextFrameEditor();
        Assert.True(e.Validate(out _));
    }
}
```

- [ ] **Step 5: Run tests** — expected: 4/4 passing.
- [ ] **Step 6: Commit** — `feat(studio): TextFrameEditor + dialog (family editor)`.

#### Tasks 7b–7d: UserDefinedTextEditor, UrlFrameEditor, UserDefinedUrlEditor

Same exact 6-step structure as Task 7a, applied to:
- **`UserDefinedTextEditor`** — fields `Encoding` (combo), `Description` (TextBox), `Value` (multiline). `Validate` requires `Description` non-empty. Standard `CreateNew(tag)` (not family). MenuLabel: `"User-defined text (TXXX)"`. IsUniqueInstance=false.
- **`UrlFrameEditor`** — fields `Identifier` (read-only label), `Url` (TextBox). `Validate` requires `Uri.TryCreate(Url, UriKind.Absolute, out _)`. Family editor; same `CreateNew(tag, identifier)` shape as TextFrameEditor.
- **`UserDefinedUrlEditor`** — fields `Encoding`, `Description`, `Url`. `Validate` requires Description non-empty + URL well-formed.

Each gets one commit.

---

### Task 8: Pattern 1 reference editor — `CommentEditor`

Frame: `Id3v2CommentFrame`. Fields: `Encoding` (combo), `Language` (3-char ISO-639-2, default "eng"), `ShortContentDescription`, `Text`. Sets the bar for Pattern 1 quality; Wave 1 subagents follow it.

- [ ] **Step 1: Write `CommentEditorTests.cs`** — round-trip + Validate language length + Cancel semantics.

```csharp
namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;
using Xunit;

public class CommentEditorTests
{
    [Fact]
    public void LoadSave_RoundTrip()
    {
        var frame = new Id3v2CommentFrame(Id3v2Version.Id3v240)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF16LittleEndian,
            Language = "eng",
            ShortContentDescription = "desc",
            Text = "comment text",
        };
        var e = new CommentEditor();
        e.Load(frame);
        var copy = new Id3v2CommentFrame(Id3v2Version.Id3v240);
        e.Save(copy);
        Assert.Equal(frame.TextEncoding, copy.TextEncoding);
        Assert.Equal(frame.Language, copy.Language);
        Assert.Equal(frame.ShortContentDescription, copy.ShortContentDescription);
        Assert.Equal(frame.Text, copy.Text);
    }

    [Theory]
    [InlineData("en", false)]   // too short
    [InlineData("eng", true)]
    [InlineData("ENGL", false)] // too long
    [InlineData("",   false)]
    public void Validate_LanguageMustBe3Chars(string lang, bool expectedValid)
    {
        var e = new CommentEditor { Language = lang };
        Assert.Equal(expectedValid, e.Validate(out _));
    }
}
```

- [ ] **Step 2: Run tests, expect FAIL.**
- [ ] **Step 3: `CommentEditor.cs`**

```csharp
namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2CommentFrame),
    Category = Id3v2FrameCategory.CommentsAndLyrics,
    MenuLabel = "Comment (COMM)",
    Order = 9,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class CommentEditor : ITagItemEditor<Id3v2CommentFrame>, INotifyPropertyChanged
{
    private Id3v2FrameEncodingType _encoding = Id3v2FrameEncodingType.Default;
    private string _language = "eng";
    private string _shortDescription = string.Empty;
    private string _text = string.Empty;

    public Id3v2FrameEncodingType Encoding { get => _encoding; set => Set(ref _encoding, value); }
    public string Language { get => _language; set => Set(ref _language, value); }
    public string ShortContentDescription { get => _shortDescription; set => Set(ref _shortDescription, value); }
    public string Text { get => _text; set => Set(ref _text, value); }

    public Id3v2CommentFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2CommentFrame frame)
    {
        Load(frame);
        var dialog = new CommentEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true) return false;
        Save(frame);
        return true;
    }

    public void Load(Id3v2CommentFrame f)
    {
        Encoding = f.TextEncoding;
        // Library properties are non-nullable strings per AudioVideoLib/Tags/Id3v2CommentFrame.cs;
        // assign directly without `??` defaults (defaults are baked into the frame class).
        Language = f.Language;
        ShortContentDescription = f.ShortContentDescription;
        Text = f.Text;
    }

    public void Save(Id3v2CommentFrame f)
    {
        f.TextEncoding = Encoding;
        f.Language = Language;
        f.ShortContentDescription = ShortContentDescription;
        f.Text = Text;
    }

    public bool Validate(out string? error)
    {
        if (Language?.Length != 3)
        {
            error = "Language must be a 3-character ISO-639-2 code (e.g. \"eng\").";
            return false;
        }
        error = null;
        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T storage, T value, [CallerMemberName] string? prop = null)
    {
        if (Equals(storage, value)) return;
        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
```

- [ ] **Step 4: `CommentEditorDialog.xaml(.cs)`** — Pattern 1 shell (per spec §4.1) with 4 labeled fields bound to Encoding/Language/ShortContentDescription/Text.
- [ ] **Step 5: Run tests, expect PASS.**
- [ ] **Step 6: Commit** — `feat(studio): CommentEditor (Pattern 1 reference)`.

---

### Task 9: Pattern 2 reference editor — `EtcoEditor`

Frame: `Id3v2EventTimingCodesFrame` — exposes `TimeStampFormat` (`Id3v2TimeStampFormat`) and `KeyEvents` (`ICollection<Id3v2KeyEvent>`). `Id3v2KeyEvent` has `EventType` (`Id3v2KeyEventType`) and `TimeStamp` (uint). Form field: `TimeStampFormat` (combo). Grid: rows of `{ EventType, TimeStamp }`.

- [ ] **Step 1: Write `EtcoEditorTests.cs`**:

```csharp
namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;
using Xunit;

public class EtcoEditorTests
{
    [Fact]
    public void LoadSaveRoundTrip_PopulatedFrame()
    {
        var frame = new Id3v2EventTimingCodesFrame(Id3v2Version.Id3v240)
        {
            TimeStampFormat = Id3v2TimeStampFormat.AbsoluteTimeMilliseconds,
        };
        // Per the lib's Id3v2EventTimingCodesFrame API, populate events. Specific event-type/property
        // names verified at implementation time; the test asserts symmetric round-trip.
        var editor = new EtcoEditor();
        editor.Load(frame);
        var copy = new Id3v2EventTimingCodesFrame(Id3v2Version.Id3v240);
        editor.Save(copy);
        Assert.Equal(frame.TimeStampFormat, copy.TimeStampFormat);
    }

    [Fact]
    public void EmptyEventList_RoundTrips()
    {
        var frame = new Id3v2EventTimingCodesFrame(Id3v2Version.Id3v240);
        var editor = new EtcoEditor();
        editor.Load(frame);
        Assert.Empty(editor.Entries);
        var copy = new Id3v2EventTimingCodesFrame(Id3v2Version.Id3v240);
        editor.Save(copy);
        // (concrete event-list comparison done at implementation time)
    }

    [Fact]
    public void AddRow_AppendsToEntries()
    {
        var editor = new EtcoEditor();
        editor.AddRow(new EtcoRowVm { EventType = Id3v2KeyEventType.Padding, TimeStamp = 500 });
        Assert.Single(editor.Entries);
    }

    [Fact]
    public void Validate_NonMonotonicTimestamps_Fails()
    {
        var editor = new EtcoEditor { TimeStampFormat = Id3v2TimeStampFormat.AbsoluteTimeMilliseconds };
        editor.AddRow(new EtcoRowVm { EventType = Id3v2KeyEventType.MainPartStart, TimeStamp = 1000 });
        editor.AddRow(new EtcoRowVm { EventType = Id3v2KeyEventType.MainPartEnd,   TimeStamp = 500 });
        Assert.False(editor.Validate(out var err));
        Assert.Contains("monotonic", err, System.StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Run tests, expect FAIL.**
- [ ] **Step 3: `EtcoEditor.cs`** — inherits `CollectionEditorBase<Id3v2EventTimingCodesFrame, EtcoRowVm>`, decorated with:

```csharp
[Id3v2FrameEditor(typeof(Id3v2EventTimingCodesFrame),
    Category = Id3v2FrameCategory.TimingAndSync,
    MenuLabel = "Event timing codes (ETCO)",
    Order = 13,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = true)]
```

`EtcoRowVm` is a class with `EventType` (`Id3v2KeyEventType`) and `TimeStamp` (`int`, matching the lib's signed-int `Id3v2KeyEvent.TimeStamp`) settable properties so DataGrid can edit them inline.

`Id3v2KeyEvent.EventType` and `TimeStamp` have `private set` accessors, so `SaveRows` cannot mutate existing event instances — it must replace them:

```csharp
public override void LoadRows(Id3v2EventTimingCodesFrame frame)
{
    Entries.Clear();
    foreach (var e in frame.KeyEvents)
    {
        Entries.Add(new EtcoRowVm { EventType = e.EventType, TimeStamp = e.TimeStamp });
    }
}

public override void SaveRows(Id3v2EventTimingCodesFrame frame)
{
    frame.KeyEvents.Clear();
    foreach (var r in Entries)
    {
        frame.KeyEvents.Add(new Id3v2KeyEvent(r.EventType, r.TimeStamp));
    }
}

public override bool Validate(out string? error)
{
    // ID3v2 spec §4.6 requires events ordered by timestamp when TimeStampFormat is absolute.
    // Enforce monotonic non-decreasing order against the editor's TimeStampFormat field.
    var prev = int.MinValue;
    foreach (var r in Entries)
    {
        if (r.TimeStamp < prev)
        {
            error = "Events must be in monotonic non-decreasing timestamp order " +
                    "(per ID3v2 spec §4.6).";
            return false;
        }
        prev = r.TimeStamp;
    }
    error = null;
    return true;
}
```

- [ ] **Step 4: `EtcoEditorDialog.xaml(.cs)`** — Pattern 2 shell (Form + DataGrid bound to `Entries`); toolbar with Add/Remove/Up/Down delegating to base methods.
- [ ] **Step 5: Run tests, expect PASS.**
- [ ] **Step 6: Commit** — `feat(studio): EtcoEditor (Pattern 2 reference)`.

---

### Task 10: Pattern 3 reference editor — `CdmEditor`

Frame: `Id3v2CompressedDataMetaFrame` (v2.2 only). Wraps a child frame with compression. Note: the frame class has no `(Id3v2Version)` ctor — the attribute uses `KnownIdentifier = "CDM"`.

- [ ] **Step 1: Write `CdmEditorTests.cs`** — snapshot exclusion (already covered by `WrapperEditorBaseTests`); validate that OK without selection fails Validate.

```csharp
namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using Xunit;

public class CdmEditorTests
{
    [Fact]
    public void Validate_NoChildSelected_Fails()
    {
        var e = new CdmEditor();
        Assert.False(e.Validate(out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Validate_ChildSelected_Passes()
    {
        var e = new CdmEditor
        {
            SelectedChild = new AudioVideoLib.Tags.Id3v2TextFrame(AudioVideoLib.Tags.Id3v2Version.Id3v220, "TT2"),
        };
        Assert.True(e.Validate(out _));
    }
}
```

- [ ] **Step 2: Run tests, expect FAIL.**
- [ ] **Step 3: `CdmEditor.cs`** — inherits `WrapperEditorBase<Id3v2CompressedDataMetaFrame>`, decorated with:

```csharp
[Id3v2FrameEditor(typeof(Id3v2CompressedDataMetaFrame),
    Category = Id3v2FrameCategory.Containers,
    MenuLabel = "Compressed data meta (CDM)",
    Order = 32,
    SupportedVersions = Id3v2VersionMask.V221,   // CDM is hard-coded to v2.2.1; CRM (separate editor) is v2.2.0
    IsUniqueInstance = false,
    KnownIdentifier = "CDM")]
```

`CreateNew(tag)` returns `new Id3v2CompressedDataMetaFrame()` (no version param — frame class fixes version internally). `Edit` calls `TakeSnapshot(tag, frame)` then constructs the dialog.

- [ ] **Step 4: `CdmEditorDialog.xaml(.cs)`** — Pattern 3 shell (per spec §4.3) with `ComboBox` bound to `WrappableSnapshot` / `SelectedChild`.
- [ ] **Step 5: Run tests, expect PASS.**
- [ ] **Step 6: Commit** — `feat(studio): CdmEditor (Pattern 3 reference)`.

---

### Task 11: Retrofit existing editors via the adapter pattern

For each of APIC / USLT, create a non-Window adapter `XxxEditor` decorated with the attribute and implementing `ITagItemEditor<TFrame>`. The existing dialog files (`ApicEditorDialog.xaml(.cs)`, `UsltEditorDialog.xaml(.cs)`) move to `Editors/Id3v2/`, retain their UI and codebehind, but their `static bool Edit(Window, TFrame)` methods are renamed to `internal static bool EditCore(Window, TFrame)` — called by the adapter's `Edit` method only. For BinaryDataDialog, three adapters share the dialog (`PrivBinaryEditor`, `UfidBinaryEditor`, `McdiBinaryEditor`), and the dialog's three static `Edit` overloads are renamed to `EditPriv`, `EditUfid`, `EditMcdi`.

- [ ] **Step 1: Move files**

```bash
mkdir -p AudioVideoLib.Studio/Editors/Id3v2
git mv AudioVideoLib.Studio/ApicEditorDialog.xaml      AudioVideoLib.Studio/Editors/Id3v2/
git mv AudioVideoLib.Studio/ApicEditorDialog.xaml.cs   AudioVideoLib.Studio/Editors/Id3v2/
git mv AudioVideoLib.Studio/UsltEditorDialog.xaml      AudioVideoLib.Studio/Editors/Id3v2/
git mv AudioVideoLib.Studio/UsltEditorDialog.xaml.cs   AudioVideoLib.Studio/Editors/Id3v2/
git mv AudioVideoLib.Studio/BinaryDataDialog.xaml      AudioVideoLib.Studio/Editors/Id3v2/
git mv AudioVideoLib.Studio/BinaryDataDialog.xaml.cs   AudioVideoLib.Studio/Editors/Id3v2/
```

(Run from project root; the `.git` dir at `E:\Projects\AudioVideoLib\` is auto-discovered. If the working tree has the project root, paths above work as written.)

- [ ] **Step 2: Update namespaces**

In each moved file, change `namespace AudioVideoLib.Studio;` → `namespace AudioVideoLib.Studio.Editors.Id3v2;`. Update `x:Class` in XAML accordingly. Adjust call-site `using` directives in `MainWindow.xaml.cs` (these are removed in Task 12 anyway).

- [ ] **Step 3: Rename static methods**

In each moved dialog:
- `ApicEditorDialog`: rename `public static bool Edit(Window, Id3v2AttachedPictureFrame)` → `internal static bool EditCore(Window, Id3v2AttachedPictureFrame)`
- `UsltEditorDialog`: same renaming for its static `Edit`
- `BinaryDataDialog`: rename three `Edit` overloads to `EditPriv` / `EditUfid` / `EditMcdi`

- [ ] **Step 4: Create `ApicEditor.cs`**

```csharp
namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;
using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2AttachedPictureFrame),
    Category = Id3v2FrameCategory.Attachments,
    MenuLabel = "Attached picture (APIC)",
    Order = 27,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class ApicEditor : ITagItemEditor<Id3v2AttachedPictureFrame>
{
    public Id3v2AttachedPictureFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2AttachedPictureFrame frame)
        => ApicEditorDialog.EditCore(owner, frame);
}
```

- [ ] **Step 5: Create `UsltEditor.cs`** — same shape, MenuLabel="Unsynchronized lyrics (USLT)", Order=11.

- [ ] **Step 6: Create `PrivBinaryEditor.cs`, `UfidBinaryEditor.cs`, `McdiBinaryEditor.cs`**

```csharp
// PrivBinaryEditor.cs
namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;
using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2PrivateFrame),
    Category = Id3v2FrameCategory.System,
    MenuLabel = "Private (PRIV)",
    Order = 34,
    SupportedVersions = Id3v2VersionMask.V230 | Id3v2VersionMask.V240,
    IsUniqueInstance = false)]
public sealed class PrivBinaryEditor : ITagItemEditor<Id3v2PrivateFrame>
{
    public Id3v2PrivateFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);
    public bool Edit(Window owner, Id3v2PrivateFrame frame)
        => BinaryDataDialog.EditPriv(owner, frame);
}
```

`UfidBinaryEditor`: `MenuLabel="Unique file identifier (UFID)"`, `Order=5`, `SupportedVersions=All`, `IsUniqueInstance=false`. Calls `BinaryDataDialog.EditUfid`.

`McdiBinaryEditor`: `MenuLabel="Music CD identifier (MCDI)"`, `Order=6`, `SupportedVersions=All`, `IsUniqueInstance=true`. Calls `BinaryDataDialog.EditMcdi`.

- [ ] **Step 7: Tests**

Create `ApicEditorTests`, `UsltEditorTests`, `BinaryDataDialogAdaptersTests` covering: constructor; resolve via registry; CreateNew uses tag version. Tests do NOT call `Edit(...)` (would require STA + showing the existing Window dialogs); the adapter's Edit is delegating to existing dialog code that has its own coverage path.

```csharp
namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;
using Xunit;

public class ApicEditorTests
{
    [Fact]
    public void CreateNew_UsesTagVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var f = new ApicEditor().CreateNew(tag);
        Assert.Equal(Id3v2Version.Id3v240, f.Version);
    }

    [Fact]
    public void Registered_ForAttachedPictureFrame()
    {
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(ApicEditor).Assembly, t => t == typeof(ApicEditor));
        Assert.True(r.TryResolve(typeof(Id3v2AttachedPictureFrame), out _));
    }
}
```

Same shape for UsltEditorTests and the three binary adapter tests.

- [ ] **Step 8: Build + run all tests**

```bash
dotnet build AudioVideoLib.Studio
dotnet test AudioVideoLib.Studio.Tests/
```

Expected: clean build; all tests passing.

- [ ] **Step 9: Commit** — `refactor(studio): retrofit APIC/USLT/PRIV/UFID/MCDI via adapter pattern`.

---

### Task 12: Strip per-frame switches from MainWindow

**Files:**
- Modify: `AudioVideoLib.Studio/App.xaml.cs`
- Modify: `AudioVideoLib.Studio/MainWindow.xaml.cs`
- Modify: `AudioVideoLib.Studio/MainWindow.xaml`
- Modify: `AudioVideoLib.Studio/TagTabs.cs`

- [ ] **Step 1: Initialise registry in `App.xaml.cs.OnStartup`**

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    TagItemEditorRegistry.Shared.RegisterFromAssembly(typeof(MainWindow).Assembly);
#if DEBUG
    ValidateRegistry();
#endif
}

#if DEBUG
private static void ValidateRegistry()
{
    foreach (var entry in TagItemEditorRegistry.Shared.Entries)
    {
        if (entry.Attribute is Id3v2FrameEditorAttribute a)
        {
            if (a.SupportedVersions == Id3v2VersionMask.None)
                throw new InvalidOperationException($"Editor {entry.EditorType.Name} has SupportedVersions=None.");
            if (string.IsNullOrEmpty(a.MenuLabel))
                throw new InvalidOperationException($"Editor {entry.EditorType.Name} has empty MenuLabel.");
            if (a.MenuLabel.Length > 60)
                throw new InvalidOperationException(
                    $"Editor {entry.EditorType.Name} MenuLabel exceeds 60 chars: {a.MenuLabel.Length}.");
        }
    }
}
#endif
```

- [ ] **Step 2: Add `AddFrame` / `FindRow` / `RefreshFrameRow` to TagTabs**

In `TagTabs.cs`:

```csharp
// Add to `Id3v2TabViewModel` (TagTabs.cs) — this is the actual class name; `TagTabViewModel` is its abstract base.
public Id3v2FrameRow AddFrame(Id3v2Frame frame)
{
    ArgumentNullException.ThrowIfNull(frame);
    Tag.SetFrame(frame);
    var row = new Id3v2FrameRow(frame, () => IsDirty = true);
    AdvancedFrames.Add(row);
    IsDirty = true;
    return row;
}

public Id3v2FrameRow? FindRow(Id3v2Frame frame)
    => AdvancedFrames.FirstOrDefault(r => ReferenceEquals(r.Frame, frame));

public void RefreshFrameRow(Id3v2Frame frame)
{
    var row = FindRow(frame);
    if (row is not null) RefreshRow(row);
}
```

Mark old per-frame methods `[Obsolete("Use AddFrame(Id3v2Frame). Removed in next release.")]` and rewire them:

```csharp
[Obsolete("Use AddFrame(Id3v2Frame). Removed next release.")]
public Id3v2FrameRow AddTextFrame(string identifier, string value = "")
{
    var f = new Id3v2TextFrame(Tag.Version, identifier);
    if (!string.IsNullOrEmpty(value)) f.Values.Add(value);
    return AddFrame(f);
}

[Obsolete("...")]
public Id3v2FrameRow AddUrlFrame(string identifier, string url = "") { /* ... */ }
[Obsolete("...")]
public Id3v2FrameRow AddPictureFrame() { /* ... */ }
[Obsolete("...")]
public Id3v2FrameRow AddLyricsFrame() { /* ... */ }
[Obsolete("...")]
public Id3v2FrameRow AddPrivateFrame() { /* ... */ }
[Obsolete("...")]
public Id3v2FrameRow AddUniqueFileIdentifierFrame() { /* ... */ }
```

- [ ] **Step 3: Replace `AddFrameButton_Click` / dispatch in `MainWindow.xaml.cs`**

```csharp
private void AddFrameButton_Click(object sender, RoutedEventArgs e)
{
    if (sender is not Button button || button.ContextMenu is not { } menu) return;
    if (CurrentId3v2Tab is not { Tag: Id3v2Tag id3v2 } tab) return;

    var model = Id3v2AddMenuBuilder.BuildModel(TagItemEditorRegistry.Shared, id3v2);
    Id3v2AddMenuBuilder.Populate(menu, model, entry => OnAddOrEditFrame(entry, tab, id3v2));

    menu.PlacementTarget = button;
    menu.Placement = PlacementMode.Bottom;
    menu.IsOpen = true;
}

private void OnAddOrEditFrame(Id3v2MenuEntry entry, Id3v2TabViewModel tab, Id3v2Tag tag)
{
    if (entry.IsEditExisting)
    {
        var existing = tag.Frames.FirstOrDefault(f => f.Identifier == entry.FrameIdentifier);
        if (existing is null) return;
        if (DispatchEdit(existing)) tab.RefreshFrameRow(existing);
        return;
    }

    var newFrame = ConstructFrameFor(entry.FrameIdentifier, tag);
    if (newFrame is null) return;
    if (DispatchEdit(newFrame)) tab.AddFrame(newFrame);
}

private bool DispatchEdit(Id3v2Frame frame)
    => TagItemEditorRegistry.Shared.TryResolve(frame.GetType(), out var editor)
       && editor.Edit(this, frame);

private Id3v2Frame? ConstructFrameFor(string identifier, Id3v2Tag tag)
{
    // Family text/URL frames are constructed inline (the registered family editor's
    // standard `CreateNew(tag)` throws by spec §3.5; the side-channel `CreateNew(tag, identifier)`
    // overload is a public convenience but the menu path inlines the construction here for
    // simplicity — both produce identical frames).
    if (Id3v2KnownTextFrameIds.All.Any(i => i.Identifier == identifier || i.V220Identifier == identifier))
        return new Id3v2TextFrame(tag.Version, identifier);

    if (Id3v2KnownUrlFrameIds.All.Any(i => i.Identifier == identifier || i.V220Identifier == identifier))
        return new Id3v2UrlLinkFrame(tag.Version, identifier);

    // Registry path: find the editor whose attribute identifier matches; delegate to its CreateNew.
    foreach (var entry in TagItemEditorRegistry.Shared.Entries)
    {
        if (entry.Attribute is not Id3v2FrameEditorAttribute a) continue;
        var ident = Id3v2AddMenuBuilder.IdentifierFor(a, tag.Version);
        if (ident == identifier) return (Id3v2Frame)entry.Adapter.CreateNew(tag);
    }
    return null;
}

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
        ManageFramesDialog.ShowFor(this, id3v2, tab);
}
```

- [ ] **Step 4: (no-op — `IdentifierFor` stays `internal`)**

`Id3v2AddMenuBuilder.IdentifierFor` was originally specced as `internal static`. MainWindow and `ManageFramesViewModel` are in the same assembly (`AudioVideoLib.Studio`), so `internal` is sufficient. If a future cross-assembly consumer needs it, change visibility then. No work needed in Phase 0.

- [ ] **Step 5: Delete the now-unreachable code**

Remove from `MainWindow.xaml.cs`:
- `AddFrameMenuItem_Click` and the entire `case Id3v2AttachedPictureFrame…` chain in the existing dispatch
- `CommonTextFrames` / `CommonUrlFrames` static fields
- The old `AdvancedGrid_MouseDoubleClick` switch body (replaced above)

Remove from `MainWindow.xaml`:
- The current Add Frame menu's manually-built sub-items (the `<MenuItem Header="Text frame">` etc. — they're built dynamically now)

- [ ] **Step 6: Add toolbar button to `MainWindow.xaml`**

Next to the existing Add Frame button, add:
```xaml
<Button Content="Manage frames…" Click="ManageFramesButton_Click" Margin="6,0,0,0" Padding="10,3" />
```

- [ ] **Step 7: Build + manual smoke**

```bash
dotnet build AudioVideoLib.Studio
```

Launch Studio, open a file with an ID3v2 tag, click Add Frame — confirm the cascading menu now shows Text frame > / URL frame > / category groupings with the 12 registered editors. Double-click APIC / USLT / PRIV / UFID / MCDI in the Advanced view — confirm editor opens.

- [ ] **Step 8: Commit** — `refactor(studio): registry-driven Add menu and frame dispatch`.

---

### Task 13: Activate the BuildModel registry tests

The registry-backed tests in `Id3v2AddMenuBuilderTests` (those that needed real editors registered) can now run. Add them — they target the live `TagItemEditorRegistry.Shared`:

- [ ] **Step 1: Append tests to `Id3v2AddMenuBuilderTests.cs`**

```csharp
[Fact]
public void BuildModel_LiveRegistry_V240_HasAttachmentsCategory()
{
    var tag = new Id3v2Tag(Id3v2Version.Id3v240);
    var model = Id3v2AddMenuBuilder.BuildModel(TagItemEditorRegistry.Shared, tag);
    Assert.Contains(model.Categories, c => c.Category == Id3v2FrameCategory.Attachments
                                           && c.Entries.Any(e => e.FrameIdentifier == "APIC"));
}

[Fact]
public void BuildModel_LiveRegistry_V230_DoesNotIncludeContainers()
{
    var tag = new Id3v2Tag(Id3v2Version.Id3v230);
    var model = Id3v2AddMenuBuilder.BuildModel(TagItemEditorRegistry.Shared, tag);
    Assert.DoesNotContain(model.Categories, c => c.Category == Id3v2FrameCategory.Containers);
}

[Fact]
public void BuildModel_LiveRegistry_V220_IncludesCdmInContainers()
{
    var tag = new Id3v2Tag(Id3v2Version.Id3v220);
    var model = Id3v2AddMenuBuilder.BuildModel(TagItemEditorRegistry.Shared, tag);
    var containers = model.Categories.FirstOrDefault(c => c.Category == Id3v2FrameCategory.Containers);
    Assert.NotNull(containers);
    Assert.Contains(containers!.Entries, e => e.FrameIdentifier == "CDM");
}
```

Note: `TagItemEditorRegistry.Shared` is populated in `App.xaml.cs.OnStartup`, but xUnit doesn't run `OnStartup`. Tests that need a populated `Shared` use the `StudioFixture` defined below, declared via `[Collection("Studio")]`.

Define the fixture once in `AudioVideoLib.Studio.Tests/Editors/StudioFixture.cs`:

```csharp
namespace AudioVideoLib.Studio.Tests.Editors;

using AudioVideoLib.Studio.Editors;
using Xunit;

public sealed class StudioFixture
{
    private static readonly object _initLock = new();
    private static bool _initialised;

    public StudioFixture()
    {
        // Populate Shared once. The lock prevents a TOCTOU race if two collections
        // ever instantiate this fixture concurrently (xUnit collections run in parallel).
        lock (_initLock)
        {
            if (_initialised) return;
            TagItemEditorRegistry.Shared.RegisterFromAssembly(typeof(MainWindow).Assembly);
            _initialised = true;
        }
    }
}

[CollectionDefinition("Studio")]
public class StudioCollection : ICollectionFixture<StudioFixture> { }
```

Tests that need `Shared` populated declare `[Collection("Studio")]` on the class.

- [ ] **Step 2: Run tests** — expected: 3/3 passing (after fixture setup).
- [ ] **Step 3: Commit** — `test(studio): live-registry menu builder tests`.

---

### Task 14: Registry-completeness meta-test (gated)

**Files:**
- Create: `AudioVideoLib.Studio.Tests/Editors/Id3v2/RegistryCompletenessTests.cs`

```csharp
namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using System.Linq;
using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;
using Xunit;
using Xunit.Abstractions;

[Collection("Studio")]
public class RegistryCompletenessTests
{
    private readonly ITestOutputHelper _out;
    public RegistryCompletenessTests(ITestOutputHelper output) => _out = output;

    /// Flipped to true in Phase 2.1 (after all wave editors are registered).
    public const bool RegistrationComplete = false;

    [Fact]
    public void EveryConcreteId3v2Frame_HasRegisteredEditor()
    {
        var libAsm = typeof(Id3v2Frame).Assembly;
        var allFrames = libAsm.GetTypes()
            .Where(t => !t.IsAbstract && typeof(Id3v2Frame).IsAssignableFrom(t))
            .ToArray();

        var missing = allFrames
            .Where(t => !TagItemEditorRegistry.Shared.TryResolve(t, out _))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToArray();

        if (missing.Length > 0)
        {
            _out.WriteLine($"Missing editors for ({missing.Length}):");
            foreach (var name in missing) _out.WriteLine($"  - {name}");
        }

        if (RegistrationComplete) Assert.Empty(missing);
    }

    [Fact]
    public void EveryEditor_HasMatchingTestClass()
    {
        var testAsm = typeof(RegistryCompletenessTests).Assembly;
        var testClassNames = testAsm.GetTypes()
            .Where(t => !t.IsAbstract)
            .Select(t => t.Name)
            .ToHashSet();

        var missingTests = TagItemEditorRegistry.Shared.Entries
            .Select(e => e.EditorType.Name + "Tests")
            .Where(n => !testClassNames.Contains(n))
            .OrderBy(n => n)
            .ToArray();

        if (missingTests.Length > 0)
        {
            _out.WriteLine($"Editors without matching XxxTests class ({missingTests.Length}):");
            foreach (var name in missingTests) _out.WriteLine($"  - {name}");
        }

        if (RegistrationComplete) Assert.Empty(missingTests);
    }
}
```

- [ ] **Run tests** — expected: 2/2 passing; output lists ~27 missing editors (informational).
- [ ] **Commit** — `test(studio): registry completeness meta-tests (gated until Phase 2)`.

---

### Task 15: Manage Frames dialog (OQ-5)

**Files:**
- Create: `AudioVideoLib.Studio/Editors/ManageFramesViewModel.cs`
- Create: `AudioVideoLib.Studio/Editors/ManageFramesDialog.xaml(.cs)`
- Create: `AudioVideoLib.Studio/Converters/BoolToInTagConverter.cs`
- Create: `AudioVideoLib.Studio.Tests/Editors/ManageFramesViewModelTests.cs`

- [ ] **Step 1: `BoolToInTagConverter.cs`**

```csharp
namespace AudioVideoLib.Studio.Converters;

using System;
using System.Globalization;
using System.Windows.Data;

public sealed class BoolToInTagConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is bool b && b) ? "in tag" : "—";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

Register in `App.xaml`. Existing file is currently:

```xaml
<Application x:Class="AudioVideoLib.Studio.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="DarkTheme.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

Edit it to declare the new namespace prefixes (`conv` for the converter, `sys` for `System.Enum`, `tags` for the AudioVideoLib namespace) and add the converter plus the encoding-values `ObjectDataProvider` as siblings of `MergedDictionaries`:

```xaml
<Application x:Class="AudioVideoLib.Studio.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:sys="clr-namespace:System;assembly=mscorlib"
             xmlns:conv="clr-namespace:AudioVideoLib.Studio.Converters"
             xmlns:tags="clr-namespace:AudioVideoLib.Tags;assembly=AudioVideoLib"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="DarkTheme.xaml" />
            </ResourceDictionary.MergedDictionaries>

            <conv:BoolToInTagConverter x:Key="BoolToInTagConverter" />

            <!-- Enum-values providers consumed by editor ComboBoxes (TextFrameEditor, CommentEditor, etc.) -->
            <ObjectDataProvider x:Key="Id3v2EncodingValues"
                                MethodName="GetValues" ObjectType="{x:Type sys:Enum}">
                <ObjectDataProvider.MethodParameters>
                    <x:Type TypeName="tags:Id3v2FrameEncodingType" />
                </ObjectDataProvider.MethodParameters>
            </ObjectDataProvider>

            <ObjectDataProvider x:Key="Id3v2KeyEventTypeValues"
                                MethodName="GetValues" ObjectType="{x:Type sys:Enum}">
                <ObjectDataProvider.MethodParameters>
                    <x:Type TypeName="tags:Id3v2KeyEventType" />
                </ObjectDataProvider.MethodParameters>
            </ObjectDataProvider>

            <ObjectDataProvider x:Key="Id3v2TimeStampFormatValues"
                                MethodName="GetValues" ObjectType="{x:Type sys:Enum}">
                <ObjectDataProvider.MethodParameters>
                    <x:Type TypeName="tags:Id3v2TimeStampFormat" />
                </ObjectDataProvider.MethodParameters>
            </ObjectDataProvider>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

Editor dialogs reference these resources from their ComboBox `ItemsSource`:
```xaml
<ComboBox ItemsSource="{Binding Source={StaticResource Id3v2EncodingValues}}"
          SelectedValue="{Binding Encoding}" />
```

- [ ] **Step 2: `ManageFramesViewModel.cs`** — produces a flat row list, exposes `ApplyFilter(query)` and `GetActionLabel(row)` as pure functions.

```csharp
namespace AudioVideoLib.Studio.Editors;

using System;
using System.Collections.Generic;
using System.Linq;
using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

public sealed class ManageFramesViewModel
{
    public sealed record Row(string Identifier, string Name, string Category,
                             bool ExistsInTag, bool IsUniqueInstance, Type FrameType);

    private readonly IReadOnlyList<Row> _all;

    public ManageFramesViewModel(TagItemEditorRegistry registry, Id3v2Tag tag)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(tag);
        _all = registry.Entries
            .Where(e => e.Attribute is Id3v2FrameEditorAttribute a && a.SupportedVersions.Contains(tag.Version))
            .Select(e =>
            {
                var attr = (Id3v2FrameEditorAttribute)e.Attribute;
                var ident = Id3v2AddMenuBuilder.IdentifierFor(attr, tag.Version) ?? "?";
                var exists = tag.Frames.Any(f => f.GetType() == attr.ItemType);
                return new Row(ident, attr.MenuLabel, attr.Category.ToDisplay(),
                               exists, attr.IsUniqueInstance, attr.ItemType);
            })
            .OrderBy(r => r.Identifier, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<Row> All => _all;

    public IReadOnlyList<Row> ApplyFilter(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return _all;
        var q = query.Trim();
        return _all.Where(r =>
            r.Identifier.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            r.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            r.Category.Contains(q, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    public string GetActionLabel(Row row)
        => row is { ExistsInTag: true, IsUniqueInstance: true } ? "Edit" : "Add";
}
```

- [ ] **Step 3: Tests**

```csharp
namespace AudioVideoLib.Studio.Tests.Editors;

using System.Linq;
using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;
using Xunit;

[Collection("Studio")]
public class ManageFramesViewModelTests
{
    [Fact]
    public void All_PopulatesFromRegistry_FilteredByVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var vm = new ManageFramesViewModel(TagItemEditorRegistry.Shared, tag);
        Assert.Contains(vm.All, e => e.Identifier == "APIC");
        Assert.DoesNotContain(vm.All, e => e.Identifier == "CDM"); // v2.2-only
    }

    [Theory]
    [InlineData("apic")] [InlineData("APIC")] [InlineData("attached")] [InlineData("Attachments")]
    public void ApplyFilter_MatchesIdNameCategory_CaseInsensitive(string query)
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var vm = new ManageFramesViewModel(TagItemEditorRegistry.Shared, tag);
        Assert.Contains(vm.ApplyFilter(query), e => e.Identifier == "APIC");
    }

    [Fact]
    public void GetActionLabel_UniqueAlreadyInTag_ReturnsEdit()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        tag.SetFrame(new Id3v2MusicCdIdentifierFrame(Id3v2Version.Id3v240) { TableOfContents = new byte[8] });
        var vm = new ManageFramesViewModel(TagItemEditorRegistry.Shared, tag);
        var mcdi = vm.All.Single(e => e.Identifier == "MCDI");
        Assert.Equal("Edit", vm.GetActionLabel(mcdi));
    }

    [Fact]
    public void GetActionLabel_NotInTag_ReturnsAdd()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var vm = new ManageFramesViewModel(TagItemEditorRegistry.Shared, tag);
        var apic = vm.All.Single(e => e.Identifier == "APIC");
        Assert.Equal("Add", vm.GetActionLabel(apic));
    }

    [Fact]
    public void ExistsInTag_ReflectsCurrentTagState()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        tag.SetFrame(new Id3v2MusicCdIdentifierFrame(Id3v2Version.Id3v240) { TableOfContents = new byte[8] });
        var vm = new ManageFramesViewModel(TagItemEditorRegistry.Shared, tag);
        Assert.True(vm.All.Single(r => r.Identifier == "MCDI").ExistsInTag);
        Assert.False(vm.All.Single(r => r.Identifier == "APIC").ExistsInTag);
    }
}
```

- [ ] **Step 4: `ManageFramesDialog.xaml(.cs)`** — DataGrid bound to filtered VM rows; SearchBox keyup re-applies filter; `ShowFor(Window owner, Id3v2Tag tag, Id3v2TabViewModel tab)` static helper.

XAML key parts (DataGrid columns):

```xaml
<DataGrid x:Name="EntriesGrid" AutoGenerateColumns="False"
          CanUserAddRows="False" CanUserDeleteRows="False"
          SelectionMode="Single" MouseDoubleClick="Grid_MouseDoubleClick">
    <DataGrid.Columns>
        <DataGridTextColumn Header="ID"       Binding="{Binding Identifier}" Width="80"  IsReadOnly="True" />
        <DataGridTextColumn Header="Name"     Binding="{Binding Name}"       Width="*"   IsReadOnly="True" />
        <DataGridTextColumn Header="Category" Binding="{Binding Category}"   Width="180" IsReadOnly="True" />
        <DataGridTextColumn Header="Status"
                            Binding="{Binding ExistsInTag, Converter={StaticResource BoolToInTagConverter}}"
                            Width="80" IsReadOnly="True" />
    </DataGrid.Columns>
</DataGrid>
```

Codebehind: holds a `ManageFramesViewModel` field; SearchBox.KeyUp event calls `EntriesGrid.ItemsSource = _vm.ApplyFilter(SearchBox.Text)`. Action button / double-click call `MainWindow.OnAddOrEditFrame` (or the equivalent code path) — the dialog has a callback delegate the owner sets.

- [ ] **Step 5: Run tests** — expected: 5/5 ViewModel tests passing.
- [ ] **Step 6: Commit** — `feat(studio): Manage Frames toolbar dialog`.

---

### Task 16: Final Phase 0 validation

- [ ] **Step 1: `dotnet test AudioVideoLib.Studio.Tests/`** — all green; `RegistryCompletenessTests` lists missing editors as informational output (not failure).
- [ ] **Step 2: `dotnet test AudioVideoLib.Tests/`** — all existing library tests pass (the obsolete-thinwrappers in TagTabs preserve behaviour).
- [ ] **Step 3: `dotnet build -c Release`** — zero warnings, zero errors.
- [ ] **Step 4: Manual smoke**:
  - Launch Studio, open a file with an ID3v2 tag.
  - Add Frame menu shows correct categories and entries for the file's tag version.
  - Double-click on each of APIC / USLT / PRIV / UFID / MCDI opens its editor.
  - Add a Comment, Event timing codes, and (on a v2.2 tag) a CDM — confirm the three reference editors work end-to-end.
  - Cancel an Add — confirm the tag is not mutated.
  - "Manage frames…" toolbar button opens the dialog with all registered editors and search filtering.

---

## Phase 0 done definition

- All 16 tasks committed; full test suite green.
- 12 editors registered: 4 family (TextFrame / TXXX / UrlFrame / WXXX) + 3 reference (Comment / ETCO / CDM) + 5 retrofit (APIC / USLT / PRIV / UFID / MCDI).
- `MainWindow.xaml.cs` no longer contains any per-frame `case` block.
- `TagTabs.cs` per-frame `Add*` methods are `[Obsolete]` thin wrappers over the new uniform `AddFrame(Id3v2Frame)`.
- Manage Frames dialog reachable from toolbar; produces correct filtered results.
- Add path is cancellation-safe: a cancelled Add does not commit a frame to the tag.
- `RegistryCompletenessTests` reports 27 missing editor names (expected — those are Waves 1–3).
