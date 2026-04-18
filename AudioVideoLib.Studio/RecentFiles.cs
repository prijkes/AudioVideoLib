namespace AudioVideoLib.Studio;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class RecentFiles
{
    private static int MaxEntries => AppSettings.Current.RecentFilesCount;

    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AudioVideoLib.Studio",
        "recent.txt");

    public static IReadOnlyList<string> Load()
    {
        if (!File.Exists(StorePath))
        {
            return [];
        }

        try
        {
            return [.. File.ReadAllLines(StorePath)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Take(MaxEntries)];
        }
        catch (IOException)
        {
            return [];
        }
    }

    public static void Add(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var max = MaxEntries;
        var list = Load().ToList();
        list.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, path);
        if (list.Count > max)
        {
            list.RemoveRange(max, list.Count - max);
        }

        Save(list);
    }

    public static void Clear() => Save([]);

    private static void Save(IReadOnlyList<string> list)
    {
        try
        {
            var dir = Path.GetDirectoryName(StorePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllLines(StorePath, list);
        }
        catch (IOException)
        {
            // best-effort; ignore write failures
        }
    }
}
