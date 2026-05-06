namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

public sealed class IplsRowVm : INotifyPropertyChanged
{
    public string Involvement { get => field; set => Set(ref field, value); } = string.Empty;
    public string Involvee { get => field; set => Set(ref field, value); } = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T storage, T value, [CallerMemberName] string? prop = null)
    {
        if (Equals(storage, value))
        {
            return;
        }
        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}

[Id3v2FrameEditor(typeof(Id3v2InvolvedPeopleListFrame),
    Category = Id3v2FrameCategory.People,
    MenuLabel = "Involved people list (IPLS)",
    Order = 19,
    SupportedVersions = Id3v2VersionMask.V220 | Id3v2VersionMask.V221 | Id3v2VersionMask.V230,
    IsUniqueInstance = true)]
public sealed class IplsEditor
    : CollectionEditorBase<Id3v2InvolvedPeopleListFrame, IplsRowVm>, INotifyPropertyChanged
{
    public Id3v2FrameEncodingType Encoding { get => field; set => Set(ref field, value); }

    public override Id3v2InvolvedPeopleListFrame CreateNew(object tag)
        => new(((Id3v2Tag)tag).Version);

    public override bool Edit(Window owner, Id3v2InvolvedPeopleListFrame frame)
    {
        Load(frame);
        var dialog = new IplsEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2InvolvedPeopleListFrame f)
    {
        Encoding = f.TextEncoding;
        LoadRows(f);
    }

    public void Save(Id3v2InvolvedPeopleListFrame f)
    {
        // Clear collection before changing encoding so its setter doesn't reject existing entries.
        f.InvolvedPeople.Clear();
        f.TextEncoding = Encoding;
        SaveRows(f);
    }

    public override void LoadRows(Id3v2InvolvedPeopleListFrame f)
    {
        Entries.Clear();
        foreach (var p in f.InvolvedPeople)
        {
            Entries.Add(new IplsRowVm { Involvement = p.Involvement ?? string.Empty, Involvee = p.Involvee ?? string.Empty });
        }
    }

    public override void SaveRows(Id3v2InvolvedPeopleListFrame f)
    {
        f.InvolvedPeople.Clear();
        foreach (var r in Entries)
        {
            f.InvolvedPeople.Add(new Id3v2InvolvedPeople(r.Involvement ?? string.Empty, r.Involvee ?? string.Empty));
        }
    }

    public override bool Validate(out string? error)
    {
        foreach (var r in Entries)
        {
            if (string.IsNullOrEmpty(r.Involvement) || string.IsNullOrEmpty(r.Involvee))
            {
                error = "Each row must have a non-empty Involvement and Involvee.";
                return false;
            }
        }
        error = null;
        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T storage, T value, [CallerMemberName] string? prop = null)
    {
        if (Equals(storage, value))
        {
            return;
        }
        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
