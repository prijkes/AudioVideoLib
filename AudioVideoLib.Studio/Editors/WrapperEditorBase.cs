namespace AudioVideoLib.Studio.Editors;

using System;
using System.Collections.Generic;
using System.Windows;

using AudioVideoLib.Tags;

public abstract class WrapperEditorBase<TFrame> : ITagItemEditor<TFrame>
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

    public abstract TFrame CreateNew(object tag);
    public abstract bool Edit(Window owner, TFrame frame);
    public abstract bool Validate(out string? error);
}
