namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using AudioVideoLib.Studio.Editors;

public sealed class Id3v2FrameEditorAttribute(Type frameType) : TagItemEditorAttribute(frameType)
{
    public Id3v2FrameCategory Category { get; init; }
    public Id3v2VersionMask SupportedVersions { get; init; } = Id3v2VersionMask.All;
    public bool IsUniqueInstance { get; init; }

    /// <summary>
    /// Used when the frame class lacks an `(Id3v2Version)` ctor and reflection-based
    /// identifier resolution would fail. Required for `Id3v2CompressedDataMetaFrame` (CDM)
    /// and `Id3v2EncryptedMetaFrame` (CRM); optional for everything else.
    /// </summary>
    public string? KnownIdentifier { get; init; }
}
