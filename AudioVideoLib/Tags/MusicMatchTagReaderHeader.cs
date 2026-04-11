/*
 * Date: 2012-11-04
 * Sources used: 
 *  http://emule-xtreme.googlecode.com/svn-history/r6/branches/emule/id3lib/doc/musicmatch.txt
 */
using System;
using System.Linq;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store a MusicMatch tag.
    /// </summary>
    public sealed partial class MusicMatchTagReader
    {
        private static readonly byte[] HeaderIdentifierBytes = System.Text.Encoding.ASCII.GetBytes(MusicMatchTag.HeaderIdentifier);

        private static readonly byte[] FooterIdentifierBytes = System.Text.Encoding.ASCII.GetBytes(MusicMatchTag.FooterIdentifier);

        ////------------------------------------------------------------------------------------------------------------------------------

        private static void ValidateHeader(MusicMatchHeader header, MusicMatchHeader footer)
        {
#if DEBUG
            if (header != null && footer != null)
            {
                if (header.MusicMatchVersion != footer.MusicMatchVersion)
                {
                    throw new System.IO.InvalidDataException(
                        String.Format("The MusicMatch header version {0} does not match footer version {1}.", header.MusicMatchVersion, footer.MusicMatchVersion));
                }
            }

            if (header != null)
            {
                if (header.Padding1.Any(c => c != 0x00))
                {
                    throw new System.IO.InvalidDataException(
                        String.Format(
                            "The MusicMatch header has invalid padding1 bytes: {0}.",
                            String.Join(" ", header.Padding1.Where(c => c != 0x00).Select(c => String.Format("0:x2")))));
                }

                if (header.Padding2.Any(c => c != 0x00))
                {
                    throw new System.IO.InvalidDataException(
                        String.Format(
                            "The MusicMatch header has invalid padding2 bytes: {0}.",
                            String.Join(" ", header.Padding2.Where(c => c != 0x00).Select(c => String.Format("0:x2")))));
                }

                if (header.Padding3.Any(c => c != 0x00))
                {
                    throw new System.IO.InvalidDataException(
                        String.Format(
                            "The MusicMatch header has invalid padding3 bytes: {0}.",
                            String.Join(" ", header.Padding3.Where(c => c != 0x00).Select(c => String.Format("0:x2")))));
                }

                if (header.SpacePadding2.Any(c => c != 0x20))
                {
                    throw new System.IO.InvalidDataException(
                        String.Format(
                            "The MusicMatch header has invalid space padding bytes: {0}.",
                            String.Join(" ", header.Padding1.Where(c => c != 0x20).Select(c => String.Format("0:x2")))));
                }
            }

            if (footer != null)
            {
            }
#else
            return;
#endif
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /*
         * An optional tag header often precedes the tag data in a MusicMatch tag.
         * Although the rules that determine this header's required presence are unknown, 
         * the header is usually found in tag versions up to and including 2.50, and is usually lacking otherwise.
         * Luckily, its format is rigid and therefore its presence is easy to determine.
         * The data in the header are not vital to the correct parsing of the rest of the tag and can thus be discarded.
         * The header is the only optional section in a MusicMatch tag.
         * All other sections are required to consider the tag valid.
         * 
         * The header section is always 256 bytes in length.
         * It begins with three 10-byte subsections, and ends with 226 bytes of space (0x20) padding.
         * Each of the first three subsections contains an 8-byte ASCII text string followed by two bytes of null (0x00) padding.
         */
        private sealed class MusicMatchHeader
        {
            /// <summary>
            /// Gets or sets the position.
            /// </summary>
            /// <value>
            /// The position.
            /// </value>
            public long Position { get; set; }
            
            /// <summary>
            /// Gets the identifier.
            /// </summary>
            /// <value>
            /// The identifier.
            /// </value>
            /// The first subsection serves as a sync string: its 8-byte string is always "18273645".
            public string Identifier { get; set; }

            public byte[] Padding1 { get; set; }

            public byte[] SpacePadding1 { get; set; }

            /// <summary>
            /// Gets the Xing encoder version.
            /// </summary>
            /// <value>
            /// The Xing encoder version.
            /// </value>
            /// The second subsection's 8-byte string is the version of the Xing encoder used to encode the mp3 file.
            /// The last four bytes of this string are usually '0' (0x30).  An example of this string is "1.010000".
            public string XingEncoderVersion { get; set; }

            public byte[] Padding2 { get; set; }

            /// <summary>
            /// Gets the music match version.
            /// </summary>
            /// <value>
            /// The music match version.
            /// </value>
            /// The third and final 10-byte subsection is the version of the MusicMatch Jukebox used to encode the mp3 file.
            /// The last four bytes of this string are usually '0' (0x30).  An example of this string is "2.120000".
            public string MusicMatchVersion { get; set; }

            public byte[] Padding3 { get; set; }

            public byte[] SpacePadding2 { get; set; }

            ////------------------------------------------------------------------------------------------------------------------------------

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                return Equals(obj as MusicMatchHeader);
            }

            /// <summary>
            /// Equals the specified <see cref="MusicMatchHeader"/>.
            /// </summary>
            /// <param name="hdr">The <see cref="MusicMatchHeader"/>.</param>
            /// <returns>true if equal; false otherwise.</returns>
            public bool Equals(MusicMatchHeader hdr)
            {
                if (ReferenceEquals(null, hdr))
                    return false;

                if (ReferenceEquals(this, hdr))
                    return true;

                return (hdr.Identifier == Identifier) && (hdr.XingEncoderVersion == XingEncoderVersion)
                       && (hdr.MusicMatchVersion == MusicMatchVersion);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = Identifier != null ? Identifier.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ (XingEncoderVersion != null ? XingEncoderVersion.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (MusicMatchVersion != null ? MusicMatchVersion.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }
    }
}
