namespace AudioVideoLib.Studio.Diff;

using System;
using System.Collections.Generic;
using System.Linq;

using AudioVideoLib.Tags;

public enum DiffKind
{
    Same,
    Added,
    Removed,
    Changed,
}

public sealed record TagDiffRow(string Section, string Field, DiffKind Kind, string? LeftValue, string? RightValue);

public static class TagDiff
{
    public static IReadOnlyList<TagDiffRow> Compare(
        IReadOnlyList<IAudioTag> leftTags,
        VorbisComments? leftVorbis,
        IReadOnlyList<IAudioTag> rightTags,
        VorbisComments? rightVorbis)
    {
        var rows = new List<TagDiffRow>();

        CompareId3v2(leftTags.OfType<Id3v2Tag>().FirstOrDefault(), rightTags.OfType<Id3v2Tag>().FirstOrDefault(), rows);
        CompareId3v1(leftTags.OfType<Id3v1Tag>().FirstOrDefault(), rightTags.OfType<Id3v1Tag>().FirstOrDefault(), rows);
        CompareApe(leftTags.OfType<ApeTag>().FirstOrDefault(), rightTags.OfType<ApeTag>().FirstOrDefault(), rows);
        CompareVorbis(leftVorbis, rightVorbis, rows);
        CompareLyrics3v2(leftTags.OfType<Lyrics3v2Tag>().FirstOrDefault(), rightTags.OfType<Lyrics3v2Tag>().FirstOrDefault(), rows);
        CompareLyrics3v1(leftTags.OfType<Lyrics3Tag>().FirstOrDefault(), rightTags.OfType<Lyrics3Tag>().FirstOrDefault(), rows);
        CompareMusicMatch(leftTags.OfType<MusicMatchTag>().FirstOrDefault(), rightTags.OfType<MusicMatchTag>().FirstOrDefault(), rows);

        return rows;
    }

    private static void CompareId3v2(Id3v2Tag? left, Id3v2Tag? right, List<TagDiffRow> rows)
    {
        if (left == null && right == null)
        {
            return;
        }

        if (left == null)
        {
            rows.Add(new("ID3v2", "(tag)", DiffKind.Added, null, right!.Version.ToString()));
            return;
        }

        if (right == null)
        {
            rows.Add(new("ID3v2", "(tag)", DiffKind.Removed, left.Version.ToString(), null));
            return;
        }

        if (left.Version != right.Version)
        {
            rows.Add(new("ID3v2", "Version", DiffKind.Changed, left.Version.ToString(), right.Version.ToString()));
        }

        var leftMap = BuildId3v2Map(left);
        var rightMap = BuildId3v2Map(right);
        var allKeys = leftMap.Keys.Union(rightMap.Keys).OrderBy(k => k, StringComparer.Ordinal);

        foreach (var key in allKeys)
        {
            var inLeft = leftMap.TryGetValue(key, out var leftVal);
            var inRight = rightMap.TryGetValue(key, out var rightVal);
            if (inLeft && !inRight)
            {
                rows.Add(new("ID3v2", key, DiffKind.Removed, leftVal, null));
            }
            else if (!inLeft && inRight)
            {
                rows.Add(new("ID3v2", key, DiffKind.Added, null, rightVal));
            }
            else if (!string.Equals(leftVal, rightVal, StringComparison.Ordinal))
            {
                rows.Add(new("ID3v2", key, DiffKind.Changed, leftVal, rightVal));
            }
        }
    }

