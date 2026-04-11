/*
 * Date: 2013-11-24
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://en.wikipedia.org/wiki/APE_tag
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1_specification
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2_specification
 *  http://www.monkeysaudio.com/developers.html
 */

namespace AudioVideoLib.Tags;

/// <summary>
/// Class used to store an <see cref="ApeTag"/> item.
/// </summary>
public partial class ApeItem
{
    private static ApeItem GetItem(ApeVersion version, string key, int flags)
    {
        var itemType = (ApeItemType)((flags & HeaderFlags.ItemType) >> 1);
        var item = itemType switch
        {
            ApeItemType.ContainsBinary => new ApeBinaryItem(version, key),
            ApeItemType.CodedUTF8 => new ApeUtf8Item(version, key),
            ApeItemType.IsLocator => new ApeLocatorItem(version, key),
            ApeItemType.Reserved => new ApeItem(version, key) { IsReadOnly = true },
            _ => new ApeItem(version, key) { IsReadOnly = true },
        };
        return item;
    }
}
