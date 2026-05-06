namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

public partial class AencEditorDialog : Window
{
    public AencEditorDialog() => InitializeComponent();

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var editor = (AencEditor)DataContext;
        if (!editor.Validate(out var error))
        {
            MessageBox.Show(this, error, "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void LoadFromFile_Click(object sender, RoutedEventArgs e)
        => ((AencEditor)DataContext).LoadDataFromFile(this);

    private void ClearData_Click(object sender, RoutedEventArgs e)
        => ((AencEditor)DataContext).ClearData();
}
