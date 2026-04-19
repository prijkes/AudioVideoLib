namespace AudioVideoLib.Studio;

using System.Collections.Generic;
using System.Linq;
using System.Text;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

public sealed record LintSection(string Title, IReadOnlyList<ValidationIssue> Issues);

public sealed record LintReportData(
    string FilePath,
    long FileSize,
    IReadOnlyList<LintSection> Sections)
{
    public int TotalErrors => Sections.SelectMany(s => s.Issues).Count(i => i.Severity == ValidationSeverity.Error);

    public int TotalWarnings => Sections.SelectMany(s => s.Issues).Count(i => i.Severity == ValidationSeverity.Warning);

    public int TotalInfos => Sections.SelectMany(s => s.Issues).Count(i => i.Severity == ValidationSeverity.Info);
}

public static class LintReport
{
    public static LintReportData Build(FileDossier dossier, IAudioStream? audio, IReadOnlyList<IAudioTagOffset> offsets)
    {
        var sections = new List<LintSection>();

        if (dossier.ParseWarnings.Count > 0)
        {
            sections.Add(new LintSection("Tag parse warnings", dossier.ParseWarnings));
        }

        // Per-tag validation via the existing TagValidator.
        foreach (var tab in dossier.TagTabs)
        {
            var issues = tab switch
            {
                VorbisTabViewModel v => TagValidator.ValidateVorbisForStudio(v),
                Id3v2TabViewModel v2 => TagValidator.Validate(v2.Tag),
                Id3v1TabViewModel v1 => TagValidator.Validate(v1.Tag),
                ApeTabViewModel ape => TagValidator.Validate(ape.Tag),
                Lyrics3v1TabViewModel l1 => TagValidator.Validate(l1.Tag),
                Lyrics3v2TabViewModel l2 => TagValidator.Validate(l2.Tag),
                MusicMatchTabViewModel mm => TagValidator.Validate(mm.Tag),
                _ => (IReadOnlyList<ValidationIssue>)[],
            };

            if (issues.Count > 0)
            {
                sections.Add(new LintSection(tab.Header, issues));
            }
        }

        var frameIssues = FrameIntegrityChecker.Check(audio);
        if (frameIssues.Count > 0)
        {
            sections.Add(new LintSection("Audio frame integrity", frameIssues));
        }

        var unsyncIssues = Id3v2UnsyncChecker.Check(dossier.FileBytes, offsets);
        if (unsyncIssues.Count > 0)
        {
            sections.Add(new LintSection("ID3v2 unsynchronization", unsyncIssues));
        }

        var structIssues = StructureIntegrityChecker.Check(dossier.FileBytes, offsets, audio);
        if (structIssues.Count > 0)
        {
            sections.Add(new LintSection("File structure", structIssues));
        }

        var containerIssues = audio switch
        {
            RiffStream riff => ContainerLinter.CheckRiff(riff),
            Mp4Stream mp4 => ContainerLinter.CheckMp4(mp4),
            AsfStream asf => ContainerLinter.CheckAsf(asf),
            MatroskaStream mkv => ContainerLinter.CheckMatroska(mkv),
            DsfStream or DffStream => ContainerLinter.CheckDsd(audio),
            _ => (IReadOnlyList<ValidationIssue>)[],
        };

        if (containerIssues.Count > 0)
        {
            sections.Add(new LintSection("Container metadata", containerIssues));
        }

        return new LintReportData(dossier.FilePath, dossier.FileSize, sections);
    }

    public static string FormatMarkdown(LintReportData report)
    {
        var sb = new StringBuilder();
        sb.Append("# Lint report\n\n");
        sb.Append("**File:** `").Append(report.FilePath).Append("`  \n");
        sb.Append("**Size:** ").Append(report.FileSize.ToString("N0")).Append(" bytes  \n");
        sb.Append("**Summary:** ").Append(report.TotalErrors).Append(" errors, ")
          .Append(report.TotalWarnings).Append(" warnings, ")
          .Append(report.TotalInfos).Append(" info\n\n");

        if (report.Sections.Count == 0)
        {
            sb.Append("_No issues found._\n");
            return sb.ToString();
        }

        foreach (var section in report.Sections)
        {
            sb.Append("## ").Append(section.Title).Append("\n\n");
            foreach (var severity in new[] { ValidationSeverity.Error, ValidationSeverity.Warning, ValidationSeverity.Info })
            {
                var group = section.Issues.Where(i => i.Severity == severity).ToList();
                if (group.Count == 0)
                {
                    continue;
                }

                sb.Append("**").Append(severity.ToString().ToUpperInvariant()).Append("** (").Append(group.Count).Append(")\n\n");
                foreach (var issue in group)
                {
                    sb.Append("- ").Append(issue.Message).Append('\n');
                }

                sb.Append('\n');
            }
        }

        return sb.ToString();
    }

    public static string FormatPlainText(LintReportData report)
    {
        var sb = new StringBuilder();
        sb.Append("Lint report for ").Append(report.FilePath).Append('\n');
        sb.Append("Size: ").Append(report.FileSize.ToString("N0")).Append(" bytes\n");
        sb.Append(report.TotalErrors).Append(" errors, ")
          .Append(report.TotalWarnings).Append(" warnings, ")
          .Append(report.TotalInfos).Append(" info\n");
        sb.Append(new string('=', 60)).Append('\n').Append('\n');

        if (report.Sections.Count == 0)
        {
            sb.Append("No issues found.\n");
            return sb.ToString();
        }

        foreach (var section in report.Sections)
        {
            sb.Append(section.Title).Append('\n');
            sb.Append(new string('-', section.Title.Length)).Append('\n');
            foreach (var severity in new[] { ValidationSeverity.Error, ValidationSeverity.Warning, ValidationSeverity.Info })
            {
                var group = section.Issues.Where(i => i.Severity == severity).ToList();
                if (group.Count == 0)
                {
                    continue;
                }

                sb.Append(severity.ToString().ToUpperInvariant()).Append(" (").Append(group.Count).Append(")\n");
                foreach (var issue in group)
                {
                    sb.Append("  • ").Append(issue.Message).Append('\n');
                }
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }
}
