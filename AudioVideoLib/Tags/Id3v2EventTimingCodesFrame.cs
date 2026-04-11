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
using System.IO;

using AudioVideoLib.Collections;
using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing event timing codes.
    /// </summary>
    /// <remarks>
    /// This frame allows synchronization with key events in a song or sound.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
    /// </remarks>
    public sealed class Id3v2EventTimingCodesFrame : Id3v2Frame
    {
        private readonly EventList<Id3v2KeyEvent> _keyEvents = new EventList<Id3v2KeyEvent>();

        private Id3v2TimeStampFormat _timeStampFormat;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2EventTimingCodesFrame" /> class with version <see cref="Id3v2Version.Id3v230" />.
        /// </summary>
        public Id3v2EventTimingCodesFrame() : base(Id3v2Version.Id3v230)
        {
            BindKeyEventListEvents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2EventTimingCodesFrame" /> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version" /> is not supported by this frame.</exception>
        public Id3v2EventTimingCodesFrame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));

            BindKeyEventListEvents();
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
        /// Gets or sets the key events.
        /// </summary>
        /// <value>
        /// The key events.
        /// </value>
        /// <remarks>
        /// When adding items, the key events will be sorted in chronological order.
        /// </remarks>
        public ICollection<Id3v2KeyEvent> KeyEvents
        {
            get
            {
                return _keyEvents;
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
                    int length = 0;
                    stream.WriteByte((byte)TimeStampFormat);
                    foreach (Id3v2KeyEvent key in KeyEvents)
                    {
                        // 0xFF - one more byte of events follows (all the following bytes with the value 0xFF have the same function)
                        int b;
                        while ((b = ((int)key.EventType >> (length++ * 8)) & 0xFF) == 0xFF)
                            stream.WriteByte((byte)b);

                        stream.WriteByte((byte)b);
                        stream.WriteBigEndianInt32(key.TimeStamp);
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

                    UnbindKeyEventListEvents();
                    _keyEvents.Clear();
                    while (stream.Position < stream.Length)
                    {
                        // 0xFF - one more byte of events follows (all the following bytes with the value 0xFF have the same function)
                        int b = 0, i = 0, y;
                        while (((y = stream.ReadByte()) & 0xFF) == 0xFF)
                            b |= y << (i++ * 8);

                        b |= y << (i * 8);
                        _keyEvents.Add(new Id3v2KeyEvent((Id3v2KeyEventType)b, stream.ReadBigEndianInt32()));
                    }
                    BindKeyEventListEvents();
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "ETC" : "ETCO"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2EventTimingCodesFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2EventTimingCodesFrame"/>.
        /// </summary>
        /// <param name="etc">The <see cref="Id3v2EventTimingCodesFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> property is equal.
        /// </remarks>
        public bool Equals(Id3v2EventTimingCodesFrame etc)
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

        private bool IsValidKeyEventType(Id3v2KeyEventType keyEventType)
        {
            // 0x17 - 0xDF  reserved for future use (Id3v2.2.0 and earlier: 0x0E - 0xDF) (Id3v2.3.0: 0x15 - 0xDF) (Id3v2.4.0 and later: 0x17 - 0xDF)
            // 0xE0 - 0xEF  not predefined sync 0-F
            // 0xF0 - 0xFC  reserved for future use
            // 0xFF      one more byte of events follows (all the following bytes with the value 0xFF have the same function)
            //
            // The 'Not predefined sync's (0xE0 - 0xEF) are for user events.
            // You might want to synchronize your music to something, like setting of an explosion on-stage, 
            // turning on your screensaver etc.
            Id3v2KeyEventType maxAllowedOrderedKeyFrameType;
            if (Version < Id3v2Version.Id3v230)
                maxAllowedOrderedKeyFrameType = Id3v2KeyEventType.UnwantedNoise;
            else if ((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240))
                maxAllowedOrderedKeyFrameType = Id3v2KeyEventType.ThemeEnd;
            else // Id3v2.4.0 and later
                maxAllowedOrderedKeyFrameType = Id3v2KeyEventType.ProfanityEnd;

            return ((keyEventType >= Id3v2KeyEventType.Padding) && (keyEventType <= maxAllowedOrderedKeyFrameType))
                   || ((keyEventType >= (Id3v2KeyEventType)0xE0) && keyEventType <= (Id3v2KeyEventType)0xEF)
                   || ((keyEventType >= Id3v2KeyEventType.AudioEnd) && (keyEventType <= Id3v2KeyEventType.AudioFileEnds))
                   || (keyEventType == (Id3v2KeyEventType)0xFF);
        }

        private void BindKeyEventListEvents()
        {
            _keyEvents.ItemAdd += KeyEventAdd;

            _keyEvents.ItemReplace += KeyEventReplace;
        }

        private void UnbindKeyEventListEvents()
        {
            _keyEvents.ItemAdd -= KeyEventAdd;

            _keyEvents.ItemReplace -= KeyEventReplace;
        }

        private void KeyEventAdd(object sender, ListItemAddEventArgs<Id3v2KeyEvent> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.Item == null)
                throw new NullReferenceException("e.Item may not be null");

            if (!IsValidKeyEventType(e.Item.EventType))
                throw new InvalidDataException(String.Format("value contains one or more key event types not supported in version {0}.", Version));

            for (int i = 0; i < _keyEvents.Count; i++)
            {
                Id3v2KeyEvent keyEvent = _keyEvents[i];
                if (keyEvent.EventType >= e.Item.EventType)
                {
                    e.Index = i;
                    break;
                }
            }
        }

        private void KeyEventReplace(object sender, ListItemReplaceEventArgs<Id3v2KeyEvent> e)
        {
            if (e == null)
                throw new ArgumentException("e");

            if (e.NewItem == null)
                throw new NullReferenceException("e.NewItem may not be null");

            _keyEvents.RemoveAt(e.Index);
            e.Cancel = true;
            _keyEvents.Add(e.NewItem);
        }
    }
}
