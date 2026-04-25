# ASF / WMA

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
