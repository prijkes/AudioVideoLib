namespace AudioVideoLib.Tags;

using System;
using System.Text;

using AudioVideoLib.IO;

/// <summary>
/// Class to store an APE tag.
/// </summary>
public sealed partial class ApeTagReader
{
    private static readonly byte[] TagIdentifierBytes = Encoding.ASCII.GetBytes(ApeTag.TagIdentifier);

    private static readonly byte[] Reserved = new byte[StreamBuffer.Int64Size];

    ////------------------------------------------------------------------------------------------------------------------------------

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets a value indicating whether the tag is valid or not.
    /// </summary>
    /// <param name="header">The header.</param>
    /// <returns>
    ///   <c>true</c> if it is valid; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">header</exception>
    private static bool IsValidTag(ApeHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);

        // Validity check - only version 2.0+ may have a header.
        return header.FrameCount <= ApeTag.MaxAllowedFields && (header.Size - ApeTag.FooterSize) <= ApeTag.MaxAllowedSize
               && ((header.Flags == 0) || (header.Version >= ApeVersion.Version2));
    }

    /// <summary>
    /// The header or footer for a <see cref="ApeTag"/>.
    /// </summary>
    //// The footer at the end of APE tagged files (can also optionally be at the front of the tag).
    private sealed class ApeHeader
    {
        public long Position { get; set; }

        public ApeVersion Version { get; set; }

        /// <summary>
        /// Gets or sets the size of the <see cref="ApeTag"/>.
        /// </summary>
        /// <value>
        /// The size of the <see cref="ApeTag"/>.
        /// </value>
        /// <remarks>
        /// 32 bits size in bytes, including footer and all tag items excluding header to be as compatible as possible with APE tags 1.000.
        /// </remarks>
        public int Size { get; set; }

        public int FrameCount { get; set; }

        public int Flags { get; set; }

        public byte[] ReservedBytes { get; set; } = null!;
    }
}
