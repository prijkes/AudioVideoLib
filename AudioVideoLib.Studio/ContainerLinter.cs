namespace AudioVideoLib.Studio;

using System.Collections.Generic;
using System.Text.RegularExpressions;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

/// <summary>
/// Sanity checks for the container-embedded tag formats added in the v0.1
/// expansion. Each entry point inspects a single container's metadata and
/// returns a flat list of <see cref="ValidationIssue"/>s.
/// </summary>
internal static class ContainerLinter
{
    private static readonly HashSet<int> StandardDsdSampleRates =
    [
        2_822_400, 5_644_800, 11_289_600, 22_579_200,
    ];

    private static readonly Regex BwfDateRegex = new(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.Compiled);
    private static readonly Regex BwfTimeRegex = new(@"^\d{2}:\d{2}:\d{2}$", RegexOptions.Compiled);

    public static IReadOnlyList<ValidationIssue> CheckRiff(RiffStream riff)
    {
        var issues = new List<ValidationIssue>();

        if (riff.BextChunk is { } bext)
        {
            if (!string.IsNullOrEmpty(bext.OriginationDate) && !BwfDateRegex.IsMatch(bext.OriginationDate))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    $"BWF bext OriginationDate '{bext.OriginationDate}' doesn't match the spec format YYYY-MM-DD."));
            }

            if (!string.IsNullOrEmpty(bext.OriginationTime) && !BwfTimeRegex.IsMatch(bext.OriginationTime))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    $"BWF bext OriginationTime '{bext.OriginationTime}' doesn't match the spec format HH:MM:SS."));
            }

            if (bext.Version >= 1 && bext.Umid is { Length: > 0 } umid && IsAllZero(umid))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Info,
                    $"BWF bext is v{bext.Version} but UMID is all zero — most v1+ writers populate it."));
            }
        }

        return issues;
    }

    public static IReadOnlyList<ValidationIssue> CheckMp4(Mp4Stream mp4)
    {
        var issues = new List<ValidationIssue>();
        var tag = mp4.Tag;

        // Multiple cover-art items of the same image format — usually a leftover from a botched
        // re-tag that didn't clear the previous payload. Different formats (e.g. JPEG + PNG) are
        // a deliberate writer choice and not flagged.
        var coverArtFormatCounts = new Dictionary<Mp4CoverArtFormat, int>();
        foreach (var art in tag.CoverArt)
        {
            coverArtFormatCounts.TryGetValue(art.Format, out var c);
            coverArtFormatCounts[art.Format] = c + 1;
        }

        foreach (var kv in coverArtFormatCounts)
        {
            if (kv.Value > 1)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    $"MP4 has {kv.Value} cover-art items of format {kv.Key} — likely a duplicate."));
            }
        }

        if (tag.TrackNumber is int tn && tag.TrackTotal is int tt && tt > 0 && tn > tt)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Warning,
                $"MP4 track number {tn} exceeds total {tt}."));
        }

        if (tag.DiscNumber is int dn && tag.DiscTotal is int dt && dt > 0 && dn > dt)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Warning,
                $"MP4 disc number {dn} exceeds total {dt}."));
        }

        var seenStandardAtoms = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var item in tag.Items)
        {
            if (item.Mean is not null)
            {
                continue;
            }

            if (!seenStandardAtoms.Add(item.AtomType))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    $"MP4 atom '{item.AtomType}' appears more than once in ilst."));
            }
        }

        return issues;
    }

    public static IReadOnlyList<ValidationIssue> CheckAsf(AsfStream asf)
    {
        var issues = new List<ValidationIssue>();
        var meta = asf.MetadataTag;

        var keyCounts = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var kv in meta.ExtendedItems)
        {
            keyCounts.TryGetValue(kv.Key, out var c);
            keyCounts[kv.Key] = c + 1;
        }

        foreach (var kv in keyCounts)
        {
            if (kv.Value > 1)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    $"ASF Extended Content Description has {kv.Value} entries for key '{kv.Key}'."));
            }
        }

        return issues;
    }

    public static IReadOnlyList<ValidationIssue> CheckMatroska(MatroskaStream mkv)
    {
        var issues = new List<ValidationIssue>();

        var seen = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var entry in mkv.Tag.Entries)
        {
            foreach (var st in entry.SimpleTags)
            {
                if (string.IsNullOrEmpty(st.Name))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        "Matroska SimpleTag has empty TagName."));
                    continue;
                }

                var key = $"{entry.Targets.TargetTypeValue}/{st.Name}";
                if (!seen.Add(key))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        $"Matroska SimpleTag '{st.Name}' (target {entry.Targets.TargetTypeValue}) appears more than once."));
                }
            }
        }

        return issues;
    }

    public static IReadOnlyList<ValidationIssue> CheckDsd(IMediaContainer audio)
    {
        var (rate, channels) = audio switch
        {
            DsfStream dsf => (dsf.SampleRate, dsf.Channels),
            DffStream dff => (dff.SampleRate, dff.Channels),
            _ => (0, 0),
        };

        if (rate == 0 && channels == 0)
        {
            return [];
        }

        var issues = new List<ValidationIssue>();
        if (rate > 0 && !StandardDsdSampleRates.Contains(rate))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Info,
                $"DSD sample rate {rate:N0} Hz is not one of the standard DSD64/128/256/512 rates."));
        }

        if (channels == 0)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Warning,
                "DSD container declares zero channels."));
        }

        return issues;
    }

    private static bool IsAllZero(byte[] bytes)
    {
        foreach (var b in bytes)
        {
            if (b != 0)
            {
                return false;
            }
        }

        return true;
    }
}
