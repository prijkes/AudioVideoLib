namespace AudioVideoLib.Studio.Analysis;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

using AudioVideoLib.Formats;
using AudioVideoLib.IO;

public partial class AnalysisPanel : UserControl
{
    public AnalysisPanel()
    {
        InitializeComponent();
    }

    public sealed record SanityRow(string Label, string Value);

    public void Load(FileDossier? dossier)
    {
        var rows = new List<SanityRow>();
        var bitrates = new List<int>();

        if (dossier == null)
        {
            SanityList.ItemsSource = rows;
            Chart.SetBitrates(bitrates);
            return;
        }

        rows.Add(new("File", System.IO.Path.GetFileName(dossier.FilePath)));
        rows.Add(new("Size", $"{dossier.FileSize:N0} bytes"));

        switch (dossier.AudioStream)
        {
            case MpaStream mpa:
                BuildMpaSanity(mpa, rows, bitrates);
                break;
            case FlacStream flac:
                BuildFlacSanity(flac, rows);
                break;
            case RiffStream riff:
                BuildRiffSanity(riff, rows);
                break;
            case AiffStream aiff:
                BuildAiffSanity(aiff, rows);
                break;
            case OggStream ogg:
                BuildOggSanity(ogg, rows);
                break;
            default:
                rows.Add(new("Audio stream", "(none detected)"));
                break;
        }

        SanityList.ItemsSource = rows;
        Chart.SetBitrates(bitrates);
    }

    private static void BuildMpaSanity(MpaStream mpa, List<SanityRow> rows, List<int> bitrates)
    {
        var frames = mpa.Frames.ToList();
        rows.Add(new("Frames detected", frames.Count.ToString("N0")));
        rows.Add(new("Declared duration", FormatMs(mpa.TotalAudioLength)));

        if (frames.Count > 0)
        {
            foreach (var f in frames)
            {
                bitrates.Add(f.Bitrate);
            }

            var avg = (int)bitrates.Average();
            var min = bitrates.Min();
            var max = bitrates.Max();
            rows.Add(new("Bitrate (min / avg / max)", $"{min} / {avg} / {max} kbps"));
            rows.Add(new("Sample rate", $"{frames[0].SamplingRate:N0} Hz"));
            rows.Add(new("Channels", frames[0].ChannelMode.ToString()));

            // Sanity: average bitrate from frames × frame count should match declared duration.
            var totalDataSize = mpa.TotalAudioSize;
            if (totalDataSize > 0 && avg > 0)
            {
                var expectedMs = (long)(totalDataSize * 8.0 / (avg * 1000) * 1000);
                var diff = System.Math.Abs(expectedMs - mpa.TotalAudioLength);
                rows.Add(new("Duration from data÷bitrate", FormatMs(expectedMs)));
                rows.Add(new("Δ from declared", $"{diff:N0} ms" + (diff < 1000 ? " ✓" : " — check")));
            }
        }
    }

    private static void BuildFlacSanity(FlacStream flac, List<SanityRow> rows)
    {
        var info = flac.MetadataBlocks.OfType<FlacStreamInfoMetadataBlock>().FirstOrDefault();
        if (info != null)
        {
            rows.Add(new("Sample rate", $"{info.SampleRate:N0} Hz"));
            rows.Add(new("Channels", info.Channels.ToString()));
            rows.Add(new("Bits/sample", info.BitsPerSample.ToString()));
            rows.Add(new("Total samples", info.TotalSamples.ToString("N0")));
            if (info.TotalSamples > 0 && info.SampleRate > 0)
            {
                var seconds = (double)info.TotalSamples / info.SampleRate;
                rows.Add(new("Declared duration", FormatMs((long)(seconds * 1000))));
            }
        }
    }

    private static void BuildRiffSanity(RiffStream riff, List<SanityRow> rows)
    {
        rows.Add(new("Format type", riff.FormatType));
        rows.Add(new("Audio format", $"0x{riff.AudioFormat:X4}"));
        rows.Add(new("Sample rate", $"{riff.SampleRate:N0} Hz"));
        rows.Add(new("Channels", riff.Channels.ToString()));
        rows.Add(new("Bits/sample", riff.BitsPerSample.ToString()));
        rows.Add(new("Data size", $"{riff.DataSize:N0} bytes"));
        if (riff.TotalAudioLength > 0)
        {
            rows.Add(new("Duration", FormatMs(riff.TotalAudioLength)));
        }
    }

    private static void BuildAiffSanity(AiffStream aiff, List<SanityRow> rows)
    {
        rows.Add(new("Format type", aiff.FormatType));
        rows.Add(new("Sample rate", $"{aiff.SampleRate:0}"));
        rows.Add(new("Channels", aiff.Channels.ToString()));
        rows.Add(new("Sample size", $"{aiff.SampleSize}-bit"));
        rows.Add(new("Sample frames", aiff.SampleFrames.ToString("N0")));
        if (aiff.TotalAudioLength > 0)
        {
            rows.Add(new("Duration", FormatMs(aiff.TotalAudioLength)));
        }
    }

    private static void BuildOggSanity(OggStream ogg, List<SanityRow> rows)
    {
        rows.Add(new("Pages", ogg.PageCount.ToString("N0")));
        rows.Add(new("Total granule", ogg.TotalGranulePosition.ToString("N0")));
    }

    private static string FormatMs(long milliseconds)
    {
        var ts = TimeSpan.FromMilliseconds(milliseconds);
        return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
    }
}
