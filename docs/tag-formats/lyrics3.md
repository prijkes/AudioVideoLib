# Lyrics3

**Spec:** [id3.org/Lyrics3v2](http://id3.org/Lyrics3v2).

**Shape:** v1 is a simple "LYRICS"…"LYRICSEND" wrapper at end-of-file
(just before any ID3v1). v2 adds typed fields (IND, LYR, INF, AUT, EAL,
EAR, ETT, IMG) and a final 9-byte length+footer.

**Two distinct types:** v1 surfaces as `Lyrics3Tag` and only exposes a
single `Lyrics` string. v2 surfaces as `Lyrics3v2Tag` and exposes
strongly-typed accessors for each known field (`ExtendedTrackTitle`,
`ExtendedArtistName`, `ExtendedAlbumName`, `Lyrics`, `Genre`,
`AdditionalInformation`, `LyricsAuthorName`, `ImageFile`) plus
`Fields` for everything else. Lyric lines inside a `LYR` field carry
optional timestamps via `Lyrics3v2LyricLine.TimeStamps` (a single
line can have multiple timestamps if the same lyric repeats).

```csharp
// Lyrics3 v1: just a flat lyrics string.
var v1 = tags.Select(o => o.AudioTag).OfType<Lyrics3Tag>().FirstOrDefault();
Console.WriteLine(v1?.Lyrics);
```

```csharp
// Lyrics3 v2: walk the typed fields, pull out timestamped lyric lines.
var v2 = tags.Select(o => o.AudioTag).OfType<Lyrics3v2Tag>().FirstOrDefault();
if (v2 is not null)
{
    Console.WriteLine($"{v2.ExtendedTrackTitle?.Value} – {v2.ExtendedArtistName?.Value}");
    foreach (var line in v2.Lyrics?.LyricLines ?? [])
    {
        var stamp = line.TimeStamps.FirstOrDefault();
        Console.WriteLine($"  {stamp:hh\\:mm\\:ss}  {line.LyricLine}");
    }
}
```

```csharp
// Build a v2 tag from scratch.
var v2 = new Lyrics3v2Tag
{
    ExtendedTrackTitle = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.ExtendedTrackTitle)
    {
        Value = "A song with a title longer than the ID3v1 30-byte cap",
    },
    LyricsAuthorName = new Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier.LyricsAuthorName)
    {
        Value = "Unknown",
    },
};
File.WriteAllBytes("tag.lyrics3v2", v2.ToByteArray());
```
