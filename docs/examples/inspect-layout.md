# Inspect a file's tag and container layout

Prints every tag and container the library found, with their byte
ranges. Useful for sanity-checking a misbehaving file (overlapping
tags, unexpected APE-at-start, junk bytes between tags) without
loading the file in a hex editor. Demonstrates `IAudioTagOffset` and
`IMediaContainer.StartOffset` / `EndOffset`.

```csharp
using System;
using System.IO;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: layout <file>");
    return 1;
}

using var fs = File.OpenRead(args[0]);
var tags = AudioTags.ReadStream(fs);

fs.Position = 0;
using var streams = MediaContainers.ReadStream(fs);

Console.WriteLine($"== {Path.GetFileName(args[0])} ({fs.Length:N0} bytes) ==");

foreach (var offset in tags)
{
    var size = offset.EndOffset - offset.StartOffset;
    Console.WriteLine(
        $"  TAG  0x{offset.StartOffset:X10}..0x{offset.EndOffset:X10}  " +
        $"{size,8:N0} B  {offset.AudioTag.GetType().Name} ({offset.TagOrigin})");
}

foreach (var s in streams)
{
    Console.WriteLine(
        $"  CONT 0x{s.StartOffset:X10}..0x{s.EndOffset:X10}  " +
        $"{s.TotalMediaSize,8:N0} B  {s.GetType().Name}, {s.TotalDuration:N0} ms");
}

return 0;
```

Sample output:

```
== example.mp3 (4,521,830 bytes) ==
  TAG  0x0000000000..0x0000001234   4,660 B  Id3v2Tag (Start)
  CONT 0x0000001234..0x000044F1B6  4,517,026 B  MpaStream, 188,016 ms
  TAG  0x000044F1B6..0x000044F236     128 B  Id3v1Tag (End)
```
