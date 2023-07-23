/*
 * Date: 2011-08-22
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
using System;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing those involved and how they were involved.
    /// </summary>
    public class Id3v2InvolvedPeople
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2InvolvedPeople"/> class.
        /// </summary>
        /// <param name="involvement">The involvement of the involvee.</param>
        /// <param name="involvee">The involvee.</param>
        public Id3v2InvolvedPeople(string involvement, string involvee)
        {
            if (involvement == null)
                throw new ArgumentNullException("involvement");

            if (involvee == null)
                throw new ArgumentNullException("involvee");

            Involvement = involvement;
            Involvee = involvee;
        }

        /// <summary>
        /// Gets the involvement.
        /// </summary>
        /// <value>
        /// The involvement of the involvee.
        /// </value>
        public string Involvement { get; private set; }

        /// <summary>
        /// Gets the involvee.
        /// </summary>
        /// <value>
        /// The involvee.
        /// </value>
        public string Involvee { get; private set; }
    }
}
