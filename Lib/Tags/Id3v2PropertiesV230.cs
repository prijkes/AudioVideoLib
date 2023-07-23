/*
 * Date: 2011-08-12
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
        /// Gets or sets the position synchronization.
        /// </summary>
        /// <value>
        /// The position synchronization.
        /// </value>
        /// <remarks>
        /// This frame delivers information to the listener of how far into the audio stream he picked up; 
        /// in effect, it states the time offset of the first frame in the stream.
        /// The position is where in the audio the listener starts to receive, i.e. the beginning of the next frame.
        /// If this frame is used in the beginning of a file the value is always 0.
        /// <para />
        /// There may only be one <see cref="Id3v2PositionSynchronizationFrame"/> frame in each <see cref="Id3v2Tag"/>.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        public Id3v2PositionSynchronizationFrame PositionSynchronization
        {
            get
            {
                return (Version >= Id3v2Version.Id3v230) ? GetFrame<Id3v2PositionSynchronizationFrame>() : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v230)
                    return;

                if (value == null)
                    RemoveFrame(PositionSynchronization);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the terms of use.
        /// </summary>
        /// <value>
        /// The terms of use.
        /// </value>
        /// <remarks>
        /// This frame contains a brief description of the terms of use and ownership of the file.
        /// More detailed information concerning the legal terms might be available through <see cref="CopyrightInformation"/>.
        /// <para />
        /// There may only be one <see cref="Id3v2TermsOfUseFrame"/> frame in an <see cref="Id3v2Tag"/>.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        public Id3v2TermsOfUseFrame TermsOfUse
        {
            get
            {
                return (Version >= Id3v2Version.Id3v230) ? GetFrame<Id3v2TermsOfUseFrame>() : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v230)
                    return;

                if (value == null)
                    RemoveFrame(TermsOfUse);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the ownership.
        /// </summary>
        /// <value>
        /// The ownership.
        /// </value>
        /// <remarks>
        /// The ownership frame might be used as a reminder of a made transaction or, if signed, as proof.
        /// Note that the <see cref="Id3v2TermsOfUseFrame"/> frame and <see cref="FileOwner"/> property are good to use in conjunction with this one.
        /// <para />
        /// There may only be one <see cref="Id3v2OwnershipFrame"/> frame in an <see cref="Id3v2Tag"/>.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        public Id3v2OwnershipFrame Ownership
        {
            get
            {
                return (Version >= Id3v2Version.Id3v230) ? GetFrame<Id3v2OwnershipFrame>() : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v230)
                    return;

                if (value == null)
                    RemoveFrame(Ownership);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets the commercial frame.
        /// </summary>
        /// <value>
        /// The commercial frame.
        /// </value>
        /// <remarks>
        /// This frame enables several competing offers in the same tag by bundling all needed information.
        /// That makes this frame rather complex but it's an easier solution 
        /// than if one tries to achieve the same result with several frames.
        /// <para />
        /// There may be more than one <see cref="Id3v2CommercialFrame"/> in an <see cref="Id3v2Tag"/>, 
        /// but no two may be identical.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        public Id3v2CommercialFrame Commercial
        {
            get
            {
                return (Version >= Id3v2Version.Id3v230) ? GetFrame<Id3v2CommercialFrame>() : null;
            }

            set
            {
                if (Version < Id3v2Version.Id3v230)
                    return;

                if (value == null)
                    RemoveFrame(Commercial);
                else
                    SetFrame(value);
            }
        }

        /// <summary>
        /// Gets or sets a list of encryption method registrations.
        /// </summary>
        /// <value>
        /// A list of encryption method registrations.
        /// </value>
        /// <remarks>
        /// To identify with which method a frame has been encrypted the encryption method must be registered in the tag with this frame.
        /// <para />
        /// There may be several <see cref="Id3v2EncryptionMethodRegistrationFrame"/> frames in an <see cref="Id3v2Tag"/> 
        /// but only one containing the same <see cref="Id3v2EncryptionMethodRegistrationFrame.MethodSymbol"/> 
        /// and only one containing the same <see cref="Id3v2EncryptionMethodRegistrationFrame.OwnerIdentifier"/>.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2EncryptionMethodRegistrationFrame> EncryptionMethodRegistrations
        {
            get
            {
                return GetFrameCollection<Id3v2EncryptionMethodRegistrationFrame>();
            }

            set
            {
                RemoveFrames<Id3v2EncryptionMethodRegistrationFrame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }

        /// <summary>
        /// Gets or sets a list of group identification registrations.
        /// </summary>
        /// <value>
        /// A list of group identification registrations.
        /// </value>
        /// <remarks>
        /// This frame enables grouping of otherwise unrelated frames.
        /// This can be used when some frames are to be signed.
        /// To identify which frames belongs to a set of frames a group identifier must be registered in the tag with this frame.
        /// <para />
        /// There may be several <see cref="Id3v2GroupIdentificationRegistrationFrame"/> frames in an <see cref="Id3v2Tag"/> 
        /// but only one containing the same <see cref="Id3v2GroupIdentificationRegistrationFrame.GroupSymbol"/> 
        /// and only one containing the same <see cref="Id3v2GroupIdentificationRegistrationFrame.OwnerIdentifier"/>.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2GroupIdentificationRegistrationFrame> GroupIdentificationRegistrations
        {
            get
            {
                return GetFrameCollection<Id3v2GroupIdentificationRegistrationFrame>();
            }

            set
            {
                RemoveFrames<Id3v2GroupIdentificationRegistrationFrame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }

        /// <summary>
        /// Gets or sets a list of private frames.
        /// </summary>
        /// <value>
        /// A list of private frames.
        /// </value>
        /// <remarks>
        /// This frame is used to contain information from a software producer 
        /// that its program uses and does not fit into the other frames.
        /// <para />
        /// The tag may contain more than one <see cref="Id3v2PrivateFrame"/> frame 
        /// but only with different <see cref="Id3v2PrivateFrame.PrivateData"/>.
        /// <para />
        /// It is recommended to keep the number of <see cref="Id3v2PrivateFrame"/> frames as low as possible.
        /// <para />
        /// This frame has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2PrivateFrame> PrivateFrames
        {
            get
            {
                return GetFrameCollection<Id3v2PrivateFrame>();
            }

            set
            {
                RemoveFrames<Id3v2PrivateFrame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }

        /// <summary>
        /// Gets or sets a list of experimental relative volume adjustment (2).
        /// </summary>
        /// <value>
        /// A list of experimental relative volume adjustment (2).
        /// </value>
        /// <remarks>
        /// The <a hef="http://normalize.nongnu.org/">normalize</a> program writes these when creating a <see cref="Id3v2Tag"/> with version <see cref="Id3v2Version.Id3v230"/>.
        /// It is the same as an <see cref="Id3v2RelativeVolumeAdjustment2Frame"/> but has been back-ported to version <see cref="Id3v2Version.Id3v230"/>.
        /// <para />
        /// There may be more than one <see cref="Id3v2ExperimentalRelativeVolumeAdjustment2Frame"/> frame in each <see cref="Id3v2Tag"/>, 
        /// but only one with the same <see cref="Id3v2ExperimentalRelativeVolumeAdjustment2Frame.Identification"/>.
        /// <para/>
        /// This frame has been added as of <see cref="Id3v2Version.Id3v230"/>.
        /// </remarks>
        public Id3v2FrameCollection<Id3v2ExperimentalRelativeVolumeAdjustment2Frame> ExperimentalRelativeVolumeAdjustment2
        {
            get
            {
                return GetFrameCollection<Id3v2ExperimentalRelativeVolumeAdjustment2Frame>();
            }

            set
            {
                RemoveFrames<Id3v2ExperimentalRelativeVolumeAdjustment2Frame>(false);
                if (value != null)
                    SetFrames(value);

                ValidateFrames();
            }
        }
    }
}
