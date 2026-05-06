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
        All = [.. registry.Entries
            .Where(e => e.Attribute is Id3v2FrameEditorAttribute a && a.SupportedVersions.Contains(tag.Version))
            .Select(e =>
            {
                var attr = (Id3v2FrameEditorAttribute)e.Attribute;
                var ident = Id3v2AddMenuBuilder.IdentifierFor(attr, tag.Version) ?? "?";
                var exists = tag.Frames.Any(f => f.GetType() == attr.ItemType);
                return new Row(ident, attr.MenuLabel, attr.Category.ToDisplay(),
                               exists, attr.IsUniqueInstance, attr.ItemType);
            })
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
