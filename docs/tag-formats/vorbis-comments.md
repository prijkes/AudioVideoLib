# Vorbis Comments

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
