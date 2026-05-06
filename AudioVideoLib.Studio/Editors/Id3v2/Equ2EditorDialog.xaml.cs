namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

public partial class Equ2EditorDialog : Window
{
    public Equ2EditorDialog() => InitializeComponent();

    private Equ2Editor Editor => (Equ2Editor)DataContext;

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
        => Editor.AddRow(new Equ2RowVm { Frequency = 0, VolumeAdjustment = 0 });

    private void Remove_Click(object sender, RoutedEventArgs e)
        => Editor.RemoveRow(Grid.SelectedIndex);

    private void Up_Click(object sender, RoutedEventArgs e)
        => Editor.MoveUp(Grid.SelectedIndex);

    private void Down_Click(object sender, RoutedEventArgs e)
        => Editor.MoveDown(Grid.SelectedIndex);
}
