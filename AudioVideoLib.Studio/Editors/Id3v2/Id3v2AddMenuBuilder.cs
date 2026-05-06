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
        var existing = attribute.IsUniqueInstance
            && tag.Frames.Any(f => f.GetType() == attribute.ItemType);
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
            var entries = category switch
            {
                Id3v2FrameCategory.TextFrames => BuildTextFamilyEntries(versionMask),
                Id3v2FrameCategory.UrlFrames => BuildUrlFamilyEntries(versionMask),
                _ => BuildRegistryEntries(registry, category, version, tag),
            };

            if (entries.Count > 0)
            {
                categories.Add(new Id3v2MenuCategory(category, category.ToDisplay(), entries));
            }
        }

        return new Id3v2MenuModel(categories);
    }

    public static void Populate(
        ContextMenu menu,
        Id3v2MenuModel model,
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

    internal static string? IdentifierFor(Id3v2FrameEditorAttribute attr, Id3v2Version version)
    {
        if (!string.IsNullOrEmpty(attr.KnownIdentifier))
        {
            return attr.KnownIdentifier;
        }
        try
        {
            var ctor = attr.ItemType.GetConstructor([typeof(Id3v2Version)]);
            return ctor is null
                ? null
                : ((Id3v2Frame)ctor.Invoke([version])).Identifier;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<Id3v2FrameCategory> CategoriesInDisplayOrder() =>
    [
        Id3v2FrameCategory.TextFrames,
        Id3v2FrameCategory.UrlFrames,
        Id3v2FrameCategory.Identification,
        Id3v2FrameCategory.CommentsAndLyrics,
        Id3v2FrameCategory.TimingAndSync,
        Id3v2FrameCategory.People,
        Id3v2FrameCategory.AudioAdjustment,
        Id3v2FrameCategory.CountersAndRatings,
        Id3v2FrameCategory.Attachments,
        Id3v2FrameCategory.CommerceAndRights,
        Id3v2FrameCategory.EncryptionAndCompression,
        Id3v2FrameCategory.Containers,
        Id3v2FrameCategory.System,
        Id3v2FrameCategory.Experimental,
    ];

    private static IReadOnlyList<Id3v2MenuEntry> BuildTextFamilyEntries(Id3v2VersionMask versionMask)
    {
        return [.. Id3v2KnownTextFrameIds.All
            .Where(i => (i.SupportedVersions & versionMask) != 0)
            .OrderBy(i => i.FriendlyName, StringComparer.OrdinalIgnoreCase)
            .Select(i =>
            {
                var ident = Id3v2KnownTextFrameIds.IdentifierFor(i, versionMask);
                return new Id3v2MenuEntry($"{ident} — {i.FriendlyName}", ident, IsEditExisting: false);
            })];
    }

    private static IReadOnlyList<Id3v2MenuEntry> BuildUrlFamilyEntries(Id3v2VersionMask versionMask)
    {
        return [.. Id3v2KnownUrlFrameIds.All
            .Where(i => (i.SupportedVersions & versionMask) != 0)
            .OrderBy(i => i.FriendlyName, StringComparer.OrdinalIgnoreCase)
            .Select(i =>
            {
                var ident = Id3v2KnownUrlFrameIds.IdentifierFor(i, versionMask);
                return new Id3v2MenuEntry($"{ident} — {i.FriendlyName}", ident, IsEditExisting: false);
            })];
    }

    private static IReadOnlyList<Id3v2MenuEntry> BuildRegistryEntries(
        TagItemEditorRegistry registry,
        Id3v2FrameCategory category,
        Id3v2Version version,
        Id3v2Tag tag)
    {
        return [.. registry.Entries
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
            })];
    }
}
