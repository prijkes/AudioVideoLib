/*
 * Date: 2011-04-11
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://en.wikipedia.org/wiki/APE_tag
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1_specification
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2_specification
 *  http://www.monkeysaudio.com/developers.html
 */

using System;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an APE tag.
    /// </summary>
    public sealed partial class ApeTagReader
    {
        private static readonly byte[] TagIdentifierBytes = Encoding.ASCII.GetBytes(ApeTag.TagIdentifier);

        private static readonly byte[] Reserved = new byte[StreamBuffer.Int64Size];

        ////------------------------------------------------------------------------------------------------------------------------------

        private static void ValidateHeader(ApeHeader header, ApeHeader footer)
        {
#if DEBUG
            if (header != null && footer != null)
            {
                if (header.Version != footer.Version)
                {
                    throw new System.IO.InvalidDataException(
                        String.Format("The APE header version {0} does not match footer version {1}.", header.Version, footer.Version));
                }

                if (header.Size != footer.Size)
                    throw new System.IO.InvalidDataException(String.Format("The APE header tag has a different size {0} than the footer size {1}", header.Size, footer.Size));
            }

            if ((header != null) && (header.Version >= ApeVersion.Version2))
            {
                //if (!header.IsHeader)
                //    throw new System.IO.InvalidDataException("The APE header tag is claiming to be the footer!");

                // This is valid it seems.
                //if (header.UseHeader)
                //    throw new System.IO.InvalidDataException("The APE header tag is claiming there's a header!");

                //if ((footer == null) && header.UseFooter)
                //    throw new System.IO.InvalidDataException("The APE header tag is claiming there's a footer when there isn't!");

                //if (!System.Linq.Enumerable.All(header.ReservedBytes, b => b == 0x00))
                //{
                //    throw new System.IO.InvalidDataException(
                //        String.Format(
                //            "The APE header tag's \"reserved\" field contains non-null bytes: {0}",
                //            String.Join(" ", System.Linq.Enumerable.Select(header.ReservedBytes, b => "0x" + b.ToString("X2")))));
                //}
            }

            if ((footer != null) && (footer.Version >= ApeVersion.Version2))
            {
                //if (footer.IsHeader)
                //    throw new System.IO.InvalidDataException("The APE footer tag is claiming to be the header!");

                //if ((header == null) && footer.UseHeader)
                //    throw new System.IO.InvalidDataException("The APE header tag is claiming there's a header when there isn't!");

                // This is valid it seems.
                //if (footer.UseFooter)
                //    throw new System.IO.InvalidDataException("The APE footer tag is claiming there's a footer!");

                //if (!System.Linq.Enumerable.All(footer.ReservedBytes, b => b == 0x00))
                //{
                //    throw new System.IO.InvalidDataException(
                //        String.Format(
                //            "The APE footer tag's \"reserved\" field contains non-null bytes: {0}",
                //            String.Join(" ", System.Linq.Enumerable.Select(footer.ReservedBytes, b => "0x" + b.ToString("X2")))));
                //}
            }
#else
            return;
#endif
        }

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
            if (header == null)
                throw new ArgumentNullException("header");

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

            public byte[] ReservedBytes { get; set; }
        }
    }
}
