/*
 * Date: 2011-08-14
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 *  http://forums.asp.net/t/1057992.aspx/1
 *  http://www.codeproject.com/Articles/1474/Events-and-event-handling-in-C
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
    /// Class for storing a text frame.
    /// <para />
    /// This frame supports <see cref="Id3v2Version" /> up to and including <see cref="Id3v2Version.Id3v240" />.
    /// </summary>
    public sealed partial class Id3v2TextFrame : Id3v2Frame
    {
        private const char TextValueDelimiter = '\0';

        private readonly string _identifier;

        private readonly Id3v2TextList _valueList = new Id3v2TextList();

        private Id3v2FrameEncodingType _frameEncodingType;

        private static readonly Dictionary<Id3v2FrameEncodingType, byte[]> EncodingTypeDelimiters = new Dictionary<Id3v2FrameEncodingType, byte[]>
            {
                {
                    Id3v2FrameEncodingType.Default,
                    Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default).GetBytes(new[] { TextDelimiter })
                },
                {
                    Id3v2FrameEncodingType.UTF16BigEndian,
                    Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.UTF16BigEndian).GetBytes(new[] { TextDelimiter })
                },
                {
                    Id3v2FrameEncodingType.UTF16BigEndianWithoutBom, 
                    Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.UTF16BigEndianWithoutBom).GetBytes(new[] { TextDelimiter })
                },
                {
                    Id3v2FrameEncodingType.UTF16LittleEndian,
                    Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.UTF16LittleEndian).GetBytes(new[] { TextDelimiter })
                },
                {
                    Id3v2FrameEncodingType.UTF7,
                    Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.UTF7).GetBytes(new[] { TextDelimiter })
                },
                {
                    Id3v2FrameEncodingType.UTF8,
                    Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.UTF8).GetBytes(new[] { TextDelimiter })
                }
            };

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2TextFrame" /> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="identifier">The identifier of the frame type for the <see cref="Id3v2Version" /> supplied.</param>
        /// <exception cref="System.IO.InvalidDataException">identifier is not a valid identifier.</exception>
        /// <remarks>
        /// When the <paramref name="identifier"/> is not a valid/known identifier for the <paramref name="version"/>, it will look through all the known identifiers
        /// and see if a known identifier partly matches the <paramref name="identifier"/>. If found, it will get the proper identifier for the <paramref name="version"/>;
        /// otherwise, an <exception cref="InvalidDataException">invalid identifier exception</exception> is thrown.
        /// </remarks>
        public Id3v2TextFrame(Id3v2Version version, string identifier) : base(version)
        {
            if (!IsValidTextIdentifier(version, identifier))
            {
                // Maybe the identifier is actually a valid identifier but for the wrong version; try to find the 'real' identifier here.
                KeyValuePair<Id3v2TextFrameIdentifier, Dictionary<string, Id3v2Version[]>>[] pairs =
                    Identifiers.Where(
                        textFramePair =>
                        textFramePair.Value.OrderByDescending(f => f.Key).Any(f => f.Key.IndexOf(identifier, StringComparison.OrdinalIgnoreCase) >= 0))
                        .ToArray();

                // Grab the 'real' identifier for the version supplied.
                identifier = pairs.Any() ? pairs[0].Value.Where(t => t.Value.Contains(version)).Select(t => t.Key).FirstOrDefault() : null;

                if (String.IsNullOrEmpty(identifier))
                    throw new InvalidDataException("identifier is not a valid identifier.");
            }
            _identifier = identifier;
            BindValueListEvents();
        }

        ////------------------------------------------------------------------------------------------------------------------------------
        
        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                using (StreamBuffer stream = new StreamBuffer())
                {
                    // A string should look like this in byte form:
                    // TextEncoding + preamble + string1 + TextDelimiter + preamble + string2 + TextDelimiter + preamble + string3
                    Encoding encoding = Id3v2FrameEncoding.GetEncoding(TextEncoding);
                    byte[] textDelimiterBytes;
                    if (!EncodingTypeDelimiters.TryGetValue(_frameEncodingType, out textDelimiterBytes))
                        textDelimiterBytes = encoding.GetBytes(new[] { TextDelimiter });

                    byte[] preamble = Id3v2FrameEncoding.GetEncodingPreamble(TextEncoding);

                    // Text Encoding
                    stream.WriteByte(Id3v2FrameEncoding.GetEncodingTypeValue(TextEncoding));

                    // Loop through all non-empty strings.
                    // Writing empty strings will go wrong when reading them again...
                    List<string> values = Values.Where(f => !String.IsNullOrEmpty(f)).ToList();
                    for (int i = 0; i < values.Count; i++)
                    {
                        // Only write the text delimiter after the first value.
                        if (i > 0)
                            stream.Write(textDelimiterBytes);

                        // Write the first preamble after each value.
                        stream.Write(preamble);

                        // String
                        byte[] value = encoding.GetBytes(values[i]);
                        stream.Write(value);
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
                    // Read the used encoding from the stream
                    _frameEncodingType = Id3v2FrameEncoding.ReadEncodingTypeFromStream(stream);
                    Encoding encoding = Id3v2FrameEncoding.GetEncoding(_frameEncodingType);

                    // Since we're reading the data from a byte array, we can't assure that all values are valid, so unbind the events here.
                    UnbindValueListEvents();

                    // Clear the list.
                    Values.Clear();

                    // Read the values.
                    while (stream.Position < stream.Length)
                        Values.Add(stream.ReadString(encoding, TextDelimiter));

                    // Bind the events again.
                    BindValueListEvents();
                }
            }
        }

        /// <inheritdoc />
        public override string Identifier
        {
            get
            {
                Dictionary<string, Id3v2Version[]> entry =
                    Identifiers.Where(
                        i => i.Value != null && i.Value.Any(f => String.Equals(f.Key, base.Identifier, StringComparison.OrdinalIgnoreCase)))
                        .Select(i => i.Value)
                        .FirstOrDefault();

                return (entry != null)
                           ? entry.Where(d => d.Value != null && d.Value.Contains(Version)).Select(d => d.Key).FirstOrDefault() ?? base.Identifier
                           : _identifier;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the custom text delimiter.
        /// </summary>
        public static char TextDelimiter
        {
            get
            {
                return TextValueDelimiter;
            }
        }

        /// <summary>
        /// Gets or sets the text encoding type.
        /// </summary>
        /// <value>
        /// The text encoding.
        /// </value>
        /// <remarks>
        /// An <see cref="InvalidDataException"/> will be thrown when <see cref="Values"/> contains any entry not valid in the new <see cref="Id3v2FrameEncodingType"/>.
        /// </remarks>
        public Id3v2FrameEncodingType TextEncoding
        {
            get
            {
                return _frameEncodingType;
            }

            set
            {
                if (Values.Any(t => !String.IsNullOrEmpty(t) && !IsValidTextString(t, value, false)))
                {
                    throw new InvalidDataException(
                        "The values contains one or more string entries with one or more invalid characters for the specified frame encoding type.");
                }
                _frameEncodingType = value;
            }
        }

        /// <summary>
        /// Gets or sets a list of values.
        /// </summary>
        /// <value>
        /// A list of values.
        /// </value>
        /// <remarks>
        /// New lines are not allowed.
        /// </remarks>
        public Id3v2TextList Values
        {
            get
            {
                return _valueList;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the identifier as string.
        /// </summary>
        /// <param name="version">The <see cref="Id3v2Version"/>.</param>
        /// <param name="identifier">The <see cref="Id3v2TextFrameIdentifier"/>.</param>
        /// <returns>
        /// The identifier as string for the specified <see cref="Id3v2TextFrameIdentifier"/>, or null if not found.
        /// </returns>
        public static string GetIdentifier(Id3v2Version version, Id3v2TextFrameIdentifier identifier)
        {
            Dictionary<string, Id3v2Version[]> identifiers;
            return Identifiers.TryGetValue(identifier, out identifiers)
                       ? identifiers.Where(v => (v.Value == null) || v.Value.Contains(version)).Select(v => v.Key).FirstOrDefault()
                       : null;
        }

        /// <summary>
        /// Determines whether the text identifier is valid for the specified version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="identifier">The identifier.</param>
        /// <returns>
        ///   <c>true</c> if the identifier is valid for the specified version; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// A valid text identifier is made out of the characters capital A-Z and 0-9, and starts with the capital letter 'T'.
        /// </remarks>
        public static bool IsValidTextIdentifier(Id3v2Version version, string identifier)
        {
            if (identifier == null)
                throw new ArgumentNullException("identifier");

            return IsValidIdentifier(version, identifier) && identifier.StartsWith("T");
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Id3v2Frame frame)
        {
            return Equals(frame as Id3v2TextFrame);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <param name="textFrame">The <see cref="Id3v2TextFrame"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        /// <remarks>
        /// There may only be one <see cref="Id3v2TextFrame"/> frame of its kind in an <see cref="Id3v2Tag"/>.
        /// </remarks>
        public bool Equals(Id3v2TextFrame textFrame)
        {
            if (ReferenceEquals(null, textFrame))
                return false;

            if (ReferenceEquals(this, textFrame))
                return true;

            return (textFrame.Version == Version) && String.Equals(textFrame.Identifier, Identifier, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Version.GetHashCode() * 397) ^ (Identifier.GetHashCode() * 397) ^ (TextDelimiter.GetHashCode() * 397);
            }
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
            // See if the identifier is a known identifier.
            Dictionary<string, Id3v2Version[]> entry =
                Identifiers.Where(i => i.Value.Any(f => String.Equals(f.Key, base.Identifier, StringComparison.OrdinalIgnoreCase)))
                    .Select(i => i.Value)
                    .FirstOrDefault();

            // If the identifier is known, see if it exists in the given version.
            // If the identifier isn't known, see if the supplied version can support it.
            return (entry != null) ? entry.Any(d => d.Value.Contains(version)) : base.IsVersionSupported(version);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private void BindValueListEvents()
        {
            _valueList.ItemAdd += ValueAdd;

            _valueList.ItemReplace += ValueReplace;
        }

        private void UnbindValueListEvents()
        {
            _valueList.ItemAdd -= ValueAdd;

            _valueList.ItemReplace -= ValueReplace;
        }

        private void ValueAdd(object sender, ListItemAddEventArgs<string> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.Item == null)
                throw new NullReferenceException("e.Item may not be null");

            if (!IsValidTextString(e.Item, TextEncoding, false))
                throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");
        }

        private void ValueReplace(object sender, ListItemReplaceEventArgs<string> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.NewItem == null)
                throw new NullReferenceException("e.NewItem may not be null");

            if (!IsValidTextString(e.NewItem, TextEncoding, false))
                throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");
        }
    }
}
