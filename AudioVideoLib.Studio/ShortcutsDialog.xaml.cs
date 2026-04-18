namespace AudioVideoLib.Studio;

using System.Collections.Generic;
using System.Windows;

public partial class ShortcutsDialog : Window
{
    public ShortcutsDialog()
    {
        InitializeComponent();
        Grid.ItemsSource = Shortcuts;
    }

    public sealed record ShortcutRow(string Gesture, string Action);

    public static IReadOnlyList<ShortcutRow> Shortcuts { get; } =
    [
        new("Ctrl+O",        "Open file"),
        new("Ctrl+S",        "Save"),
        new("Ctrl+Shift+S",  "Save as..."),
        new("Ctrl+W",        "Close file"),
        new("F5",            "Refresh"),
        new("Ctrl+G",        "Go to offset..."),
        new("Ctrl+F",        "Find..."),
        new("F3",            "Find next"),
        new("Shift+F3",      "Find previous"),
        new("F1",            "Show this dialog"),
    ];

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
