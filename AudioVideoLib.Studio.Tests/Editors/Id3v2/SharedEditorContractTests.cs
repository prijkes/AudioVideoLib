namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

/// <summary>
/// Contract tests that exercise behaviour shared by every registered ID3v2 frame
/// editor. Replaces ~29 byte-identical CreateNew_UsesTagVersion / LoadDataFromFile
/// boilerplate copies with theory-driven coverage that walks the registry, so
/// new editors are picked up automatically.
/// </summary>
[Collection("Studio")]
public class SharedEditorContractTests
{
    /// <summary>
    /// Editors whose <c>CreateNew(object tag)</c> contract requires an additional
    /// identifier argument. They throw on the no-arg overload by design and are
    /// covered by their own per-editor tests.
    /// </summary>
    private static readonly HashSet<Type> CreateNewSkip =
    [
        typeof(TextFrameEditor),
        typeof(UrlFrameEditor),
    ];

    /// <summary>
    /// Asserts that for every registered editor, <c>CreateNew(tag)</c> returns a
    /// non-null frame whose <c>Version</c> matches the tag's version. Replaces
    /// the copy-pasted <c>CreateNew_UsesTagVersion</c> / <c>CreateNew_UsesV240</c>
    /// per-editor tests.
    /// </summary>
    /// <remarks>
    /// Version selection picks the lowest <see cref="Id3v2Version"/> the editor's
    /// <see cref="Id3v2FrameEditorAttribute.SupportedVersions"/> mask permits.
    /// This naturally exercises v2.2-only editors at v2.20, v2.2.1-only editors
    /// (CDM, CRM) at v2.21, v2.4-only editors (ASPI, EQU2, RVA2, SEEK, SIGN, XRVA)
    /// at v2.40, and the rest at the earliest version they support.
    /// <para />
    /// <see cref="RgadEditor"/> always returns a default-constructed frame and
    /// ignores the tag; the assertion still holds because RGAD is v2.30-only and
    /// the default ctor produces v2.30.
    /// </remarks>
    [Theory]
    [MemberData(nameof(EditorsByVersion))]
    public void CreateNew_UsesTagVersion(Type editorType, Id3v2Version version)
    {
        var entry = TagItemEditorRegistry.Shared.Entries
            .Single(e => e.EditorType == editorType);

        var tag = new Id3v2Tag(version);
        var frame = entry.Adapter.CreateNew(tag);

        Assert.NotNull(frame);
        var f = Assert.IsAssignableFrom<Id3v2Frame>(frame);
        Assert.Equal(version, f.Version);
    }

    /// <summary>
    /// Asserts that every <see cref="BinaryDataEditorBase"/>-derived editor
    /// updates <see cref="BinaryDataEditorBase.DataInfo"/> when bytes are loaded
    /// from a file and reverts to its initial empty-state string after Clear.
    /// Captures the initial DataInfo dynamically so editors that override the
    /// empty-state wording (e.g. "No data." in EncrEditor / GridEditor) pass
    /// without special-casing.
    /// </summary>
    [Theory]
    [MemberData(nameof(BinaryDataEditors))]
    public void LoadDataFromFile_AndClear_UpdateState(Type editorType)
    {
        var editor = (BinaryDataEditorBase)Activator.CreateInstance(editorType)!;
        var emptyDataInfo = editor.DataInfo;

        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, [1, 2, 3]);
            editor.LoadDataFromFile(path);
            Assert.NotEqual(emptyDataInfo, editor.DataInfo);
            Assert.Equal("3 bytes", editor.DataInfo);

            editor.ClearData();
            Assert.Equal(emptyDataInfo, editor.DataInfo);
        }
        finally
        {
            File.Delete(path);
        }
    }

    public static IEnumerable<object[]> EditorsByVersion()
    {
        foreach (var entry in EnsureRegistryEntries())
        {
            if (CreateNewSkip.Contains(entry.EditorType))
            {
                continue;
            }
            if (entry.Attribute is not Id3v2FrameEditorAttribute id3Attr)
            {
                continue;
            }
            yield return [entry.EditorType, LowestSupportedVersion(id3Attr.SupportedVersions)];
        }
    }

    public static IEnumerable<object[]> BinaryDataEditors()
    {
        foreach (var entry in EnsureRegistryEntries())
        {
            if (typeof(BinaryDataEditorBase).IsAssignableFrom(entry.EditorType))
            {
                yield return [entry.EditorType];
            }
        }
    }

    /// <summary>
    /// xUnit evaluates MemberData before the <c>StudioFixture</c> collection
    /// fixture has populated <see cref="TagItemEditorRegistry.Shared"/>. Touching
    /// the fixture ensures one-time registration runs (it's idempotent and locks
    /// internally) so theory-data enumeration sees the editors.
    /// </summary>
    private static IReadOnlyList<TagItemEditorRegistry.RegistrationEntry> EnsureRegistryEntries()
    {
        _ = new StudioFixture();
        return TagItemEditorRegistry.Shared.Entries;
    }

    private static Id3v2Version LowestSupportedVersion(Id3v2VersionMask mask) =>
        (mask & Id3v2VersionMask.V220) != 0 ? Id3v2Version.Id3v220
        : (mask & Id3v2VersionMask.V221) != 0 ? Id3v2Version.Id3v221
        : (mask & Id3v2VersionMask.V230) != 0 ? Id3v2Version.Id3v230
        : (mask & Id3v2VersionMask.V240) != 0 ? Id3v2Version.Id3v240
        : throw new InvalidOperationException(
            $"Editor's SupportedVersions mask is empty: {mask}");
}
