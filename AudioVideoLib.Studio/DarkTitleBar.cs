namespace AudioVideoLib.Studio;

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Interop;

/// <summary>
/// Switches the WPF window's non-client area (title bar, frame) to Windows 10/11 dark mode
/// via <c>DwmSetWindowAttribute</c>. The attribute name and value differ between Windows
/// 10 1809 (20H2) and the formal 1903+ release; this helper tries both.
/// </summary>
/// <remarks>
/// Apply by calling <see cref="Apply"/> from <see cref="Window.SourceInitialized"/>
/// (the HWND must already exist). The effect is purely cosmetic — no impact on
/// rendering of the WPF content area.
/// </remarks>
[SupportedOSPlatform("windows")]
internal static class DarkTitleBar
{
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;
    private const int DwmwaUseImmersiveDarkMode = 20;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    /// <summary>
    /// Applies dark mode to the given window's title bar.
    /// </summary>
    /// <param name="window">The WPF window. Must have an HWND (call after SourceInitialized).</param>
    public static void Apply(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var enabled = 1;

        // Try the post-20H1 attribute first; fall back to the older one if needed.
        if (DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int)) != 0)
        {
            DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModeBefore20H1, ref enabled, sizeof(int));
        }
    }
}
