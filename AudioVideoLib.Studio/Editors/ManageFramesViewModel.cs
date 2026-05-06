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

    public ManageFramesViewModel(TagItemEditorRegistry registry, Id3v2Tag tag)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(tag);

        var versionMask = tag.Version.ToMask();

        // Registry-backed editors (one entry per editor — text/URL family editors contribute
        // a single base-class entry which is replaced by per-identifier entries below).
        var registryRows = registry.Entries
            .Where(e => e.Attribute is Id3v2FrameEditorAttribute a
                        && a.SupportedVersions.Contains(tag.Version)
                        && a.ItemType != typeof(Id3v2TextFrame)
                        && a.ItemType != typeof(Id3v2UrlLinkFrame))
            .Select(e =>
            {
                var attr = (Id3v2FrameEditorAttribute)e.Attribute;
                var ident = Id3v2AddMenuBuilder.IdentifierFor(attr, tag.Version) ?? "?";
                var exists = tag.Frames.Any(f => f.GetType() == attr.ItemType);
                return new Row(ident, attr.MenuLabel, attr.Category.ToDisplay(),
                               exists, attr.IsUniqueInstance, attr.ItemType);
            });

        // Family text frames — one row per known identifier (TIT2, TPE1, TALB, …).
        var textFrameCategory = Id3v2FrameCategory.TextFrames.ToDisplay();
        var textRows = Id3v2KnownTextFrameIds.All
            .Where(t => (t.SupportedVersions & versionMask) != 0)
            .Select(t =>
            {
                var ident = Id3v2KnownTextFrameIds.IdentifierFor(t, versionMask);
                var exists = tag.Frames.Any(f =>
                    f is Id3v2TextFrame && string.Equals(f.Identifier, ident, StringComparison.Ordinal));
                return new Row(ident, t.FriendlyName, textFrameCategory,
                               exists, IsUniqueInstance: false, typeof(Id3v2TextFrame));
            });

        var urlFrameCategory = Id3v2FrameCategory.UrlFrames.ToDisplay();
        var urlRows = Id3v2KnownUrlFrameIds.All
            .Where(u => (u.SupportedVersions & versionMask) != 0)
            .Select(u =>
            {
                var ident = Id3v2KnownUrlFrameIds.IdentifierFor(u, versionMask);
                var exists = tag.Frames.Any(f =>
                    f is Id3v2UrlLinkFrame && string.Equals(f.Identifier, ident, StringComparison.Ordinal));
                return new Row(ident, u.FriendlyName, urlFrameCategory,
                               exists, IsUniqueInstance: false, typeof(Id3v2UrlLinkFrame));
            });

        All = [.. registryRows
            .Concat(textRows)
            .Concat(urlRows)
            .OrderBy(r => r.Identifier, StringComparer.Ordinal)];
    }

    public IReadOnlyList<Row> All { get; }

    public IReadOnlyList<Row> ApplyFilter(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return All;
        }
        var q = query.Trim();
        return [.. All.Where(r =>
            r.Identifier.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            r.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            r.Category.Contains(q, StringComparison.OrdinalIgnoreCase))];
    }

    public string GetActionLabel(Row row)
        => row is { ExistsInTag: true, IsUniqueInstance: true } ? "Edit" : "Add";
}
