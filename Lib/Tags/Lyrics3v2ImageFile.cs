/*
 * Date: 2012-12-09
 * Sources used: 
 *  http://id3.org/Lyrics3v2
 */
using System;
using System.Collections.Generic;

using AudioVideoLib.Collections;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store a Lyrics3v2 image file.
    /// </summary>
    public sealed class Lyrics3v2ImageFile
    {
        // Description can be up to 250 chars long.
        private const int MaxDescriptionLength = 250;

        private string _description;

        private readonly EventList<TimeSpan> _timeStamps = new EventList<TimeSpan>();

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the filename.
        /// </summary>
        /// <value>
        /// The filename.
        /// </value>
        /// <remarks>
        /// Filename can be in one of these formats: 
        /// * Filename only - when the image is located in the same path as the MP3 file (preferred, since if you move the mp3 file this will still be correct)
        /// <para />
        /// * Relative Path + Filename - when the image is located in a subdirectory below the MP3 file (i.e. images\cover.jpg)
        /// <para />
        /// * Full path + Filename - when the image is located in a totally different path or drive.
        /// This will not work if the image is moved or drive letters has changed, and so should be avoided if possible (i.e. c:\images\artist.jpg) 
        /// </remarks>
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        /// <remarks>
        /// Description and is optional.
        /// <para />
        /// The description can be up to 250 chars long, and will be truncated when longer.
        /// </remarks>
        public string Description
        {
            get
            {
                return _description;
            }

            set
            {
                _description = (!String.IsNullOrEmpty(value) && value.Length > MaxDescriptionLength) ? value.Substring(0, MaxDescriptionLength) : value;
            }
        }

        /// <summary>
        /// Gets or sets a list of time stamps for the image file.
        /// </summary>
        /// <value>
        /// A list of time stamps for the image file.
        /// </value>
        /// <remarks>
        /// Timestamps are optional.
        /// <para />
        /// If an image has a timestamp, then the visible image should automatically switch to that image on the timestamp play time, 
        /// just the same as the selected lyrics line is switched based on timestamps.
        /// </remarks>
        public IList<TimeSpan> TimeStamps
        {
            get
            {
                return _timeStamps;
            }
        }
    }
}
