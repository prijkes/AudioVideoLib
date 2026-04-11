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

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an Id3v2 tag.
    /// </summary>
    public partial class Id3v2Tag
    {
        /// <summary>
        /// Gets or sets the audio seek point index.
        /// </summary>
        /// <value>
        /// The audio seek point index.
        /// </value>
        /// <remarks>
        /// Audio files with variable bitrates are intrinsically difficult to deal with in the case of seeking within the file.
        /// The <see cref="Id3v2AudioSeekPointIndexFrame"/> frame makes seeking easier by providing a list a seek points within the audio file.
        /// The seek points are a fractional offset within the audio data, 
        /// providing a starting point from which to find an appropriate point to start decoding.
        /// The presence of an <see cref="Id3v2AudioSeekPointIndexFrame"/> frame requires the existence of the <see cref="Id3v2Tag.Length"/> property, 
        /// indicating the duration of the file in milliseconds.
        /// <para />
        /// There may only be one <see cref="Id3v2AudioSeekPointIndexFrame"/> frame in an <see cref="Id3v2Tag"/>.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2AudioSeekPointIndexFrame AudioSeekPointIndex
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) ? GetFrame<Id3v2AudioSeekPointIndexFrame>() : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v240)
                    return;

                if (value == null)
                    RemoveFrame(AudioSeekPointIndex);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets a list equalisation (2).
        /// </summary>
        /// <value>
        /// A list of equalisation (2).
        /// </value>
        /// <remarks>
        /// This frame allows the user to predefine an equalisation curve within the audio file.
        /// <para />
        /// There may be more than one <see cref="Id3v2Equalisation2Frame"/> frame in each <see cref="Id3v2Tag"/>, 
        /// but only one with the <see cref="Id3v2Equalisation2Frame.Identification"/>.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2Equalisation2Frame> Equalisation2
        {
            get
            {
                return GetFrameCollection<Id3v2Equalisation2Frame>();
            }

            set
            {
                RemoveFrames<Id3v2Equalisation2Frame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }

        /// <summary>
        /// Gets or sets a list of relative volume adjustment (2).
        /// </summary>
        /// <value>
        /// A list of relative volume adjustment (2).
        /// </value>
        /// <remarks>
        /// This frame allows the user to say how much he wants to increase/decrease the volume on each channel when the file is played.
        /// The purpose is to be able to align all files to a reference volume, so that you don't have to change the volume constantly.
        /// This frame may also be used to balance adjust the audio.
        /// The volume adjustment is encoded as a fixed point decibel value, 16 bit signed integer representing (adjustment*512), 
        /// giving +/- 64 dB with a precision of 0.001953125 dB. E.g. +2 dB is stored as 0x04 0x00 and -2 dB is 0xFC 0x00.
        /// <para />
        /// There may be more than one <see cref="Id3v2RelativeVolumeAdjustment2Frame"/> frame in each <see cref="Id3v2Tag"/>, 
        /// but only one with the same <see cref="Id3v2RelativeVolumeAdjustment2Frame.Identification"/>.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2RelativeVolumeAdjustment2Frame> RelativeVolumeAdjustment2
        {
            get
            {
                return GetFrameCollection<Id3v2RelativeVolumeAdjustment2Frame>();
            }

            set
            {
                RemoveFrames<Id3v2RelativeVolumeAdjustment2Frame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }

        /// <summary>
        /// Gets or sets the seek frame.
        /// </summary>
        /// <value>
        /// The seek value.
        /// </value>
        /// <remarks>
        /// This frame indicates where other tags in a file/stream can be found.
        /// <para />
        /// There may only be one <see cref="Id3v2SeekFrame"/> in an <see cref="Id3v2Tag"/>.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2SeekFrame Seek
        {
            get
            {
                return (Version >= Id3v2Version.Id3v240) ? GetFrame<Id3v2SeekFrame>() : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v240)
                    return;

                if (value == null)
                    RemoveFrame(Seek);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets a list of signatures.
        /// </summary>
        /// <value>
        /// A list of signatures.
        /// </value>
        /// <remarks>
        /// This frame enables a group of frames, grouped with the 'Group identification registration', to be signed.
        /// Although signatures can reside inside the registration frame, 
        /// it might be desired to store the signature elsewhere, e.g. in watermarks.
        /// <para />
        /// There may be more than one <see cref="Id3v2SignatureFrame"/> in an <see cref="Id3v2Tag"/>, but no two may be identical.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v240"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2SignatureFrame> Signatures
        {
            get
            {
                return GetFrameCollection<Id3v2SignatureFrame>();
            }

            set
            {
                RemoveFrames<Id3v2SignatureFrame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }
    }
}
