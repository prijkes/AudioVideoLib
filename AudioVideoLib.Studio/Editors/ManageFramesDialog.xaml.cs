namespace AudioVideoLib.Studio.Editors;

using System;
using System.Windows;
using System.Windows.Input;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

public partial class ManageFramesDialog : Window
{
    private ManageFramesViewModel _vm = null!;
    private Id3v2Tag _tag = null!;
    private Action<Id3v2MenuEntry>? _onAction;

    public ManageFramesDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Opens the Manage Frames dialog modally for <paramref name="tag"/>. The
    /// <paramref name="onAction"/> callback is invoked when the user activates a
    /// row (action button or double-click) — owners typically wire this to the
    /// same path used by the Add Frame menu.
    /// </summary>
    public static void ShowFor(
        Window owner,
        Id3v2Tag tag,
        Action<Id3v2MenuEntry> onAction)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentNullException.ThrowIfNull(onAction);

        var dlg = new ManageFramesDialog { Owner = owner };
        dlg.Initialise(tag, onAction);
        dlg.ShowDialog();
    }

    private void Initialise(Id3v2Tag tag, Action<Id3v2MenuEntry> onAction)
    {
        _tag = tag;
        _onAction = onAction;
        _vm = new ManageFramesViewModel(TagItemEditorRegistry.Shared, tag);
        EntriesGrid.ItemsSource = _vm.All;
    }

    private void Refresh()
    {
        _vm = new ManageFramesViewModel(TagItemEditorRegistry.Shared, _tag);
        EntriesGrid.ItemsSource = _vm.ApplyFilter(SearchBox.Text);
        UpdateActionButton();
    }

    private void ApplyFilterFromSearch()
    {
        if (_vm is null)
        {
            return;
        }
        EntriesGrid.ItemsSource = _vm.ApplyFilter(SearchBox.Text);
        UpdateActionButton();
    }

    private void UpdateActionButton()
    {
        if (EntriesGrid.SelectedItem is ManageFramesViewModel.Row row)
        {
            ActionButton.IsEnabled = true;
            ActionButton.Content = _vm.GetActionLabel(row);
        }
        else
        {
            ActionButton.IsEnabled = false;
            ActionButton.Content = "Add";
        }
    }

    private void TriggerAction(ManageFramesViewModel.Row row)
    {
        if (_onAction is null)
        {
            return;
        }
        var label = _vm.GetActionLabel(row);
        var isEdit = string.Equals(label, "Edit", StringComparison.Ordinal);
        var ident = row.Identifier;
        _onAction(new Id3v2MenuEntry(label, ident, isEdit));
        Refresh();
    }

    private void SearchBox_KeyUp(object sender, KeyEventArgs e) => ApplyFilterFromSearch();

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => ApplyFilterFromSearch();

    private void EntriesGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        => UpdateActionButton();

    private void EntriesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (EntriesGrid.SelectedItem is ManageFramesViewModel.Row row)
        {
            TriggerAction(row);
        }
    }

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (EntriesGrid.SelectedItem is ManageFramesViewModel.Row row)
        {
            TriggerAction(row);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
