namespace AudioVideoLib.Studio;

using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();

        var asm = Assembly.GetExecutingAssembly();
        var version = asm.GetName().Version?.ToString(3) ?? "0.0.0";
        var informational = asm
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        VersionText.Text = string.IsNullOrEmpty(informational) || informational == version
            ? $"Version {version}"
            : $"Version {version} ({informational})";
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
