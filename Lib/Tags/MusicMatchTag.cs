/*
 * Date: 2010-08-12
 * Sources used: 
 *  http://emule-xtreme.googlecode.com/svn-history/r6/branches/emule/id3lib/doc/musicmatch.txt
 */
using System;
using System.Linq;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store a MusicMatch tag.
    /// </summary>
    /// The MusicMatch Tagging Format was designed to store specific types of audio meta-data inside the audio file itself.
    /// As the format was used exclusively by the MusicMatch Jukebox application, it is used only with MPEG-1/2 layer III files encoded with that program.
    /// However, its tagging format is not inherently exclusive of other audio formats, and could conceivably be used with other types of encodings.
    /// <para />
    /// MusicMatch tags were originally designed to come at the very end of MP3 files, after all of the MP3 audio frames.
    /// Starting with Jukebox version 3.1, the application became more ID3-friendly and started placing ID3v1 tags after the MusicMatch tag as well.
    /// In practice, since very few applications outside of the MusicMatch Jukebox are capable of reading and understanding this format, 
    /// it is not unusual to find MusicMatch tags "buried" within mp3 files, coming before other types of tagging formats in a file, 
    /// such as Lyrics3 or ID3v2.4.0.
    /// Such "relocations" are not uncommon, and therefore any software application that intends to find, read, 
    /// and parse MusicMatch tags should be flexible in this endeavor, despite the apparent intentions of the original specification.
    /// <para />
    /// Although various sections of a MusicMatch tag are fixed in length, other sections are not, and so tag lengths can vary from one file to another.
    /// A valid MusicMatch tag will be at least 8 kilobytes (8192 bytes) in length.
    /// Those tags with image data will often be much larger.
    /// <para />
    /// The byte-order in 4-byte pointers and multibyte numbers for MusicMatch tags is least-significant byte (LSB) first, also known as "little endian".
    /// For example, 0x12345678 is encoded as 0x78 0x56 0x34 0x12.
    public sealed partial class MusicMatchTag : IAudioTag
    {
        /// <summary>
        /// The size of the <see cref="MusicMatchTag"/> header.
        /// </summary>
        //// Header size is always 256 bytes in length. Header is optional though.
        public const int HeaderSize = 256;

        /// <summary>
        /// The size of the <see cref="MusicMatchTag"/> footer.
        /// </summary>
        public const int FooterSize = 48;

        /// <summary>
        /// The header identifier for a <see cref="MusicMatchTag"/>.
        /// </summary>
        public const string HeaderIdentifier = "18273645";

        /// <summary>
        /// The footer identifier for a <see cref="MusicMatchTag"/>.
        /// </summary>
        public const string FooterIdentifier = "Brava Software Inc.";

        /// <summary>
        /// The size of the data offset fields.
        /// </summary>
        public const int DataOffsetFieldsSize = 20;

        /// <summary>
        /// The audio meta data sizes
        /// </summary>
        /// <remarks>
        /// In all versions of the MusicMatch format up to and including 3.00, this audio meta data length is always 7868 bytes in length.
        /// All subsequent versions allowed three possible length for this section: 7936, 8004, and 8132 bytes.
        /// The conditions under which a particular length from these three possibilities was used is unknown.
        /// In all cases, this section is padded with dashes ($2D) to achieve this constant size.
        /// </remarks>
        //private static readonly Dictionary<float, int> AudioMetaDataSizes = new Dictionary<float, int> { { 3.00f, 7868 }, { 3.10f, 7936 } };
        public static int[] AudioMetaDataSizes
        {
            get
            {
                return new[] { 7868, 7936, 8004, 8132 };
            }
        }

        private static readonly byte[] HeaderIdentifierBytes = System.Text.Encoding.ASCII.GetBytes(HeaderIdentifier);

        private static readonly byte[] FooterIdentifierBytes = System.Text.Encoding.ASCII.GetBytes(FooterIdentifier);
        
        private const byte NullByte = 0x00;

        private const byte PaddingByte = 0x20;

        private string _version = "3.100000";

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="MusicMatchTag"/> class.
        /// </summary>
        public MusicMatchTag()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MusicMatchTag" /> class.
        /// </summary>
        /// <param name="majorVersion">The major version.</param>
        /// <param name="minorVersion">The minor version.</param>
        /// <remarks>
        /// The version when combined can, at most, be 8 bytes only.
        /// One of it will be used as the version decimal byte, so only 7 bytes can be used.
        /// </remarks>
        public MusicMatchTag(int majorVersion, int minorVersion)
        {
            string version = String.Format("{0}.{1}", majorVersion, minorVersion);
            version = (version.Length > 8) ? version.Substring(0, 8) : version.PadRight(8);
            Version = version;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public string Version
        {
            get
            {
                return _version;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _version = value;
                _version = (_version.Length > 8) ? _version.Substring(0, 8) : _version.PadRight(8);
            }
        }

        /// <summary>
        /// Gets the Xing encoder version.
        /// </summary>
        /// <value>
        /// The Xing encoder version.
        /// </value>
        /// The second subsection's 8-byte string is the version of the Xing encoder used to encode the mp3 file.
        /// The last four bytes of this string are usually '0' (0x30).  An example of this string is "1.010000".
        public string XingEncoderVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a header should be added to the <see cref="MusicMatchTag"/>.
        /// </summary>
        /// <value>
        /// <c>true</c> if a footer should be added to the <see cref="MusicMatchTag"/>; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// A header is not required for the <see cref="MusicMatchTag"/>.
        /// </remarks>
        public bool UseHeader { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as MusicMatchTag);
        }

        /// <inheritdoc/>
        public bool Equals(IAudioTag other)
        {
            return Equals(other as MusicMatchTag);
        }

        /// <summary>
        /// Equals the specified <see cref="MusicMatchTag"/>.
        /// </summary>
        /// <param name="tag">The <see cref="MusicMatchTag"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        public bool Equals(MusicMatchTag tag)
        {
            if (ReferenceEquals(null, tag))
                return false;

            if (ReferenceEquals(this, tag))
                return true;

            return true;
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        /// The value should be calculated on immutable fields only.
        public override int GetHashCode()
        {
            unchecked
            {
                return Version.GetHashCode() * 397;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------
    
        /// <inheritdoc />
        public byte[] ToByteArray()
        {
            using (StreamBuffer buffer = new StreamBuffer())
            {
                if (UseHeader)
                {
                    // Header
                    buffer.Write(HeaderIdentifierBytes);
                    buffer.WritePadding(NullByte, 2);
                    buffer.WriteString(XingEncoderVersion);
                    buffer.WritePadding(NullByte, 2);
                    buffer.WriteString(Version);
                    buffer.WritePadding(NullByte, 2);
                    buffer.WritePadding(PaddingByte, 226);
                }

                // Image extension
                int imageExtensionOffset = (int)buffer.Position;
                if ((Image != null) && !String.IsNullOrEmpty(Image.FileExtension))
                {
                    buffer.WriteString(Image.FileExtension);
                    buffer.WritePadding(PaddingByte, 4 - Image.FileExtension.Length);
                }
                else
                    buffer.WritePadding(PaddingByte, 4);

                // Image binary
                int imageBinaryOffset = (int)buffer.Position;
                if ((Image != null) && (Image.BinaryImage != null))
                {
                    buffer.WriteInt(Image.BinaryImage.Length);
                    buffer.Write(Image.BinaryImage);
                }
                else
                    buffer.WriteInt(0);

                // Unused
                int unusedOffset = (int)buffer.Position;
                buffer.WritePadding(NullByte, 4);

                // Version information
                int versionInfoOffset = (int)buffer.Position;
                buffer.WriteString(HeaderIdentifier);
                buffer.WritePadding(NullByte, 2);
                buffer.WriteString(XingEncoderVersion);
                buffer.WritePadding(NullByte, 2);
                buffer.WriteString(Version);
                buffer.WritePadding(NullByte, 2);
                buffer.WritePadding(PaddingByte, 226);

                // Audio meta-data
                // The audio meta-data is the heart of the MusicMatch tag.  It contains most
                // of the pertinent information found in other tagging formats (song title,
                // album title, artist, etc.) and some that are unique to this format (mood,
                // preference, situation).
                int audioMetaDataOffset = (int)buffer.Position;

                // Single-line text fields
                buffer.WriteShort((short)(SongTitle ?? String.Empty).Length);
                buffer.WriteString(SongTitle ?? String.Empty);
                buffer.WriteShort((short)(AlbumTitle ?? String.Empty).Length);
                buffer.WriteString(AlbumTitle ?? String.Empty);
                buffer.WriteShort((short)(ArtistName ?? String.Empty).Length);
                buffer.WriteString(ArtistName ?? String.Empty);
                buffer.WriteShort((short)(Genre ?? String.Empty).Length);
                buffer.WriteString(Genre ?? String.Empty);
                buffer.WriteShort((short)(Tempo ?? String.Empty).Length);
                buffer.WriteString(Tempo ?? String.Empty);
                buffer.WriteShort((short)(Mood ?? String.Empty).Length);
                buffer.WriteString(Mood ?? String.Empty);
                buffer.WriteShort((short)(Situation ?? String.Empty).Length);
                buffer.WriteString(Situation ?? String.Empty);
                buffer.WriteShort((short)(Preference ?? String.Empty).Length);
                buffer.WriteString(Preference ?? String.Empty);

                // Non-text fields
                buffer.WriteShort((short)(SongDuration ?? String.Empty).Length);
                buffer.WriteString(SongDuration ?? String.Empty);
                buffer.WriteDouble(CreationDate.ToOADate());
                buffer.WriteInt(PlayCounter);
                buffer.WriteShort((short)(OriginalFilename ?? String.Empty).Length);
                buffer.WriteString(OriginalFilename ?? String.Empty);
                buffer.WriteShort((short)(SerialNumber ?? String.Empty).Length);
                buffer.WriteString(SerialNumber ?? String.Empty);
                buffer.WriteShort(TrackNumber);

                // Multi-line text fields
                buffer.WriteShort((short)(Notes ?? String.Empty).Length);
                buffer.WriteString(Notes ?? String.Empty);
                buffer.WriteShort((short)(ArtistBio ?? String.Empty).Length);
                buffer.WriteString(ArtistBio ?? String.Empty);
                buffer.WriteShort((short)(Lyrics ?? String.Empty).Length);
                buffer.WriteString(Lyrics ?? String.Empty);

                // Internet addresses
                buffer.WriteShort((short)(ArtistUrl ?? String.Empty).Length);
                buffer.WriteString(ArtistUrl ?? String.Empty);
                buffer.WriteShort((short)(BuyCdUrl ?? String.Empty).Length);
                buffer.WriteString(BuyCdUrl ?? String.Empty);
                buffer.WriteShort((short)(ArtistEmail ?? String.Empty).Length);
                buffer.WriteString(ArtistEmail ?? String.Empty);

                // Null bytes
                buffer.WritePadding(NullByte, 16);

                // In all versions of the MusicMatch format up to and including 3.00, the
                // section is always 7868 bytes in length. All subsequent versions allowed
                // three possible lengths for this section: 7936, 8004, and 8132 bytes.  The
                // conditions under which a particular length from these three possibilities
                // was used is unknown. In all cases, this section is padded with dashes 
                // ($2D) to achieve this constant size.
                double version;
                if (!Double.TryParse(Version, out version))
                    version = 3.100000;

                // The padding is calculated by taking the AudioMetaDataSize minus the total size of audio meta data fields, 
                // minus the total size of the data offset fields, minus the footer size.
                long paddingSize = ((version <= 3.1) ? AudioMetaDataSizes.First() : AudioMetaDataSizes.Last()) - (buffer.Length - audioMetaDataOffset) - DataOffsetFieldsSize - FooterSize;
                buffer.WritePadding(0x2D, (int)paddingSize);

                // Data offsets
                buffer.WriteInt(imageExtensionOffset);
                buffer.WriteInt(imageBinaryOffset);
                buffer.WriteInt(unusedOffset);
                buffer.WriteInt(versionInfoOffset);
                buffer.WriteInt(audioMetaDataOffset);

                // Footer
                buffer.Write(FooterIdentifierBytes);
                buffer.WritePadding(PaddingByte, 13);
                buffer.WriteString(Version.Substring(0 ,4));
                buffer.WritePadding(PaddingByte, 12);

                return buffer.ToByteArray();
            }
        }
    }
}
