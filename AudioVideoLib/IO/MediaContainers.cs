namespace AudioVideoLib.IO;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AudioVideoLib.Collections;

/// <summary>
/// Represents a collection of audio streams.
/// </summary>
public sealed class MediaContainers : IEnumerable<IMediaContainer>
{
    private readonly Dictionary<Type, Func<IMediaContainer>> _supportedStreams = new()
    {
        { typeof(MpaStream), () => new MpaStream() },
        { typeof(FlacStream), () => new FlacStream() },
        { typeof(RiffStream), () => new RiffStream() },
        { typeof(AiffStream), () => new AiffStream() },
        { typeof(OggStream), () => new OggStream() },
        { typeof(DsfStream), () => new DsfStream() },
        { typeof(DffStream), () => new DffStream() },
        { typeof(Mp4Stream), () => new Mp4Stream() },
        { typeof(AsfStream), () => new AsfStream() },
        { typeof(MatroskaStream), () => new MatroskaStream() },
    };

    private readonly NotifyingList<IMediaContainer> _streams = [];

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaContainers" /> class.
    /// </summary>
    public MediaContainers()
    {
        _streams.ItemAdd += AudioStreamAdd;
        _streams.ItemReplace += AudioStreamReplace;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Occurs when parsing an audio stream.
    /// </summary>
    public event EventHandler<MediaContainerParseEventArgs>? MediaContainerParse;

    /// <summary>
    /// Occurs when an audio stream has been parsed.
    /// </summary>
    public event EventHandler<MediaContainerParsedEventArgs>? MediaContainerParsed;

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
    public int MaxStreamSpacingLength { get; set; } = 128;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads an <see cref="MediaContainers"/> instance from the stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>
    /// An <see cref="MediaContainers"/> instance.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">Thrown if stream is null.</exception>
    public static MediaContainers ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var audioStreams = new MediaContainers();
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
        ArgumentNullException.ThrowIfNull(stream);

        var streamsFound = 0;
        var streamLength = stream.Length;
        var startPosition = stream.Position;
        long spacing = 0;
        while (stream.Position <= streamLength && spacing < MaxStreamSpacingLength)
        {
            var audioStream = ReadMediaContainer(stream);
            if (audioStream is not null)
            {
                spacing = 0;
                streamsFound++;
                _streams.Add(audioStream);
                stream.Position = audioStream.EndOffset == startPosition ? startPosition + 1 : audioStream.EndOffset;
                continue;
            }
            spacing++;
            stream.Position++;
        }
        return streamsFound > 0;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Returns a typed enumerator over the parsed <see cref="IMediaContainer"/>s.
    /// </summary>
    /// <returns>An <see cref="IEnumerator{T}"/> of <see cref="IMediaContainer"/>.</returns>
    public IEnumerator<IMediaContainer> GetEnumerator() => _streams.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _streams.GetEnumerator();

    ////------------------------------------------------------------------------------------------------------------------------------

    private void OnMediaContainerParse(MediaContainerParseEventArgs e) => MediaContainerParse?.Invoke(this, e);

    private void OnMediaContainerParsed(MediaContainerParsedEventArgs e) => MediaContainerParsed?.Invoke(this, e);

    ////------------------------------------------------------------------------------------------------------------------------------

    private void AudioStreamAdd(object? sender, ListItemAddEventArgs<IMediaContainer> e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (e.Item is null)
        {
            throw new NullReferenceException("e.Item may not be null");
        }

        for (var i = 0; i < _streams.Count; i++)
        {
            var audioStream = _streams[i];
            if (audioStream.StartOffset >= e.Item.StartOffset)
            {
                e.Index = i;
                break;
            }
        }
    }

    private void AudioStreamReplace(object? sender, ListItemReplaceEventArgs<IMediaContainer> e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (e.NewItem is null)
        {
            throw new NullReferenceException("e.NewItem may not be null");
        }

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
    private IMediaContainer? ReadMediaContainer(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead || stream.Length == 0)
        {
            return null;
        }

        var startPosition = stream.Position;

        // The ReadFunction() SHOULD handle truncated data (currentPosition + streamLength < TotalLength)
        foreach (var audioStream in _supportedStreams.Select(pair => pair.Value()))
        {
            // Raise before parsing event.
            var parseEventArgs = new MediaContainerParseEventArgs(audioStream);
            OnMediaContainerParse(parseEventArgs);

            if (audioStream.ReadStream(stream))
            {
                // Raise after parsing event.
                var parsedEventArgs = new MediaContainerParsedEventArgs(audioStream);
                OnMediaContainerParsed(parsedEventArgs);
                return audioStream;
            }
            stream.Position = startPosition;
        }
        return null;
    }
}
