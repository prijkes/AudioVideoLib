namespace AudioVideoLib;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AudioVideoLib.IO;
using AudioVideoLib.Tags;

/// <summary>
/// Class for storing audio related info.
/// </summary>
public sealed class AudioInfo
{
    private readonly Stream _stream;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioInfo" /> class.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if stream is null.</exception>
    private AudioInfo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;
        AudioTags = new AudioTags();
        MediaContainers = new MediaContainers();
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the audio tags.
    /// </summary>
    /// <value>
    /// The audio tags.
    /// </value>
    public AudioTags AudioTags { get; }

    /// <summary>
    /// Gets the audio stream.
    /// </summary>
    /// <value>
    /// The audio stream.
    /// </value>
    public MediaContainers MediaContainers { get; }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Analyses the specified stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>
    /// An <see cref="AudioInfo"/> instance containing information about the stream.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">Thrown if stream is null.</exception>
    public static AudioInfo Analyse(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new NotSupportedException("stream not readable");
        }

        var audioInfo = new AudioInfo(stream);
        audioInfo.Analyse();

        return audioInfo;
    }

    /// <summary>
    /// Asynchronously analyses the supplied stream.
    /// </summary>
    /// <param name="stream">The stream to analyse.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task that resolves to the populated <see cref="AudioInfo"/>.</returns>
    /// <remarks>
    /// First-class async support is a follow-up: the underlying reader chain
    /// (<see cref="AudioTags.ReadTags(Stream)"/>, <see cref="MediaContainers.ReadStreams"/>) is still
    /// synchronous. This overload runs the analysis on a background thread via
    /// <see cref="Task.Run(System.Action)"/>, which keeps the call site async-friendly without
    /// blocking the caller's thread but doesn't yet do true async I/O.
    /// </remarks>
    public static Task<AudioInfo> AnalyseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return Task.Run(() => Analyse(stream), cancellationToken);
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Saves the <see cref="AudioTags" /> and <see cref="MediaContainers" /> to the specified file.
    /// </summary>
    /// <param name="file">The destination file.</param>
    /// <param name="overwrite">When <c>true</c> (default), an existing file is overwritten; when
    /// <c>false</c>, an <see cref="IOException"/> is thrown if the file exists.</param>
    /// <exception cref="IOException">Thrown when the file exists and <paramref name="overwrite"/> is <c>false</c>.</exception>
    public void Save(string file, bool overwrite = true)
    {
        var mode = overwrite ? FileMode.Create : FileMode.CreateNew;
        using var fileStream = new FileStream(file, mode);
        Save(fileStream);
    }

    /// <summary>
    /// Writes the <see cref="AudioTags" /> and <see cref="MediaContainers" /> to the supplied stream.
    /// </summary>
    /// <param name="destination">The destination stream.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="destination"/> is <c>null</c>.</exception>
    /// <remarks>
    /// Tags with <see cref="TagOrigin.Start"/> are written first (sorted by <see cref="IAudioTagOffset.StartOffset"/>),
    /// then each container, then tags with <see cref="TagOrigin.End"/>. Container walkers use their
    /// streaming <c>WriteTo</c> implementation when available.
    /// </remarks>
    public void Save(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        foreach (var tag in AudioTags.Where(t => t.TagOrigin == TagOrigin.Start).OrderBy(a => a.StartOffset))
        {
            tag.AudioTag.WriteTo(destination);
        }

        foreach (var container in MediaContainers)
        {
            container.WriteTo(destination);
        }

        foreach (var tag in AudioTags.Where(t => t.TagOrigin == TagOrigin.End).OrderBy(a => a.StartOffset))
        {
            tag.AudioTag.WriteTo(destination);
        }
    }

    /// <summary>
    /// Asynchronously saves the contents to the specified file.
    /// </summary>
    /// <param name="file">The destination file.</param>
    /// <param name="overwrite">When <c>true</c> (default), an existing file is overwritten.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <remarks>
    /// First-class async writes are a follow-up — see <see cref="AnalyseAsync"/> for the
    /// same caveat. Backed by <see cref="Task.Run(System.Action)"/>.
    /// </remarks>
    public Task SaveAsync(string file, bool overwrite = true, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        return Task.Run(() => Save(file, overwrite), cancellationToken);
    }

    /// <summary>
    /// Asynchronously writes the contents to the supplied stream.
    /// </summary>
    /// <param name="destination">The destination stream.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <remarks>
    /// First-class async writes are a follow-up — see <see cref="AnalyseAsync"/> for the
    /// same caveat. Backed by <see cref="Task.Run(System.Action)"/>.
    /// </remarks>
    public Task SaveAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        return Task.Run(() => Save(destination), cancellationToken);
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private void Analyse()
    {
        // Find all tags.
        AudioTags.ReadTags(_stream);

        // Set position to start of an audio stream.
        _stream.Position = AudioTags.Where(t => t.TagOrigin == TagOrigin.Start).Select(t => t.EndOffset).LastOrDefault();

        // Find all streams.
        MediaContainers.ReadStreams(_stream);
    }
}
