namespace AudioVideoLib.Formats;

using System;
using System.Collections.Generic;
using System.IO;

using AudioVideoLib.Cryptography;
using AudioVideoLib.IO;

/// <summary>
/// Class for FLAC audio frames.
/// </summary>
public sealed partial class FlacFrame : IAudioFrame
{
    /// <summary>
    /// The frame sync.
    /// </summary>
    private const int FrameSync = 0x7FFE;

    private readonly List<FlacSubFrame> _subFrames = [];

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="FlacFrame"/> class.
    /// </summary>
    /// <param name="stream">
    /// The stream.
    /// </param>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown if stream is null.
    /// </exception>
    private FlacFrame(IO.FlacStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        FlacStream = stream;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public long StartOffset { get; private set; }

    /// <inheritdoc/>
    public long EndOffset { get; private set; }

    /// <summary>
    /// Gets the frame length, in source-stream bytes
    /// (<see cref="EndOffset"/> &#x2212; <see cref="StartOffset"/>).
    /// Used by <see cref="IO.FlacStream.WriteTo"/> for byte-passthrough.
    /// </summary>
    public long Length => EndOffset - StartOffset;

    /// <summary>
    /// Gets the FLAC stream.
    /// </summary>
    /// <value>
    /// The FLAC stream.
    /// </value>
    public IO.FlacStream FlacStream { get; private set; }

    /// <summary>
    /// Gets the subframes in the frame.
    /// </summary>
    /// <value>
    /// A list of <see cref="FlacSubFrame"/>s in the frame.
    /// </value>
    public IEnumerable<FlacSubFrame> SubFrames => _subFrames.AsReadOnly();

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads a <see cref="FlacFrame" /> from a <see cref="Stream" />.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="flacStream">The FLAC stream.</param>
    /// <returns>
    /// true if found; otherwise, null.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">Thrown if stream is null.</exception>
    public static FlacFrame? ReadFrame(Stream stream, IO.FlacStream flacStream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(flacStream);

        var frame = new FlacFrame(flacStream);
        return frame.ReadFrame(stream as StreamBuffer ?? new StreamBuffer(stream)) ? frame : null;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private bool ReadFrame(StreamBuffer sb)
    {
        ArgumentNullException.ThrowIfNull(sb);

        StartOffset = sb.Position;

        if (!ReadHeader(sb))
        {
            return false;
        }

        for (var channel = 0; channel < Channels; channel++)
        {
            var subFrame = FlacSubFrame.ReadFrame(sb, channel, this);
            _subFrames.Add(subFrame);
        }

        // Capture the frame payload [StartOffset, currentPosition) and feed it to Crc16.Calculate.
        // The previous implementation passed an empty span, making CRC validation a no-op.
        // RFC 9639 §11.1: the 16-bit footer covers everything from the frame sync up to (but
        // not including) the stored CRC itself.
        var payloadEnd = sb.Position;
        var payloadLength = payloadEnd - StartOffset;
        var frameBytes = new byte[payloadLength];
        sb.Position = StartOffset;
        var bytesRead = sb.Read(frameBytes, 0, frameBytes.Length);
        if (bytesRead != frameBytes.Length)
        {
            return false;
        }

        sb.Position = payloadEnd;

        _crc16 = sb.ReadBigEndianInt16();
        var crc16 = Crc16.Calculate(frameBytes);
        if (_crc16 != crc16)
        {
            // Strict-rejection rule (spec §7): CRC mismatch ⇒ frame is invalid.
            return false;
        }

        EndOffset = sb.Position;
        return true;
    }
}
