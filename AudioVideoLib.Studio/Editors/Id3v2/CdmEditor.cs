namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2CompressedDataMetaFrame),
    Category = Id3v2FrameCategory.Containers,
    MenuLabel = "Compressed data meta (CDM)",
    Order = 32,
    SupportedVersions = Id3v2VersionMask.V221,
    IsUniqueInstance = false,
    KnownIdentifier = "CDM")]
public sealed class CdmEditor : WrapperEditorBase<Id3v2CompressedDataMetaFrame>
{
    public override Id3v2CompressedDataMetaFrame CreateNew(object tag) => new();

    // Snapshot is populated by the dispatch caller via IWrapperEditor.OnBeforeEdit
    // (which also resets SelectedChild on the base class). No Load step: this editor
    // has no view-model state to seed from the frame.
    public override bool Edit(Window owner, Id3v2CompressedDataMetaFrame frame)
        => EditorDialog.Run<CdmEditorDialog, Id3v2CompressedDataMetaFrame>(
            owner, frame, this, static _ => { }, Save);

    public void Save(Id3v2CompressedDataMetaFrame f)
    {
        if (SelectedChild is null)
        {
            return;
        }
        // Wrap the child's data block. WrapperEditorBase.OnAfterEdit (called by
        // MainWindow.DispatchEdit after this returns) removes SelectedChild from
        // the tag so the wrapper is the sole carrier of those bytes.
        // Per ID3v2.2.1 spec the only defined compression is ZLib.
        f.CompressionMethod = Id3v2CompressionMethod.ZLib;
        f.CompressedFrame = SelectedChild.Data ?? [];
    }

    public override bool Validate(out string? error)
    {
        if (SelectedChild is null)
        {
            error = "Select a frame to wrap with CDM compression.";
            return false;
        }
        error = null;
        return true;
    }
}
