namespace AudioVideoLib.Studio.Batch;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

public sealed record BatchRow(
    string Path,
    long Size,
    string Format,
    string Tags,
    int Errors,
    int Warnings,
    string? Error);

public static class BatchScanner
{
    private static readonly string[] DefaultExtensions = [".mp3", ".flac", ".wav", ".aif", ".aiff", ".ogg", ".oga", ".m4a"];

    public static IEnumerable<string> EnumerateFiles(string folder, bool recursive)
    {
        var opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        foreach (var file in Directory.EnumerateFiles(folder, "*.*", opt))
        {
            var ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
            if (Array.IndexOf(DefaultExtensions, ext) >= 0)
            {
                yield return file;
            }
        }
    }

    public static BatchRow ScanFile(string path)
    {
        try
        {
            var bytes = File.ReadAllBytes(path);
            using var ms = new MemoryStream(bytes);
            var offsets = AudioTags.ReadStream(ms).OfType<IAudioTagOffset>().ToList();

            // Skip past start-origin tags before scanning for audio.
            var startTagEnd = offsets
                .Where(o => o.TagOrigin == TagOrigin.Start)
                .Select(o => o.EndOffset)
                .DefaultIfEmpty(0L)
                .Max();
            ms.Position = startTagEnd;
            using var containers = MediaContainers.ReadStream(ms);
            var audio = containers.FirstOrDefault();

            var format = audio switch
            {
                MpaStream mpa => $"MPEG {mpa.Frames.FirstOrDefault()?.AudioVersion} {mpa.Frames.FirstOrDefault()?.LayerVersion}",
                FlacStream => "FLAC",
                RiffStream riff => $"WAV ({riff.FormatType})",
                AiffStream aiff => aiff.FormatType,
                OggStream => "OGG",
                _ => "unknown",
            };

            var tagNames = offsets.Select(o => o.AudioTag.GetType().Name.Replace("Tag", string.Empty)).Distinct();
            var tags = string.Join("+", tagNames);

            var errors = 0;
            var warnings = 0;
            foreach (var o in offsets)
            {
                var issues = TagValidator.Validate(o.AudioTag);
                errors += issues.Count(i => i.Severity == ValidationSeverity.Error);
                warnings += issues.Count(i => i.Severity == ValidationSeverity.Warning);
            }

            return new BatchRow(path, bytes.Length, format, tags, errors, warnings, null);
        }
        catch (Exception ex)
        {
            return new BatchRow(path, 0, "-", string.Empty, 0, 0, ex.Message);
        }
    }

    public static string ToCsv(IEnumerable<BatchRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Path,Size,Format,Tags,Errors,Warnings,Error");
        foreach (var r in rows)
        {
            sb.Append(Csv(r.Path)).Append(',')
              .Append(r.Size).Append(',')
              .Append(Csv(r.Format)).Append(',')
              .Append(Csv(r.Tags)).Append(',')
              .Append(r.Errors).Append(',')
              .Append(r.Warnings).Append(',')
              .AppendLine(Csv(r.Error ?? string.Empty));
        }

        return sb.ToString();
    }

    public static string ToJson(IEnumerable<BatchRow> rows)
    {
        var arr = new JsonArray();
        foreach (var r in rows)
        {
            arr.Add(new JsonObject
            {
                ["path"] = r.Path,
                ["size"] = r.Size,
                ["format"] = r.Format,
                ["tags"] = r.Tags,
                ["errors"] = r.Errors,
                ["warnings"] = r.Warnings,
                ["error"] = r.Error,
            });
        }

        return arr.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static string Csv(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }

        var needsQuote = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
        return !needsQuote ? s : "\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
