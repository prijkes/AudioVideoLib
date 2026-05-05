# Extending

## Add a new tag format at a flat byte position

For tags that live at the start or end of a file (like ID3v1 / APE /
Lyrics3), implement `IAudioTagReader` and register it with
`AudioTags`.

```csharp
public sealed class MyCustomTagReader : IAudioTagReader
{
    public IAudioTagOffset? ReadFromStream(Stream stream, TagOrigin tagOrigin)
    {
        // Probe the bytes at the given origin; return null if not a match.
        // On a match, build a MyCustomTag and return an AudioTagOffset.
    }
}

public sealed class MyCustomTag : IAudioTag
{
    public bool Equals(IAudioTag? other) { /* ... */ }

    // Implementers override WriteTo(Stream); ToByteArray() is a free
    // extension method (IAudioTagExtensions) that buffers the result.
    public void WriteTo(Stream destination) { /* serialize self into destination */ }
}
```

Register at runtime:

```csharp
var tags = new AudioTags();
tags.AddReader<MyCustomTagReader, MyCustomTag>();
tags.ReadTags(stream);
```

## Add a new container walker

For container formats where metadata lives inside the structure (MP4,
ASF, Matroska-style, FLAC/MPA, the format-pack walkers), implement
`IMediaContainer`. The interface extends `IDisposable`. Splice-style
walkers — `Mp4Stream`, `MatroskaStream`, `AsfStream`, the retrofitted
`FlacStream` / `MpaStream`, and the format-pack additions `MpcStream`,
`WavPackStream`, `TtaStream`, `MacStream` — hold an `ISourceReader`
populated at `ReadStream` time and consumed at `WriteTo` time:

```csharp
public sealed class FoobarStream : IMediaContainer, IDisposable
{
    private ISourceReader? _source;

    public long StartOffset { get; private set; }
    public long EndOffset { get; private set; }
    public long TotalDuration { get; private set; }
    public long TotalMediaSize => EndOffset - StartOffset;
    public int MaxFrameSpacingLength { get; set; }

    public bool ReadStream(Stream stream)
    {
        // 1. Detect magic at stream.Position. Return false if not your format.
        // 2. Capture stream position; build _source = new StreamSourceReader(stream, leaveOpen: true).
        // 3. Walk the format, populating per-frame offset/length lists.
        // 4. Set StartOffset, EndOffset, TotalDuration. Return true.
    }

    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_source is null)
        {
            throw new InvalidOperationException(
                "Source stream was detached or never read. WriteTo requires a live source.");
        }
        // Splice unchanged byte ranges from _source to destination.
        // For metadata edits, emit the modified region from the parsed model;
        // for everything else, _source.CopyTo(offset, length, destination).
    }

    public void Dispose() { _source?.Dispose(); _source = null; }
}
```

Key points:

- The walker holds an `ISourceReader` populated at `ReadStream` time
  and consumed at `WriteTo` time. The caller must keep the source
  `Stream` alive between `ReadStream` and `WriteTo` — the walker reads
  from it on demand to splice unchanged byte ranges. Wrap callers in a
  `using` block that covers both calls.
- `WriteTo` throws `InvalidOperationException` with the canonical
  message `"Source stream was detached or never read. WriteTo requires
  a live source."` if `_source` is null. This matches the
  `IMediaContainer` xmldoc contract.
- Audio is byte-passthrough: per-frame audio bytes are spliced
  verbatim from the source. The library does not re-encode audio
  anywhere; only the metadata region (tags, headers that need their
  size/CRC/offset fields rewritten) is emitted from the parsed model.
- Implementers override `void WriteTo(Stream destination)`. The
  buffer-shaped helper `byte[] ToByteArray()` is an extension method on
  `IMediaContainerExtensions` that callers get for free; concrete
  classes may also expose an instance `ToByteArray()` for a faster
  direct path (see the existing splice rewriters for examples).

## Add an ID3v2 frame type

Subclass `Id3v2Frame`, override the virtual members, and register its
identifier in `Id3v2FrameHelpers`.

```csharp
public sealed class Id3v2MyFrame : Id3v2Frame
{
    public Id3v2MyFrame() : base(Id3v2Version.Id3v240) { }

    public override string? Identifier => "MY01";

    public override bool IsVersionSupported(Id3v2Version version) =>
        version >= Id3v2Version.Id3v230;

    public override byte[] Data
    {
        get => /* serialize your model to bytes */;
        protected set => /* parse bytes into your model */;
    }
}
```

Match the existing frame subclasses in `AudioVideoLib/Tags/Id3v2*.cs`
for the conventions on encoding handling, null-tolerance, and equality.

## Style conventions

- File-scoped namespace.
- `is not null` / pattern matching over `!= null`.
- Expression-bodied members where natural.
- `[]` collection expressions.
- Primary constructors for simple records / data classes.
- Readers tolerate malformed input — return `null` / `false` or raise
  a parse-error event; don't throw on bad bytes.
- Bounds-check all length fields against the enclosing container.
- Public API gets XML docs.

## Tests

Every format ships with an xUnit test file under
`AudioVideoLib.Tests/`. The convention for new formats:

- Synthesize inputs inline with `BinaryWriter` / `MemoryStream` so
  tests don't depend on bundled binaries.
- Cover the happy path, the malformed-input path, and — if the format
  supports writing — at least one byte-identical round-trip assertion.

See `Mp4MetaTagTests.cs`, `AsfMetadataTagTests.cs`, and
`MatroskaTagTests.cs` for reference layouts.