    private static Dictionary<string, string> BuildId3v2Map(Id3v2Tag tag)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var frame in tag.Frames)
        {
            var id = frame.Identifier ?? "?";
            var (baseKey, value) = frame switch
            {
                Id3v2TextFrame t => (id, string.Join(" / ", t.Values)),
                Id3v2UrlLinkFrame u => (id, u.Url ?? string.Empty),
                Id3v2CommentFrame c => ($"{id}[{c.Language}/{c.ShortContentDescription}]", c.Text ?? string.Empty),
                Id3v2AttachedPictureFrame p => ($"{id}[{p.PictureType}]", $"{p.ImageFormat} {p.PictureData?.Length ?? 0:N0} bytes"),
                Id3v2UnsynchronizedLyricsFrame l => ($"{id}[{l.Language}/{l.ContentDescriptor}]", l.Lyrics ?? string.Empty),
                Id3v2PrivateFrame pr => ($"{id}[{pr.OwnerIdentifier}]", $"{pr.PrivateData?.Length ?? 0:N0} bytes"),
                Id3v2UniqueFileIdentifierFrame uf => ($"{id}[{uf.OwnerIdentifier}]", $"{uf.Identifier?.Length ?? 0:N0} bytes"),
                _ => (id, $"<{frame.GetType().Name}>"),
            };

            var key = baseKey;
            var i = 1;
            while (map.ContainsKey(key))
            {
                key = $"{baseKey} #{i++}";
            }

            map[key] = value;
        }

        return map;
    }

    private static void CompareId3v1(Id3v1Tag? left, Id3v1Tag? right, List<TagDiffRow> rows)
    {
        if (left == null && right == null)
        {
            return;
        }

        if (left == null)
        {
            rows.Add(new("ID3v1", "(tag)", DiffKind.Added, null, "present"));
            return;
        }

        if (right == null)
        {
            rows.Add(new("ID3v1", "(tag)", DiffKind.Removed, "present", null));
            return;
        }

        CompareField(rows, "ID3v1", "Title", left.TrackTitle, right.TrackTitle);
        CompareField(rows, "ID3v1", "Artist", left.Artist, right.Artist);
        CompareField(rows, "ID3v1", "Album", left.AlbumTitle, right.AlbumTitle);
        CompareField(rows, "ID3v1", "Year", left.AlbumYear, right.AlbumYear);
        CompareField(rows, "ID3v1", "Comment", left.TrackComment, right.TrackComment);
        CompareField(rows, "ID3v1", "Track", left.TrackNumber.ToString(), right.TrackNumber.ToString());
        CompareField(rows, "ID3v1", "Genre", left.Genre.ToString(), right.Genre.ToString());
    }

    private static void CompareApe(ApeTag? left, ApeTag? right, List<TagDiffRow> rows)
    {
        if (left == null && right == null)
        {
            return;
        }

        if (left == null)
        {
            rows.Add(new("APE", "(tag)", DiffKind.Added, null, "present"));
            return;
        }

        if (right == null)
        {
            rows.Add(new("APE", "(tag)", DiffKind.Removed, "present", null));
            return;
        }

        var leftMap = BuildApeMap(left);
        var rightMap = BuildApeMap(right);
        var allKeys = leftMap.Keys.Union(rightMap.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(k => k, StringComparer.OrdinalIgnoreCase);

        foreach (var key in allKeys)
        {
            var inLeft = leftMap.TryGetValue(key, out var leftVal);
            var inRight = rightMap.TryGetValue(key, out var rightVal);
            if (inLeft && !inRight)
            {
                rows.Add(new("APE", key, DiffKind.Removed, leftVal, null));
            }
            else if (!inLeft && inRight)
            {
                rows.Add(new("APE", key, DiffKind.Added, null, rightVal));
            }
            else if (!string.Equals(leftVal, rightVal, StringComparison.Ordinal))
            {
                rows.Add(new("APE", key, DiffKind.Changed, leftVal, rightVal));
            }
        }
    }

    private static Dictionary<string, string> BuildApeMap(ApeTag tag)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in tag.Items)
        {
            var value = item switch
            {
                ApeLocatorItem l => string.Join(" / ", l.Values),
                ApeUtf8Item u => string.Join(" / ", u.Values),
                ApeBinaryItem b => $"<binary {b.Data?.Length ?? 0:N0} bytes>",
                _ => string.Empty,
            };

            var key = item.Key;
            var baseKey = key;
            var i = 1;
            while (map.ContainsKey(key))
            {
                key = $"{baseKey} #{i++}";
            }

            map[key] = value;
        }

        return map;
    }

    private static void CompareVorbis(VorbisComments? left, VorbisComments? right, List<TagDiffRow> rows)
    {
        if (left == null && right == null)
        {
            return;
        }

        if (left == null)
        {
            rows.Add(new("Vorbis", "(comments)", DiffKind.Added, null, "present"));
            return;
        }

        if (right == null)
        {
            rows.Add(new("Vorbis", "(comments)", DiffKind.Removed, "present", null));
            return;
        }

        CompareField(rows, "Vorbis", "Vendor", left.Vendor, right.Vendor);

        var leftMap = BuildVorbisMap(left);
        var rightMap = BuildVorbisMap(right);
        var allKeys = leftMap.Keys.Union(rightMap.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(k => k, StringComparer.OrdinalIgnoreCase);

        foreach (var key in allKeys)
        {
            var inLeft = leftMap.TryGetValue(key, out var leftVal);
            var inRight = rightMap.TryGetValue(key, out var rightVal);
            if (inLeft && !inRight)
            {
                rows.Add(new("Vorbis", key, DiffKind.Removed, leftVal, null));
            }
            else if (!inLeft && inRight)
            {
                rows.Add(new("Vorbis", key, DiffKind.Added, null, rightVal));
            }
            else if (!string.Equals(leftVal, rightVal, StringComparison.Ordinal))
            {
                rows.Add(new("Vorbis", key, DiffKind.Changed, leftVal, rightVal));
            }
        }
    }

    private static Dictionary<string, string> BuildVorbisMap(VorbisComments comments)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in comments.Comments)
        {
            var key = c.Name ?? string.Empty;
            var value = c.Value ?? string.Empty;

            var baseKey = key;
            var i = 1;
            while (map.ContainsKey(key))
            {
                key = $"{baseKey} #{i++}";
            }

            map[key] = value;
        }

        return map;
    }

    private static void CompareLyrics3v2(Lyrics3v2Tag? left, Lyrics3v2Tag? right, List<TagDiffRow> rows)
    {
        if (left == null && right == null)
        {
            return;
        }

        if (left == null)
        {
            rows.Add(new("Lyrics3v2", "(tag)", DiffKind.Added, null, "present"));
            return;
        }

        if (right == null)
        {
            rows.Add(new("Lyrics3v2", "(tag)", DiffKind.Removed, "present", null));
            return;
        }

        var leftMap = left.Fields.ToDictionary(f => f.Identifier ?? "?", DescribeLyrics3Field, StringComparer.Ordinal);
        var rightMap = right.Fields.ToDictionary(f => f.Identifier ?? "?", DescribeLyrics3Field, StringComparer.Ordinal);
        var allKeys = leftMap.Keys.Union(rightMap.Keys).OrderBy(k => k, StringComparer.Ordinal);

        foreach (var key in allKeys)
        {
            var inLeft = leftMap.TryGetValue(key, out var leftVal);
            var inRight = rightMap.TryGetValue(key, out var rightVal);
            if (inLeft && !inRight)
            {
                rows.Add(new("Lyrics3v2", key, DiffKind.Removed, leftVal, null));
            }
            else if (!inLeft && inRight)
            {
                rows.Add(new("Lyrics3v2", key, DiffKind.Added, null, rightVal));
            }
            else if (!string.Equals(leftVal, rightVal, StringComparison.Ordinal))
            {
                rows.Add(new("Lyrics3v2", key, DiffKind.Changed, leftVal, rightVal));
            }
        }
    }

    private static string DescribeLyrics3Field(Lyrics3v2Field f)
    {
        return f is Lyrics3v2TextField t
            ? t.Value ?? string.Empty
            : f.Data is { Length: > 0 } d ? $"<{d.Length:N0} bytes>" : string.Empty;
    }

    private static void CompareLyrics3v1(Lyrics3Tag? left, Lyrics3Tag? right, List<TagDiffRow> rows)
    {
        if (left == null && right == null)
        {
            return;
        }

        CompareField(rows, "Lyrics3", "Lyrics", left?.Lyrics, right?.Lyrics);
    }

    private static void CompareMusicMatch(MusicMatchTag? left, MusicMatchTag? right, List<TagDiffRow> rows)
    {
        if (left == null && right == null)
        {
            return;
        }

        if (left == null)
        {
            rows.Add(new("MusicMatch", "(tag)", DiffKind.Added, null, right!.Version));
            return;
        }

        if (right == null)
        {
            rows.Add(new("MusicMatch", "(tag)", DiffKind.Removed, left.Version, null));
            return;
        }

        CompareField(rows, "MusicMatch", "Version", left.Version, right.Version);
        CompareField(rows, "MusicMatch", "Title", left.SongTitle, right.SongTitle);
        CompareField(rows, "MusicMatch", "Artist", left.ArtistName, right.ArtistName);
        CompareField(rows, "MusicMatch", "Album", left.AlbumTitle, right.AlbumTitle);
        CompareField(rows, "MusicMatch", "Genre", left.Genre, right.Genre);
    }

    private static void CompareField(List<TagDiffRow> rows, string section, string field, string? left, string? right)
    {
        var l = left ?? string.Empty;
        var r = right ?? string.Empty;
        if (l == r)
        {
            return;
        }

        var kind = l.Length == 0 ? DiffKind.Added
                 : r.Length == 0 ? DiffKind.Removed
                 : DiffKind.Changed;
        rows.Add(new(section, field, kind, left, right));
    }
}
