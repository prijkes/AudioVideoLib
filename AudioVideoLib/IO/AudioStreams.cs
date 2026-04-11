/*
 * Date: 2013-01-26
 * Sources used: 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AudioVideoLib.Collections;
using AudioVideoLib.Formats;

namespace AudioVideoLib.IO
{
    /// <summary>
    /// Represents a collection of audio streams.
    /// </summary>
    public sealed class AudioStreams : IEnumerable<IAudioStream>
    {
        private readonly Dictionary<Type, Func<IAudioStream>> _supportedStreams = new Dictionary<Type, Func<IAudioStream>>
            {
                { typeof(MpaStream), () => new MpaStream() },
                { typeof(FlacStream), () => new FlacStream() }
            };
        
        private readonly EventList<IAudioStream> _streams = new EventList<IAudioStream>();

        // Max length of spacing, in bytes, between 2 streams. If there is spacing between streams, this means that a stream is corrupted.
        private int _maxStreamSpacingLength = 128;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioStreams" /> class.
        /// </summary>
        public AudioStreams()
        {
            _streams.ItemAdd += AudioStreamAdd;

            _streams.ItemReplace += AudioStreamReplace;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Occurs when parsing an audio stream.
        /// </summary>
        public event EventHandler<AudioStreamParseEventArgs> AudioStreamParse;

        /// <summary>
        /// Occurs when an audio stream has been parsed.
        /// </summary>
        public event EventHandler<AudioStreamParsedEventArgs> AudioStreamParsed;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the max length of spacing, in bytes, between 2 streams when searching for streams.
        /// </summary>
        /// <value>
        ///  The max length of spacing.
        /// </value>
        /// <remarks>
        /// When searching for tags, spacing might exist between 2 streams.
        /// Setting the max spacing length to a large value will decrease performance but increase accuracy, while a lower value will increase performance but decrease accuracy.
        /// </remarks>
        public int MaxStreamSpacingLength
        {
            get
            {
                return _maxStreamSpacingLength;
            }

            set
            {
                _maxStreamSpacingLength = value;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Reads an <see cref="AudioStreams"/> instance from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>
        /// An <see cref="AudioStreams"/> instance.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown if stream is null.</exception>
        public static AudioStreams ReadStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            AudioStreams audioStreams = new AudioStreams();
            audioStreams.ReadStreams(stream);
            return audioStreams;
        }

        /// <summary>
        /// Reads audio streams from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>
        /// true if one or more audio streams are read from the stream; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown if stream is null.</exception>
        public bool ReadStreams(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            int streamsFound = 0;
            long streamLength = stream.Length;
            long startPosition = stream.Position;
            long spacing = 0;
            while ((stream.Position <= streamLength) && (spacing < MaxStreamSpacingLength))
            {
                IAudioStream audioStream = ReadAudioStream(stream);
                if (audioStream != null)
                {
                    spacing = 0;
                    streamsFound++;
                    _streams.Add(audioStream);
                    stream.Position = (audioStream.EndOffset == startPosition) ? startPosition + 1 : audioStream.EndOffset;
                    continue;
                }
                spacing++;
                stream.Position++;
            }
            return streamsFound > 0;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator<IAudioStream> IEnumerable<IAudioStream>.GetEnumerator()
        {
            return _streams.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            return _streams.GetEnumerator();
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private void OnAudioStreamParse(AudioStreamParseEventArgs e)
        {
            EventHandler<AudioStreamParseEventArgs> audioTagParse = AudioStreamParse;
            if (audioTagParse != null)
                audioTagParse(this, e);
        }

        private void OnAudioStreamParsed(AudioStreamParsedEventArgs e)
        {
            EventHandler<AudioStreamParsedEventArgs> audioTagParsed = AudioStreamParsed;
            if (audioTagParsed != null)
                audioTagParsed(this, e);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private void AudioStreamAdd(object sender, ListItemAddEventArgs<IAudioStream> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.Item == null)
                throw new NullReferenceException("e.Item may not be null");

            for (int i = 0; i < _streams.Count; i++)
            {
                IAudioStream audioStream = _streams[i];
                if (audioStream.StartOffset >= e.Item.StartOffset)
                {
                    e.Index = i;
                    break;
                }
            }
        }

        private void AudioStreamReplace(object sender, ListItemReplaceEventArgs<IAudioStream> e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (e.NewItem == null)
                throw new NullReferenceException("e.NewItem may not be null");

            _streams.RemoveAt(e.Index);
            e.Cancel = true;
            _streams.Add(e.NewItem);
        }
        
        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Finds an audio stream in the stream from the current position.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>
        /// True if an audio stream was found; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown if stream is null.</exception>
        private IAudioStream ReadAudioStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if ((stream.CanRead == false) || (stream.Length == 0))
                return null;

            long startPosition = stream.Position;

            // The ReadFunction() SHOULD handle truncated data (currentPosition + streamLength < TotalLength)
            foreach (IAudioStream audioStream in _supportedStreams.Select(pair => pair.Value()))
            {
                // Raise before parsing event.
                AudioStreamParseEventArgs parseEventArgs = new AudioStreamParseEventArgs(audioStream);
                OnAudioStreamParse(parseEventArgs);

                if (audioStream.ReadStream(stream))
                {
                    // Raise after parsing event.
                    AudioStreamParsedEventArgs parsedEventArgs = new AudioStreamParsedEventArgs(audioStream);
                    OnAudioStreamParsed(parsedEventArgs);
                    return audioStream;
                }
                stream.Position = startPosition;
            }
            return null;
        }
    }
}
