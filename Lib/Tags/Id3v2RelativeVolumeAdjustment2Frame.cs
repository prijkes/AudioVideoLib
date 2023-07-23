/*
 * Date: 2011-07-06
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 *  http://www.getid3.org/source/index.php?p=module.tag.id3v2.phps
 *  https://github.com/taglib/taglib/blob/master/taglib/mpeg/id3v2/frames/relativevolumeframe.cpp
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioVideoLib.Collections;
using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing a relative volume adjustment (2).
    /// </summary>
    /// <remarks>
    /// This frame allows the user to say how much he wants to increase/decrease the volume on each channel when the file is played.
    /// The purpose is to be able to align all files to a reference volume, so that you don't have to change the volume constantly.
    /// This frame may also be used to balance adjust the audio.
    /// The volume adjustment is encoded as a fixed point decibel value, 16 bit signed integer representing (adjustment*512), 
    /// giving +/- 64 dB with a precision of 0.001953125 dB. E.g. +2 dB is stored as 0x04 0x00 and -2 dB is 0xFC 0x00.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> <see cref="Id3v2Version.Id3v240"/> only.
    /// </remarks>
    public sealed class Id3v2RelativeVolumeAdjustment2Frame : Id3v2Frame
    {
        private string _identification;

        private readonly EventList<Id3v2ChannelInformation> _channelInformation = new EventList<Id3v2ChannelInformation>();

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2RelativeVolumeAdjustment2Frame"/> class with version <see cref="Id3v2Version.Id3v240"/>.
        /// </summary>
        public Id3v2RelativeVolumeAdjustment2Frame() : base(Id3v2Version.Id3v240)
        {
            BindChannelInformationListEvents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2RelativeVolumeAdjustment2Frame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2RelativeVolumeAdjustment2Frame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));

            BindChannelInformationListEvents();
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the identification.
        /// </summary>
        /// <value>
        /// The identification.
        /// </value>
        /// <remarks>
        /// The 'identification' string is used to identify the situation and/or device where this adjustment should apply.
        /// </remarks>
        public string Identification
        {
            get
            {
                return _identification;
            }

            set
            {
                if (!String.IsNullOrEmpty(value) && !IsValidDefaultTextString(value, false))
                    throw new InvalidDataException("value contains one or more invalid characters.");

                _identification = value;
            }
        }

        /// <summary>
        /// Gets the channel information.
        /// </summary>
        /// <value>
        /// The channel information.
        /// </value>
        public IList<Id3v2ChannelInformation> ChannelInformation
        {
            get
            {
                return _channelInformation;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                Encoding defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
                using (StreamBuffer stream = new StreamBuffer())
                {
                    // Identification
                    if (!String.IsNullOrEmpty(Identification))
                        stream.WriteString(Identification, defaultEncoding);

                    // String terminator (0x00 in encoding)
                    stream.Write(defaultEncoding.GetBytes("\0"));

                    // Channel informations
                    foreach (Id3v2ChannelInformation ci in ChannelInformation)
                    {
                        stream.WriteByte((byte)ci.ChannelType);
                        stream.WriteBigEndianInt16(Convert.ToInt16((double)ci.VolumeAdjustment * 512));
                        stream.WriteByte(ci.BitsRepresentingPeak);
                        stream.WriteBigEndianBytes(ci.PeakVolume, (int)Math.Ceiling((double)ci.BitsRepresentingPeak / 8));
                    }
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
                    _identification = stream.ReadString(defaultEncoding, true);

                    UnbindChannelInformationListEvents();
                    _channelInformation.Clear();

                    // Read the channel informations
                    while (stream.Position < stream.Length)
                    {
                        Id3v2ChannelType channelType = (Id3v2ChannelType)stream.ReadByte();
                        int volumeAdjustmentInt16 = stream.ReadBigEndianInt16();
                        float volumeAdjustment = Convert.ToSingle((double)volumeAdjustmentInt16 / 512);
                        byte bitsRepresentingPeak = (byte)stream.ReadByte();
                        long peakVolume = 0;
                        if (bitsRepresentingPeak > 0)
                        {
                            int peakVolumeBytes = (int)Math.Ceiling((double)bitsRepresentingPeak / 8);
                            peakVolume = stream.ReadBigEndianInt(peakVolumeBytes);
                        }
                        _channelInformation.Add(new Id3v2ChannelInformation(channelType, volumeAdjustment, bitsRepresentingPeak, peakVolume));
                    }
                    BindChannelInformationListEvents();
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return "RVA2"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2RelativeVolumeAdjustment2Frame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2RelativeVolumeAdjustment2Frame"/>.
        /// </summary>
        /// <param name="rva2">The <see cref="Id3v2RelativeVolumeAdjustment2Frame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> and <see cref="Identification"/> properties are equal (case-insensitive).
        /// </remarks>
        public bool Equals(Id3v2RelativeVolumeAdjustment2Frame rva2)
        {
            if (ReferenceEquals(null, rva2))
                return false;

            if (ReferenceEquals(this, rva2))
                return true;

            return (rva2.Version == Version) && String.Equals(rva2.Identification, Identification, StringComparison.OrdinalIgnoreCase);
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
            return (version >= Id3v2Version.Id3v240);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static bool IsValidChannelType(Id3v2ChannelType channelType)
        {
            return Enum.TryParse(channelType.ToString(), true, out channelType);
        }

        private static void ChannelInformationAdd(object sender, ListItemAddEventArgs<Id3v2ChannelInformation> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.Item == null)
                throw new NullReferenceException("e.Item may not be null");

            if (!IsValidChannelType(e.Item.ChannelType))
                throw new InvalidDataException("value contains one or more invalid channel types.");
        }

        private void ChannelInformationReplace(object sender, ListItemReplaceEventArgs<Id3v2ChannelInformation> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.NewItem == null)
                throw new NullReferenceException("e.NewItem may not be null");

            ChannelInformation.RemoveAt(e.Index);
            e.Cancel = true;
            ChannelInformation.Add(e.NewItem);
        }

        private void BindChannelInformationListEvents()
        {
            _channelInformation.ItemAdd += ChannelInformationAdd;

            _channelInformation.ItemReplace += ChannelInformationReplace;
        }

        private void UnbindChannelInformationListEvents()
        {
            _channelInformation.ItemAdd -= ChannelInformationAdd;

            _channelInformation.ItemReplace -= ChannelInformationReplace;
        }
    }
}
