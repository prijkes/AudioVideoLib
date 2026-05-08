namespace AudioVideoLib.Studio.Editors;

using System;
using System.IO;
using System.Windows;

using AudioVideoLib.Studio.Mvvm;

using Microsoft.Win32;

/// <summary>
/// Adds binary-payload plumbing (Data / DataInfo / file-load / clear) to an
/// editor view-model. Derived classes specify the file dialog Title/Filter via
/// abstract overrides.
/// </summary>
public abstract class BinaryDataEditorBase : ObservableObject
{
    public byte[] Data { get => field; set => Set(ref field, value ?? []); } = [];

    /// <summary>
    /// Display string for <see cref="Data"/>. <c>virtual</c> so editors can keep
    /// their existing empty-state wording.
    /// </summary>
    public virtual string DataInfo => Data.Length == 0 ? "(no data)" : $"{Data.Length:N0} bytes";

    protected abstract string FileDialogTitle { get; }

    protected abstract string FileDialogFilter { get; }

    public void LoadDataFromFile(string path) => Data = File.ReadAllBytes(path);

    public void ClearData() => Data = [];

    internal void LoadDataFromFile(Window owner)
    {
        var dlg = new OpenFileDialog { Title = FileDialogTitle, Filter = FileDialogFilter };
        if (dlg.ShowDialog(owner) != true)
        {
            return;
        }
        try
        {
            LoadDataFromFile(dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner, $"Could not read file:\n\n{ex.Message}", "Load",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    protected override void OnPropertyChanged(string? prop)
    {
        base.OnPropertyChanged(prop);
        if (prop == nameof(Data))
        {
            base.OnPropertyChanged(nameof(DataInfo));
        }
    }
}
