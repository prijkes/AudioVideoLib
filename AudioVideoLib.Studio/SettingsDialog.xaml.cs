namespace AudioVideoLib.Studio;

using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

public partial class SettingsDialog : Window
{
    public SettingsDialog()
    {
        InitializeComponent();
        LoadInto(AppSettings.Current);
    }

    private void LoadInto(AppSettings s)
    {
        SelectCombo(BytesPerRowBox, s.HexBytesPerRow);
        SelectCombo(FontSizeBox, s.HexFontSize);
        MaxFramesBox.Text = s.MaxAudioFramesInTree.ToString(CultureInfo.InvariantCulture);
        MaxPagesBox.Text = s.MaxOggPagesInTree.ToString(CultureInfo.InvariantCulture);
        RecentCountBox.Text = s.RecentFilesCount.ToString(CultureInfo.InvariantCulture);
    }

    private static void SelectCombo(ComboBox box, int value)
    {
        foreach (var item in box.Items.OfType<ComboBoxItem>())
        {
            if (int.TryParse(item.Content?.ToString(), out var n) && n == value)
            {
                box.SelectedItem = item;
                return;
            }
        }

        box.Text = value.ToString(CultureInfo.InvariantCulture);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var next = new AppSettings
        {
            HexBytesPerRow = ReadCombo(BytesPerRowBox, AppSettings.Current.HexBytesPerRow),
            HexFontSize = ReadCombo(FontSizeBox, AppSettings.Current.HexFontSize),
            MaxAudioFramesInTree = ReadInt(MaxFramesBox, AppSettings.Current.MaxAudioFramesInTree),
            MaxOggPagesInTree = ReadInt(MaxPagesBox, AppSettings.Current.MaxOggPagesInTree),
            RecentFilesCount = ReadInt(RecentCountBox, AppSettings.Current.RecentFilesCount),
        };

        AppSettings.Replace(next);
        DialogResult = true;
        Close();
    }

    private void Reset_Click(object sender, RoutedEventArgs e) => LoadInto(new AppSettings());

    private static int ReadCombo(ComboBox box, int fallback)
    {
        return box.SelectedItem is ComboBoxItem item && int.TryParse(item.Content?.ToString(), out var n)
            ? n
            : int.TryParse(box.Text, out var v) ? v : fallback;
    }

    private static int ReadInt(TextBox box, int fallback) =>
        int.TryParse(box.Text, out var v) ? v : fallback;
}
