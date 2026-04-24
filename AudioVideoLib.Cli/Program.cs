namespace AudioVideoLib.Cli;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

using AudioVideoLib.Formats;
using AudioVideoLib.IO;
using AudioVideoLib.Tags;

internal static class Program
{
    internal static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            PrintHelp();
            return args.Length == 0 ? 1 : 0;
        }

        try
        {
            return args[0] switch
            {
                "info" => RunInfo(args),
                "batch" => RunBatch(args),
                "validate" => RunValidate(args),
                _ => Fail($"Unknown command: {args[0]}"),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 2;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("avs — AudioVideoLib command-line inspector");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  avs info <file>                    Print format, tags, audio summary");
        Console.WriteLine("  avs validate <file>                Exit 0 if clean, 1 if errors");
        Console.WriteLine("  avs batch <folder> [--recursive]   Scan folder, print JSON array to stdout");
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 2;
    }

    private static int RunInfo(string[] args)
    {
        if (args.Length < 2)
        {
            return Fail("info requires a file path");
        }

        var path = args[1];
        var (offsets, audio, bytes) = Load(path);

        Console.WriteLine($"File:  {path}");
        Console.WriteLine($"Size:  {bytes.Length:N0} bytes");
        Console.WriteLine();

        Console.WriteLine("Tags:");
        if (offsets.Count == 0)
        {
            Console.WriteLine("  (none)");
        }
        else
        {
            foreach (var o in offsets)
            {
                Console.WriteLine($"  {o.AudioTag.GetType().Name,-20} @ 0x{o.StartOffset:X8} .. 0x{o.EndOffset:X8}  ({o.EndOffset - o.StartOffset:N0} bytes)");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Audio:");
        switch (audio)
        {
            case MpaStream mpa:
                var first = mpa.Frames.FirstOrDefault();
                if (first != null)
                {
                    Console.WriteLine($"  MPEG {first.AudioVersion} {first.LayerVersion}");
                    Console.WriteLine($"  {first.SamplingRate} Hz, {first.ChannelMode}, {first.Bitrate} kbps");
                    Console.WriteLine($"  Frames: {mpa.Frames.Count():N0}");
                    Console.WriteLine($"  Duration: {FormatMs(mpa.TotalDuration)}");
                }
                break;
            case FlacStream flac:
                var info = flac.MetadataBlocks.OfType<FlacStreamInfoMetadataBlock>().FirstOrDefault();
                if (info != null)
                {
                    Console.WriteLine($"  FLAC {info.SampleRate} Hz, {info.Channels} ch, {info.BitsPerSample}-bit");
                    Console.WriteLine($"  Total samples: {info.TotalSamples:N0}");
                }
                break;
            case RiffStream riff:
                Console.WriteLine($"  WAV {riff.SampleRate} Hz, {riff.Channels} ch, {riff.BitsPerSample}-bit");
                Console.WriteLine($"  Data: {riff.DataSize:N0} bytes");
                break;
            case AiffStream aiff:
                Console.WriteLine($"  {aiff.FormatType} {aiff.SampleRate:0} Hz, {aiff.Channels} ch, {aiff.SampleSize}-bit");
                Console.WriteLine($"  Sample frames: {aiff.SampleFrames:N0}");
                break;
            case OggStream ogg:
                Console.WriteLine($"  OGG {ogg.PageCount:N0} pages");
                break;
            default:
                Console.WriteLine("  (no audio stream detected)");
                break;
        }

        return 0;
    }

    private static int RunValidate(string[] args)
    {
        if (args.Length < 2)
        {
            return Fail("validate requires a file path");
        }

        var path = args[1];
        var (offsets, _, _) = Load(path);
        var errors = 0;
        var warnings = 0;

        foreach (var o in offsets)
        {
            foreach (var issue in BasicValidate(o.AudioTag))
            {
                if (issue.Severity == "Error")
                {
                    errors++;
                    Console.WriteLine($"ERROR   {o.AudioTag.GetType().Name}: {issue.Message}");
                }
                else if (issue.Severity == "Warning")
                {
                    warnings++;
                    Console.WriteLine($"WARN    {o.AudioTag.GetType().Name}: {issue.Message}");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine($"{errors} error(s), {warnings} warning(s)");
        return errors > 0 ? 1 : 0;
    }

    private static int RunBatch(string[] args)
    {
        if (args.Length < 2)
        {
            return Fail("batch requires a folder path");
        }

        var folder = args[1];
        var recursive = args.Any(a => a is "--recursive" or "-r");
        if (!Directory.Exists(folder))
        {
            return Fail($"not a folder: {folder}");
        }

        var rows = new JsonArray();
        string[] extensions = [".mp3", ".flac", ".wav", ".aif", ".aiff", ".ogg", ".oga", ".m4a"];
        var opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (var file in Directory.EnumerateFiles(folder, "*.*", opt))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (Array.IndexOf(extensions, ext) < 0)
            {
                continue;
            }

            try
            {
                var (offsets, audio, bytes) = Load(file);
                var tagList = string.Join("+", offsets.Select(o => o.AudioTag.GetType().Name.Replace("Tag", string.Empty)).Distinct());
                var format = audio switch
                {
                    MpaStream => "MPEG",
                    FlacStream => "FLAC",
                    RiffStream => "WAV",
                    AiffStream => "AIFF",
                    OggStream => "OGG",
                    _ => "unknown",
                };

                rows.Add(new JsonObject
                {
                    ["path"] = file,
                    ["size"] = bytes.Length,
                    ["format"] = format,
                    ["tags"] = tagList,
                });
            }
            catch (Exception ex)
            {
                rows.Add(new JsonObject
                {
                    ["path"] = file,
                    ["error"] = ex.Message,
                });
            }
        }

        Console.WriteLine(rows.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    private static (IReadOnlyList<IAudioTagOffset> Offsets, IMediaContainer? Audio, byte[] Bytes) Load(string path)
    {
        var bytes = File.ReadAllBytes(path);
        using var ms = new MemoryStream(bytes);
        var offsets = AudioTags.ReadStream(ms).OfType<IAudioTagOffset>().ToList();
        var startTagEnd = offsets
            .Where(o => o.TagOrigin == TagOrigin.Start)
            .Select(o => o.EndOffset)
            .DefaultIfEmpty(0L)
            .Max();
        ms.Position = startTagEnd;
        var audio = MediaContainers.ReadStream(ms).FirstOrDefault();
        return (offsets, audio, bytes);
    }

    private sealed record CliIssue(string Severity, string Message);

    private static IEnumerable<CliIssue> BasicValidate(IAudioTag tag)
    {
        // Minimal inline validation — the CLI can't reference the Studio project (WPF).
        switch (tag)
        {
            case Id3v2Tag v2:
                foreach (var frame in v2.Frames)
                {
                    var id = frame.Identifier ?? string.Empty;
                    var expected = v2.Version < Id3v2Version.Id3v230 ? 3 : 4;
                    if (id.Length != expected)
                    {
                        yield return new("Error", $"Frame '{id}' has {id.Length} chars, expected {expected}.");
                    }
                }
                break;
            case Id3v1Tag v1:
                if (v1.TrackTitle?.Length > 30)
                {
                    yield return new("Error", "Title exceeds 30 chars.");
                }
                break;
            case ApeTag ape:
                foreach (var item in ape.Items)
                {
                    if (item.Key.Length is < 2 or > 255)
                    {
                        yield return new("Error", $"APE key length {item.Key.Length} out of bounds (2..255).");
                    }
                }
                break;
        }
    }

    private static string FormatMs(long milliseconds)
    {
        var ts = TimeSpan.FromMilliseconds(milliseconds);
        return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
    }
}
