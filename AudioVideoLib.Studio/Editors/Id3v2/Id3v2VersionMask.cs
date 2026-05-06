namespace AudioVideoLib.Studio.Editors.Id3v2;

using System;
using AudioVideoLib.Tags;

[Flags]
public enum Id3v2VersionMask
{
    None = 0,
    V220 = 1 << 0,
    V221 = 1 << 1,    // distinct from V220 because Id3v2CompressedDataMetaFrame (CDM) is hard-coded to v2.2.1
    V230 = 1 << 2,
    V240 = 1 << 3,
    All  = V220 | V221 | V230 | V240,
}

public static class Id3v2VersionMaskExtensions
{
    public static Id3v2VersionMask ToMask(this Id3v2Version version) => version switch
    {
        Id3v2Version.Id3v220 => Id3v2VersionMask.V220,
        Id3v2Version.Id3v221 => Id3v2VersionMask.V221,
        Id3v2Version.Id3v230 => Id3v2VersionMask.V230,
        Id3v2Version.Id3v240 => Id3v2VersionMask.V240,
        _ => Id3v2VersionMask.None,
    };

    public static bool Contains(this Id3v2VersionMask mask, Id3v2Version version)
        => (mask & version.ToMask()) != Id3v2VersionMask.None;
}
