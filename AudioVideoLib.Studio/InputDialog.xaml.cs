namespace AudioVideoLib.Studio;

using System.Windows;

public partial class InputDialog : Window
{
    public InputDialog()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ValueText.Focus();
            ValueText.SelectAll();
        };
    }

    public string Prompt
    {
        get => PromptText.Text;
        set => PromptText.Text = value;
    }

    public string Value
    {
        get => ValueText.Text;
        set => ValueText.Text = value;
    }

    public static string? Ask(Window owner, string title, string prompt, string initial = "")
    {
        var dlg = new InputDialog
        {
            Title = title,
            Prompt = prompt,
            Value = initial,
            Owner = owner,
        };
        return dlg.ShowDialog() == true ? dlg.Value : null;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
