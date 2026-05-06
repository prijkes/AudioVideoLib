namespace AudioVideoLib.Studio.Editors;

using System.Collections.ObjectModel;
using System.Windows;

public abstract class CollectionEditorBase<TFrame, TRow> : ITagItemEditor<TFrame>
{
    public ObservableCollection<TRow> Entries { get; } = [];

    public void AddRow(TRow row) => Entries.Add(row);

    public void RemoveRow(int index)
    {
        if (index >= 0 && index < Entries.Count)
        {
            Entries.RemoveAt(index);
        }
    }

    public void MoveUp(int index)
    {
        if (index <= 0 || index >= Entries.Count)
        {
            return;
        }
        (Entries[index - 1], Entries[index]) = (Entries[index], Entries[index - 1]);
    }

    public void MoveDown(int index)
    {
        if (index < 0 || index >= Entries.Count - 1)
        {
            return;
        }
        (Entries[index], Entries[index + 1]) = (Entries[index + 1], Entries[index]);
    }

    public abstract TFrame CreateNew(object tag);
    public abstract bool Edit(Window owner, TFrame frame);
    public abstract void LoadRows(TFrame frame);
    public abstract void SaveRows(TFrame frame);
    public abstract bool Validate(out string? error);
}
