namespace AudioVideoLib.Studio;

using System.Collections.Generic;

using AudioVideoLib.IO;

public static class FrameIntegrityChecker
{
    public static IReadOnlyList<ValidationIssue> Check(IAudioStream? audio)
    {
        if (audio is not MpaStream mpa)
        {
            return [];
        }

        var issues = new List<ValidationIssue>();
        var idx = 0;
        foreach (var frame in mpa.Frames)
        {
            idx++;
            if (!frame.IsCrcProtected)
            {
                continue;
            }

            var computed = frame.CalculateCrc();
            var stored = (ushort)frame.Crc;
            if (computed == 0)
            {
                // CalculateCrc bails on missing audio data — treat as informational, not an error.
                issues.Add(new(ValidationSeverity.Info,
                    $"Frame #{idx} at 0x{frame.StartOffset:X8} claims CRC protection but has no payload to verify."));
                continue;
            }

            if ((ushort)computed != stored)
            {
                issues.Add(new(ValidationSeverity.Error,
                    $"Frame #{idx} at 0x{frame.StartOffset:X8}: stored CRC 0x{stored:X4} ≠ computed 0x{(ushort)computed:X4}."));
            }
        }

        return issues;
    }
}
