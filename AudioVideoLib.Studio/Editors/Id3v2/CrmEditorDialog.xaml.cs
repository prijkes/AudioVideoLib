namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;

public partial class CrmEditorDialog : Window
{
    public CrmEditorDialog() => InitializeComponent();

    private void Ok_Click(object sender, RoutedEventArgs e) => EditorDialogActions.Ok(this);
    private void Cancel_Click(object sender, RoutedEventArgs e) => EditorDialogActions.Cancel(this);

    // CrmEditor is a WrapperEditorBase, not a BinaryDataEditorBase, so the shared
    // EditorDialogActions.LoadFromFile/ClearData helpers can't dispatch to it.
    private void LoadFromFile_Click(object sender, RoutedEventArgs e)
        => ((CrmEditor)DataContext).LoadDataFromFile(this);

    private void ClearData_Click(object sender, RoutedEventArgs e)
        => ((CrmEditor)DataContext).ClearData();
}
