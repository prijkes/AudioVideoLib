/*
 * Date: 2010-08-12
 * Sources used:
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://phoxis.org/2010/05/08/synch-safe/
 *  http://en.wikipedia.org/wiki/Synchsafe
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 *  http://internet.ls-la.net/folklore/url-regexpr.html
 */

using System;
using System.Collections.Generic;
using System.Linq;

using AudioVideoLib.Collections;
using AudioVideoLib.Cryptography;
using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store an ID3v2 tag.
    /// </summary>
    /// <remarks>
    /// An <see cref="Id3v2Tag"/> contains a header, <see cref="Id3v2Frame"/>s and optional <see cref="PaddingSize"/>.
    /// </remarks>
    public sealed partial class Id3v2Tag : IAudioTag
    {
        /// <summary>
        /// The max amounts of <see cref="Id3v2Frame"/>s an <see cref="Id3v2Tag"/> can have.
        /// </summary>
        //// totalFrames <= MaxAllowedFrames
        public const int MaxAllowedFrames = (1024 * 1024) - 1; // or 0x000FFFFF

        /// <summary>
        /// The max size the <see cref="Id3v2Tag"/> can be.
        /// </summary>
        /// <remarks>
        /// The max size of an <see cref="Id3v2Tag"/> is 256MB.
        /// </remarks>
        //// The Id3v2 size is stored as a 32 bit synchsafe integer, making a total of 28 effective bits (representing up to 256MB).
        //// Hence, a larger value indicates an invalid size value.
        //// _totalFramesSize <= MaxAllowedSize
        public const int MaxAllowedSize = (1024 * 1024 * 256) - 1; // or 0x0FFFFFFF

        private readonly EventList<Id3v2Frame> _frames = new EventList<Id3v2Frame>();

        private Id3v2ExtendedHeader _extendedHeader;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2Tag"/> class.
        /// </summary>
        public Id3v2Tag()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2Tag"/> class.
        /// </summary>
        /// <param name="version">The tag version.</param>
        public Id3v2Tag(Id3v2Version version)
        {
            Version = version;

            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id3v2Tag" /> class.
        /// </summary>
        /// <param name="version">The tag version.</param>
        /// <param name="flags">The flags.</param>
        public Id3v2Tag(Id3v2Version version, int flags)
        {
            Version = version;

            Flags = flags;

            Initialize();
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the extended header.
        /// </summary>
        /// <value>
        /// The extended header.
        /// </value>
        public Id3v2ExtendedHeader ExtendedHeader
        {
            get
            {
                return _extendedHeader;
            }

            set
            {
                UseExtendedHeader = (value != null);
                _extendedHeader = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to preserve frames after tag alteration.
        /// </summary>
        /// <value>
        /// <c>true</c> to preserve frames after tag alteration; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// When set to true, all frames will be written to the byte array when calling <see cref="ToByteArray"/>, 
        /// regardless of the frame's <see cref="Id3v2Frame.TagAlterPreservation"/> flag.
        /// <para />
        /// Default is true.
        /// </remarks>
        public bool PreserveFramesAfterTagAlteration { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to preserve frames after file alteration.
        /// </summary>
        /// <value>
        /// <c>true</c> to preserve frames after file alteration; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// When set to true, all frames will be written to the byte array when calling <see cref="ToByteArray"/>, 
        /// regardless of the frame's <see cref="Id3v2Frame.FileAlterPreservation"/> flag.
        /// <para />
        /// Default is true.
        /// </remarks>
        public bool PreserveFramesAfterFileAlteration { get; set; }

        /// <summary>
        /// Gets a read-only list of frames in the tag.
        /// </summary>
        /// <value>A read-only list of <see cref="Id3v2Frame"/>s in the tag.</value>
        public IEnumerable<Id3v2Frame> Frames
        {
            get
            {
                return _frames.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets or sets the amount of bytes to use as padding.
        /// </summary>
        /// <remarks>
        /// The 'Size of padding' is simply the total tag size excluding the frames and the headers, in other words the padding.
        /// <para />
        /// This does not include the <see cref="Id3v2ExtendedHeader.PaddingSize"/>.
        /// </remarks>
        public int PaddingSize { get; set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as Id3v2Tag);
        }

        /// <inheritdoc/>
        public bool Equals(IAudioTag other)
        {
            return Equals(other as Id3v2Tag);
        }

        /// <summary>
        /// Equals the specified <see cref="Id3v2Tag"/>.
        /// </summary>
        /// <param name="tag">The <see cref="Id3v2Tag"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        public bool Equals(Id3v2Tag tag)
        {
            if (ReferenceEquals(null, tag))
                return false;

            if (ReferenceEquals(this, tag))
                return true;

            //EqualityComparer<Id3v2Frame> comparer = EqualityComparer<Id3v2Frame>.Default;
            //using (IEnumerator<Id3v2Frame> e1 = tag.Frames.GetEnumerator())
            //{
            //    using (IEnumerator<Id3v2Frame> e2 = Frames.GetEnumerator())
            //    {
            //        while (e1.MoveNext())
            //        {
            //            if (!(e2.MoveNext() && comparer.Equals(e1.Current, e2.Current)))
            //            {
            //                comparer.Equals(e1.Current, e2.Current);
            //                return false;
            //            }
            //        }

            //        if (e2.MoveNext())
            //            return false;
            //    }
            //}
            //return true;

            return (tag.Version == Version) && (tag.Flags == Flags) && tag.ExtendedHeader.Equals(ExtendedHeader) && tag.Frames.SequenceEqual(Frames);
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
                return (Version.GetHashCode() * 397) ^ (UseHeader.GetHashCode() * 397);
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the first frame of type T.
        /// </summary>
        /// <typeparam name="T">The frame type.</typeparam>
        /// <returns>
        /// The first frame of type T if found; otherwise, null.
        /// </returns>
        public T GetFrame<T>() where T : Id3v2Frame
        {
            return _frames.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Gets the <see cref="Id3v2TextFrame"/>.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>The <see cref="Id3v2TextFrame"/> if found; otherwise, null.</returns>
        public Id3v2TextFrame GetTextFrame(Id3v2TextFrameIdentifier identifier)
        {
            string id = Id3v2TextFrame.GetIdentifier(Version, identifier);
            return _frames.OfType<Id3v2TextFrame>().FirstOrDefault(f => String.Equals(f.Identifier, id, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the <see cref="Id3v2UrlLinkFrame"/>.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>The <see cref="Id3v2UrlLinkFrame"/> if found; otherwise, null.</returns>
        public Id3v2UrlLinkFrame GetUrlLinkFrame(Id3v2UrlLinkFrameIdentifier identifier)
        {
            string id = Id3v2UrlLinkFrame.GetIdentifier(Version, identifier);
            return _frames.OfType<Id3v2UrlLinkFrame>().FirstOrDefault(f => String.Equals(f.Identifier, id, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the first frame of type T with a matching frame identifier.
        /// </summary>
        /// <typeparam name="T">The frame type.</typeparam>
        /// <param name="identifier">The identifier of the frame.</param>
        /// <returns>
        /// The first frame of type T with a matching frame identifier if found; otherwise, null.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="identifier"/> is null.</exception>
        public T GetFrame<T>(string identifier) where T : Id3v2Frame
        {
            if (identifier == null)
                throw new ArgumentNullException("identifier");

            return _frames.OfType<T>().FirstOrDefault(f => String.Equals(f.Identifier, identifier, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all <see cref="Id3v2UrlLinkFrame"/>s.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>
        /// The <see cref="Id3v2UrlLinkFrame"/>s.
        /// </returns>
        public IEnumerable<Id3v2UrlLinkFrame> GetUrlLinkFrames(Id3v2UrlLinkFrameIdentifier identifier)
        {
            string id = Id3v2UrlLinkFrame.GetIdentifier(Version, identifier);
            return _frames.OfType<Id3v2UrlLinkFrame>().Where(f => String.Equals(f.Identifier, id, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all frames of type T.
        /// </summary>
        /// <typeparam name="T">The frame type.</typeparam>
        /// <returns>
        /// The frames of type T.
        /// </returns>
        public IEnumerable<T> GetFrames<T>() where T : Id3v2Frame
        {
            return _frames.OfType<T>();
        }

        /// <summary>
        /// Gets all frames of type T and with a matching frame identifier.
        /// </summary>
        /// <typeparam name="T">The frame type.</typeparam>
        /// <param name="identifier">The identifier of the frame.</param>
        /// <returns>
        /// A list of frames of type T with a matching frame identifier.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="identifier"/> is null.</exception>
        public IEnumerable<T> GetFrames<T>(string identifier) where T : Id3v2Frame
        {
            if (identifier == null)
                throw new ArgumentNullException("identifier");

            return _frames.OfType<T>().Where(f => String.Equals(f.Identifier, identifier, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Updates the first matching frame if found; else, adds a new frame.
        /// </summary>
        /// <param name="frame">Frame to add to the <see cref="Id3v2Tag" />.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="frame"/> is null.</exception>
        /// <exception cref="InvalidVersionException">Thrown if the version of the tag does not match the version of the <paramref name="frame"/>.</exception>
        /// <remarks>
        /// The frame needs to have the same version set as the <see cref="Id3v2Tag" />, otherwise an <see cref="InvalidVersionException" /> will be thrown.
        /// </remarks>
        public void SetFrame(Id3v2Frame frame)
        {
            if (frame == null)
                throw new ArgumentNullException("frame");

            if (frame.Version != Version)
                throw new InvalidVersionException("The version of the frame needs to to match the version of the tag.");

            int i, frameCount = _frames.Count;
            for (i = 0; i < frameCount; i++)
            {
                if (!ReferenceEquals(_frames[i], frame)
                    && (!String.Equals(_frames[i].Identifier, frame.Identifier, StringComparison.OrdinalIgnoreCase) || !_frames[i].Equals(frame)))
                    continue;

                _frames[i] = frame;
                break;
            }

            if (i == frameCount)
                _frames.Add(frame);
        }

        /// <summary>
        /// Updates a list of frames with a matching identifier if found; else, adds it.
        /// </summary>
        /// <param name="frames">The frames.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="frames"/> is null.</exception>
        /// <remarks>
        /// The frames needs to have the same version set as the <see cref="Id3v2Tag" />, otherwise an <see cref="InvalidVersionException" /> will be thrown.
        /// </remarks>
        public void SetFrames(IEnumerable<Id3v2Frame> frames)
        {
            if (frames == null)
                throw new ArgumentNullException("frames");

            UnbindFrameEvents();
            foreach (Id3v2Frame frame in frames)
                SetFrame(frame);

            BindFrameEvents();
            ValidateFrames();
        }

        /// <summary>
        /// Removes the frame.
        /// </summary>
        /// <param name="frame">The frame.</param>
        public void RemoveFrame(Id3v2Frame frame)
        {
            // Try to remove by reference first before trying to remove by calling the Equal() on all frames
            if ((frame != null) && !_frames.Remove(_frames.FirstOrDefault(f => ReferenceEquals(f, frame))))
                _frames.Remove(frame);
        }

        /// <summary>
        /// Removes all frames of type T.
        /// </summary>
        /// <typeparam name="T">A class of type <see cref="Id3v2Frame" />.</typeparam>
        public void RemoveFrames<T>() where T : Id3v2Frame
        {
            RemoveFrames<T>(true);
        }

        /// <summary>
        /// Removes the frames.
        /// </summary>
        /// <param name="frames">The frames.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if frames is null.</exception>
        public void RemoveFrames(IEnumerable<Id3v2Frame> frames)
        {
            RemoveFrames(frames, true);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Places the <see cref="Id3v2Tag"/> into a byte array.
        /// </summary>
        /// <returns>
        /// A byte array that represents the <see cref="Id3v2Tag"/>.
        /// </returns>
        /// <remarks>
        /// Frames which have <see cref="Id3v2Frame.TagAlterPreservation"/> or <see cref="Id3v2Frame.FileAlterPreservation"/> set to false won't be written to the byte array.
        /// </remarks>
        public byte[] ToByteArray()
        {
            StreamBuffer data = new StreamBuffer();
            if (ExtendedHeader != null)
            {
                if ((Version >= Id3v2Version.Id3v230) && (Version < Id3v2Version.Id3v240))
                {
                    int extendedHeaderSize = ExtendedHeader.GetHeaderSize(Version);
                    int extendedHeaderFlags = ExtendedHeader.GetFlags(Version);
                    data.WriteBigEndianInt32(extendedHeaderSize);
                    data.WriteBigEndianInt16((short)extendedHeaderFlags);
                    data.WriteBigEndianInt32(ExtendedHeader.PaddingSize + PaddingSize);
                    if (ExtendedHeader.CrcDataPresent)
                    {
                        int crc = CalculateCrc32();
                        data.WriteBigEndianInt32(crc);
                    }
                }
                else if (Version >= Id3v2Version.Id3v240)
                {
                    int extendedHeaderSize = ExtendedHeader.GetHeaderSize(Version);
                    extendedHeaderSize = GetSynchsafeValue(extendedHeaderSize);
                    int extendedHeaderFlags = ExtendedHeader.GetFlags(Version);
                    int extendedHeaderFlagsFieldLength = ExtendedHeader.GetFlagsFieldLength(Version);
                    data.WriteBigEndianInt32(extendedHeaderSize);
                    data.WriteByte((byte)extendedHeaderFlagsFieldLength);
                    data.WriteBytes(extendedHeaderFlags, extendedHeaderFlagsFieldLength);
                    if (ExtendedHeader.TagIsUpdate)
                        data.WriteByte(0x00);

                    if (ExtendedHeader.CrcDataPresent)
                    {
                        long crc = CalculateCrc32() & 0xFFFFFFFF;
                        crc = GetSynchsafeValue(crc);
                        data.WriteBigEndianBytes(crc, 5);
                    }

                    if (ExtendedHeader.TagIsRestricted)
                    {
                        byte[] extendedHeaderBytes = ExtendedHeader.TagRestrictions.ToByte();
                        data.Write(extendedHeaderBytes);
                    }
                }
            }

            // Write the frames
            foreach (byte[] byteField in GetFrames().Select(frame => frame.ToByteArray()))
                data.Write(byteField);

            // For version Id3v2.4.0 and later we don't do the unsynch here:
            // unsynchronization [S:6.1] is done on frame level, instead of on tag level, making it easier to skip frames, 
            // increasing the stream ability of the tag.
            // The unsynchronization flag in the header [S:3.1] indicates if all frames has been unsynchronized, 
            // while the new unsynchronization flag in the frame header [S:4.1.2] indicates unsynchronization.
            if (UseUnsynchronization && (Version < Id3v2Version.Id3v240))
            {
                byte[] synchronizedData = data.ToByteArray();
                byte[] unsynchronizedData = GetUnsynchronizedData(synchronizedData, 0, synchronizedData.Length);
                data = new StreamBuffer(unsynchronizedData) { Position = unsynchronizedData.Length };
            }

            // It is OPTIONAL to include padding after the final frame (at the end of the ID3 tag), 
            // making the size of all the frames together smaller than the size given in the tag header. 
            // A possible purpose of this padding is to allow for adding a few additional frames or enlarge
            // existing frames within the tag without having to rewrite the entire file.
            // The value of the padding bytes must be 0x00.
            // A tag MUST NOT have any padding between the frames or between the tag header and the frames.
            // Furthermore it MUST NOT have any padding when a tag footer is added to the tag.
            if (!UseFooter)
            {
                data.WritePadding(0, PaddingSize);

                if (ExtendedHeader != null)
                    data.WritePadding(0, ExtendedHeader.PaddingSize);
            }

            using (StreamBuffer fullBuffer = new StreamBuffer())
            {
                // tagSize is the size of the complete tag after unsynchronization, including padding, 
                // excluding the header (and footer in Id3v2.4.0) but not excluding the extended header
                // (total tag size - 10 (or 20 if footer is present)).
                int dataSize = (int)data.Length;
                if (UseHeader)
                {
                    fullBuffer.Write(HeaderIdentifierBytes);
                    fullBuffer.WriteShort((short)(Convert.ToInt16(Version) / 10));
                    fullBuffer.WriteByte((byte)Flags);
                    fullBuffer.WriteBigEndianInt32(GetSynchsafeValue(dataSize));
                }

                // Data.
                fullBuffer.Write(data.ToByteArray());

                // To speed up the process of locating an Id3v2 tag when searching from the end of a file, 
                // a footer can be added to the tag.
                // It is REQUIRED to add a footer to an appended tag, i.e. a tag located after all audio data. 
                // The footer is a copy of the header, but with a different identifier.
                if (UseFooter)
                {
                    fullBuffer.Write(FooterIdentifierBytes);
                    fullBuffer.WriteShort((short)(Convert.ToInt16(Version) / 10));
                    fullBuffer.WriteByte((byte)Flags);
                    fullBuffer.WriteBigEndianInt32(GetSynchsafeValue(dataSize));
                }
                return fullBuffer.ToByteArray();
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string version = Version.ToString();
            return (version.Length == 1) ? "Id3v2" : version;
        }

        /// <summary>
        /// Calculates the CRC32 of the <see cref="Id3v2Tag"/>.
        /// </summary>
        /// <returns>The CRC32 of the <see cref="Id3v2Tag"/>.</returns>
        /// <remarks>
        /// This property only applies for version <see cref="Id3v2Version.Id3v230"/> and later.
        /// </remarks>
        public int CalculateCrc32()
        {
            if (Version < Id3v2Version.Id3v230)
                return 0;

            using (StreamBuffer stream = new StreamBuffer())
            {
                // Id3v2.3.0
                // The CRC should be calculated before unsynchronisation on the data between the extended header and the padding, i.e. the frames and only the frames.
                foreach (Id3v2Frame frame in GetFrames())
                    stream.Write(frame.ToByteArray());

                // Id3v2.4.0
                // The CRC is calculated on all the data between the header and footer as indicated by the header's size field, minus the extended header.
                // Note that this includes the padding (if there is any), but excludes the footer.
                if (Version >= Id3v2Version.Id3v240)
                    stream.WritePadding(0, PaddingSize);

                return Crc32.Calculate(stream.ToByteArray());
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes this instance, setting up events, etc.
        /// </summary>
        private void Initialize()
        {
            PreserveFramesAfterFileAlteration = PreserveFramesAfterTagAlteration = true;

            BindFrameEvents();
        }

        /// <summary>
        /// Gets a list of frames which contain at least 1 byte, 
        /// and which are to be preserved after file or tag alteration (only checked when the <see cref="Version"/> is earlier than <see cref="Id3v2Version.Id3v230"/>.
        /// </summary>
        /// <returns>
        /// The frames which will be written into a byte array when calling <see cref="ToByteArray"/> or when calculating the <see cref="CalculateCrc32"/>
        /// </returns>
        private IEnumerable<Id3v2Frame> GetFrames()
        {
            // Don't return  0-byte data fields as they won't be parsed again (according to the specs the data should contain at least 1 byte).
            return
                _frames.Where(
                    frame =>
                    (Version < Id3v2Version.Id3v230)
                    || ((PreserveFramesAfterTagAlteration || frame.TagAlterPreservation)
                        && (PreserveFramesAfterFileAlteration || frame.FileAlterPreservation))).Where(
                            frame =>
                                {
                                    byte[] data = frame.Data;
                                    return (data != null) && data.Length > 0;
                                });
        }

        private void ItemAdd<T>(object sender, CollectionItemAddEventArgs<T> e) where T : Id3v2Frame
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.Item == null)
                throw new NullReferenceException("e.Item may not be null");

            SetFrame(e.Item);
        }

        private void ItemRemove<T>(object sender, CollectionItemRemoveEventArgs<T> e) where T : Id3v2Frame
        {
            if (e != null && e.Item != null)
                RemoveFrame(e.Item);
        }

        private void AddFrameCollectionEvents<T>(EventCollection<T> list) where T : Id3v2Frame
        {
            // Remove a possible already added ItemAdd delegate
            list.ItemAdd -= ItemAdd;

            // Add it
            list.ItemAdd += ItemAdd;

            // Remove a possible already added ItemRemove delegate
            list.ItemRemove -= ItemRemove;

            // Add it
            list.ItemRemove += ItemRemove;
        }

        private Id3v2FrameCollection<T> GetFrameCollection<T>(IEnumerable<T> items) where T : Id3v2Frame
        {
            Id3v2FrameCollection<T> list = new Id3v2FrameCollection<T>();
            list.AddRange(items);
            AddFrameCollectionEvents(list);
            return list;
        }

        private Id3v2FrameCollection<T> GetFrameCollection<T>() where T : Id3v2Frame
        {
            IEnumerable<T> frames = GetFrames<T>();
            return GetFrameCollection(frames);
        }

        private Id3v2FrameCollection<Id3v2UrlLinkFrame> GetFrameCollection(Id3v2UrlLinkFrameIdentifier identifier)
        {
            IEnumerable<Id3v2UrlLinkFrame> frames = GetUrlLinkFrames(identifier);
            return GetFrameCollection(frames);
        }

        private void RemoveFrames<T>(bool validateFrames) where T : Id3v2Frame
        {
            UnbindFrameEvents();
            _frames.RemoveAll(f => f is T);
            BindFrameEvents();
            ValidateFrames();
        }

        private void RemoveFrames(IEnumerable<Id3v2Frame> frames, bool validateFrames)
        {
            if (frames == null)
                throw new ArgumentNullException("frames");

            UnbindFrameEvents();
            foreach (Id3v2Frame frame in frames)
                RemoveFrame(frame);

            BindFrameEvents();
            ValidateFrames();
        }

        private void RemoveFrames<T>(Id3v2UrlLinkFrameIdentifier identifier)
        {
            string id = Id3v2UrlLinkFrame.GetIdentifier(Version, identifier);
            _frames.RemoveAll(f => f is Id3v2UrlLinkFrame && String.Equals(f.Identifier, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
