namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2TextFrame),
    Category = Id3v2FrameCategory.TextFrames,
    MenuLabel = "Text frame",
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = false)]
public sealed class TextFrameEditor : ITagItemEditor<Id3v2TextFrame>, INotifyPropertyChanged
{
    public string Identifier { get => field; set => Set(ref field, value); } = "TIT2";
    public string Value { get => field; set => Set(ref field, value); } = string.Empty;
    public Id3v2FrameEncodingType Encoding { get => field; set => Set(ref field, value); }

    public Id3v2TextFrame CreateNew(object tag)
        => throw new InvalidOperationException(
            "TextFrameEditor needs an identifier. Use CreateNew(tag, identifier) instead.");

    public Id3v2TextFrame CreateNew(object tag, string identifier)
        => new(((Id3v2Tag)tag).Version, identifier);

    public bool Edit(Window owner, Id3v2TextFrame frame)
    {
        Load(frame);
        var dialog = new TextFrameEditorDialog { Owner = owner, DataContext = this };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }
        Save(frame);
        return true;
    }

    public void Load(Id3v2TextFrame frame)
    {
        Identifier = frame.Identifier;
        Encoding = frame.TextEncoding;
        Value = string.Join("\n", frame.Values);
    }

    public void Save(Id3v2TextFrame frame)
    {
        frame.TextEncoding = Encoding;
        frame.Values.Clear();
        foreach (var raw in Value.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0)
            {
                continue;
            }
            frame.Values.Add(line);
        }
    }

    public bool Validate(out string? error)
    {
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
