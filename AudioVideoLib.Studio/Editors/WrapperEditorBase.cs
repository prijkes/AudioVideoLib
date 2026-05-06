namespace AudioVideoLib.Studio.Editors;

using System;
using System.Collections.Generic;
using System.Windows;

using AudioVideoLib.Tags;

/// <summary>
/// Marker interface that lets non-generic dispatch code (MainWindow,
/// future automation) feed a wrapper editor its tag context BEFORE
/// the dialog opens (snapshot the wrappable frames) AND AFTER it
/// commits (remove the selected child from the tag — the wrapper
/// has just absorbed its bytes), without reflection or DataContext
/// fishing.
/// </summary>
public interface IWrapperEditor
{
    void OnBeforeEdit(Id3v2Tag tag, Id3v2Frame self);

    /// <summary>
    /// Called by the dispatch path after <see cref="ITagItemEditor{TItem}.Edit"/>
    /// returns true. The wrapper has serialised the selected child's bytes into
    /// its data block; the original child must now be removed so the tag doesn't
    /// keep both copies.
    /// </summary>
    void OnAfterEdit(Id3v2Tag tag, Id3v2Frame self);
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

    public virtual void OnAfterEdit(Id3v2Tag tag, Id3v2Frame self)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (SelectedChild is { } child)
        {
            tag.RemoveFrame(child);
        }
    }

    public abstract TFrame CreateNew(object tag);
    public abstract bool Edit(Window owner, TFrame frame);
    public abstract bool Validate(out string? error);
}
