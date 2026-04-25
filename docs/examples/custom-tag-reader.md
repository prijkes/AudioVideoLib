# Custom tag format: end-to-end reader + writer

`AudioTags.AddReader<TR, TT>()` plugs any third-party tag format into
the same scanner the built-in formats (ID3v1, ID3v2, APE, Lyrics3,
MusicMatch) use. This page walks through a complete example: a tiny
**ProcessedStamp** tag that lives at the end of the file and records
which tool last touched the file, when, and a free-text note. Useful
for pipeline auditing.

## On-disk format

| Offset (from start of tag) | Bytes | Field |
|---|---|---|
| 0 | 4 | magic — ASCII `"PRCS"` |
| 4 | 1 | version — currently `1` |
| 5 | 1 | tool-name length (`0..255`) |
| 6 | n | tool name (UTF-8) |
| 6+n | 1 | note length (`0..255`) |
| 7+n | m | note (UTF-8) |
| 7+n+m | 8 | timestamp UTC — Unix epoch milliseconds, big-endian |
| 15+n+m | 4 | total size — total tag length in bytes, big-endian |

The total-size suffix is what the reader probes for: pull the last
four bytes, seek backward by that amount, check the magic, parse.

## The tag

```csharp
using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

using AudioVideoLib.Tags;

public sealed class ProcessedStamp : IAudioTag
{
    public const int Version = 1;
    public static readonly byte[] Magic = "PRCS"u8.ToArray();

    public string ToolName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;

    public bool Equals(IAudioTag? other) =>
        other is ProcessedStamp s
        && s.ToolName == ToolName
        && s.Note == Note
        && s.TimestampUtc.ToUnixTimeMilliseconds() == TimestampUtc.ToUnixTimeMilliseconds();

    public byte[] ToByteArray()
    {
        var toolBytes = Encoding.UTF8.GetBytes(ToolName);
        var noteBytes = Encoding.UTF8.GetBytes(Note);
        if (toolBytes.Length > 255) throw new InvalidOperationException("ToolName too long");
        if (noteBytes.Length > 255) throw new InvalidOperationException("Note too long");

        var totalSize = 4 + 1 + 1 + toolBytes.Length + 1 + noteBytes.Length + 8 + 4;
        var buf = new byte[totalSize];
        var pos = 0;

        Magic.CopyTo(buf, pos);            pos += 4;
        buf[pos++] = Version;
        buf[pos++] = (byte)toolBytes.Length;
        toolBytes.CopyTo(buf, pos);        pos += toolBytes.Length;
        buf[pos++] = (byte)noteBytes.Length;
        noteBytes.CopyTo(buf, pos);        pos += noteBytes.Length;

        BinaryPrimitives.WriteInt64BigEndian(buf.AsSpan(pos), TimestampUtc.ToUnixTimeMilliseconds());
        pos += 8;
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(pos), totalSize);

        return buf;
    }
}
```

## The reader

```csharp
public sealed class ProcessedStampReader : IAudioTagReader
{
    public IAudioTagOffset? ReadFromStream(Stream stream, TagOrigin tagOrigin)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (tagOrigin != TagOrigin.End) return null;            // end-of-file only

        var pos = stream.Position;
        if (pos < 4 + 1 + 2 + 8 + 4) return null;               // smallest possible tag

        // Read the trailing 4-byte total-size field.
        stream.Position = pos - 4;
        Span<byte> sizeBytes = stackalloc byte[4];
        if (stream.Read(sizeBytes) != 4) return null;
        var totalSize = BinaryPrimitives.ReadInt32BigEndian(sizeBytes);
        if (totalSize < 4 + 1 + 2 + 8 + 4 || totalSize > pos) return null;

        // Seek back to the start of the tag and verify the magic.
        var startOffset = pos - totalSize;
        stream.Position = startOffset;
        Span<byte> magicBytes = stackalloc byte[4];
        if (stream.Read(magicBytes) != 4) return null;
        if (!magicBytes.SequenceEqual(ProcessedStamp.Magic)) return null;

        var version = stream.ReadByte();
        if (version != ProcessedStamp.Version) return null;

        var toolLen = stream.ReadByte();
        if (toolLen < 0) return null;
        var toolBuf = new byte[toolLen];
        if (stream.Read(toolBuf) != toolLen) return null;

        var noteLen = stream.ReadByte();
        if (noteLen < 0) return null;
        var noteBuf = new byte[noteLen];
        if (stream.Read(noteBuf) != noteLen) return null;

        Span<byte> tsBytes = stackalloc byte[8];
        if (stream.Read(tsBytes) != 8) return null;
        var ts = BinaryPrimitives.ReadInt64BigEndian(tsBytes);

        var stamp = new ProcessedStamp
        {
            ToolName = Encoding.UTF8.GetString(toolBuf),
            Note = Encoding.UTF8.GetString(noteBuf),
            TimestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(ts),
        };

        return new AudioTagOffset(tagOrigin, startOffset, pos, stamp);
    }
}
```

## The writer

`IAudioTagWriter` is currently a marker interface — the actual
serialisation lives on `IAudioTag.ToByteArray()`. Defining a type that
implements `IAudioTagWriter` is conventional for symmetry with the
reader but is not required by the scanner.

```csharp
public sealed class ProcessedStampWriter : IAudioTagWriter
{
    public byte[] ToByteArray(ProcessedStamp tag) => tag.ToByteArray();
}
```

## Wire it up and use it

Reading + appending + read-modify-write all flow through the standard
`AudioTags` / `AudioInfo` shape — once the reader is registered, the
custom tag is treated like any other end-of-file tag.

```csharp
using System;
using System.IO;
using System.Linq;

using AudioVideoLib;
using AudioVideoLib.Tags;

// 1) Read any existing ProcessedStamp from a file.
using (var fs = File.OpenRead("track.mp3"))
{
    var tags = new AudioTags();
    tags.AddReader<ProcessedStampReader, ProcessedStamp>();
    tags.ReadTags(fs);

    if (tags.Select(o => o.AudioTag).OfType<ProcessedStamp>().FirstOrDefault() is { } existing)
    {
        Console.WriteLine($"last touched by {existing.ToolName} at {existing.TimestampUtc:u}: {existing.Note}");
    }
}

// 2) Append a fresh stamp and save the file alongside.
using (var fs = File.OpenRead("track.mp3"))
{
    var info = AudioInfo.Analyse(fs);
    info.AudioTags.AddReader<ProcessedStampReader, ProcessedStamp>();

    info.AudioTags.AddTag(
        new ProcessedStamp
        {
            ToolName = "loudness-normaliser",
            Note = "EBU R128, target -23 LUFS",
            TimestampUtc = DateTimeOffset.UtcNow,
        },
        TagOrigin.End);

    info.Save("track.stamped.mp3");
}
```

> **Tag ordering at write time:** `AudioInfo.Save` writes start-origin
> tags first (sorted by `StartOffset`), then the container bytes, then
> end-origin tags. A freshly-added tag has offsets `0,0`, so when
> multiple end-origin tags exist the new one is emitted before the
> ones that already had positive offsets — adjust the `AudioTagOffset`
> if you need a specific order.

## Removing the built-in readers

Symmetrically, `AudioTags.RemoveReader<TR>()` drops a registered
reader. Useful when a custom format aliases an existing magic that
you'd rather not have probed:

```csharp
var tags = new AudioTags();
tags.RemoveReader<MusicMatchTagReader>();    // skip MusicMatch probing
tags.AddReader<ProcessedStampReader, ProcessedStamp>();
```
