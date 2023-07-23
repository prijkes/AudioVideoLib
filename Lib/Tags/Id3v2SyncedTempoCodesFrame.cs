/*
 * Date: 2011-05-28
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

using AudioVideoLib.Collections;
using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing synced tempo codes.
    /// </summary>
    /// <remarks>
    /// For a more accurate description of the tempo of a musical piece this frame might be used.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
    /// </remarks>
    public sealed class Id3v2SyncedTempoCodesFrame : Id3v2Frame
    {
        private readonly EventList<Id3v2TempoCode> _tempoCodes = new EventList<Id3v2TempoCode>();

        private Id3v2TimeStampFormat _timeStampFormat;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2SyncedTempoCodesFrame"/> class with <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2SyncedTempoCodesFrame() : base(Id3v2Version.Id3v230)
        {
            BindTempoCodeListEvents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2SyncedTempoCodesFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2SyncedTempoCodesFrame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));

            BindTempoCodeListEvents();
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the time stamp format.
        /// </summary>
        /// <value>
        /// The time stamp.
        /// </value>
        public Id3v2TimeStampFormat TimeStampFormat
        {
            get
            {
                return _timeStampFormat;
            }

            set
            {
                if (!IsValidTimeStampFormat(value))
                    throw new ArgumentOutOfRangeException("value");

                _timeStampFormat = value;
            }
        }

        /// <summary>
        /// Gets or sets the tempo data.
        /// </summary>
        /// <value>
        /// The tempo data.
        /// </value>
        /// <remarks>
        /// The tempo data will be sorted and saved in chronological order.
        /// </remarks>
        public ICollection<Id3v2TempoCode> TempoData
        {
            get
            {
                return _tempoCodes;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                using (StreamBuffer stream = new StreamBuffer())
                {
                    stream.WriteByte((byte)TimeStampFormat);
                    foreach (Id3v2TempoCode tempCode in TempoData)
                    {
                        stream.WriteBigEndianBytes(tempCode.BeatsPerMinute, (tempCode.BeatsPerMinute >= 0xFF) ? 2 : 1);
                        stream.WriteBigEndianInt32(tempCode.TimeStamp);
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
                    _timeStampFormat = (Id3v2TimeStampFormat)stream.ReadByte();

                    UnbindTempoCodeListEvents();
                    _tempoCodes.Clear();
                    while (stream.Position < stream.Length)
                    {
                        int beatsPerMinute = stream.ReadByte();
                        if (beatsPerMinute >= 0xFF)
                            beatsPerMinute += stream.ReadByte();

                        _tempoCodes.Add(new Id3v2TempoCode(beatsPerMinute, stream.ReadBigEndianInt32()));
                    }
                    BindTempoCodeListEvents();
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "STC" : "SYTC"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2SyncedTempoCodesFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2SyncedTempoCodesFrame"/>.
        /// </summary>
        /// <param name="etc">The <see cref="Id3v2SyncedTempoCodesFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> property is equal.
        /// </remarks>
        public bool Equals(Id3v2SyncedTempoCodesFrame etc)
        {
            if (ReferenceEquals(null, etc))
                return false;

            if (ReferenceEquals(this, etc))
                return true;

            return etc.Version == Version;
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
            return true;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private void BindTempoCodeListEvents()
        {
            _tempoCodes.ItemAdd += TempoCodeAdd;

            _tempoCodes.ItemReplace += TempoCodeReplace;
        }

        private void UnbindTempoCodeListEvents()
        {
            _tempoCodes.ItemAdd -= TempoCodeAdd;

            _tempoCodes.ItemReplace -= TempoCodeReplace;
        }

        private void TempoCodeAdd(object sender, ListItemAddEventArgs<Id3v2TempoCode> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.Item == null)
                throw new NullReferenceException("e.Item may not be null");

            for (int i = 0; i < _tempoCodes.Count; i++)
            {
                Id3v2TempoCode tempoCode = _tempoCodes[i];
                if (tempoCode.TimeStamp >= e.Item.TimeStamp)
                {
                    e.Index = i;
                    break;
                }
            }
        }

        private void TempoCodeReplace(object sender, ListItemReplaceEventArgs<Id3v2TempoCode> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.NewItem == null)
                throw new NullReferenceException("e.NewItem may not be null");

            _tempoCodes.RemoveAt(e.Index);
            e.Cancel = true;
            _tempoCodes.Add(e.NewItem);
        }
    }
}
