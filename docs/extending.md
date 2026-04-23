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
    public byte[] ToByteArray() { /* ... */ }
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
ASF, Matroska-style), implement `IAudioStream`:

```csharp
public sealed class MyContainerStream : IAudioStream
{
    public long StartOffset { get; private set; }
    public long EndOffset { get; private set; }
    public long TotalAudioLength { get; /* ms */ }
    public long TotalAudioSize { get; /* bytes */ }
    public int MaxFrameSpacingLength { get; set; } = 0;

    public bool ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        // Probe; return false if not our format.
        // Otherwise walk the structure, populating your own properties.
    }

    public byte[] ToByteArray() => /* serialize */;
}
```

Register with the `AudioStreams` factory — add your type to the
dictionary in `AudioVideoLib/IO/AudioStreams.cs`:

```csharp
private readonly Dictionary<Type, Func<IAudioStream>> _supportedStreams = new()
{
    // ...
    { typeof(MyContainerStream), () => new MyContainerStream() },
};
```

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
