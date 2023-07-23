/*
 * Date: 2011-08-14
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
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
    /// Class for storing the equalization (2).
    /// </summary>
    /// <remarks>
    /// This frame allows the user to predefine an equalization curve within the audio file.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> <see cref="Id3v2Version.Id3v240"/> only.
    /// </remarks>
    public sealed class Id3v2Equalisation2Frame : Id3v2Frame
    {
        private readonly EventList<Id3v2AdjustmentPoint> _adjustmentPoints = new EventList<Id3v2AdjustmentPoint>();

        private Id3v2InterpolationMethod _interpolationMethod;

        private string _identification;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2Equalisation2Frame"/> class with version <see cref="Id3v2Version.Id3v240"/>.
        /// </summary>
        public Id3v2Equalisation2Frame() : base(Id3v2Version.Id3v240)
        {
            BindAdjustmentPointsListEvents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2Equalisation2Frame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2Equalisation2Frame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));

            BindAdjustmentPointsListEvents();
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the interpolation method.
        /// </summary>
        /// <value>
        /// The interpolation method.
        /// </value>
        /// <remarks>
        /// The 'interpolation method' describes which method is preferred 
        /// when an interpolation between the adjustment point that follows.
        /// </remarks>
        public Id3v2InterpolationMethod InterpolationMethod
        {
            get
            {
                return _interpolationMethod;
            }

            set
            {
                if (!IsValidInterpolationMethod(value))
                    throw new ArgumentOutOfRangeException("value");

                _interpolationMethod = value;
            }
        }

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
        /// Gets or sets the <see cref="Id3v2AdjustmentPoint"/> adjustment points.
        /// </summary>
        /// <value>
        /// The <see cref="Id3v2AdjustmentPoint"/> adjustment points.
        /// </value>
        /// <remarks>
        /// One frequency should only be described once in the frame.
        /// <para />
        /// When adding items, the adjustment point will be ordered increasingly with reference to frequency.
        /// </remarks>
        public ICollection<Id3v2AdjustmentPoint> AdjustmentPoints
        {
            get
            {
                return _adjustmentPoints;
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
                    // Interpolation method
                    stream.WriteByte((byte)InterpolationMethod);

                    // Identification
                    if (Identification != null)
                        stream.WriteString(Identification, defaultEncoding);

                    // String terminator (0x00 in encoding)
                    stream.Write(defaultEncoding.GetBytes("\0"));

                    // Adjustment points
                    foreach (Id3v2AdjustmentPoint adjustmentPoint in AdjustmentPoints)
                    {
                        stream.WriteBigEndianInt16(adjustmentPoint.Frequency);
                        stream.WriteBigEndianInt16(adjustmentPoint.VolumeAdjustment);
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
                    _interpolationMethod = (Id3v2InterpolationMethod)stream.ReadByte();
                    _identification = stream.ReadString(defaultEncoding, true);

                    // Read the adjustment points
                    UnbindAdjustmentPointsListEvents();
                    _adjustmentPoints.Clear();
                    while (stream.Position < stream.Length)
                        _adjustmentPoints.Add(new Id3v2AdjustmentPoint((short)stream.ReadBigEndianInt16(), (short)stream.ReadBigEndianInt16()));

                    BindAdjustmentPointsListEvents();
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return "EQU2"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2Equalisation2Frame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2Equalisation2Frame"/>.
        /// </summary>
        /// <param name="equalisation2">The <see cref="Id3v2Equalisation2Frame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> and <see cref="Identification"/> properties are equal (case-insensitive).
        /// </remarks>
        public bool Equals(Id3v2Equalisation2Frame equalisation2)
        {
            if (ReferenceEquals(null, equalisation2))
                return false;

            if (ReferenceEquals(this, equalisation2))
                return true;

            return (equalisation2.Version == Version)
                   && String.Equals(equalisation2.Identification, Identification, StringComparison.OrdinalIgnoreCase);
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

        private static bool IsValidInterpolationMethod(Id3v2InterpolationMethod interpolationMethod)
        {
            return Enum.TryParse(interpolationMethod.ToString(), true, out interpolationMethod);
        }

        private void BindAdjustmentPointsListEvents()
        {
            _adjustmentPoints.ItemAdd += AdjustmentPointAdd;

            _adjustmentPoints.ItemReplace += AdjustmentPointReplace;
        }

        private void UnbindAdjustmentPointsListEvents()
        {
            _adjustmentPoints.ItemAdd -= AdjustmentPointAdd;

            _adjustmentPoints.ItemReplace -= AdjustmentPointReplace;
        }

        // Manually insert the AdjustmentPoint here so we can keep the list always sorted by Frequency.
        private void AdjustmentPointAdd(object sender, ListItemAddEventArgs<Id3v2AdjustmentPoint> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.Item == null)
                throw new NullReferenceException("e.Item may not be null");

            for (int i = 0; i < _adjustmentPoints.Count; i++)
            {
                Id3v2AdjustmentPoint adjustmentPoint = _adjustmentPoints[i];
                if (adjustmentPoint.Frequency >= e.Item.Frequency)
                {
                    e.Index = i;
                    break;
                }
            }
        }

        private void AdjustmentPointReplace(object sender, ListItemReplaceEventArgs<Id3v2AdjustmentPoint> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.NewItem == null)
                throw new NullReferenceException("e.NewItem may not be null");

            _adjustmentPoints.RemoveAt(e.Index);
            e.Cancel = true;
            _adjustmentPoints.Add(e.NewItem);
        }
    }
}
