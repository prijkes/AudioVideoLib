/*
 * Date: 2013-01-05
 */
using System;
using System.IO;
using System.Text;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

namespace AudioVideoLibExamples
{
    /// <summary>
    /// 
    /// </summary>
    public class Id3v2ExperimentalTestFrame : Id3v2Frame
    {
        private Id3v2FrameEncodingType _textEncodingType;

        private string _taggingLibraryUsed = "AudioVideoLib";

        private string _taggingLibraryAuthor = "AudioVideoLib";

        private string _taggingLibraryWebsite = "http://dummy.in/";

        private DateTime _dateOfTag = DateTime.Now;

        private bool _taggingLibrarySupportsFrame = true;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2ExperimentalTestFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2ExperimentalTestFrame() : base(Id3v2Version.Id3v230)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2ExperimentalTestFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        public Id3v2ExperimentalTestFrame(Id3v2Version version) : base(version)
        {
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the text encoding type.
        /// </summary>
        /// <value>
        /// The text encoding type.
        /// </value>
        /// <exception cref="System.IO.InvalidDataException">
        /// TaggingLibraryUsed or TaggingLibraryAuthor contains one or more invalid characters for the specified frame encoding type.
        /// </exception>
        public Id3v2FrameEncodingType TextEncodingType
        {
            get
            {
                return _textEncodingType;
            }

            set
            {
                if (!String.IsNullOrEmpty(TaggingLibraryUsed) && !IsValidTextString(TaggingLibraryUsed, value, false))
                    throw new InvalidDataException("TaggingLibraryUsed contains one or more invalid characters for the specified frame encoding type.");

                if (!String.IsNullOrEmpty(TaggingLibraryAuthor) && !IsValidTextString(TaggingLibraryAuthor, value, false))
                    throw new InvalidDataException("TaggingLibraryAuthor contains one or more invalid characters for the specified frame encoding type.");

                _textEncodingType = value;
            }
        }

        /// <summary>
        /// Gets or sets the tagging library used.
        /// </summary>
        /// <value>
        /// The tagging library used.
        /// </value>
        /// <exception cref="System.IO.InvalidDataException">value contains one or more invalid characters for the current frame encoding type.</exception>
        public string TaggingLibraryUsed
        {
            get
            {
                return _taggingLibraryUsed;
            }

            set
            {
                if (!String.IsNullOrEmpty(value) && !IsValidTextString(value, TextEncodingType, false))
                    throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");

                _taggingLibraryUsed = value;
            }
        }

        /// <summary>
        /// Gets or sets the tagging library author.
        /// </summary>
        /// <value>
        /// The tagging library author.
        /// </value>
        /// <exception cref="System.ArgumentNullException">value</exception>
        /// <exception cref="System.IO.InvalidDataException">value contains one or more invalid characters for the current frame encoding type.</exception>
        public string TaggingLibraryAuthor
        {
            get
            {
                return _taggingLibraryAuthor;
            }

            set
            {
                if (!String.IsNullOrEmpty(value) && !IsValidTextString(value, TextEncodingType, false))
                    throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");

                _taggingLibraryAuthor = value;
            }
        }

        /// <summary>
        /// Gets or sets the tagging library website.
        /// </summary>
        /// <value>
        /// The tagging library website.
        /// </value>
        /// <exception cref="System.ArgumentNullException">value</exception>
        /// <exception cref="System.IO.InvalidDataException">value contains one or more invalid characters.</exception>
        public string TaggingLibraryWebsite
        {
            get
            {
                return _taggingLibraryWebsite;
            }

            set
            {
                if (!String.IsNullOrEmpty(TaggingLibraryWebsite))
                {
                    if (!IsValidDefaultTextString(value, false))
                        throw new InvalidDataException("value contains one or more invalid characters.");

                    if (!IsValidUrl(value))
                        throw new InvalidDataException("value is not a valid RFC 1738 URL.");
                }
                _taggingLibraryWebsite = value;
            }
        }

        /// <summary>
        /// Gets or sets the date of tag.
        /// </summary>
        /// <value>
        /// The date of tag.
        /// </value>
        public DateTime DateOfTag
        {
            get
            {
                return _dateOfTag;
            }

            set
            {
                _dateOfTag = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the tagging library supports this frame.
        /// </summary>
        /// <value>
        /// <c>true</c> if the tagging library supports this frame; otherwise, <c>false</c>.
        /// </value>
        public bool TaggingLibrarySupportsFrame
        {
            get
            {
                return _taggingLibrarySupportsFrame;
            }

            set
            {
                _taggingLibrarySupportsFrame = value;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        /// This will get called from the base class - the <see cref="Id3v2Frame"/> - when reading or writing data.
        /// At this point, the data has already been decompressed / decrypted.
        /// The <see cref="Id3v2Frame"/> class will take care of writing everything else, including compressing / encrypting the frame again.
        /// Here we only need to read / write specific data this frame uses.
        public override byte[] Data
        {
            get
            {
                // Get the language encoding for the default encoding type.
                Encoding defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);

                // Get the encoding for the given encoding type.
                Encoding textEncoding = Id3v2FrameEncoding.GetEncoding(TextEncodingType);

                // Write the frame into a stream buffer.
                using (StreamBuffer stream = new StreamBuffer())
                {
                    // Get the preamble of the given encoding type.
                    byte[] preamble = Id3v2FrameEncoding.GetEncodingPreamble(TextEncodingType);

                    // Write the given encoding type value as defined in the Id3v2 spec.
                    stream.WriteByte(Id3v2FrameEncoding.GetEncodingTypeValue(TextEncodingType));

                    // Write the preamble for the encoding specific string.
                    stream.Write(preamble);

                    // Write the tagging library used string in the given encoding.
                    if (TaggingLibraryUsed != null)
                        stream.WriteString(TaggingLibraryUsed, textEncoding);

                    // Write the string terminator in the given encoding.
                    stream.Write(textEncoding.GetBytes("\0"));

                    // Write the preamble for the encoding specific string.
                    stream.Write(preamble);

                    // Write the tagging library author string in the given encoding.
                    if (TaggingLibraryAuthor != null)
                        stream.WriteString(TaggingLibraryAuthor, textEncoding);

                    // Write the string terminator in the given encoding.
                    stream.Write(textEncoding.GetBytes("\0"));

                    // Write the tagging library website in in the default encoding.
                    if (TaggingLibraryWebsite != null)
                        stream.WriteString(TaggingLibraryWebsite);

                    // Write the string terminator in the given encoding.
                    stream.Write(defaultEncoding.GetBytes("\0"));

                    // Write the date.
                    stream.WriteDouble(DateOfTag.ToOADate());

                    // Write the bool value.
                    stream.WriteByte((byte)(TaggingLibrarySupportsFrame ? 0x00 : 0x01));

                    // Return the stream as a byte array.
                    return stream.ToByteArray();
                }
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                Encoding defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    // Read the text encoding.
                    TextEncodingType = Id3v2FrameEncoding.ReadEncodingTypeFromStream(stream);
                    Encoding textEncoding = Id3v2FrameEncoding.GetEncoding(TextEncodingType);
                    _taggingLibraryUsed = stream.ReadString(textEncoding);
                    _taggingLibraryAuthor = stream.ReadString(textEncoding);
                    _taggingLibraryWebsite = stream.ReadString(defaultEncoding);
                    _dateOfTag = DateTime.FromOADate(stream.ReadDouble());
                    _taggingLibrarySupportsFrame = stream.ReadByte() != 0x00;
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "XFT" : "XFTS"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2ExperimentalTestFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2ExperimentalTestFrame"/>.
        /// </summary>
        /// <param name="testFrame">The <see cref="Id3v2ExperimentalTestFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        public bool Equals(Id3v2ExperimentalTestFrame testFrame)
        {
            if (ReferenceEquals(null, testFrame))
                return false;

            if (ReferenceEquals(this, testFrame))
                return true;

            return (testFrame.Version == Version)
                   && String.Equals(testFrame.TaggingLibraryUsed, TaggingLibraryUsed, StringComparison.OrdinalIgnoreCase)
                   && String.Equals(testFrame.TaggingLibraryAuthor, TaggingLibraryAuthor, StringComparison.OrdinalIgnoreCase)
                   && String.Equals(testFrame.TaggingLibraryWebsite, TaggingLibraryWebsite, StringComparison.OrdinalIgnoreCase)
                   && testFrame.DateOfTag.Equals(DateOfTag)
                   && (testFrame.TaggingLibrarySupportsFrame == TaggingLibrarySupportsFrame);
        }
    }
}
