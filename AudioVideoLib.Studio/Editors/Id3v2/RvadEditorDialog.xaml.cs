namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;

public partial class RvadEditorDialog : Window
{
    public RvadEditorDialog() => InitializeComponent();

    private void Ok_Click(object sender, RoutedEventArgs e) => EditorDialogActions.Ok(this);
    private void Cancel_Click(object sender, RoutedEventArgs e) => EditorDialogActions.Cancel(this);
}
