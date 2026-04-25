# Tag formats

Each section below covers the shape of the tag, how the library exposes
it, the quirks worth knowing, and round-trip behaviour.

## ID3v1

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

## ID3v2

**Spec:** [id3.org](https://id3.org) — 2.2.0 (1998), 2.3.0 (1999),
2.4.0 (2000).

**Shape:** Frames prefixed with a 10-byte header (6 bytes for 2.2).
Each frame has an identifier ("TIT2", "APIC", …), a flags word, and a
payload whose layout is identifier-specific.

**Encodings:** Per-frame, one of Latin-1, UTF-16LE with BOM, UTF-16BE,
UTF-8 (2.4+ only).

**Parse errors don't abort the tag:** `Id3v2TagReader.FrameParseError`
(and the bubbling `AudioTags.Id3v2FrameParseError`) fire for each frame
that can't be constructed. The walker skips that frame and continues.

**Unsynchronization:** 2.3 and 2.4 support `$FF $00` escaping to keep
the tag from being mistaken for an MPEG frame sync. The library
resynchronizes on read and re-unsynchronizes on write when the flag is
set.

**Extended header CRC:** 2.3 and 2.4 can carry a CRC-32 over the frame
data. `Id3v2TagReader` validates it on read; `Id3v2Tag.ToByteArray()`
recomputes it on write.

**Language codes:** Frames with a language subfield (`COMM`, `USLT`,
etc.) accept both ISO 639-2/B (bibliographic, e.g. `dut`) and
ISO 639-2/T (terminological, e.g. `nld`). The library also accepts
`XXX` (unknown) per 2.4+.

```csharp
// Read the well-known text frames; properties hide the version-specific
// identifier mapping (TIT2 / TT2, TYER vs TDRC, etc.).
var id3v2 = tags.Select(o => o.AudioTag).OfType<Id3v2Tag>().First();
var title  = id3v2.TrackTitle?.Values.FirstOrDefault();
var artist = id3v2.Artist?.Values.FirstOrDefault();
var year   = id3v2.RecordingTime?.Values.FirstOrDefault()    // 2.4
          ?? id3v2.YearRecording?.Values.FirstOrDefault();   // 2.2 / 2.3
```

```csharp
// Set a TIT2, asking for UTF-8 encoding (only honoured by 2.4+).
var title = new Id3v2TextFrame(id3v2.Version, "TIT2") { TextEncoding = Id3v2FrameEncodingType.UTF8 };
title.Values.Add("Track 1");
id3v2.TrackTitle = title;
```

```csharp
// Bulk-convert every text frame in a 2.4 tag to UTF-8.
if (id3v2.Version >= Id3v2Version.Id3v240)
{
    foreach (var frame in id3v2.GetFrames<Id3v2TextFrame>())
    {
        frame.TextEncoding = Id3v2FrameEncodingType.UTF8;
    }
}
```

```csharp
// Extract every embedded picture.
foreach (var pic in id3v2.AttachedPictures)
{
    var ext = pic.ImageFormat.Split('/').Last();
    File.WriteAllBytes($"cover-{pic.PictureType}.{ext}", pic.PictureData);
}
```

## APE

**Spec:** [hydrogenaudio wiki](https://wiki.hydrogenaud.io/index.php?title=APE_tag) —
v1 (APE Monkey's Audio, 1999), v2 (2002).

**Shape:** Either at file end (APEv1 / APEv2) or file start (APEv2 only).
32-byte header + items + 32-byte footer (header is optional in v2).

**Items:** Key (ASCII, 2–255 chars) + value. Value type is one of
UTF-8 text (default), binary (cover art, raw bytes), or locator (URL
redirect).

**Multiple APE tags:** legal (and seen in the wild — two APEv2 at the
end, sometimes from a botched re-tag). Both surface as separate
`ApeTag` instances in the offset list; the Studio shows them as
`APEv2 (1)`, `APEv2 (2)`.

```csharp
// Walk every APE item, printing key + type + (for UTF-8 items) values.
var ape = tags.Select(o => o.AudioTag).OfType<ApeTag>().First();
foreach (var item in ape.Items)
{
    Console.WriteLine($"{item.Key} ({item.ItemType})");
    if (item is ApeUtf8Item utf8)
    {
        Console.WriteLine($"  {string.Join(" / ", utf8.Values)}");
    }
}

// Add (or replace) a UTF-8 item.
var album = new ApeUtf8Item(ape.Version, ApeItemKey.AlbumName);
album.Values.Add("Hello");
ape.SetItem(album);

// Add a binary cover-art item.
var cover = new ApeBinaryItem(ape.Version, "Cover Art (Front)") { Data = jpegBytes };
ape.SetItem(cover);
```

## Lyrics3

**Spec:** [id3.org/Lyrics3v2](http://id3.org/Lyrics3v2).

**Shape:** v1 is a simple "LYRICS"…"LYRICSEND" wrapper at end-of-file
(just before any ID3v1). v2 adds typed fields (IND, LYR, INF, AUT, EAL,
EAR, ETT, IMG) and a final 9-byte length+footer.

## MusicMatch

**Shape:** 8 KB-plus trailing block with fixed-size text fields (title,
artist, album, tempo, mood, situation…), free-text URLs, creation
date, track number, image offset / extension / binary. Not editable in
the Studio; read-only.

## Vorbis Comments

**Spec:** [xiph.org](https://xiph.org/vorbis/doc/v-comment.html).

**Shape:** Vendor string + N (name, value) entries. Always UTF-8.
Appears standalone in OGG streams and inside FLAC's
`VORBIS_COMMENT` metadata block.

Exposed via `VorbisComments` inside `FlacVorbisCommentsMetadataBlock`
for FLAC files, and as the standalone `VorbisComments.ReadStream` parser
for OGG payloads (the OGG walker itself currently only surfaces pages
and codec metadata — the comment packet is reachable through
`flac.VorbisCommentsMetadataBlock?.VorbisComments` for FLAC files).

```csharp
// FLAC: replace the TITLE entries inside the VORBIS_COMMENT metadata block.
var flac = streams.OfType<FlacStream>().First();
var vc = flac.VorbisCommentsMetadataBlock?.VorbisComments;
if (vc is not null)
{
    for (var i = vc.Comments.Count - 1; i >= 0; i--)
    {
        if (string.Equals(vc.Comments[i].Name, "TITLE", StringComparison.OrdinalIgnoreCase))
        {
            vc.Comments.RemoveAt(i);
        }
    }
    vc.Comments.Add(new VorbisComment { Name = "TITLE", Value = "New title" });
}
File.WriteAllBytes("out.flac", flac.ToByteArray());
```

## MP4 / iTunes ilst

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
`stco`/`co64`. The library doesn't rewrite those tables — document
this with the [round-trip notes](round-trip.md).

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

## ASF / WMA

**Spec:** Microsoft ASF spec.

**Shape:** Header Object contains a Content Description Object (fixed
schema: Title, Author, Copyright, Description, Rating), an Extended
Content Description Object (typed key/value pairs), and — inside the
Header Extension Object — Metadata Object and Metadata Library Object
(per-stream typed key/value pairs).

**Typed values:** `AsfTypedValue` is a tagged union over Unicode
string, byte array, BOOL (32-bit on disk), DWORD, QWORD, WORD.

**GUIDs:** `Guid.ToByteArray()` produces ASF's mixed-endian layout
(three LE groups + one BE group) natively, so no byte-swap is needed.

**Writing:** `AsfStream.ToByteArray()` splices replacement CDO / ECDO
objects into the Header Object by GUID. Existing MO / MLO objects are
preserved verbatim.

```csharp
var asf = streams.OfType<AsfStream>().Single();
var meta = asf.MetadataTag;

Console.WriteLine($"{meta.Title} – {meta.Author}");
foreach (var (name, value) in meta.ExtendedItems)
{
    Console.WriteLine($"  {name} ({value.Type}) = {value.AsString ?? value.AsDword.ToString()}");
}

// Append two ECDO items, one string and one DWORD.
meta.AddExtended("WM/Mood", AsfTypedValue.FromString("Energetic"));
meta.AddExtended("WM/BeatsPerMinute", AsfTypedValue.FromDword(128));

File.WriteAllBytes("out.wma", asf.ToByteArray());
```

## Matroska / WebM

**Spec:** [matroska.org](https://matroska.org).

**Shape:** EBML container. Metadata lives inside `Segment.Tags`, which
holds zero or more `Tag` elements. Each `Tag` has a `Targets`
(TargetTypeValue: 30 = track, 50 = album, etc., plus UID lists) and
zero or more `SimpleTag` children (Name / Value or Binary, with
optional nested `SimpleTag`s).

**Walker scope:** Only walks the first Segment. Skips `Cluster` and
`BlockGroup` (too large to descend for metadata purposes). Duration
comes from `Segment.Info.Duration × TimecodeScale`.

**Writing:** `MatroskaStream.ToByteArray()` splices the new Tags
element in place while preserving the segment's size VINT length, so
Cluster / Cues offsets stay valid.

```csharp
var mkv = streams.OfType<MatroskaStream>().Single();

foreach (var entry in mkv.Tag.Entries)
{
    Console.WriteLine($"target {entry.Targets.TargetTypeValue} ({entry.Targets.TargetType})");
    foreach (var st in entry.SimpleTags)
    {
        Console.WriteLine($"  {st.Name} [{st.Language}] = {st.Value}");
    }
}

// Append a track-level title applied to track UID 1.
var trackEntry = new MatroskaTagEntry
{
    Targets = { TargetTypeValue = MatroskaTag.TrackLevel, TargetType = "TRACK" },
};
trackEntry.Targets.TrackUids.Add(1);
trackEntry.SimpleTags.Add(new MatroskaSimpleTag { Name = "TITLE", Value = "Bonus" });
mkv.Tag.Entries.Add(trackEntry);

File.WriteAllBytes("out.mkv", mkv.ToByteArray());
```

## WAV / AIFF container-level metadata

- **WAV `LIST` chunk with form-type `INFO`:** ASCII key/value
  (`INAM`/`IART`/`IPRD`/`ICRD`/`ICMT`/`IGNR`/`ITRK`/`IENG`/`ISFT`/`ICOP`).
  Multiple `LIST INFO` chunks merge in file order.
- **WAV `bext` (BWF):** broadcast metadata — description, originator,
  origination date / time, time reference, UMID, loudness fields (v1+),
  coding history. Surfaced as `RiffStream.BextChunk`.
- **WAV `iXML`:** UTF-8 XML for film/audio production. Raw payload
  preserved for byte-identical round-trip.
- **WAV `id3 ` chunk:** full ID3v2 tag — parsed via the existing
  `Id3v2TagReader` and exposed as `RiffStream.EmbeddedId3v2`.
- **AIFF `NAME` / `AUTH` / `ANNO` / `COMT`:** ASCII text (or, per spec,
  Mac Roman). Exposed as `AiffStream.TextChunks` with typed
  `Name`, `Author`, `Annotation`, and `Comments` (timestamped).

```csharp
// All four WAV side-channels surface as their own properties on RiffStream.
var riff = streams.OfType<RiffStream>().Single();

if (riff.InfoTag is { } info)
{
    Console.WriteLine($"INFO {info.Title} / {info.Artist} ({info.CreationDate})");
}
if (riff.BextChunk is { } bext)
{
    Console.WriteLine($"BWF {bext.Description} – {bext.Originator} on {bext.OriginationDate}");
}
if (riff.IxmlChunk is { IsWellFormed: true } ixml)
{
    Console.WriteLine($"iXML PROJECT={ixml.ProjectName} SCENE={ixml.SceneName}");
}
if (riff.EmbeddedId3v2?.AudioTag is Id3v2Tag id3v2)
{
    Console.WriteLine($"ID3v2 in 'id3 ' chunk: {id3v2.TrackTitle?.Values.FirstOrDefault()}");
}
```

```csharp
// AIFF text chunks.
var aiff = streams.OfType<AiffStream>().Single();
if (aiff.TextChunks is { IsEmpty: false } text)
{
    Console.WriteLine($"NAME: {text.Name}");
    Console.WriteLine($"AUTH: {text.Author}");
    Console.WriteLine($"ANNO: {text.Annotation}");
    foreach (var c in text.Comments)
    {
        Console.WriteLine($"  COMT @ {c.TimeStampUtc:u}: {c.Text}");
    }
}
```

## DSF / DFF (DSD audio)

- **DSF** (Sony): DSD / fmt / data chunks (LE), optional ID3v2 metadata
  pointed to by the DSD chunk's metadata pointer field. Exposed as
  `DsfStream.EmbeddedId3v2`.
- **DFF** (Philips): FRM8 form (BE) with FVER / PROP / DSD / optional
  DIIN / COMT / ID3 sub-chunks. Exposed as `DffStream.EmbeddedId3v2`.

```csharp
// DSF / DFF expose any ID3v2 inside the container directly as a fully-parsed Id3v2Tag.
foreach (var dsf in streams.OfType<DsfStream>())
{
    Console.WriteLine($"DSF: {dsf.EmbeddedId3v2?.TrackTitle?.Values.FirstOrDefault()}");
}
foreach (var dff in streams.OfType<DffStream>())
{
    Console.WriteLine($"DFF: {dff.EmbeddedId3v2?.TrackTitle?.Values.FirstOrDefault()}");
}
```
