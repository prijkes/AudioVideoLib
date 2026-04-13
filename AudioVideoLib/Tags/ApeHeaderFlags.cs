namespace AudioVideoLib.Tags;

/// <summary>
/// Class to store an APE tag.
/// </summary>
public sealed partial class ApeTag
{
    /// <summary>
    /// <see cref="ApeTag"/> header flags.
    /// </summary>
    //// Footer (and header) flags
    private struct ApeHeaderFlags
    {
        /// <summary>
        /// Flag indicating whether the <see cref="ApeTag"/> contains a header.
        /// </summary>
        public const int ContainsHeader = 1 << 31;

        /// <summary>
        /// Flag indicating whether the <see cref="ApeTag"/> does not contain a footer.
        /// </summary>
        public const int ContainsNoFooter = 1 << 30;

        /// <summary>
        /// Flag indicating whether the header instance is the header or footer.
        /// </summary>
        public const int IsHeader = 1 << 29;

        /// <summary>
        /// Flag indicating whether the <see cref="ApeTag"/> is read only or read/write.
        /// </summary>
        public const int IsReadOnly = 1 << 0;
    }
}
