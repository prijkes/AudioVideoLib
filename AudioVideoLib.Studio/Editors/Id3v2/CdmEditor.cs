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

    public override bool Edit(Window owner, Id3v2CompressedDataMetaFrame frame)
    {
        // Snapshot is populated by the dispatch caller via IWrapperEditor.OnBeforeEdit.
        var dialog = new CdmEditorDialog { Owner = owner, DataContext = this };
        return dialog.ShowDialog() == true;
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
