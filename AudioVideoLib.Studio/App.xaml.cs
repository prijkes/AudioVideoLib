namespace AudioVideoLib.Studio;

using System;
using System.Text;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Editors.Id3v2;

public partial class App : Application
{
    static App()
    {
        // Make Windows code pages (Shift-JIS, GB18030, EUC-KR, KOI8-R, …) available.
        // .NET Core ships only a small built-in set; this provider unlocks the rest
        // so the per-field encoding picker can offer them.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Apply Windows 10/11 dark title-bar to every Window class instance.
        // FrameworkElement.LoadedEvent is routed and fires after the HWND exists.
        EventManager.RegisterClassHandler(
            typeof(Window),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnAnyWindowLoaded));
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        TagItemEditorRegistry.Shared.RegisterFromAssembly(typeof(MainWindow).Assembly);
#if DEBUG
        ValidateRegistry();
#endif
    }

#if DEBUG
    private static void ValidateRegistry()
    {
        foreach (var entry in TagItemEditorRegistry.Shared.Entries)
        {
            if (entry.Attribute is Id3v2FrameEditorAttribute a)
            {
                if (a.SupportedVersions == Id3v2VersionMask.None)
                {
                    throw new InvalidOperationException(
                        $"Editor {entry.EditorType.Name} has SupportedVersions=None.");
                }
                if (string.IsNullOrEmpty(a.MenuLabel))
                {
                    throw new InvalidOperationException(
                        $"Editor {entry.EditorType.Name} has empty MenuLabel.");
                }
                if (a.MenuLabel.Length > 60)
                {
                    throw new InvalidOperationException(
                        $"Editor {entry.EditorType.Name} MenuLabel exceeds 60 chars: {a.MenuLabel.Length}.");
                }
            }
        }
    }
#endif

    private static void OnAnyWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window)
        {
            DarkTitleBar.Apply(window);
        }
    }
}
