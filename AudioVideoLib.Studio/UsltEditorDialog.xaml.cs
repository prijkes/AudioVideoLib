namespace AudioVideoLib.Studio;

using System.Windows;

using AudioVideoLib.Tags;

public partial class UsltEditorDialog : Window
{
    public UsltEditorDialog()
    {
        InitializeComponent();
    }

    public static bool Edit(Window owner, Id3v2UnsynchronizedLyricsFrame frame)
    {
        var dlg = new UsltEditorDialog { Owner = owner };
        dlg.LanguageBox.Text = frame.Language ?? "eng";
        dlg.DescriptorBox.Text = frame.ContentDescriptor ?? string.Empty;
        dlg.LyricsBox.Text = frame.Lyrics ?? string.Empty;

        if (dlg.ShowDialog() != true)
        {
            return false;
        }

        frame.TextEncoding = Id3v2FrameEncodingType.UTF8;
        frame.Language = string.IsNullOrEmpty(dlg.LanguageBox.Text) ? "eng" : dlg.LanguageBox.Text;
        frame.ContentDescriptor = dlg.DescriptorBox.Text ?? string.Empty;
        frame.Lyrics = dlg.LyricsBox.Text ?? string.Empty;
        return true;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
