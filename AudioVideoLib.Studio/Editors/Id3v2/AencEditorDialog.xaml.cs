namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;

public partial class AencEditorDialog : Window
{
    public AencEditorDialog() => InitializeComponent();

    private void Ok_Click(object sender, RoutedEventArgs e) => EditorDialogActions.Ok(this);
    private void Cancel_Click(object sender, RoutedEventArgs e) => EditorDialogActions.Cancel(this);
    private void LoadFromFile_Click(object sender, RoutedEventArgs e) => EditorDialogActions.LoadFromFile(this);
    private void ClearData_Click(object sender, RoutedEventArgs e) => EditorDialogActions.ClearData(this);
}
