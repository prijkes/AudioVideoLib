namespace AudioVideoLib.Studio;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AudioVideoLib.Tags;

public enum ValidationSeverity
{
    Info,
    Warning,
    Error,
}

public sealed record ValidationIssue(ValidationSeverity Severity, string Message);

public static class TagValidator
{
    public static IReadOnlyList<ValidationIssue> Validate(IAudioTag tag) => tag switch
    {
        Id3v2Tag v2 => ValidateId3v2(v2),
        Id3v1Tag v1 => ValidateId3v1(v1),
        ApeTag ape => ValidateApe(ape),
        Lyrics3v2Tag l3 => ValidateLyrics3v2(l3),
        Lyrics3Tag l3v1 => ValidateLyrics3v1(l3v1),
        MusicMatchTag mm => ValidateMusicMatch(mm),
        _ => [new ValidationIssue(ValidationSeverity.Info, $"No validator for {tag.GetType().Name}.")],
    };

    public static IReadOnlyList<ValidationIssue> ValidateVorbisForStudio(VorbisTabViewModel vm) =>
        ValidateVorbis(vm.Comments);

    private static IReadOnlyList<ValidationIssue> ValidateId3v2(Id3v2Tag tag)
    {
        var issues = new List<ValidationIssue>();
        var framesList = tag.Frames.ToList();
        var expectedIdLen = tag.Version < Id3v2Version.Id3v230 ? 3 : 4;
        var seenFrames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var frame in framesList)
        {
            var id = frame.Identifier ?? string.Empty;
            if (id.Length != expectedIdLen)
            {
                issues.Add(new(ValidationSeverity.Error, $"Frame '{id}' has {id.Length} chars, expected {expectedIdLen} for {tag.Version}."));
                continue;
            }

            for (var i = 0; i < id.Length; i++)
            {
                var c = id[i];
                var ok = c is (>= 'A' and <= 'Z') or (>= '0' and <= '9');
                if (!ok)
                {
                    issues.Add(new(ValidationSeverity.Error, $"Frame id '{id}' contains invalid character at position {i} (must be A-Z or 0-9)."));
                    break;
                }
            }

            switch (frame)
            {
                case Id3v2TextFrame text when text.Values == null || text.Values.Count == 0:
                    issues.Add(new(ValidationSeverity.Warning, $"Text frame '{id}' is empty."));
                    break;

                case Id3v2AttachedPictureFrame pic:
                    if (pic.PictureData == null || pic.PictureData.Length == 0)
                    {
                        issues.Add(new(ValidationSeverity.Warning, $"APIC frame has no picture data."));
                    }

                    if (string.IsNullOrEmpty(pic.ImageFormat))
                    {
                        issues.Add(new(ValidationSeverity.Warning, $"APIC frame has no MIME type."));
                    }

                    break;

                case Id3v2UrlLinkFrame url when string.IsNullOrEmpty(url.Url):
                    issues.Add(new(ValidationSeverity.Warning, $"URL frame '{id}' has an empty URL."));
                    break;

                case Id3v2CommentFrame comm when string.IsNullOrEmpty(comm.Text):
                    issues.Add(new(ValidationSeverity.Info, $"Comment frame has an empty body."));
                    break;
            }

            // Duplicate detection: some frames may legitimately repeat (APIC with different types, TXXX, COMM, etc.)
            var isRepeatable = id is "APIC" or "PIC" or "TXXX" or "TXX" or "COMM" or "COM" or "USLT" or "ULT" or "WXXX" or "WXX" or "UFID" or "UFI" or "PRIV" or "GEOB" or "GEO";
            if (!isRepeatable && !seenFrames.Add(id))
            {
                issues.Add(new(ValidationSeverity.Warning, $"Frame '{id}' appears more than once."));
            }
        }

        if (framesList.Count == 0)
        {
            issues.Add(new(ValidationSeverity.Warning, "Tag contains no frames."));
        }

