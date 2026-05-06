namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

public partial class RgadEditorDialog : Window
{
    public RgadEditorDialog() => InitializeComponent();

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var editor = (RgadEditor)DataContext;
        if (!editor.Validate(out var error))
        {
            MessageBox.Show(this, error, "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
