/*
 * Date: 2012-12-30
  * Sources used: 
 *  http://web.archive.org/web/20120101134344/http://www.id3.org/iTunes_Normalization_settings
 */
using System;
using System.Globalization;
using System.Text;

using AudioVideoLib;
using AudioVideoLib.IO;
using AudioVideoLib.Tags;

namespace AudioVideoLibExamples
{
    /// <summary>
    /// iTunes writes a standard comment with a description of iTunNORM. This contains the normalization information it uses.
    /// </summary>
    public class Id3v2iTunesNormalizationFrame : Id3v2Frame
    {
        private const string Prefix = " ";

        private Id3v2FrameEncodingType _frameEncodingType;

        private string _language = "eng", _shortContentDescription = "iTunNORM";

        private int _volumeAdjustment1Left,
                    _volumeAdjustment1Right,
                    _volumeAdjustment2Left,
                    _volumeAdjustment2Right,
                    _unknown1Left,
                    _unknown1Right,
                    _peakValueLeft,
                    _peakValueRight,
                    _unknown2Left,
                    _unknown2Right;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2CommentFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2iTunesNormalizationFrame() : base(Id3v2Version.Id3v230)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2CommentFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        public Id3v2iTunesNormalizationFrame(Id3v2Version version) : base(version)
        {
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the volume adjustment in milliWatt / dBm as 1 / 1000 watt for the left audio channel.
        /// </summary>
        /// <value>
        /// The volume adjustment in milliWatt / dBm for the left audio channel, based on 1/1000 Watt.
        /// </value>
        public int VolumeAdjustment1Left
        {
            get
            {
                return _volumeAdjustment1Left;
            }

            set
            {
                _volumeAdjustment1Left = value;
            }
        }

        /// <summary>
        /// Gets or sets the volume adjustment in milliWatt / dBm as 1 / 1000 watt for the right audio channel.
        /// </summary>
        /// <value>
        /// The volume adjustment in milliWatt / dBm for the right audio channel, based on 1/1000 Watt.
        /// </value>
        public int VolumeAdjustment1Right
        {
            get
            {
                return _volumeAdjustment1Right;
            }

            set
            {
                _volumeAdjustment1Right = value;
            }
        }

        /// <summary>
        /// Gets or sets the volume adjustment in milliWatt / dBm as 1 /2500 watt for the left audio channel.
        /// </summary>
        /// <value>
        /// The volume adjustment in mlliWatt / dBm for the left audio channel, based on 1/2500 Watt.
        /// </value>
        public int VolumeAdjustment2Left
        {
            get
            {
                return _volumeAdjustment2Left;
            }

            set
            {
                _volumeAdjustment2Left = value;
            }
        }

        /// <summary>
        /// Gets or sets the volume adjustment in milliWatt / dBm as 1 /2500 watt for the right audio channel.
        /// </summary>
        /// <value>
        /// The volume adjustment in mlliWatt / dBm for the right audio channel, based on 1/2500 Watt.
        /// </value>
        public int VolumeAdjustment2Right
        {
            get
            {
                return _volumeAdjustment2Right;
            }

            set
            {
                _volumeAdjustment2Right = value;
            }
        }

        /// <summary>
        /// Gets or sets the unknown1 for the left audio channel.
        /// </summary>
        /// <value>
        /// The unknown1 for the left audio channel.
        /// </value>
        /// <remarks>
        /// Unknown field - always the same values for songs that only differs in volume - so maybe some statistical values.
        /// </remarks>
        public int Unknown1Left
        {
            get
            {
                return _unknown1Left;
            }

            set
            {
                _unknown1Left = value;
            }
        }

        /// <summary>
        /// Gets or sets the unknown1 for the right audio channel.
        /// </summary>
        /// <value>
        /// The unknown1 for the right audio channel.
        /// </value>
        /// <remarks>
        /// Unknown field - always the same values for songs that only differs in volume - so maybe some statistical values.
        /// </remarks>
        public int Unknown1Right
        {
            get
            {
                return _unknown1Right;
            }

            set
            {
                _unknown1Right = value;
            }
        }

        /// <summary>
        /// Gets or sets the peak value (maximum sample) as absolute (positive) value for the left audio channel.
        /// </summary>
        /// <value>
        /// The peak value (maximum sample) as absolute (positive) value for the left audio channel.
        /// </value>
        /// <remarks>
        /// The peak value (maximum sample) is an absolute (positive) value; therefore up to 32768 (for songs using 16-Bit samples).
        /// </remarks>
        public int PeakValueLeft
        {
            get
            {
                return _peakValueLeft;
            }

            set
            {
                _peakValueLeft = value;
            }
        }

        /// <summary>
        /// Gets or sets the peak value (maximum sample) as absolute (positive) value for the right audio channel.
        /// </summary>
        /// <value>
        /// The peak value (maximum sample) as absolute (positive) value for the right audio channel.
        /// </value>
        /// <remarks>
        /// The peak value (maximum sample) is an absolute (positive) value; therefore up to 32768 (for songs using 16-Bit samples).
        /// </remarks>
        public int PeakValueRight
        {
            get
            {
                return _peakValueRight;
            }

            set
            {
                _peakValueRight = value;
            }
        }

        /// <summary>
        /// Gets or sets the unknown2 for the left audio channel.
        /// </summary>
        /// <value>
        /// The unknown2 for the left audio channel.
        /// </value>
        /// <remarks>
        /// Unknown field - always the same values for songs that only differs in volume.
        /// </remarks>
        public int Unknown2Left
        {
            get
            {
                return _unknown2Left;
            }

            set
            {
                _unknown2Left = value;
            }
        }

        /// <summary>
        /// Gets or sets the unknown2 for the right audio channel.
        /// </summary>
        /// <value>
        /// The unknown2 for the right audio channel.
        /// </value>
        /// <remarks>
        /// Unknown field - always the same values for songs that only differs in volume.
        /// </remarks>
        public int Unknown2Right
        {
            get
            {
                return _unknown2Right;
            }

            set
            {
                _unknown2Right = value;
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
                Encoding languageEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);

                // Get the encoding for the given encoding type.
                Encoding encoding = Id3v2FrameEncoding.GetEncoding(_frameEncodingType);

                // Write the frame into a stream buffer.
                using (StreamBuffer stream = new StreamBuffer())
                {
                    // Get the preamble of the given encoding type.
                    byte[] preamble = Id3v2FrameEncoding.GetEncodingPreamble(_frameEncodingType);

                    // Write the given encoding type value as defined in the Id3v2 spec.
                    stream.WriteByte(Id3v2FrameEncoding.GetEncodingTypeValue(_frameEncodingType));

                    // Write the language string in the default encoding type.
                    stream.WriteString(_language, languageEncoding);

                    // Write the preamble for the encoding specific string.
                    stream.Write(preamble);

                    // Write the short content description string in the given encoding type - this is always 'iTunNORM'
                    stream.WriteString(_shortContentDescription, encoding);
                    
                    // Write the string terminator in the given encoding.
                    stream.Write(encoding.GetBytes("\0"));

                    // Write the preamble for the frame specific values.
                    stream.Write(preamble);

                    // values
                    stream.WriteString(_volumeAdjustment1Left.ToString("X8"), encoding);
                    stream.WriteString(Prefix, encoding);
                    stream.WriteString(_volumeAdjustment1Right.ToString("X8"), encoding);
                    stream.WriteString(Prefix, encoding);
                    stream.WriteString(_volumeAdjustment2Left.ToString("X8"));
                    stream.WriteString(Prefix, encoding);
                    stream.WriteString(_volumeAdjustment2Right.ToString("X8"));
                    stream.WriteString(Prefix, encoding);
                    stream.WriteString(_unknown1Left.ToString("X8"));
                    stream.WriteString(Prefix, encoding);
                    stream.WriteString(_unknown1Right.ToString("X8"));
                    stream.WriteString(Prefix, encoding);
                    stream.WriteString(_peakValueLeft.ToString("X8"));
                    stream.WriteString(Prefix, encoding);
                    stream.WriteString(_peakValueRight.ToString("X8"));
                    stream.WriteString(Prefix, encoding);
                    stream.WriteString(_unknown2Left.ToString("X8"));
                    stream.WriteString(Prefix, encoding);
                    stream.WriteString(_unknown2Right.ToString("X8"));

                    // Return the stream as a byte array.
                    return stream.ToByteArray();
                }
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                Encoding languageEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    _frameEncodingType = Id3v2FrameEncoding.ReadEncodingTypeFromStream(stream);
                    Encoding encoding = Id3v2FrameEncoding.GetEncoding(_frameEncodingType);
                    _language = stream.ReadString(3, languageEncoding);
                    _shortContentDescription = stream.ReadString(encoding);
                    string text = stream.ReadString(encoding);

                    // values
                    text = text.Replace(Prefix, String.Empty);
                    if (text.Length != 80)
                        text = text.PadRight(80 - text.Length, '0');

                    // The iTunNORM tag consists of 5 value pairs.
                    // These 10 values are encoded as ASCII Hex values of 8 characters each inside the tag (plus a space as prefix).
                    // The tag can be found in MP3, AIFF, AAC and Apple Lossless files.
                    //
                    // The relevant information is what is encoded in these 5 value pairs.
                    // The first value of each pair is for the left audio channel, the second value of each pair is for the right channel.
                    //
                    // iTunes is choosing the maximum value of the both first pairs (of the first 4 values) to adjust the whole song.
                    Int32.TryParse(text.Substring(0, 8), NumberStyles.AllowHexSpecifier, null, out _volumeAdjustment1Left);
                    Int32.TryParse(text.Substring(8, 8), NumberStyles.AllowHexSpecifier, null, out _volumeAdjustment1Right);
                    Int32.TryParse(text.Substring(16, 8), NumberStyles.AllowHexSpecifier, null, out _volumeAdjustment2Left);
                    Int32.TryParse(text.Substring(24, 8), NumberStyles.AllowHexSpecifier, null, out _volumeAdjustment2Right);
                    Int32.TryParse(text.Substring(32, 8), NumberStyles.AllowHexSpecifier, null, out _unknown1Left);
                    Int32.TryParse(text.Substring(40, 8), NumberStyles.AllowHexSpecifier, null, out _unknown1Right);
                    Int32.TryParse(text.Substring(48, 8), NumberStyles.AllowHexSpecifier, null, out _peakValueLeft);
                    Int32.TryParse(text.Substring(56, 8), NumberStyles.AllowHexSpecifier, null, out _peakValueRight);
                    Int32.TryParse(text.Substring(64, 8), NumberStyles.AllowHexSpecifier, null, out _unknown2Left);
                    Int32.TryParse(text.Substring(72, 8), NumberStyles.AllowHexSpecifier, null, out _unknown2Right);
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "COM" : "COMM"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2iTunesNormalizationFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2iTunesNormalizationFrame"/>.
        /// </summary>
        /// <param name="normalizationFrame">The <see cref="Id3v2iTunesNormalizationFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        public bool Equals(Id3v2iTunesNormalizationFrame normalizationFrame)
        {
            if (ReferenceEquals(null, normalizationFrame))
                return false;

            if (ReferenceEquals(this, normalizationFrame))
                return true;

            return (normalizationFrame.Version == Version)
                   && (normalizationFrame.VolumeAdjustment1Left == VolumeAdjustment1Left)
                   && (normalizationFrame.VolumeAdjustment1Right == VolumeAdjustment1Right)
                   && (normalizationFrame.VolumeAdjustment2Left == VolumeAdjustment2Left)
                   && (normalizationFrame.VolumeAdjustment2Right == VolumeAdjustment2Right)
                   && (normalizationFrame.Unknown1Left == Unknown1Left)
                   && (normalizationFrame.Unknown1Right == Unknown1Right)
                   && (normalizationFrame.PeakValueLeft == PeakValueLeft)
                   && (normalizationFrame.PeakValueRight == PeakValueRight)
                   && (normalizationFrame.Unknown2Left == Unknown2Left)
                   && (normalizationFrame.Unknown2Right == Unknown2Right);
        }
    }
}
