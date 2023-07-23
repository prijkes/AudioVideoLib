/*
 * Date: 2014-02-17
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */

namespace AudioVideoLib.Tags
{
    public sealed partial class Id3v2TagReader
    {
        private sealed class Id3v2Header
        {
            public long Position { get; set; }

            public string Identifier { get; set; }

            public Id3v2Version Version { get; set; }

            public int Flags { get; set; }

            /// <summary>
            /// Gets the size of the <see cref="Id3v2Tag"/> as read from a stream.
            /// </summary>
            /// <value>
            /// The size of the <see cref="Id3v2Tag"/> as read from a stream.
            /// </value>
            /// <remarks>
            /// The Id3v2 tag size is the sum of the byte length of the extended header, the padding and the frames after unsynchronization.
            /// If a footer is present this equals to ('total size' - 20) bytes, otherwise ('total size' - 10) bytes.
            /// </remarks>
            /// This does not include the footer size nor the header size.
            public int Size { get; set; }

            ////------------------------------------------------------------------------------------------------------------------------------

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                return Equals(obj as Id3v2Header);
            }

            /// <summary>
            /// Equals the specified <see cref="Id3v2Header"/>.
            /// </summary>
            /// <param name="hdr">The <see cref="Id3v2Header"/>.</param>
            /// <returns>true if equal; false otherwise.</returns>
            public bool Equals(Id3v2Header hdr)
            {
                if (ReferenceEquals(null, hdr))
                    return false;

                if (ReferenceEquals(this, hdr))
                    return true;

                return (Version == hdr.Version) && (Flags == hdr.Flags) && (Size == hdr.Size);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = Identifier != null ? Identifier.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ Version.GetHashCode();
                    hashCode = (hashCode * 397) ^ Flags.GetHashCode();
                    hashCode = (hashCode * 397) ^ Size.GetHashCode();
                    return hashCode;
                }
            }
        }
    }
}
