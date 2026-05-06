namespace AudioVideoLib.Studio.Editors;

using System;
using System.Collections.Generic;
using System.Windows;

using AudioVideoLib.Tags;

/// <summary>
/// Marker interface that lets non-generic dispatch code (MainWindow,
/// future automation) feed a wrapper editor its tag context BEFORE
/// the dialog opens, without reflection or DataContext fishing.
/// </summary>
public interface IWrapperEditor
{
    void OnBeforeEdit(Id3v2Tag tag, Id3v2Frame self);
}

public abstract class WrapperEditorBase<TFrame> : ITagItemEditor<TFrame>, IWrapperEditor
    where TFrame : Id3v2Frame
{
    public IReadOnlyList<Id3v2Frame> WrappableSnapshot { get; private set; } = [];
    public Id3v2Frame? SelectedChild { get; set; }

    public void TakeSnapshot(Id3v2Tag tag, TFrame self)
    {
        ArgumentNullException.ThrowIfNull(tag);

        var list = new List<Id3v2Frame>();
        foreach (var f in tag.Frames)
        {
            if (ReferenceEquals(f, self))
            {
                continue;
            }
            if (f is Id3v2CompressedDataMetaFrame or Id3v2EncryptedMetaFrame)
            {
                continue;
            }
            list.Add(f);
        }
        WrappableSnapshot = list;
    }

    public virtual void OnBeforeEdit(Id3v2Tag tag, Id3v2Frame self) => TakeSnapshot(tag, (TFrame)self);

    public abstract TFrame CreateNew(object tag);
    public abstract bool Edit(Window owner, TFrame frame);
    public abstract bool Validate(out string? error);
}
