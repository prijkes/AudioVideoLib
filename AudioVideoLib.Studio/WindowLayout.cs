namespace AudioVideoLib.Studio;

using System;
using System.Globalization;
using System.IO;
using System.Windows;

public static class WindowLayout
{
    private static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AudioVideoLib.Studio",
        "window.txt");

    public sealed record Layout(double Left, double Top, double Width, double Height, WindowState State);

    public static Layout? Load()
    {
        try
        {
            if (!File.Exists(Path))
            {
                return null;
            }

            var parts = File.ReadAllText(Path).Trim().Split(',');
            return parts.Length != 5
                ? null
                : new Layout(
                    double.Parse(parts[0], CultureInfo.InvariantCulture),
                    double.Parse(parts[1], CultureInfo.InvariantCulture),
                    double.Parse(parts[2], CultureInfo.InvariantCulture),
                    double.Parse(parts[3], CultureInfo.InvariantCulture),
                    Enum.Parse<WindowState>(parts[4]));
        }
        catch
        {
            return null;
        }
    }

    public static void Save(Window window)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(Path);
            if (dir != null)
            {
                Directory.CreateDirectory(dir);
            }

            var rect = window.WindowState == WindowState.Normal
                ? new Rect(window.Left, window.Top, window.Width, window.Height)
                : window.RestoreBounds;

            File.WriteAllText(Path, string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4}",
                rect.Left, rect.Top, rect.Width, rect.Height, window.WindowState));
        }
        catch
        {
            // best-effort
        }
    }
}
