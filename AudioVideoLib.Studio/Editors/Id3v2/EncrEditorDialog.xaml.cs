namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

public partial class EncrEditorDialog : Window
{
    public EncrEditorDialog() => InitializeComponent();

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var editor = (EncrEditor)DataContext;
        if (!editor.Validate(out var error))
        {
            MessageBox.Show(this, error, "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void LoadFromFile_Click(object sender, RoutedEventArgs e)
        => ((EncrEditor)DataContext).LoadDataFromFile(this);

    private void ClearData_Click(object sender, RoutedEventArgs e)
        => ((EncrEditor)DataContext).ClearData();
}
