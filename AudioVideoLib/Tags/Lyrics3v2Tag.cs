/*
 * Date: 2012-11-09
 * Sources used: 
 *  http://id3.org/Lyrics3v2
 *  http://www.mpx.cz/mp3manager/tags.htm
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using AudioVideoLib.Collections;
using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store a Lyrics3v2 tag.
    /// </summary>
    public sealed partial class Lyrics3v2Tag : IAudioTag
    {
        /// <summary>
        /// The header identifier for a <see cref="Lyrics3Tag"/>.
        /// </summary>
        public const string HeaderIdentifier = "LYRICSBEGIN";

        /// <summary>
        /// The footer identifier for a <see cref="Lyrics3Tag"/>.
        /// </summary>
        public const string FooterIdentifier = "LYRICS200";

        /// <summary>
        /// Delimiter used between lines.
        /// </summary>
        public const string NewLine = "\r\n";

        /// <summary>
        /// The tag size length, in bytes.
        /// </summary>
        public const int TagSizeLength = 6;

        private static readonly byte[] HeaderIdentifierBytes = System.Text.Encoding.ASCII.GetBytes(HeaderIdentifier);

        private static readonly byte[] FooterIdentifierBytes = System.Text.Encoding.ASCII.GetBytes(FooterIdentifier);

        private readonly EventList<Lyrics3v2Field> _fields = new EventList<Lyrics3v2Field>();

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the fields in the tag.
        /// </summary>
        /// <value>A list of <see cref="Lyrics3v2Field"/>s in the tag.</value>
        public IEnumerable<Lyrics3v2Field> Fields
        {
            get { return _fields.AsReadOnly(); }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as Lyrics3v2Tag);
        }

        /// <inheritdoc/>
        public bool Equals(IAudioTag other)
        {
            return Equals(other as Lyrics3v2Tag);
        }

        /// <summary>
        /// Equals the specified <see cref="Lyrics3Tag"/>.
        /// </summary>
        /// <param name="tag">The <see cref="Lyrics3Tag"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        public bool Equals(Lyrics3v2Tag tag)
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
                return 0;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the field of type T.
        /// </summary>
        /// <typeparam name="T">The field type.</typeparam>
        /// <returns>
        /// The field of type T if found; otherwise, null.
        /// </returns>
        public T GetField<T>() where T : Lyrics3v2Field
        {
            return _fields.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Gets the <see cref="Lyrics3v2TextField"/>.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>The <see cref="Lyrics3v2TextField"/> if found; otherwise, null.</returns>
        public Lyrics3v2TextField GetField(Lyrics3v2TextFieldIdentifier identifier)
        {
            string id = Lyrics3v2TextField.GetIdentifier(identifier);
            return _fields.OfType<Lyrics3v2TextField>().FirstOrDefault(f => String.Equals(f.Identifier, id, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the first field of type T with a matching field identifier.
        /// </summary>
        /// <typeparam name="T">The field type.</typeparam>
        /// <param name="identifier">The identifier of the field.</param>
        /// <returns>
        /// The first field of type T with a matching field identifier if found; otherwise, null.
        /// </returns>
        public T GetField<T>(string identifier) where T : Lyrics3v2Field
        {
            if (identifier == null)
                throw new ArgumentNullException("identifier");

            return _fields.OfType<T>().FirstOrDefault(f => String.Equals(f.Identifier, identifier, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all fields of type T.
        /// </summary>
        /// <typeparam name="T">The field type.</typeparam>
        /// <returns>
        /// A list of fields of type T.
        /// </returns>
        public IEnumerable<T> GetFields<T>() where T : Lyrics3v2Field
        {
            return _fields.OfType<T>();
        }

        /// <summary>
        /// Updates the first field with a matching field identifier if found; else, adds a new field.
        /// </summary>
        /// <param name="field">Field to add to the <see cref="Lyrics3v2Tag"/>.</param>
        public void SetField(Lyrics3v2Field field)
        {
            if (field == null)
                throw new ArgumentNullException("field");

            int i, fieldCount = _fields.Count;
            for (i = 0; i < fieldCount; i++)
            {
                if (!ReferenceEquals(_fields[i], field) && !String.Equals(_fields[i].Identifier, field.Identifier, StringComparison.OrdinalIgnoreCase))
                    continue;

                _fields[i] = field;
                break;
            }

            if (i == fieldCount)
                _fields.Add(field);
        }

        /// <summary>
        /// Updates a list of fields with a matching identifier if found; else, adds them.
        /// </summary>
        /// <param name="fields">The fields.</param>
        public void SetFields(IEnumerable<Lyrics3v2Field> fields)
        {
            if (fields == null)
                throw new ArgumentNullException("fields");

            foreach (Lyrics3v2Field field in fields)
                SetField(field);
        }

        /// <summary>
        /// Removes the first field with a matching identifier.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if identifier is null.</exception>
        public void RemoveField(string identifier)
        {
            if (identifier == null)
                throw new ArgumentNullException("identifier");

            Lyrics3v2Field field = _fields.FirstOrDefault(f => String.Equals(f.Identifier, identifier, StringComparison.OrdinalIgnoreCase));
            if (field != null)
                _fields.Remove(field);
        }

        /// <summary>
        /// Removes the field.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if field is null.</exception>
        public void RemoveField(Lyrics3v2Field field)
        {
            if (field == null)
                throw new ArgumentNullException("field");

            _fields.RemoveAll(f => ReferenceEquals(f, field));
        }

        /// <summary>
        /// Removes all fields with a matching identifier.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if identifier is null.</exception>
        public void RemoveFields(string identifier)
        {
            if (identifier == null)
                throw new ArgumentNullException("identifier");

            _fields.RemoveAll(f => String.Equals(f.Identifier, identifier, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Removes all fields of type T.
        /// </summary>
        /// <typeparam name="T">A class of type <see cref="Lyrics3v2Field" />.</typeparam>
        public void RemoveFields<T>() where T : Lyrics3v2Field
        {
            _fields.RemoveAll(f => f is T);
        }

        /// <summary>
        /// Removes the fields.
        /// </summary>
        /// <param name="fields">The fields.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if fields is null.</exception>
        public void RemoveFields(IEnumerable<Lyrics3v2Field> fields)
        {
            if (fields == null)
                throw new ArgumentNullException("fields");

            foreach (Lyrics3v2Field field in fields)
                RemoveField(field);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public byte[] ToByteArray()
        {
            using (StreamBuffer buffer = new StreamBuffer())
            {
                buffer.Write(HeaderIdentifierBytes);

                foreach (byte[] byteField in _fields.Select(field => field.ToByteArray()))
                    buffer.Write(byteField);

                buffer.WriteString(buffer.Length.ToString("D" + TagSizeLength, CultureInfo.InvariantCulture));
                buffer.Write(FooterIdentifierBytes);
                return buffer.ToByteArray();
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Lyrics3v2";
        }
    }
}
