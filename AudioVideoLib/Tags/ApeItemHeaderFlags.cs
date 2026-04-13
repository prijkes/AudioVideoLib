namespace AudioVideoLib.Tags;

/// <summary>
/// Class to store an Ape Item in an <see cref="ApeTag"/>.
/// </summary>
public partial class ApeItem
{
    /// <summary>
    /// Field flags.
    /// </summary>
    private struct HeaderFlags
    {
        /// <summary>
        /// Flag indicating whether the tag or item is read only.
        /// </summary>
        public const int ReadOnly = 0x01;

        /// <summary>
        /// Flag indicating the item type. See <see cref="ApeItemType"/> for possible values.
        /// </summary>
        public const int ItemType = 0x06;
    }
}
