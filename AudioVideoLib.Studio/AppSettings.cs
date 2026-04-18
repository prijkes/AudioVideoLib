namespace AudioVideoLib.Studio;

using System;
using System.IO;
using System.Text.Json;

public sealed class AppSettings
{
    private static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AudioVideoLib.Studio",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public int HexBytesPerRow { get; set; } = 16;

    public int HexFontSize { get; set; } = 11;

    public int MaxAudioFramesInTree { get; set; } = 200;

    public int MaxOggPagesInTree { get; set; } = 500;

    public int RecentFilesCount { get; set; } = 10;

    public static event EventHandler? Changed;

    public static AppSettings Current { get; private set; } = Load();

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(Path))
            {
                var json = File.ReadAllText(Path);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded != null)
                {
                    loaded.Normalize();
                    return loaded;
                }
            }
        }
        catch
        {
            // fall through to defaults
        }

        return new AppSettings();
    }

    public void Save()
    {
        Normalize();
        try
        {
            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(Path, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // best-effort; not fatal
        }
    }

    public static void Replace(AppSettings next)
    {
        next.Normalize();
        next.Save();
        Current = next;
        Changed?.Invoke(null, EventArgs.Empty);
    }

    private void Normalize()
    {
        HexBytesPerRow = HexBytesPerRow switch
        {
            < 4 => 4,
            > 64 => 64,
            _ => HexBytesPerRow,
        };
        HexFontSize = HexFontSize switch
        {
            < 8 => 8,
            > 24 => 24,
            _ => HexFontSize,
        };
        MaxAudioFramesInTree = Math.Clamp(MaxAudioFramesInTree, 10, 100_000);
        MaxOggPagesInTree = Math.Clamp(MaxOggPagesInTree, 10, 1_000_000);
        RecentFilesCount = Math.Clamp(RecentFilesCount, 1, 50);
    }
}
