namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using System;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

[Collection("Studio")]
public class Id3v2FrameLookupTests
{
    [Fact]
    public void TryFindEntryByIdentifier_KnownIdentifier_ReturnsTrue()
    {
        Assert.True(Id3v2FrameLookup.TryFindEntryByIdentifier(
            TagItemEditorRegistry.Shared, "APIC", Id3v2Version.Id3v240, out var entry));
        Assert.Equal(typeof(Id3v2AttachedPictureFrame), entry.Adapter.ItemType);
    }

    [Fact]
    public void TryFindEntryByIdentifier_VersionAwareIdentifier_HandlesV220Variant()
    {
        // UFID on v2.4 vs UFI on v2.2 — both resolve to the same editor entry.
        Assert.True(Id3v2FrameLookup.TryFindEntryByIdentifier(
            TagItemEditorRegistry.Shared, "UFID", Id3v2Version.Id3v240, out var v240));
        Assert.True(Id3v2FrameLookup.TryFindEntryByIdentifier(
            TagItemEditorRegistry.Shared, "UFI", Id3v2Version.Id3v220, out var v220));
        Assert.Equal(v240.Adapter.ItemType, v220.Adapter.ItemType);
    }

    [Fact]
    public void TryFindEntryByIdentifier_UnsupportedVersion_ReturnsFalse()
    {
        // RGAD is v2.3-only; querying it on v2.2 must skip the entry.
        Assert.False(Id3v2FrameLookup.TryFindEntryByIdentifier(
            TagItemEditorRegistry.Shared, "RGAD", Id3v2Version.Id3v220, out _));
    }

    [Fact]
    public void TryFindEntryByIdentifier_FamilyEditor_NotMatched()
    {
        // TIT2's editor (TextFrameEditor) declares ItemType=Id3v2TextFrame; it must be
        // skipped here so callers don't get an editor that can't construct the right
        // identifier from CreateNew(tag) alone. The family path is handled separately.
        Assert.False(Id3v2FrameLookup.TryFindEntryByIdentifier(
            TagItemEditorRegistry.Shared, "TIT2", Id3v2Version.Id3v240, out _));
    }

    [Fact]
    public void TryFindEntryByIdentifier_UnknownIdentifier_ReturnsFalse()
    {
        Assert.False(Id3v2FrameLookup.TryFindEntryByIdentifier(
            TagItemEditorRegistry.Shared, "XXXX", Id3v2Version.Id3v240, out _));
    }

    [Fact]
    public void TryFindEntryByIdentifier_EmptyIdentifier_ReturnsFalse()
    {
        // Empty string should fall through the loop and return false rather than throw —
        // the empty-vs-null distinction is intentional (null is a programmer error).
        Assert.False(Id3v2FrameLookup.TryFindEntryByIdentifier(
            TagItemEditorRegistry.Shared, string.Empty, Id3v2Version.Id3v240, out _));
    }

    [Fact]
    public void TryFindEntryByIdentifier_CaseInsensitive()
    {
        Assert.True(Id3v2FrameLookup.TryFindEntryByIdentifier(
            TagItemEditorRegistry.Shared, "apic", Id3v2Version.Id3v240, out _));
    }

    [Fact]
    public void TryFindEntryByIdentifier_NullRegistry_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Id3v2FrameLookup.TryFindEntryByIdentifier(null!, "APIC", Id3v2Version.Id3v240, out _));
    }

    [Fact]
    public void TryFindEntryByIdentifier_NullIdentifier_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Id3v2FrameLookup.TryFindEntryByIdentifier(
                TagItemEditorRegistry.Shared, null!, Id3v2Version.Id3v240, out _));
    }

    [Fact]
    public void TryFindEntryByIdentifier_FrameWithThrowingCtor_DoesNotThrow()
    {
        // Pins the IdentifierFor try/catch fallthrough at the public seam: RGAD's
        // version-aware ctor throws on v2.4 (the lib's IsVersionSupported quirk), but
        // the lookup catches and proceeds rather than propagating the exception.
        var ex = Record.Exception(() =>
            Id3v2FrameLookup.TryFindEntryByIdentifier(
                TagItemEditorRegistry.Shared, "ANYTHING", Id3v2Version.Id3v240, out _));
        Assert.Null(ex);
    }
}
