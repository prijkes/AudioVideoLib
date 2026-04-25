# MP4 / iTunes ilst

**Spec:** ISO/IEC 14496-12 base + Apple's iTunes metadata convention.

**Shape:** Nested atoms under `moov.udta.meta.ilst`. Each ilst child is
a container atom whose four-CC is the field identifier
(`©nam` = Title, `©ART` = Artist, `covr` = cover art, …), with a `data`
sub-atom holding the actual payload.

**Strongly-typed properties:** `Mp4MetaTag.Title`, `Artist`, `Album`,
`AlbumArtist`, `Year`, `Genre`, `Composer`, `Comment`, `Tool`,
`TrackNumber`/`TrackTotal`, `DiscNumber`/`DiscTotal`, `Bpm`,
`Compilation`, `CoverArt` (list), plus a `FreeFormItems` dictionary
keyed by `(mean, name)` for `----` atoms (e.g. MusicBrainz IDs).

**`covr`:** Holds JPEG or PNG bytes — format detected from the magic.

**Writing:** `Mp4Stream.ToByteArray()` splices the new ilst into the
existing `moov.udta.meta` chain, patching enclosing atom sizes as
needed. Audio offsets are preserved if `mdat` is before `moov`; if
`moov` is before `mdat`, growing or shrinking `moov` would invalidate
`stco`/`co64`. The library doesn't rewrite those tables — see the
[round-trip notes](../round-trip.md) for the exact semantics.

```csharp
var mp4 = streams.OfType<Mp4Stream>().Single();
var tag = mp4.Tag;

Console.WriteLine($"{tag.Title} – {tag.Artist} [{tag.Album}] {tag.Year}");
Console.WriteLine($"track {tag.TrackNumber}/{tag.TrackTotal}, BPM {tag.Bpm}");

tag.Title = "New title";
tag.SetFreeFormItem("com.apple.iTunes", "MusicBrainz Track Id", "guid-here");

foreach (var cover in tag.CoverArt)
{
    File.WriteAllBytes($"cover.{cover.Format.ToString().ToLowerInvariant()}", cover.Data);
}

File.WriteAllBytes("out.m4a", mp4.ToByteArray());
```
