/*
 * Date: 2012-12-27
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 *  http://web.archive.org/web/20080415005443/http://replaygain.hydrogenaudio.org/rg_data_format.html
 *  http://web.archive.org/web/20081223230059/http://replaygain.hydrogenaudio.org/file_format_id3v2.html
 */
using System;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class for storing ReplayGain.
    /// </summary>
    /// <remarks>
    /// <a href="http://wiki.hydrogenaudio.org/index.php?title=ReplayGain">ReplayGain</a> is the name of a technique invented to achieve the same perceived playback loudness of audio files.
    /// It defines an algorithm to measure the perceived loudness of audio data.
    /// <para />
    /// ReplayGain allows the loudness of each song within a collection of songs to be consistent.
    /// This is called 'Track Gain' (or 'Radio Gain' in earlier parlance).
    /// It also allows the loudness of a specific sub-collection (an "album") to be consistent with the rest of the collection, while allowing the dynamics from song to song on the album to remain intact.
    /// This is called 'Album Gain' (or 'Audiophile Gain' in earlier parlance). This is especially important when listening to classical music albums, because quiet tracks need to remain a certain degree quieter than the louder ones.
    /// <para />
    /// ReplayGain is different from peak normalization.
    /// Peak normalization merely ensures that the peak amplitude reaches a certain level.
    /// This does not ensure equal loudness.
    /// The ReplayGain technique measures the <i>effective power</i> of the waveform (i.e. the RMS power after applying an "equal loudness contour"), and then adjusts the amplitude of the waveform accordingly.
    /// The result is that Replay Gained waveforms are usually more uniformly amplified than peak-normalized waveforms. 
    /// </remarks>
    public class Id3v2ReplayGain
    {
        /// <summary>
        /// The name code mask.
        /// </summary>
        public const int NameCodeMask = 0xE000;

        /// <summary>
        /// The originator code mask.
        /// </summary>
        public const int OriginatorCodeMask = 0x1C00;

        /// <summary>
        /// The sign mask.
        /// </summary>
        public const int SignMask = 0x200;

        /// <summary>
        /// The adjustment mask.
        /// </summary>
        public const int AdjustmentMask = 0x1FF;

        private Id3v2ReplayGainSign _sign;

        private short _adjustment;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the name code.
        /// </summary>
        /// <value>
        /// The name code.
        /// </value>
        /// <remarks>
        /// For each Replay Gain Adjustment field, if the name code is <see cref="Id3v2NameCode.NotSet"/>, 
        /// then players should ignore the rest of that individual field.
        /// <para />
        /// For each Replay Gain Adjustment field, 
        /// if the name code is an unrecognized value (i.e. not <see cref="Id3v2NameCode.RadioGainAdjustment"/> or <see cref="Id3v2NameCode.AudiophileGainAdjustment"/>), 
        /// then players should ignore the rest of that individual field.
        /// <para />
        /// If no valid Replay Gain Adjustment fields are found (i.e. all name codes are either <see cref="Id3v2NameCode.NotSet"/> or unknown), 
        /// then the player should proceed as if the file contained no Replay Gain Adjustment information.
        /// </remarks>
        public Id3v2NameCode NameCode { get; set; }

        /// <summary>
        /// Gets or sets the originator code.
        /// </summary>
        /// <value>
        /// The originator code.
        /// </value>
        /// <remarks>
        /// For each Replay Gain Adjustment field, if the name code is valid, but the Originator code is <see cref="Id3v2OriginatorCode.Unspecified"/>, 
        /// then the player should ignore that Replay Gain adjustment field.
        /// <para />
        /// For each Replay Gain Adjustment field, if the name code is valid, but the Originator code is unknown, 
        /// then the player should &lt;b&gt;still&lt;/b&gt; use the information within that Replay Gain Adjustment field.
        /// This is because, even if we are unsure as to how the adjustment was determined, any valid Replay Gain adjustment is more useful than none at all.
        /// <para />
        /// If no valid Replay Gain Adjustment fields are found (i.e. all originator codes are <see cref="Id3v2OriginatorCode.Unspecified"/>), 
        /// then the player should proceed as if the file contained no Replay Gain Adjustment information.
        /// </remarks>
        public Id3v2OriginatorCode OriginatorCode { get; set; }

        /// <summary>
        /// Gets or sets the replay gain adjustment sign.
        /// </summary>
        /// <value>
        /// The replay gain adjustment sign value.
        /// </value>
        /// <remarks>
        /// The <see cref="Adjustment"/> field can not be zero when this field has been set to <see cref="Id3v2ReplayGainSign.Negative"/>.
        /// Doing so will cause an <see cref="InvalidOperationException"/> exception.
        /// </remarks>
        public Id3v2ReplayGainSign Sign
        {
            get
            {
                return _sign;
            }

            set
            {
                if ((value == Id3v2ReplayGainSign.Negative) && (_adjustment == 0))
                    throw new InvalidOperationException("Replay gain adjustment can not have a negative zero.");

                _sign = value;
            }
        }

        /// <summary>
        /// Gets or sets the replay gain adjustment.
        /// </summary>
        /// <value>
        /// The replay gain adjustment.
        /// </value>
        /// <remarks>
        /// The replay gain adjustment can not have a negative zero.
        /// That is, this field can not be zero when <see cref="Sign"/> has been set to <see cref="Id3v2ReplayGainSign.Negative"/>.
        /// Doing so will cause an <see cref="InvalidOperationException"/> exception.
        /// </remarks>
        public short Adjustment
        {
            get
            {
                return _adjustment;
            }

            set
            {
                if ((_sign == Id3v2ReplayGainSign.Negative) && (value == 0))
                    throw new InvalidOperationException("Replay gain adjustment can not have a negative zero.");

                _adjustment = value;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Writes the <see cref="Id3v2ReplayGain"/> into a byte array.
        /// </summary>
        /// <returns>
        /// A byte array that represents the <see cref="Id3v2ReplayGain"/> instance.
        /// </returns>
        public byte[] ToByteArray()
        {
            using (StreamBuffer stream = new StreamBuffer())
            {
                int value = ((int)NameCode & NameCodeMask) << 13;
                value &= ((int)OriginatorCode & OriginatorCodeMask) << 10;
                value &= ((int)Sign & SignMask) << 9;
                value &= Adjustment & AdjustmentMask;
                stream.WriteShort((short)value);
                return stream.ToByteArray();
            }
        }
    }
}
