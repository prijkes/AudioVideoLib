namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Tags;

public partial class XrvaEditorDialog : Window
{
    public XrvaEditorDialog() => InitializeComponent();

    private XrvaEditor Editor => (XrvaEditor)DataContext;

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (!Editor.Validate(out var error))
        {
            MessageBox.Show(this, error, "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Add_Click(object sender, RoutedEventArgs e)
        => Editor.AddRow(new XrvaRowVm
        {
            ChannelType = Id3v2ChannelType.MasterVolume,
            VolumeAdjustment = 0f,
            BitsRepresentingPeak = 0,
            PeakVolume = 0,
        });

    private void Remove_Click(object sender, RoutedEventArgs e)
        => Editor.RemoveRow(Grid.SelectedIndex);

    private void Up_Click(object sender, RoutedEventArgs e)
        => Editor.MoveUp(Grid.SelectedIndex);

    private void Down_Click(object sender, RoutedEventArgs e)
        => Editor.MoveDown(Grid.SelectedIndex);
}
