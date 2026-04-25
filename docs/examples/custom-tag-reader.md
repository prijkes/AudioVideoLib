# Register a custom tag reader

`AudioTags.AddReader<TR, TT>` plugs in any `IAudioTagReader`. Useful
for proprietary headers or trailers that aren't part of the built-in
list.

```csharp
using System.IO;

using AudioVideoLib.Tags;

public sealed class MyTagReader : IAudioTagReader
{
    public IAudioTagOffset? ReadFromStream(Stream stream, TagOrigin tagOrigin)
    {
        // Probe; return null if not a match. On a match, build a MyTag and
        // return an AudioTagOffset(tagOrigin, startOffset, endOffset, tag).
        return null;
    }
}

public sealed class MyTag : IAudioTag
{
    public bool Equals(IAudioTag? other) => ReferenceEquals(this, other);
    public byte[] ToByteArray() => [];
}

// Wire it in alongside the built-in readers.
var tags = new AudioTags();
tags.AddReader<MyTagReader, MyTag>();
tags.ReadTags(File.OpenRead("track.mp3"));
```

The same hook works in reverse — call `tags.RemoveReader<TR>()` to drop
a built-in reader if you don't want, say, MusicMatch detection running
on every file.
