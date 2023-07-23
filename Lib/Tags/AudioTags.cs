/*
 * Date: 2010-05-19
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AudioVideoLib.Collections;

namespace AudioVideoLib.Tags
{
    /*
        There are different tags in the wild for MPA files.
        A few of them for example are: MusicMatch, Id3v1, Id3v2, Lyrics3, APE, etc.
        These tags could appear in the same MPA file, which means a MPA file can hold more than one tag for it's info.
        There isn't a predefined order in which they should be written to the file.
        This means that we shouldn't be surprised if we find an APE tag before an Id3v1 tag.
    
        Tags possible at the beginning of a MPA file:
        * MusicMatch
        * Id3v2
        * Lyrics3
        * APE (APEv2 only)
    
        Tags possible at the end of a MPA file:
        * MusicMatch
        * Id3v1
        * Id3v2
        * APE (APEv1, APEv2)
    */

    /// <summary>
    /// Represents a collection of audio tag offsets.
    /// </summary>
    public sealed class AudioTags : IEnumerable<IAudioTagOffset>
    {
        private readonly Dictionary<Type, Func<IAudioTagReader>> _audioTagFactory = new Dictionary<Type, Func<IAudioTagReader>>
            {
                { typeof(Id3v1Tag), () => new Id3v1TagReader() },
                { typeof(Id3v2Tag), () => new Id3v2TagReader() },
                { typeof(ApeTag), () => new ApeTagReader() },
                { typeof(MusicMatchTag), () => new MusicMatchTagReader() },
                { typeof(Lyrics3Tag), () => new Lyrics3TagReader() },
                { typeof(Lyrics3v2Tag), () => new Lyrics3v2TagReader() }
            };

        private readonly EventList<IAudioTagOffset> _tags = new EventList<IAudioTagOffset>();

        // Max length of spacing, in bytes, between 2 tags. If there is spacing between frames, this means that a frame is corrupted.
        private int _maxTagSpacingLength = 128;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Occurs when parsing an audio tag.
        /// </summary>
        public event EventHandler<AudioTagParseEventArgs> AudioTagParse;

        /// <summary>
        /// Occurs when an audio tag has been parsed.
        /// </summary>
        public event EventHandler<AudioTagParsedEventArgs> AudioTagParsed;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the max length of spacing, in bytes, between 2 tags when searching for tags.
        /// </summary>
        /// <value>
        ///  The max length of spacing.
        /// </value>
        /// <remarks>
        /// When searching for tags, spacing might exist between 2 tags.
        /// Setting the max spacing length to a large value will decrease performance but increase accuracy, while a lower value will increase performance but decrease accuracy.
        /// </remarks>
        public int MaxTagSpacingLength
        {
            get
            {
                return _maxTagSpacingLength;
            }

            set
            {
                _maxTagSpacingLength = value;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Reads tags from a <see cref="Stream"/> as a new <see cref="AudioTags"/> instance.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>
        /// An <see cref="AudioTags" /> instance.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="stream"/> is null.</exception>
        public static AudioTags ReadStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            AudioTags audioTags = new AudioTags();
            audioTags.ReadTags(stream);
            return audioTags;
        }

        /// <summary>
        /// Reads tags from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>The amount of tags read from the given.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="stream"/> is null.</exception>
        public int ReadTags(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            long streamLength = stream.Length;
            long startPosition = stream.Position;
            long streamPosition = startPosition;

            // Tags at the start of the stream.
            List<IAudioTagOffset> tagsAtStart = ReadTagsAtStart(stream, streamPosition, streamLength);
            stream.Position = startPosition;

            // Tags at the end of the stream.
            stream.Position = streamPosition = streamLength;
            List<IAudioTagOffset> tagsAtEnd = ReadTagsAtEnd(stream, streamPosition);

            stream.Position = startPosition;
            
            _tags.AddRange(tagsAtStart.OrderBy(t => t.StartOffset));
            _tags.AddRange(tagsAtEnd.OrderBy(t => t.StartOffset));
            return tagsAtStart.Count + tagsAtEnd.Count;
        }

        /// <summary>
        /// Read tags from a stream based on the <see cref="TagOrigin"/>.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="tagOrigin">The tag origin.</param>
        /// <returns>
        /// true if one or more audio tags are read from the stream; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="stream" /> is null.</exception>
        public int ReadTags(Stream stream, TagOrigin tagOrigin)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            long streamLength = stream.Length;
            long startPosition = stream.Position;
            long streamPosition = startPosition;

            // List to store new tags found in the stream.
            List<IAudioTagOffset> tags = (tagOrigin == TagOrigin.Start)
                                             ? ReadTagsAtStart(stream, streamPosition, streamLength)
                                             : ReadTagsAtEnd(stream, streamPosition);

            _tags.AddRange(tags.OrderBy(t => t.StartOffset));
            return tags.Count;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        public void AddTag(IAudioTag audioTag, TagOrigin tagOrigin)
        {
            _tags.Add(new AudioTagOffset(tagOrigin, 0, 0, audioTag));
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Adds an <see cref="IAudioTagReader" /> to the list of audio tag readers.
        /// </summary>
        /// <typeparam name="TR">The type of the tag reader.</typeparam>
        /// <typeparam name="TT">The type of the tag.</typeparam>
        /// <remarks>
        /// The added <see cref="IAudioTagReader" /> will be added at the end of the audio tag readers,
        /// and called when no other <see cref="IAudioTagReader" /> could find a tag when calling a ReadTags method.
        /// </remarks>
        public void AddReader<TR, TT>() where TR : IAudioTagReader, new() where TT : IAudioTag, new()
        {
            if (!_audioTagFactory.ContainsKey(typeof(TT)))
                _audioTagFactory.Add(typeof(TT), () => new TR());
        }

        /// <summary>
        /// Removes a type from the list of audio tag readers.
        /// </summary>
        /// <typeparam name="TR">The <see cref="IAudioTagReader"/> to remove.</typeparam>
        /// <returns>true if the type of <see cref="TR"/> is successfully found and removed; otherwise, false.</returns>
        public bool RemoveReader<TR>() where TR : IAudioTagReader, new()
        {
            List<Type> tagReaderTypes = _audioTagFactory.Where(r => r.Value.GetType() == typeof(TR)).Select(f => f.Key).ToList();
            return tagReaderTypes.Select(tr => _audioTagFactory.Remove(tr)).All(tr => tr);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator<IAudioTagOffset> IEnumerable<IAudioTagOffset>.GetEnumerator()
        {
            return _tags.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            return _tags.GetEnumerator();
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Raises the <see cref="AudioTagParse" /> event.
        /// </summary>
        /// <param name="e">The <see cref="AudioTagParseEventArgs" /> instance containing the event data.</param>
        private void OnAudioTagParse(AudioTagParseEventArgs e)
        {
            EventHandler<AudioTagParseEventArgs> audioTagParse = AudioTagParse;
            if (audioTagParse != null)
                audioTagParse(this, e);
        }

        /// <summary>
        /// Raises the <see cref="AudioTagParsed" /> event.
        /// </summary>
        /// <param name="e">The <see cref="AudioTagParsedEventArgs" /> instance containing the event data.</param>
        private void OnAudioTagParsed(AudioTagParsedEventArgs e)
        {
            EventHandler<AudioTagParsedEventArgs> audioTagParsed = AudioTagParsed;
            if (audioTagParsed != null)
                audioTagParsed(this, e);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private List<IAudioTagOffset> ReadTagsAtStart(Stream stream, long streamPosition, long streamLength)
        {
            // List to store new tags found in the stream.
            List<IAudioTagOffset> tags = new List<IAudioTagOffset>();

            // Tags at the start
            long spacing = 0;
            do
            {
                stream.Position = streamPosition;

                IAudioTagOffset audioTagOffset = ReadTag(stream, TagOrigin.Start);
                if (audioTagOffset != null && audioTagOffset.AudioTag != null)
                {
                    spacing = 0;
                    tags.Add(audioTagOffset);

                    // We need to subtract 1 here because the streamPosition will otherwise skip a byte in the while loop
                    streamPosition = audioTagOffset.EndOffset - 1;
                    continue;
                }
                spacing++;
            }
            while ((++streamPosition <= streamLength) && (spacing < MaxTagSpacingLength));

            return tags;
        }

        private List<IAudioTagOffset> ReadTagsAtEnd(Stream stream, long streamPosition)
        {
            // List to store new tags found in the stream.
            List<IAudioTagOffset> tags = new List<IAudioTagOffset>();

            if (streamPosition < 0)
                return tags;

            // Tags at the end
            long spacing = 0;
            do
            {
                stream.Position = streamPosition;

                IAudioTagOffset audioTagOffset = ReadTag(stream, TagOrigin.End);
                if (audioTagOffset != null && audioTagOffset.AudioTag != null)
                {
                    spacing = 0;
                    tags.Add(audioTagOffset);

                    // We need to add 1 here because the streamPosition will otherwise skip a byte in the while loop
                    streamPosition = audioTagOffset.StartOffset + 1;
                    continue;
                }
                spacing++;
            }
            while ((--streamPosition >= 0) && (spacing < MaxTagSpacingLength));

            return tags;
        }

        private IAudioTagOffset ReadTag(Stream stream, TagOrigin tagOrigin)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if ((stream.CanRead == false) || (stream.Length == 0))
                return null;

            long startPosition = stream.Position;

            foreach (IAudioTagReader reader in _audioTagFactory.Select(pair => pair.Value()))
            {
                // Raise before parsing event.
                AudioTagParseEventArgs parseEventArgs = new AudioTagParseEventArgs(reader, tagOrigin);
                OnAudioTagParse(parseEventArgs);

                IAudioTagOffset tagOffset = reader.ReadFromStream(stream, tagOrigin);
                if (tagOffset != null)
                {
                    // Raise after parsing event.
                    AudioTagParsedEventArgs parsedEventArgs = new AudioTagParsedEventArgs(tagOffset);
                    OnAudioTagParsed(parsedEventArgs);

                    return tagOffset;
                }
                stream.Position = startPosition;
            }
            return null;
        }
    }
}
