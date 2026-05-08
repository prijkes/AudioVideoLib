namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

public partial class EtcoEditorDialog : Window
{
    public EtcoEditorDialog() => InitializeComponent();

    private EtcoEditor Editor => (EtcoEditor)DataContext;

    private void Ok_Click(object sender, RoutedEventArgs e) => EditorDialogActions.Ok(this);
    private void Cancel_Click(object sender, RoutedEventArgs e) => EditorDialogActions.Cancel(this);

    private void Add_Click(object sender, RoutedEventArgs e)
        => Editor.AddRow(new EtcoRowVm { EventType = Id3v2KeyEventType.Padding, TimeStamp = 0 });

    private void Remove_Click(object sender, RoutedEventArgs e)
        => Editor.RemoveRow(Grid.SelectedIndex);

    private void Up_Click(object sender, RoutedEventArgs e)
        => Editor.MoveUp(Grid.SelectedIndex);

    private void Down_Click(object sender, RoutedEventArgs e)
        => Editor.MoveDown(Grid.SelectedIndex);
}
