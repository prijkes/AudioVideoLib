namespace AudioVideoLib.Tags;

/// <summary>
/// Class used to store an <see cref="ApeTag"/> item.
/// </summary>
public partial class ApeItem
{
    private static ApeItem GetItem(ApeVersion version, string key, int flags)
    {
        var itemType = (ApeItemType)((flags & HeaderFlags.ItemType) >> 1);
        return itemType switch
        {
            ApeItemType.ContainsBinary => new ApeBinaryItem(version, key),
            ApeItemType.CodedUTF8 => new ApeUtf8Item(version, key),
            ApeItemType.IsLocator => new ApeLocatorItem(version, key),
            _ => new ApeItem(version, key) { IsReadOnly = true },
        };
    }
}
