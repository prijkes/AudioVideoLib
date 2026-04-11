/*
 * Date: 2011-06-18
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
using System.Linq;
using System.Text;

using AudioVideoLib.Collections;
using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing a list of involved people.
    /// </summary>
    /// <remarks>
    /// Since there might be a lot of people contributing to an audio file in various ways, such as musicians and technicians, 
    /// the <see cref="Id3v2TextFrame"/> is often insufficient to list everyone involved in a project.
    /// The <see cref="Id3v2InvolvedPeopleListFrame"/> frame is a frame containing the names of those involved, and how they were involved.
    /// <para />
    /// This frame has been replaced by the two frames <see cref="Id3v2Tag.MusicianCreditsList"/> and <see cref="Id3v2Tag.InvolvedPeopleList2"/> as of <see cref="Id3v2Version.Id3v240"/>.
    /// <para />
    /// This frame supports <see cref="Id3v2Version"/> up to but not including <see cref="Id3v2Version.Id3v240"/>.
    /// </remarks>
    public sealed class Id3v2InvolvedPeopleListFrame : Id3v2Frame
    {
        private readonly EventList<Id3v2InvolvedPeople> _involvedPeople = new EventList<Id3v2InvolvedPeople>();

        private Id3v2FrameEncodingType _frameEncodingType;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2InvolvedPeopleListFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        public Id3v2InvolvedPeopleListFrame() : base(Id3v2Version.Id3v230)
        {
            BindInvolvedPeopleListEvents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2InvolvedPeopleListFrame"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
        public Id3v2InvolvedPeopleListFrame(Id3v2Version version) : base(version)
        {
            if (!IsVersionSupported(version))
                throw new InvalidVersionException(String.Format("Version {0} not supported by this frame.", version));

            BindInvolvedPeopleListEvents();
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the text encoding, see <see cref="Id3v2FrameEncodingType"/> for possible values.
        /// </summary>
        /// <value>
        /// The text encoding.
        /// </value>
        /// <remarks>
        /// An <see cref="InvalidDataException"/> will be thrown when <see cref="InvolvedPeople"/> contains any <see cref="Id3v2InvolvedPeople.Involvee"/> or 
        /// <see cref="Id3v2InvolvedPeople.Involvement"/> entry not valid in the new <see cref="Id3v2FrameEncodingType"/>.
        /// </remarks>
        public Id3v2FrameEncodingType TextEncoding
        {
            get
            {
                return _frameEncodingType;
            }
            
            set
            {
                if (_involvedPeople.Any(t => !String.IsNullOrEmpty(t.Involvee) && !IsValidTextString(t.Involvee, value, false)))
                {
                    throw new InvalidDataException(
                        "InvolvedPeople contains one or more Involvee entries with one or more invalid characters for the specified frame encoding type.");
                }

                if (_involvedPeople.Any(t => !String.IsNullOrEmpty(t.Involvement) && !IsValidTextString(t.Involvement, value, false)))
                {
                    throw new InvalidDataException(
                        "InvolvedPeople contains one or more Involvement entries with one or more invalid characters for the specified frame encoding type.");
                }

                _frameEncodingType = value;
            }
        }

        /// <summary>
        /// Gets or sets a list of involved people.
        /// </summary>
        /// <value>
        /// A list of involved people.
        /// </value>
        /// <remarks>
        /// An <see cref="InvalidDataException"/> will be thrown when adding any <see cref="Id3v2InvolvedPeople.Involvee"/> or 
        /// <see cref="Id3v2InvolvedPeople.Involvement"/> entry not valid in the <see cref="Id3v2FrameEncodingType"/>.
        /// </remarks>
        public IList<Id3v2InvolvedPeople> InvolvedPeople
        {
            get
            {
                return _involvedPeople;
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
                    // Text encoding.
                    stream.WriteByte(Id3v2FrameEncoding.GetEncodingTypeValue(TextEncoding));

                    byte[] preamble = Id3v2FrameEncoding.GetEncodingPreamble(TextEncoding);
                    Encoding encoding = Id3v2FrameEncoding.GetEncoding(TextEncoding);
                    byte[] stringTerminator = encoding.GetBytes("\0");
                    foreach (Id3v2InvolvedPeople ip in InvolvedPeople)
                    {
                        // Preamble.
                        stream.Write(preamble);

                        // Encode the involvement field and add it.
                        if (ip.Involvement != null)
                            stream.WriteString(ip.Involvement, encoding);

                        // Append the 0x00 in the specified encoding.
                        stream.Write(stringTerminator);

                        // Preamble
                        stream.Write(preamble);

                        // Encode the involvee field and add it.
                        if (ip.Involvee != null)
                            stream.WriteString(ip.Involvee, encoding);

                        // Append the 0x00 in the specified encoding.
                        stream.Write(stringTerminator);
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
                    _frameEncodingType = Id3v2FrameEncoding.ReadEncodingTypeFromStream(stream);
                    Encoding encoding = Id3v2FrameEncoding.GetEncoding(_frameEncodingType);

                    UnbindInvolvedPeopleListEvents();
                    _involvedPeople.Clear();
                    while (stream.Position < stream.Length)
                        _involvedPeople.Add(new Id3v2InvolvedPeople(stream.ReadString(encoding), stream.ReadString(encoding)));

                    BindInvolvedPeopleListEvents();
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get { return (Version < Id3v2Version.Id3v230) ? "IPL" : "IPLS"; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2InvolvedPeopleListFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2InvolvedPeopleListFrame"/>.
        /// </summary>
        /// <param name="ipl">The <see cref="Id3v2InvolvedPeopleListFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// Both instances are equal when their <see cref="Version"/> property is equal.
        /// </remarks>
        public bool Equals(Id3v2InvolvedPeopleListFrame ipl)
        {
            if (ReferenceEquals(null, ipl))
                return false;

            if (ReferenceEquals(this, ipl))
                return true;

            return ipl.Version == Version;
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

        ////------------------------------------------------------------------------------------------------------------------------------

        private void BindInvolvedPeopleListEvents()
        {
            _involvedPeople.ItemAdd += InvolvedPeopleAdd;

            _involvedPeople.ItemReplace += InvolvedPeopleReplace;
        }

        private void UnbindInvolvedPeopleListEvents()
        {
            _involvedPeople.ItemAdd -= InvolvedPeopleAdd;

            _involvedPeople.ItemReplace -= InvolvedPeopleReplace;
        }

        private void InvolvedPeopleAdd(object sender, ListItemAddEventArgs<Id3v2InvolvedPeople> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.Item == null)
                throw new NullReferenceException("e.Item may not be null");

            if (!String.IsNullOrEmpty(e.Item.Involvee) && !IsValidTextString(e.Item.Involvee, TextEncoding, false))
            {
                throw new InvalidDataException(
                    "value contains one or more Involvee entries with one or more invalid characters for the current frame encoding type.");
            }

            if (!String.IsNullOrEmpty(e.Item.Involvement) && !IsValidTextString(e.Item.Involvement, TextEncoding, false))
            {
                throw new InvalidDataException(
                    "value contains one or more Involvement entries with one or more invalid characters for the current frame encoding type.");
            }
        }

        private void InvolvedPeopleReplace(object sender, ListItemReplaceEventArgs<Id3v2InvolvedPeople> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.NewItem == null)
                throw new NullReferenceException("e.NewItem may not be null");

            _involvedPeople.RemoveAt(e.Index);
            e.Cancel = true;
            _involvedPeople.Add(e.NewItem);
        }
    }
}
