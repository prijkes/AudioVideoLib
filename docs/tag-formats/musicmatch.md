# MusicMatch

**Shape:** 8 KB-plus trailing block with fixed-size text fields (title,
artist, album, tempo, mood, situation…), free-text URLs, creation
date, track number, image offset / extension / binary. Not editable in
the Studio's GUI yet; the library itself supports both read and write.

**Strongly-typed properties:** `MusicMatchTag.SongTitle`, `AlbumTitle`,
`ArtistName`, `Genre`, `Tempo`, `Mood`, `Situation`, `Preference`,
`SongDuration`, `CreationDate`, `PlayCounter`, `OriginalFilename`,
`SerialNumber`, `TrackNumber`, plus the multi-line `Notes`,
`ArtistBio`, and `Lyrics`, the URL fields `ArtistUrl`, `BuyCdUrl`,
`ArtistEmail`, and the `Image` (extension + binary).

```csharp
// Read.
var mm = tags.Select(o => o.AudioTag).OfType<MusicMatchTag>().FirstOrDefault();
if (mm is not null)
{
    Console.WriteLine($"{mm.SongTitle} – {mm.ArtistName} [{mm.AlbumTitle}]");
    Console.WriteLine($"mood={mm.Mood} tempo={mm.Tempo} situation={mm.Situation}");
    Console.WriteLine($"track {mm.TrackNumber}, plays {mm.PlayCounter}, duration {mm.SongDuration}");
}
```

```csharp
// Tweak the rating ("Preference") on every file in a directory.
foreach (var path in Directory.EnumerateFiles(dir, "*.mp3"))
{
    using var fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
    var info = AudioInfo.Analyse(fs);
    if (info.AudioTags.Select(o => o.AudioTag).OfType<MusicMatchTag>().FirstOrDefault() is { } mm)
    {
        mm.Preference = "Excellent";
        info.Save(path);
    }
}
```
