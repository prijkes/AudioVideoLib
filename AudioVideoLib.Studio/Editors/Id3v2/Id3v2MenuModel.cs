namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Collections.Generic;

public sealed record Id3v2MenuEntry(string Label, string FrameIdentifier, bool IsEditExisting);

public sealed record Id3v2MenuCategory(
    Id3v2FrameCategory Category,
    string Header,
    IReadOnlyList<Id3v2MenuEntry> Entries);

public sealed record Id3v2MenuModel(IReadOnlyList<Id3v2MenuCategory> Categories);
