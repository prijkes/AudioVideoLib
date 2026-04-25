# Matroska / WebM

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
