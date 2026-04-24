namespace AudioVideoLib.Studio;

using System.Collections.Generic;
using System.Linq;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

public static class StructureIntegrityChecker
{
    public static IReadOnlyList<ValidationIssue> Check(
        byte[] fileBytes,
        IReadOnlyList<IAudioTagOffset> offsets,
        IMediaContainer? audio)
    {
        var issues = new List<ValidationIssue>();
        var sorted = offsets.OrderBy(o => o.StartOffset).ToList();

        // Overlap: any pair where [s1,e1) intersects [s2,e2).
        for (var i = 0; i < sorted.Count; i++)
        {
            for (var j = i + 1; j < sorted.Count; j++)
            {
                var a = sorted[i];
                var b = sorted[j];
                if (a.EndOffset > b.StartOffset && b.EndOffset > a.StartOffset)
                {
                    issues.Add(new(ValidationSeverity.Error,
                        $"{a.AudioTag.GetType().Name} at 0x{a.StartOffset:X8}..0x{a.EndOffset:X8} overlaps " +
                        $"{b.AudioTag.GetType().Name} at 0x{b.StartOffset:X8}..0x{b.EndOffset:X8}."));
                }
            }
        }

        // Trailing garbage: bytes after the last end-origin tag (or EOF expectation).
        var endOrigin = sorted.Where(o => o.TagOrigin == TagOrigin.End).ToList();
        if (endOrigin.Count > 0)
        {
            var lastEnd = endOrigin.Max(o => o.EndOffset);
            if (lastEnd < fileBytes.Length)
            {
                var trail = fileBytes.Length - lastEnd;
                issues.Add(new(ValidationSeverity.Warning,
                    $"{trail:N0} bytes of trailing data after the last end-origin tag (ends at 0x{lastEnd:X8}, file is {fileBytes.Length:N0} bytes)."));
            }
        }

        // Leading start-origin tag should begin at offset 0.
        var startOrigin = sorted.Where(o => o.TagOrigin == TagOrigin.Start).ToList();
        if (startOrigin.Count > 0)
        {
            var first = startOrigin.MinBy(o => o.StartOffset)!;
            if (first.StartOffset != 0)
            {
                issues.Add(new(ValidationSeverity.Warning,
                    $"{first.AudioTag.GetType().Name} at 0x{first.StartOffset:X8} is flagged start-origin but file has {first.StartOffset:N0} leading bytes before it."));
            }
        }

        // Declared vs actual tag size.
        foreach (var offset in sorted)
        {
            if (offset.AudioTag is Id3v2Tag v2)
            {
                CheckId3v2Size(fileBytes, offset, v2, issues);
            }
        }

        // Audio stream sanity: frame count > 0 when a stream was detected.
        if (audio is MpaStream mpa && !mpa.Frames.Any())
        {
            issues.Add(new(ValidationSeverity.Warning, "MPEG stream detected but produced zero frames."));
        }

        return issues;
    }

    private static void CheckId3v2Size(byte[] fileBytes, IAudioTagOffset offset, Id3v2Tag tag, List<ValidationIssue> issues)
    {
        var start = offset.StartOffset;
        if (start + 10 > fileBytes.Length)
        {
            return;
        }

        // Synchsafe size at bytes 6..9 of the header.
        var raw = (fileBytes[start + 6] << 24) | (fileBytes[start + 7] << 16) | (fileBytes[start + 8] << 8) | fileBytes[start + 9];
        var declared = DecodeSynchsafe(raw);
        // "Declared" excludes the 10-byte header and includes the (optional) 10-byte footer.
        var expectedEnd = start + 10 + declared;
        if (tag.UseFooter)
        {
            // Footer adds 10 bytes past the declared size.
            expectedEnd += 10;
        }

        if (expectedEnd != offset.EndOffset)
        {
            issues.Add(new(ValidationSeverity.Warning,
                $"ID3v2 at 0x{start:X8}: declared size puts end at 0x{expectedEnd:X8} but reader stopped at 0x{offset.EndOffset:X8}."));
        }
    }

    private static int DecodeSynchsafe(int value)
    {
        return ((value >> 3) & 0x0FE00000)
             | ((value >> 2) & 0x001FC000)
             | ((value >> 1) & 0x00003F80)
             | (value & 0x0000007F);
    }
}
