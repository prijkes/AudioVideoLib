namespace AudioVideoLib.Studio;

using System.Collections.Generic;

using AudioVideoLib.Tags;

public static class Id3v2UnsyncChecker
{
    // Unsynchronisation rule: within the tag body, every 0xFF byte must be followed by 0x00 or
    // the upper bits of a valid frame sync (0xE0..0xFF) — we only flag 0xFF followed by a value
    // that would produce a false audio sync (first-nibble 0xE..0xF with second-nibble bit set).
    public static IReadOnlyList<ValidationIssue> Check(byte[] fileBytes, IReadOnlyList<IAudioTagOffset> offsets)
    {
        var issues = new List<ValidationIssue>();
        foreach (var offset in offsets)
        {
            if (offset.AudioTag is not Id3v2Tag tag || !tag.UseUnsynchronization)
            {
                continue;
            }

            var start = offset.StartOffset + 10; // past "ID3" header
            var end = offset.EndOffset;
            if (tag.UseFooter)
            {
                end -= 10;
            }

            if (start < 0 || end > fileBytes.Length || start >= end)
            {
                continue;
            }

            var violations = 0;
            var firstAt = -1L;
            for (var i = start; i < end - 1; i++)
            {
                if (fileBytes[i] != 0xFF)
                {
                    continue;
                }

                var next = fileBytes[i + 1];
                // Any 0xFF in the body must have been prefixed with an inserted 0x00 during unsync.
                // If the following byte has its top 3 bits set (0xE0..0xFF) or equals 0xFF, that would be a false frame-sync.
                if ((next & 0xE0) == 0xE0 || next == 0xFF)
                {
                    violations++;
                    if (firstAt < 0)
                    {
                        firstAt = i;
                    }
                }
            }

            if (violations > 0)
            {
                issues.Add(new(ValidationSeverity.Error,
                    $"ID3v2 tag at 0x{offset.StartOffset:X8} has unsynchronization flag set but {violations} false-sync " +
                    $"byte sequence(s) found (first at 0x{firstAt:X8})."));
            }
        }

        return issues;
    }
}
