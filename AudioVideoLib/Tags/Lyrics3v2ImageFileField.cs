/*
 * Date: 2012-11-16
 * Sources used: 
 *  http://id3.org/Lyrics3v2
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using AudioVideoLib.Collections;
using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store a Lyrics3v2 image file field.
    /// </summary>
    public sealed class Lyrics3v2ImageFileField : Lyrics3v2Field
    {
        // Image lines include filename, description and timestamp separated by delimiter - two ASCII chars 124 ("||").
        private const string Delimiter = "||";

        private static readonly Regex TimeStampRegEx = new Regex(@"\[(\d+):(\d+)\]");

        private readonly EventList<Lyrics3v2ImageFile> _imageFiles = new EventList<Lyrics3v2ImageFile>();

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Lyrics3v2ImageFileField"/> class.
        /// </summary>
        public Lyrics3v2ImageFileField() : base("IMG")
        {
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the image files.
        /// </summary>
        /// <value>
        /// The image files.
        /// </value>
        public IList<Lyrics3v2ImageFile> ImageFiles
        {
            get
            {
                return _imageFiles;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        public override byte[] Data
        {
            get
            {
                StringBuilder strBuilder = new StringBuilder();
                for (int i = 0; i < ImageFiles.Count; i++)
                {
                    Lyrics3v2ImageFile imageFile = ImageFiles[i];
                    if (!String.IsNullOrEmpty(imageFile.Filename))
                        strBuilder.Append(imageFile.Filename);

                    strBuilder.Append(Delimiter);

                    if (!String.IsNullOrEmpty(imageFile.Description))
                        strBuilder.Append(imageFile.Description);

                    strBuilder.Append(Delimiter);

                    foreach (TimeSpan timestamp in imageFile.TimeStamps)
                        strBuilder.Append(timestamp.ToString(@"\[mm\:ss\]"));

                    if (i + 1 < ImageFiles.Count)
                        strBuilder.Append(Lyrics3v2Tag.NewLine);
                }
                return Encoding.ASCII.GetBytes(strBuilder.ToString());
            }

            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (!IsValidData(value))
                    throw new InvalidDataException("Data contains one ore more invalid characters.");

                using (StreamBuffer sb = new StreamBuffer(value))
                {
                    string images = sb.ReadString(Encoding.ASCII);

                    _imageFiles.Clear();

                    string[] entries = images.Split(new[] { Lyrics3v2Tag.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string entry in entries)
                    {
                        string[] imageLine = entry.Split(new[] { Delimiter }, StringSplitOptions.None);
                        Lyrics3v2ImageFile image = new Lyrics3v2ImageFile
                        {
                            Filename = imageLine.Length > 0 ? imageLine[0] : null,
                            Description = imageLine.Length > 1 ? imageLine[1] : null,
                        };

                        List<TimeSpan> imageTimeSpans = new List<TimeSpan>();
                        if (imageLine.Length > 2)
                        {
                            MatchCollection matches = TimeStampRegEx.Matches(imageLine[2]);
                            imageTimeSpans.AddRange(
                                from Match match in matches
                                let minutes = Int32.Parse(match.Groups[1].Value)
                                let seconds = Int32.Parse(match.Groups[2].Value)
                                select new TimeSpan(0, 0, minutes, seconds)
                            );
                        }

                        foreach (TimeSpan t in imageTimeSpans.OrderBy(x => x))
                            image.TimeStamps.Add(t);

                        _imageFiles.Add(image);
                    }
                }
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(Lyrics3v2Field audioFrame)
        {
            return Equals(audioFrame as Lyrics3v2ImageFileField);
        }

        /// <summary>
        /// Equals the specified <see cref="Lyrics3v2ImageFileField"/>.
        /// </summary>
        /// <param name="field">The <see cref="Lyrics3v2ImageFileField"/>.</param>
        /// <returns>
        /// true if equal; false otherwise.
        /// </returns>
        public bool Equals(Lyrics3v2ImageFileField field)
        {
            if (ReferenceEquals(null, field))
                return false;

            if (ReferenceEquals(this, field))
                return true;

            return String.Equals(field.Identifier, Identifier, StringComparison.OrdinalIgnoreCase) && field.ImageFiles.SequenceEqual(ImageFiles);
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
                return Identifier.GetHashCode() * 397;
            }
        }
    }
}
