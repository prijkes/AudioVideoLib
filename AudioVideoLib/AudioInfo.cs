namespace AudioVideoLib;

using System;
using System.IO;
using System.Linq;

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

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Saves the <see cref="AudioTags" /> and <see cref="MediaContainers" /> to specified file. File should not exist.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <remarks>
    /// An <see cref="System.ApplicationException"/> is thrown when the file already exists.
    /// </remarks>
    /// <exception cref="System.ApplicationException">thrown when the file already exists</exception>
    public void Save(string file)
    {
        if (File.Exists(file))
        {
            throw new ApplicationException($"File '{file}' already exists");
        }

        using var fileStream = new FileStream(file, FileMode.CreateNew);
        foreach (var bytes in AudioTags.Where(t => t.TagOrigin == TagOrigin.Start).OrderBy(a => a.StartOffset).Select(t => t.AudioTag.ToByteArray()))
        {
            fileStream.Write(bytes, 0, bytes.Length);
        }

        foreach (var bytes in MediaContainers.Select(a => a.ToByteArray()))
        {
            fileStream.Write(bytes, 0, bytes.Length);
        }

        foreach (var bytes in AudioTags.Where(t => t.TagOrigin == TagOrigin.End).OrderBy(a => a.StartOffset).Select(t => t.AudioTag.ToByteArray()))
        {
            fileStream.Write(bytes, 0, bytes.Length);
        }
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
