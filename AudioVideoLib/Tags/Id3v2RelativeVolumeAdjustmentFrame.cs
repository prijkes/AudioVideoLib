/*
 * Date: 2011-07-06
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
using System;
using System.IO;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing a relative volume adjustment.
    /// </summary>
    /// <remarks>
    /// This frame allows the user to say how much he wants to increase/decrease the volume on each channel while the file is played.
    /// The purpose is to be able to align all files to a reference volume, so that you don't have to change the volume constantly.
    /// This frame may also be used to balance adjust the audio.
    /// If the volume peak levels are known then this could be described with the <see cref="PeakVolumeRightChannel"/> and <see cref="PeakVolumeLeftChannel"/> fields.
    /// <para />
    /// This frame has been replaced by the <see cref="Id3v2RelativeVolumeAdjustment2Frame"/> frame as of <see cref="Id3v2Version.Id3v240"/>.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> up to but not including <see cref="Id3v2Version.Id3v240"/>.
    /// </remarks>
    public sealed class Id3v2RelativeVolumeAdjustmentFrame : Id3v2Frame
    {
        private byte _volumeDescriptionBits = StreamBuffer.Int16Size;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2RelativeVolumeAdjustmentFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2RelativeVolumeAdjustmentFrame() : base(Id3v2Version.Id3v230)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2RelativeVolumeAdjustmentFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2RelativeVolumeAdjustmentFrame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the Increment/decrement.
        /// </summary>
        /// <value>
        /// The Increment/decrement.
        /// </value>
        /// <remarks>
        /// In the increment/decrement field bit 0 is used to indicate the right channel and bit 1 is used to indicate the left channel.
        /// 1 is increment and 0 is decrement.
        /// </remarks>
        public byte IncrementDecrement { get; set; }

        /// <summary>
        /// Gets or sets the volume description bits.
        /// </summary>
        /// <value>
        /// The volume description bits.
        /// </value>
        /// <remarks>
        /// The 'bits used for volume description' field is normally 0x10 (16 bits) for MPEG 2 layer I, II and III [MPEG] and MPEG 2.5.
        /// This value may not be 0x00.
        /// The volume is always represented with whole bytes, 
        /// padded in the beginning (highest bits) when 'bits used for volume description' is not a multiple of eight.
        /// </remarks>
        public byte VolumeDescriptionBits
        {
            get
            {
                return _volumeDescriptionBits;
            }

            set
            {
                if (value == 0)
                    throw new InvalidDataException("VolumeDescriptionBits may not be 0.");

                _volumeDescriptionBits = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the relative volume change for the right channel.
        /// </summary>
        /// <value>
        /// The relative volume change for the right channel.
        /// </value>
        /// <remarks>
        /// The right channel will be the right front channel when <see cref="RelativeVolumeChangeRightBackChannel"/> is set.
        /// </remarks>
        public int RelativeVolumeChangeRightChannel { get; set; }

        /// <summary>
        /// Gets or sets the relative volume change for the left channel.
        /// </summary>
        /// <value>
        /// The relative volume change for the left channel.
        /// </value>
        /// <remarks>
        /// The left channel will be the left front channel when <see cref="RelativeVolumeChangeLeftBackChannel"/> is set.
        /// </remarks>
        public int RelativeVolumeChangeLeftChannel { get; set; }

        /// <summary>
        /// Gets or sets the peak for volume for the right channel.
        /// </summary>
        /// <value>
        /// The peak volume for the right channel.
        /// </value>
        public int PeakVolumeRightChannel { get; set; }

        /// <summary>
        /// Gets or sets the peak volume for the left channel.
        /// </summary>
        /// <value>
        /// The peak volume for the left channel.
        /// </value>
        public int PeakVolumeLeftChannel { get; set; }

        /// <summary>
        /// Gets or sets the relative volume change for the right back channel.
        /// </summary>
        /// <value>
        /// The relative volume change for the right back channel.
        /// </value>
        /// <remarks>
        /// This field has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// When set, the <see cref="RelativeVolumeChangeRightChannel"/> will be the right front channel.
        /// </remarks>
        public int RelativeVolumeChangeRightBackChannel { get; set; }

        /// <summary>
        /// Gets or sets the relative volume change for the left back channel.
        /// </summary>
        /// <value>
        /// The relative volume change for the left back channel.
        /// </value>
        /// <remarks>
        /// This field has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// When set, the <see cref="RelativeVolumeChangeLeftChannel"/> will be the left front channel.
        /// </remarks>
        public int RelativeVolumeChangeLeftBackChannel { get; set; }

        /// <summary>
        /// Gets or sets the peak volume for the right back channel.
        /// </summary>
        /// <value>
        /// The peak volume for the right back channel.
        /// </value>
        /// <remarks>
        /// This field has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        public int PeakVolumeRightBackChannel { get; set; }

        /// <summary>
        /// Gets or sets the peak volume for the left back channel.
        /// </summary>
        /// <value>
        /// The peak volume for the left back channel.
        /// </value>
        /// <remarks>
        /// This field has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        public int PeakVolumeLeftBackChannel { get; set; }

        /// <summary>
        /// Gets or sets the relative volume change for the center channel.
        /// </summary>
        /// <value>
        /// The relative volume change for the center channel.
        /// </value>
        /// <remarks>
        /// This field has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        public int RelativeVolumeChangeCenterChannel { get; set; }

        /// <summary>
        /// Gets or sets the peak volume for the center channel.
        /// </summary>
        /// <value>
        /// The peak volume for the center channel.
        /// </value>
        /// <remarks>
        /// This field has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        public int PeakVolumeCenterChannel { get; set; }

        /// <summary>
        /// Gets or sets the relative volume change for the bass channel.
        /// </summary>
        /// <value>
        /// The relative volume change for the bass channel.
        /// </value>
        /// <remarks>
        /// This field has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        public int RelativeVolumeChangeBassChannel { get; set; }

        /// <summary>
        /// Gets or sets the peak volume for the bass channel.
        /// </summary>
        /// <value>
        /// The peak volume for the bass channel.
        /// </value>
        /// <remarks>
        /// This field has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        public int PeakVolumeBassChannel { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                using (StreamBuffer stream = new StreamBuffer())
                {
                    stream.WriteByte(IncrementDecrement);
                    stream.WriteByte(VolumeDescriptionBits);

                    int bytesEntry = (int)Math.Ceiling((double)VolumeDescriptionBits / 8);
                    stream.WriteBigEndianBytes(RelativeVolumeChangeRightChannel * ((IncrementDecrement & 0x1) != 0 ? 1 : -1), bytesEntry);
                    stream.WriteBigEndianBytes(RelativeVolumeChangeLeftChannel * ((IncrementDecrement & 0x2) != 0 ? 1 : -1), bytesEntry);

                    stream.WriteBigEndianBytes(PeakVolumeRightChannel, bytesEntry);
                    stream.WriteBigEndianBytes(PeakVolumeLeftChannel, bytesEntry);

                    // Fields added in Id3v2Version.Id3v230
                    if (Version >= Id3v2Version.Id3v230)
                    {
                        stream.WriteBigEndianBytes(RelativeVolumeChangeRightBackChannel * ((IncrementDecrement & 0x4) != 0 ? 1 : -1), bytesEntry);
                        stream.WriteBigEndianBytes(RelativeVolumeChangeLeftBackChannel * ((IncrementDecrement & 0x8) != 0 ? 1 : -1), bytesEntry);

                        stream.WriteBigEndianBytes(PeakVolumeRightBackChannel, bytesEntry);
                        stream.WriteBigEndianBytes(PeakVolumeLeftBackChannel, bytesEntry);

                        stream.WriteBigEndianBytes(RelativeVolumeChangeCenterChannel * ((IncrementDecrement & 0x10) != 0 ? 1 : -1), bytesEntry);
                        stream.WriteBigEndianBytes(PeakVolumeCenterChannel, bytesEntry);

                        stream.WriteBigEndianBytes(RelativeVolumeChangeBassChannel * ((IncrementDecrement & 0x20) != 0 ? 1 : -1), bytesEntry);
                        stream.WriteBigEndianBytes(PeakVolumeBassChannel, bytesEntry);
                    }
                    return stream.ToByteArray();
                }
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                using (StreamBuffer stream = new StreamBuffer(value))
                {
                    // 1 is increment and 0 is decrement. 
                    IncrementDecrement = (byte)stream.ReadByte();

                    // The 'bits used for volume description' field is normally $10 (16 bits) for MPEG 2 layer I, II and III and MPEG 2.5.
                    // This value may not be $00.
                    _volumeDescriptionBits = (byte)stream.ReadByte();

                    // The volume is always represented with whole bytes, padded in the beginning (highest bits) when 'bits used for volume description' is not a multiple of eight.
                    int bytesEntry = (int)Math.Ceiling((double)_volumeDescriptionBits / 8);

                    // The increment/decrement field bit 0 is used to indicate the right channel and bit 1 is used to indicate the left channel.
                    int amount = stream.ReadBigEndianInt(bytesEntry);
                    RelativeVolumeChangeRightChannel = amount * ((IncrementDecrement & 0x1) != 0 ? 1 : -1);
                    amount = stream.ReadBigEndianInt(bytesEntry);
                    RelativeVolumeChangeLeftChannel = amount * ((IncrementDecrement & 0x2) != 0 ? 1 : -1);

                    // If peak volume is not known these fields could be left zeroed or, if no other data follows, be completely omitted.
                    if (stream.Position >= value.Length)
                        return;

                    // If the volume peak levels are known then this could be described with the 'Peak volume right' and 'Peak volume left' field.
                    PeakVolumeRightChannel = stream.ReadBigEndianInt(bytesEntry);
                    PeakVolumeLeftChannel = stream.ReadBigEndianInt(bytesEntry);

                    // Fields below have been added in Id3v2Version.Id3v230
                    if ((Version < Id3v2Version.Id3v230) || (stream.Position >= value.Length))
                        return;

                    // The data block is then optionally followed by a volume definition for the left and right back channels.
                    // If this information is appended to the frame the first two channels will be treated as front channels.
                    // In the increment/decrement field bit 2 is used to indicate the right back channel and bit 3 for the left back channel.
                    amount = stream.ReadBigEndianInt(bytesEntry);
                    RelativeVolumeChangeRightBackChannel = amount * ((IncrementDecrement & 0x4) != 0 ? 1 : -1);
                    amount = stream.ReadBigEndianInt(bytesEntry);
                    RelativeVolumeChangeLeftBackChannel = amount * ((IncrementDecrement & 0x8) != 0 ? 1 : -1);

                    // If peak volume is not known these fields could be left zeroed or, if no other data follows, be completely omitted.
                    if ((stream.Position >= value.Length))
                        return;

                    PeakVolumeRightBackChannel = stream.ReadBigEndianInt(bytesEntry);
                    PeakVolumeLeftBackChannel = stream.ReadBigEndianInt(bytesEntry);

                    // If the center channel adjustment is present the following is appended to the existing frame, after the left and right back channels.
                    if ((stream.Position >= value.Length))
                        return;

                    // The center channel is represented by bit 4 in the increase/decrease field.
                    amount = stream.ReadBigEndianInt(bytesEntry);
                    RelativeVolumeChangeCenterChannel = amount * ((IncrementDecrement & 0x10) != 0 ? 1 : -1);

                    // If peak volume is not known these fields could be left zeroed or, if no other data follows, be completely omitted.
                    if ((stream.Position >= value.Length))
                        return;

                    PeakVolumeCenterChannel = stream.ReadBigEndianInt(bytesEntry);

                    // If the bass channel adjustment is present the following is appended to the existing frame, after the center channel.
                    if ((stream.Position >= value.Length))
                        return;
                    
                    // The bass channel is represented by bit 5 in the increase/decrease field.
                    amount = stream.ReadBigEndianInt(bytesEntry);
                    RelativeVolumeChangeBassChannel = amount * ((IncrementDecrement & 0x20) != 0 ? 1 : -1);

                    // If peak volume is not known these fields could be left zeroed or, if no other data follows, be completely omitted.
                    if ((stream.Position >= value.Length))
                        return;

                    PeakVolumeBassChannel = stream.ReadBigEndianInt(bytesEntry);
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "RVA" : "RVAD"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2RelativeVolumeAdjustmentFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2RelativeVolumeAdjustmentFrame"/>.
        /// </summary>
        /// <param name="rva">The <see cref="Id3v2RelativeVolumeAdjustmentFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        ///  Both instances are equal when their <see cref="Version"/> property is equal.
        /// </remarks>
        public bool Equals(Id3v2RelativeVolumeAdjustmentFrame rva)
        {
            if (ReferenceEquals(null, rva))
                return false;

            if (ReferenceEquals(this, rva))
                return true;

            return rva.Version == Version;
        }

        /// <summary>
        /// Determines whether the specified version is supported by the frame.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        ///   <c>true</c> if the specified version is supported; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsVersionSupported(Id3v2Version version)
        {
            return (version < Id3v2Version.Id3v240);
        }
    }
}