        return issues;
    }

    private static IReadOnlyList<ValidationIssue> ValidateId3v1(Id3v1Tag tag)
    {
        var issues = new List<ValidationIssue>();

        CheckLatin(tag.TrackTitle, "Title", 30, issues);
        CheckLatin(tag.Artist, "Artist", 30, issues);
        CheckLatin(tag.AlbumTitle, "Album", 30, issues);
        CheckLatin(tag.TrackComment, "Comment", tag.TrackCommentLength, issues);

        if (!string.IsNullOrEmpty(tag.AlbumYear))
        {
            if (tag.AlbumYear.Length != 4)
            {
                issues.Add(new(ValidationSeverity.Warning, $"Year '{tag.AlbumYear}' should be 4 digits."));
            }

            if (!int.TryParse(tag.AlbumYear, out _))
            {
                issues.Add(new(ValidationSeverity.Warning, $"Year '{tag.AlbumYear}' is not numeric."));
            }
        }

        return issues;
    }

    private static void CheckLatin(string? value, string field, int max, List<ValidationIssue> issues)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (value.Length > max)
        {
            issues.Add(new(ValidationSeverity.Error, $"{field} '{Truncate(value, 16)}' is {value.Length} characters, max is {max}."));
        }

        foreach (var c in value)
        {
            if (c > 0xFF)
            {
                issues.Add(new(ValidationSeverity.Warning, $"{field} contains non-Latin-1 character U+{(int)c:X4} (ID3v1 only supports Latin-1)."));
                break;
            }
        }
    }

    private static IReadOnlyList<ValidationIssue> ValidateApe(ApeTag tag)
    {
        var issues = new List<ValidationIssue>();
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in tag.Items)
        {
            var key = item.Key ?? string.Empty;
            if (key.Length is < 2 or > 255)
            {
                issues.Add(new(ValidationSeverity.Error, $"APE key '{Truncate(key, 32)}' has invalid length {key.Length} (must be 2..255)."));
            }

            foreach (var c in key)
            {
                if (c is < (char)0x20 or > (char)0x7E)
                {
                    issues.Add(new(ValidationSeverity.Error, $"APE key '{Truncate(key, 32)}' contains non-printable ASCII character."));
                    break;
                }
            }

            if (!keys.Add(key))
            {
                issues.Add(new(ValidationSeverity.Warning, $"APE key '{key}' appears more than once (case-insensitive)."));
            }
        }

        return issues;
    }

    private static IReadOnlyList<ValidationIssue> ValidateLyrics3v2(Lyrics3v2Tag tag)
    {
        var issues = new List<ValidationIssue>();
        foreach (var field in tag.Fields)
        {
            var id = field.Identifier ?? string.Empty;
            if (id.Length != 3)
            {
                issues.Add(new(ValidationSeverity.Error, $"Field id '{id}' is {id.Length} chars, expected 3."));
            }

            foreach (var c in id)
            {
                if (c is < 'A' or > 'Z')
                {
                    issues.Add(new(ValidationSeverity.Warning, $"Field id '{id}' should be uppercase A-Z."));
                    break;
                }
            }
        }

        return issues;
    }

    private static IReadOnlyList<ValidationIssue> ValidateLyrics3v1(Lyrics3Tag tag)
    {
        var issues = new List<ValidationIssue>();
        if (tag.Lyrics is { Length: > 5100 })
        {
            issues.Add(new(ValidationSeverity.Warning, $"Lyrics are {tag.Lyrics.Length} characters (spec limit is ~5100)."));
        }

        return issues;
    }

    private static IReadOnlyList<ValidationIssue> ValidateMusicMatch(MusicMatchTag tag)
    {
        var issues = new List<ValidationIssue>();
        // MusicMatch validation is deliberately minimal — the format is loosely specified.
        _ = tag;
        return issues;
    }

    private static IReadOnlyList<ValidationIssue> ValidateVorbis(VorbisComments comments)
    {
        var issues = new List<ValidationIssue>();
        foreach (var c in comments.Comments)
        {
            var name = c.Name ?? string.Empty;
            foreach (var ch in name)
            {
                if (ch is < (char)0x20 or > (char)0x7E or '=')
                {
                    issues.Add(new(ValidationSeverity.Error, $"Comment name '{Truncate(name, 32)}' contains invalid character (must be printable ASCII, excluding '=')."));
                    break;
                }
            }

            if (name.Length == 0)
            {
                issues.Add(new(ValidationSeverity.Error, "Comment with empty name."));
            }

            if (c.Value != null)
            {
                try
                {
                    var _ = Encoding.UTF8.GetByteCount(c.Value);
                }
                catch (EncoderFallbackException)
                {
                    issues.Add(new(ValidationSeverity.Warning, $"Comment '{name}' value is not UTF-8 encodable."));
                }
            }
        }

        return issues;
    }

    public static string Format(IReadOnlyList<ValidationIssue> issues)
    {
        if (issues.Count == 0)
        {
            return "No issues found.";
        }

        var sb = new StringBuilder();
        foreach (var severity in new[] { ValidationSeverity.Error, ValidationSeverity.Warning, ValidationSeverity.Info })
        {
            var group = issues.Where(i => i.Severity == severity).ToList();
            if (group.Count == 0)
            {
                continue;
            }

            sb.AppendLine($"{severity.ToString().ToUpperInvariant()} ({group.Count})");
            foreach (var issue in group)
            {
                sb.Append("  • ").AppendLine(issue.Message);
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
