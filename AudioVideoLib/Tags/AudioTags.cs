namespace AudioVideoLib.Tags;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AudioVideoLib;
using AudioVideoLib.Collections;

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
    private readonly Dictionary<Type, Func<IAudioTagReader>> _audioTagFactory = new()
    {
            { typeof(Id3v1Tag), () => new Id3v1TagReader() },
            { typeof(Id3v2Tag), () => new Id3v2TagReader() },
            { typeof(ApeTag), () => new ApeTagReader() },
            { typeof(MusicMatchTag), () => new MusicMatchTagReader() },
            { typeof(Lyrics3Tag), () => new Lyrics3TagReader() },
            { typeof(Lyrics3v2Tag), () => new Lyrics3v2TagReader() }
        };

    private readonly NotifyingList<IAudioTagOffset> _tags = [];

    // Max length of spacing, in bytes, between 2 tags. If there is spacing between frames, this means that a frame is corrupted.

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Occurs when parsing an audio tag.
    /// </summary>
    public event EventHandler<AudioTagParseEventArgs>? AudioTagParse;

    /// <summary>
    /// Occurs when an audio tag has been parsed.
    /// </summary>
    public event EventHandler<AudioTagParsedEventArgs>? AudioTagParsed;

    /// <summary>
    /// Occurs when a tag reader throws while parsing. The failing tag is skipped and the next
    /// reader is tried — subscribers receive the exception and context without aborting the scan.
    /// </summary>
    public event EventHandler<AudioTagParseErrorEventArgs>? AudioTagParseError;

    /// <summary>
    /// Occurs when an ID3v2 frame fails to parse. Forwarded from <see cref="Id3v2TagReader.FrameParseError"/>
    /// so callers can subscribe once at the <see cref="AudioTags"/> level.
    /// </summary>
    public event EventHandler<Id3v2FrameParseErrorEventArgs>? Id3v2FrameParseError;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// "Strict mode" sentinel for <see cref="MaxTagSpacingLength"/> — only probe the
    /// expected anchor position(s); never rescan byte-by-byte.
    /// </summary>
    public const int Strict = 0;

    /// <summary>
    /// Default value of <see cref="MaxTagSpacingLength"/> — tolerates up to 128 bytes
    /// of junk between tags before giving up.
    /// </summary>
    public const int DefaultTagSpacingLength = 128;

    /// <summary>
    /// Gets or sets the max length of spacing, in bytes, between two tags when searching the stream.
    /// </summary>
    /// <value>The max length of spacing, in bytes.</value>
    /// <remarks>
    /// Larger values cost more in scan time but tolerate more junk between tags.
    /// Set to <see cref="Strict"/> (= 0) to skip the byte-by-byte rescan entirely —
    /// the reader will only check the canonical anchor positions (start or end of
    /// stream depending on <see cref="TagOrigin"/>). Recommended for clean,
    /// well-formed input where you control how files were written.
    /// </remarks>
    public int MaxTagSpacingLength { get; set; } = DefaultTagSpacingLength;

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
        ArgumentNullException.ThrowIfNull(stream);

        var audioTags = new AudioTags();
        audioTags.ReadTags(stream);
        return audioTags;
    }

    /// <summary>
    /// Asynchronously reads tags from a <see cref="Stream"/> as a new <see cref="AudioTags"/> instance.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task that resolves to a populated <see cref="AudioTags"/>.</returns>
    /// <remarks>
    /// The underlying readers are still synchronous — this overload runs the scan on a background
    /// thread via <see cref="Task.Run(System.Action)"/> for async-friendly call sites; true async
    /// I/O is a follow-up (it requires reworking the per-format readers).
    /// </remarks>
    public static Task<AudioTags> ReadStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return Task.Run(() => ReadStream(stream), cancellationToken);
    }

    /// <summary>
    /// Reads tags from a <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>The amount of tags read from the given.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="stream"/> is null.</exception>
    public int ReadTags(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var streamLength = stream.Length;
        var startPosition = stream.Position;
        var streamPosition = startPosition;

        // Tags at the start of the stream.
        var tagsAtStart = ReadTagsAtStart(stream, streamPosition, streamLength);
        stream.Position = startPosition;

        // Tags at the end of the stream.
        stream.Position = streamPosition = streamLength;
        var tagsAtEnd = ReadTagsAtEnd(stream, streamPosition);

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
        ArgumentNullException.ThrowIfNull(stream);

        var streamLength = stream.Length;
        var startPosition = stream.Position;
        var streamPosition = startPosition;

        // List to store new tags found in the stream.
        var tags = (tagOrigin == TagOrigin.Start)
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

    /// <summary>
    /// Removes a previously parsed or added tag from the collection.
    /// </summary>
    /// <param name="offset">The tag offset to remove.</param>
    /// <returns><c>true</c> if the tag was found and removed; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="offset"/> is <c>null</c>.</exception>
    public bool RemoveTag(IAudioTagOffset offset)
    {
        ArgumentNullException.ThrowIfNull(offset);
        return _tags.Remove(offset);
    }

    /// <summary>
    /// Removes every tag whose <see cref="IAudioTagOffset.AudioTag"/> is of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The concrete <see cref="IAudioTag"/> type to remove.</typeparam>
    /// <returns>The number of tags removed.</returns>
    public int RemoveTags<T>() where T : IAudioTag
    {
        var doomed = _tags.Where(o => o.AudioTag is T).ToList();
        foreach (var item in doomed)
        {
            _tags.Remove(item);
        }

        return doomed.Count;
    }

    /// <summary>
    /// Removes every tag from the collection.
    /// </summary>
    /// <returns>The number of tags removed.</returns>
    public int Clear()
    {
        var count = _tags.Count;
        _tags.Clear();
        return count;
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
        {
            _audioTagFactory.Add(typeof(TT), () => new TR());
        }
    }

    /// <summary>
    /// Removes a type from the list of audio tag readers.
    /// </summary>
    /// <typeparam name="TR">The <see cref="IAudioTagReader"/> to remove.</typeparam>
    /// <returns>true if the type of <see cref="TR"/> is successfully found and removed; otherwise, false.</returns>
    public bool RemoveReader<TR>() where TR : IAudioTagReader, new()
    {
        List<Type> tagReaderTypes = [.. _audioTagFactory.Where(r => r.Value.GetType() == typeof(TR)).Select(f => f.Key)];
        return tagReaderTypes.Select(_audioTagFactory.Remove).All(tr => tr);
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Returns a typed enumerator over the parsed <see cref="IAudioTagOffset"/>s.
    /// </summary>
    /// <returns>An <see cref="IEnumerator{T}"/> of <see cref="IAudioTagOffset"/>.</returns>
    public IEnumerator<IAudioTagOffset> GetEnumerator() => _tags.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _tags.GetEnumerator();

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Raises the <see cref="AudioTagParse" /> event.
    /// </summary>
    /// <param name="e">The <see cref="AudioTagParseEventArgs" /> instance containing the event data.</param>
    private void OnAudioTagParse(AudioTagParseEventArgs e)
    {
        AudioTagParse?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the <see cref="AudioTagParsed" /> event.
    /// </summary>
    /// <param name="e">The <see cref="AudioTagParsedEventArgs" /> instance containing the event data.</param>
    private void OnAudioTagParsed(AudioTagParsedEventArgs e)
    {
        AudioTagParsed?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the <see cref="AudioTagParseError" /> event.
    /// </summary>
    /// <param name="e">The <see cref="AudioTagParseErrorEventArgs" /> instance containing the event data.</param>
    private void OnAudioTagParseError(AudioTagParseErrorEventArgs e) =>
        AudioTagParseError?.Invoke(this, e);

    ////------------------------------------------------------------------------------------------------------------------------------

    private List<IAudioTagOffset> ReadTagsAtStart(Stream stream, long streamPosition, long streamLength)
    {
        // List to store new tags found in the stream.
        List<IAudioTagOffset> tags = [];

        // Tags at the start
        long spacing = 0;
        do
        {
            stream.Position = streamPosition;

            var audioTagOffset = ReadTag(stream, TagOrigin.Start);
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
        List<IAudioTagOffset> tags = [];

        if (streamPosition < 0)
        {
            return tags;
        }

        // Tags at the end
        long spacing = 0;
        do
        {
            stream.Position = streamPosition;

            var audioTagOffset = ReadTag(stream, TagOrigin.End);
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

    private IAudioTagOffset? ReadTag(Stream stream, TagOrigin tagOrigin)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if ((stream.CanRead == false) || (stream.Length == 0))
        {
            return null;
        }

        var startPosition = stream.Position;

        foreach (var reader in _audioTagFactory.Select(pair => pair.Value()))
        {
            if (reader is Id3v2TagReader v2Reader)
            {
                v2Reader.FrameParseError += ForwardId3v2FrameParseError;
            }

            // Raise before parsing event.
            var parseEventArgs = new AudioTagParseEventArgs(reader, tagOrigin);
            OnAudioTagParse(parseEventArgs);

            IAudioTagOffset? tagOffset;
            try
            {
                tagOffset = reader.ReadFromStream(stream, tagOrigin);
            }
            catch (Exception ex) when (ex is InvalidDataException or ArgumentException or InvalidVersionException or EndOfStreamException)
            {
                // A single failing tag reader shouldn't kill the whole scan — surface the error
                // via AudioTagParseError so the caller can log it, then try the next reader.
                OnAudioTagParseError(new AudioTagParseErrorEventArgs(reader, tagOrigin, startPosition, ex));
                tagOffset = null;
            }

            if (tagOffset != null)
            {
                // Raise after parsing event.
                var parsedEventArgs = new AudioTagParsedEventArgs(tagOffset);
                OnAudioTagParsed(parsedEventArgs);

                return tagOffset;
            }
            stream.Position = startPosition;
        }
        return null;
    }

    private void ForwardId3v2FrameParseError(object? sender, Id3v2FrameParseErrorEventArgs e) =>
        Id3v2FrameParseError?.Invoke(this, e);
}
