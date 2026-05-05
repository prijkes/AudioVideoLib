namespace AudioVideoLib.Studio;

using System.Text;
using System.Windows;

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

    private static void OnAnyWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window)
        {
            DarkTitleBar.Apply(window);
        }
    }
}
