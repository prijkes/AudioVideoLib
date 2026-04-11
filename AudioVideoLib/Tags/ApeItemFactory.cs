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

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class used to store an <see cref="ApeTag"/> item.
    /// </summary>
    public partial class ApeItem
    {
        private static ApeItem GetItem(ApeVersion version, string key, int flags)
        {
            ApeItemType itemType = (ApeItemType)((flags & HeaderFlags.ItemType) >> 1);
            ApeItem item;
            switch (itemType)
            {
                case ApeItemType.ContainsBinary:
                    item = new ApeBinaryItem(version, key);
                    break;

                case ApeItemType.CodedUTF8:
                    item = new ApeUtf8Item(version, key);
                    break;

                case ApeItemType.IsLocator:
                    item = new ApeLocatorItem(version, key);
                    break;

                case ApeItemType.Reserved:
                    item = new ApeItem(version, key) { IsReadOnly = true };
                    break;

                default:
                    item = new ApeItem(version, key) { IsReadOnly = true };
                    break;
            }
            return item;
        }
    }
}
