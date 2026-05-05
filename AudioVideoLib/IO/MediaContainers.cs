namespace AudioVideoLib.IO;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AudioVideoLib.Collections;

/// <summary>
/// Represents a collection of audio streams.
/// </summary>
public sealed class MediaContainers : IEnumerable<IMediaContainer>, IDisposable
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
        { typeof(MacStream), () => new MacStream() },
        { typeof(MpcStream), () => new MpcStream() },
        { typeof(TtaStream), () => new TtaStream() },
        { typeof(WavPackStream), () => new WavPackStream() },
    };

    private readonly NotifyingList<IMediaContainer> _streams = [];

    private bool _disposed;

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
    /// "Strict mode" sentinel for <see cref="MaxStreamSpacingLength"/> — only probe the
    /// expected anchor position; never rescan byte-by-byte.
    /// </summary>
    public const int Strict = 0;

    /// <summary>
    /// Default value of <see cref="MaxStreamSpacingLength"/> — tolerates up to 128 bytes
    /// of junk before giving up.
    /// </summary>
    public const int DefaultStreamSpacingLength = 128;

    /// <summary>
    /// Gets or sets the max length of spacing, in bytes, between containers when searching the stream.
    /// </summary>
    /// <value>The max length of spacing, in bytes.</value>
    /// <remarks>
    /// Larger values cost more in scan time but tolerate more junk between containers.
    /// Set to <see cref="Strict"/> (= 0) to skip the byte-by-byte rescan entirely — the
    /// reader still uses the magic-byte fast path, so well-formed FLAC / OGG / WAV / MP4 /
    /// ASF / Matroska / DSF / DFF files dispatch in O(1). Useful when you know the input is
    /// clean; combine with <see cref="AudioTags"/>'s strict mode to scan a whole file in
    /// near-constant time.
    /// </remarks>
    public int MaxStreamSpacingLength { get; set; } = DefaultStreamSpacingLength;

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
    /// Asynchronously reads a <see cref="MediaContainers"/> instance from the stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task that resolves to a populated <see cref="MediaContainers"/>.</returns>
    /// <remarks>
    /// The underlying walkers are still synchronous — this overload runs the scan on a
    /// background thread via <see cref="Task.Run(System.Action)"/>. True async I/O is a
    /// follow-up.
    /// </remarks>
    public static Task<MediaContainers> ReadStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return Task.Run(() => ReadStream(stream), cancellationToken);
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

        // Fast path: peek the first few bytes and dispatch directly to the walker whose
        // magic matches. Avoids running every walker probe at every byte position when
        // we already know the format. Falls through to the brute-force scan below for
        // anything that doesn't match a known signature (e.g. MPEG audio, which can start
        // anywhere in the stream).
        if (TryMatchMagic(stream, startPosition, out var preferred)
            || TryProbeAfterId3v2(stream, startPosition, out preferred))
        {
            var walker = _supportedStreams[preferred]();
            var preferredArgs = new MediaContainerParseEventArgs(walker);
            OnMediaContainerParse(preferredArgs);

            if (walker.ReadStream(stream))
            {
                var preferredParsed = new MediaContainerParsedEventArgs(walker);
                OnMediaContainerParsed(preferredParsed);
                return walker;
            }
            stream.Position = startPosition;
        }

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

    private static bool TryMatchMagic(Stream stream, long startPosition, out Type walker)
    {
        walker = typeof(object);
        if (!stream.CanSeek || stream.Length - startPosition < 4)
        {
            return false;
        }

        Span<byte> buf = stackalloc byte[12];
        var n = stream.Read(buf);
        stream.Position = startPosition;
        if (n < 4)
        {
            return false;
        }

        // Container magics, all checked at offset 0 unless noted.
        // FLAC: "fLaC"
        if (buf[0] == 0x66 && buf[1] == 0x4C && buf[2] == 0x61 && buf[3] == 0x43)
        {
            walker = typeof(FlacStream);
            return true;
        }

        // OGG: "OggS"
        if (buf[0] == 0x4F && buf[1] == 0x67 && buf[2] == 0x67 && buf[3] == 0x53)
        {
            walker = typeof(OggStream);
            return true;
        }

        // RIFF / RIFX: "RIFF" or "RIFX"
        if (buf[0] == 0x52 && buf[1] == 0x49 && buf[2] == 0x46 && (buf[3] == 0x46 || buf[3] == 0x58))
        {
            walker = typeof(RiffStream);
            return true;
        }

        // AIFF / AIFF-C: "FORM" header
        if (buf[0] == 0x46 && buf[1] == 0x4F && buf[2] == 0x52 && buf[3] == 0x4D)
        {
            walker = typeof(AiffStream);
            return true;
        }

        // DSF: "DSD "
        if (buf[0] == 0x44 && buf[1] == 0x53 && buf[2] == 0x44 && buf[3] == 0x20)
        {
            walker = typeof(DsfStream);
            return true;
        }

        // DFF: "FRM8"
        if (buf[0] == 0x46 && buf[1] == 0x52 && buf[2] == 0x4D && buf[3] == 0x38)
        {
            walker = typeof(DffStream);
            return true;
        }

        // Matroska / WebM: 0x1A 0x45 0xDF 0xA3 (EBML header)
        if (buf[0] == 0x1A && buf[1] == 0x45 && buf[2] == 0xDF && buf[3] == 0xA3)
        {
            walker = typeof(MatroskaStream);
            return true;
        }

        // ASF: Header Object GUID first 4 bytes (LE) = 0x30 0x26 0xB2 0x75
        if (buf[0] == 0x30 && buf[1] == 0x26 && buf[2] == 0xB2 && buf[3] == 0x75)
        {
            walker = typeof(AsfStream);
            return true;
        }

        // MP4 / M4A: "ftyp" at offset 4 (size prefix at 0..3 first).
        if (n >= 8 && buf[4] == 0x66 && buf[5] == 0x74 && buf[6] == 0x79 && buf[7] == 0x70)
        {
            walker = typeof(Mp4Stream);
            return true;
        }

        // MPC SV7: 4-byte magic — first 3 bytes are 'M','P','+', 4th byte's low nibble = 7.
        if (buf[0] == 0x4D && buf[1] == 0x50 && buf[2] == 0x2B && (buf[3] & 0x0F) == 0x07)
        {
            walker = typeof(MpcStream);
            return true;
        }

        // MPC SV8: "MPCK" at offset 0.
        if (buf[0] == 0x4D && buf[1] == 0x50 && buf[2] == 0x43 && buf[3] == 0x4B)
        {
            walker = typeof(MpcStream);
            return true;
        }

        // WavPack: "wvpk" at offset 0.
        if (buf[0] == 0x77 && buf[1] == 0x76 && buf[2] == 0x70 && buf[3] == 0x6B)
        {
            walker = typeof(WavPackStream);
            return true;
        }

        // TrueAudio: "TTA1" at offset 0.
        if (buf[0] == 0x54 && buf[1] == 0x54 && buf[2] == 0x41 && buf[3] == 0x31)
        {
            walker = typeof(TtaStream);
            return true;
        }

        // Monkey's Audio (MAC): "MAC " (integer) at offset 0.
        if (buf[0] == 0x4D && buf[1] == 0x41 && buf[2] == 0x43 && buf[3] == 0x20)
        {
            walker = typeof(MacStream);
            return true;
        }

        // Monkey's Audio (MAC): "MACF" (float) at offset 0.
        if (buf[0] == 0x4D && buf[1] == 0x41 && buf[2] == 0x43 && buf[3] == 0x46)
        {
            walker = typeof(MacStream);
            return true;
        }

        // MPEG audio frames don't have a fixed header at offset 0 (they can start anywhere
        // after a tag block) — let the brute-force scan below find them. Same for any format
        // whose magic isn't recognised here.
        return false;
    }

    /// <summary>
    /// ID3v2-aware fast path: if the input starts with an ID3v2 tag, computes the
    /// offset of the first byte past the tag (header + syncsafe size + optional footer)
    /// and re-runs the magic-byte probe at that offset. Lets <c>&lt;id3v2&gt;&lt;wvpk audio&gt;</c>
    /// (and similar combos) dispatch in O(1) without a byte-by-byte rescan.
    /// </summary>
    /// <param name="stream">The seekable input stream.</param>
    /// <param name="startPosition">Position of the candidate ID3v2 header.</param>
    /// <param name="walker">When this method returns <c>true</c>, the walker type matched
    /// after skipping the tag.</param>
    /// <returns><c>true</c> if an ID3v2 header was found and a known walker matched at the
    /// post-tag offset; <c>false</c> otherwise. The stream's position is restored on return.</returns>
    private static bool TryProbeAfterId3v2(Stream stream, long startPosition, out Type walker)
    {
        walker = typeof(object);
        if (!stream.CanSeek || stream.Length - startPosition < 10)
        {
            return false;
        }

        Span<byte> id3 = stackalloc byte[10];
        stream.Position = startPosition;
        var n = stream.Read(id3);
        stream.Position = startPosition;
        if (n < 10)
        {
            return false;
        }

        // "ID3" magic at bytes 0..2.
        if (id3[0] != 0x49 || id3[1] != 0x44 || id3[2] != 0x33)
        {
            return false;
        }

        // Bytes 6..9 are the syncsafe tag-body size (28 bits, MSB of each byte ignored).
        var size = ((id3[6] & 0x7F) << 21)
                 | ((id3[7] & 0x7F) << 14)
                 | ((id3[8] & 0x7F) << 7)
                 | (id3[9] & 0x7F);

        // Bit 4 of the flags byte (0x10) = footer present (adds another 10 bytes).
        var footer = (id3[5] & 0x10) != 0 ? 10 : 0;
        var afterTagOffset = startPosition + 10 + size + footer;

        if (afterTagOffset >= stream.Length)
        {
            return false;
        }

        var savedPosition = stream.Position;
        try
        {
            return TryMatchMagic(stream, afterTagOffset, out walker);
        }
        finally
        {
            stream.Position = savedPosition;
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Disposes every contained walker. Idempotent.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        foreach (var walker in _streams)
        {
            walker.Dispose();
        }
        _disposed = true;
    }
}
