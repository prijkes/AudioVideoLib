# APE

**Spec:** [hydrogenaudio wiki](https://wiki.hydrogenaud.io/index.php?title=APE_tag) —
v1 (APE Monkey's Audio, 1999), v2 (2002).

**Shape:** Either at file end (APEv1 / APEv2) or file start (APEv2 only).
32-byte header + items + 32-byte footer (header is optional in v2).

**Items:** Key (ASCII, 2–255 chars) + value. Value type is one of
UTF-8 text (default), binary (cover art, raw bytes), or locator (URL
redirect).

**Multiple APE tags:** legal (and seen in the wild — two APEv2 at the
end, sometimes from a botched re-tag). Both surface as separate
`ApeTag` instances in the offset list; the Studio shows them as
`APEv2 (1)`, `APEv2 (2)`.

```csharp
// Walk every APE item, printing key + type + (for UTF-8 items) values.
var ape = tags.Select(o => o.AudioTag).OfType<ApeTag>().First();
foreach (var item in ape.Items)
{
    Console.WriteLine($"{item.Key} ({item.ItemType})");
    if (item is ApeUtf8Item utf8)
    {
        Console.WriteLine($"  {string.Join(" / ", utf8.Values)}");
    }
}

// Add (or replace) a UTF-8 item.
var album = new ApeUtf8Item(ape.Version, ApeItemKey.AlbumName);
album.Values.Add("Hello");
ape.SetItem(album);

// Add a binary cover-art item.
var cover = new ApeBinaryItem(ape.Version, "Cover Art (Front)") { Data = jpegBytes };
ape.SetItem(cover);
```
