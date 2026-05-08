namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;

public partial class SyltEditorDialog : Window
{
    public SyltEditorDialog() => InitializeComponent();

    private SyltEditor Editor => (SyltEditor)DataContext;

    private void Ok_Click(object sender, RoutedEventArgs e) => EditorDialogActions.Ok(this);
    private void Cancel_Click(object sender, RoutedEventArgs e) => EditorDialogActions.Cancel(this);

    private void Add_Click(object sender, RoutedEventArgs e)
        => Editor.AddRow(new SyltRowVm { Syllable = string.Empty, TimeStamp = 0 });

    private void Remove_Click(object sender, RoutedEventArgs e)
        => Editor.RemoveRow(Grid.SelectedIndex);

    private void Up_Click(object sender, RoutedEventArgs e)
        => Editor.MoveUp(Grid.SelectedIndex);

    private void Down_Click(object sender, RoutedEventArgs e)
        => Editor.MoveDown(Grid.SelectedIndex);
}
