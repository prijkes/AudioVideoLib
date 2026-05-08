namespace AudioVideoLib.Studio.Editors;

using System.Windows;

public static class EditorDialogActions
{
    /// <summary>OK handler: validates via DataContext, sets DialogResult on success.</summary>
    public static void Ok(Window dialog)
    {
        if (dialog.DataContext is IValidatedEditor v && !v.Validate(out var error))
        {
            MessageBox.Show(dialog, error, "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        dialog.DialogResult = true;
    }

    /// <summary>Cancel handler: sets DialogResult = false.</summary>
    public static void Cancel(Window dialog) => dialog.DialogResult = false;

    /// <summary>Load-from-file handler: delegates to BinaryDataEditorBase.LoadDataFromFile.</summary>
    public static void LoadFromFile(Window dialog)
    {
        if (dialog.DataContext is BinaryDataEditorBase b)
        {
            b.LoadDataFromFile(dialog);
        }
    }

    /// <summary>Clear-data handler: delegates to BinaryDataEditorBase.ClearData.</summary>
    public static void ClearData(Window dialog)
    {
        if (dialog.DataContext is BinaryDataEditorBase b)
        {
            b.ClearData();
        }
    }
}
