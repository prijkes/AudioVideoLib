namespace AudioVideoLib.Studio.Editors;

using System;
using System.Windows;

/// <summary>
/// Hosts the load -> show dialog -> save protocol shared by every ID3v2 frame editor.
/// </summary>
public static class EditorDialog
{
    /// <summary>
    /// Loads the editor's view-model from <paramref name="frame"/>, shows a
    /// <typeparamref name="TDialog"/> with <paramref name="dataContext"/>, and on OK
    /// commits the view-model back into the frame.
    /// </summary>
    public static bool Run<TDialog, TFrame>(
        Window owner,
        TFrame frame,
        object dataContext,
        Action<TFrame> load,
        Action<TFrame> save,
        Action<TDialog>? configure = null)
        where TDialog : Window, new()
    {
        ArgumentNullException.ThrowIfNull(dataContext);
        ArgumentNullException.ThrowIfNull(load);
        ArgumentNullException.ThrowIfNull(save);

        load(frame);
        var dialog = new TDialog { Owner = owner, DataContext = dataContext };
        configure?.Invoke(dialog);
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        save(frame);
        return true;
    }
}
