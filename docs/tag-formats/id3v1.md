# ID3v1

**Spec:** Eric Kemp (1996), extended in 1997 (v1.1 track field) and later
with the non-standard TAG+ enhanced block.

**Shape:** Last 128 bytes of the file. Fixed-width ASCII fields (title
30, artist 30, album 30, year 4, comment 28 or 30, genre 1). No
built-in encoding; real-world files use anything from Latin-1 to UTF-8
to Shift-JIS.

**Per-field encoding:** Because the spec doesn't specify an encoding,
AudioVideoLib stores field bytes as `byte[]` and exposes a nullable
`Encoding` per field (`TrackTitleEncoding`, `ArtistEncoding`,
`AlbumTitleEncoding`, `AlbumYearEncoding`, `TrackCommentEncoding`).
`null` means "inherit the tag-level `Encoding`". The reader attempts
strict UTF-8 decode per field; if valid *and* contains a multi-byte
sequence, it sets that field's encoding to UTF-8. Otherwise it leaves
`null` so the tag-level default applies.

`Id3v1Tag.TrackTitle` (and siblings) are string facades that decode /
encode through the effective per-field encoding. `TrackTitleRawBytes`
gives you the raw bytes directly.

**TAG+ enhanced block:** `UseExtendedTag` enables the 227-byte TAG+
preamble with extended title (+60), artist (+60), album (+60),
speed (byte), free-text genre (30), start time (6), end time (6).

```csharp
var id3v1 = tags.Select(o => o.AudioTag).OfType<Id3v1Tag>().First();
Console.WriteLine($"{id3v1.TrackTitle} – {id3v1.Artist} ({id3v1.AlbumYear})");

// Force the title to be re-encoded as Latin-1 instead of the tag-level default
// before assigning, so the byte storage and the displayed string agree.
id3v1.TrackTitleEncoding = Encoding.Latin1;
id3v1.TrackTitle = "Naïve";
File.WriteAllBytes("tag.id3v1", id3v1.ToByteArray());
```

```csharp
// A v1.1 tag with the TAG+ extended block enabled.
var id3v1 = new Id3v1Tag(Id3v1Version.Id3v11)
{
    TrackTitle = "A song with a title longer than the 30-byte v1 cap",
    Artist = "An artist with a long name",
    UseExtendedTag = true,
    TrackSpeed = Id3v1TrackSpeed.Medium,
    ExtendedTrackGenre = "Post-Hardcore",
    StartTime = TimeSpan.FromSeconds(12),
    EndTime = TimeSpan.FromMinutes(3),
};
```
