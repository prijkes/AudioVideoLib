namespace AudioVideoLib.IO;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

using AudioVideoLib.Formats;

/// <summary>
/// Walker for Musepack (<c>.mpc</c>) containers. Supports SV7 (magic
/// <c>'M','P','+',0x?7</c>) and SV8 (magic <c>MPCK</c>).
/// </summary>
/// <remarks>
/// Source-reference lifetime model per <see cref="IMediaContainer"/>:
/// <see cref="ReadStream"/> records per-packet offsets/lengths,
/// <see cref="WriteTo"/> streams those byte ranges directly from source to
/// destination via <see cref="ISourceReader.CopyTo"/>. No audio re-encoding;
/// tag handling (APEv2 footer, ID3v2 header) is delegated to <c>AudioTags</c>.
/// The walker is robust to a non-zero <see cref="StartOffset"/> when the
/// caller has advanced past an ID3v2 prefix.
/// </remarks>
public sealed class MpcStream : IMediaContainer, IDisposable
{
    private readonly List<MpcPacket> _packets = [];
    private ISourceReader? _source;

    /// <inheritdoc/>
    public long StartOffset { get; private set; }

    /// <inheritdoc/>
    public long EndOffset { get; private set; }

    /// <inheritdoc/>
    public long TotalDuration =>
        Header is null || Header.SampleRate == 0
            ? 0
            : (long)(Header.TotalSamples * 1000UL / Header.SampleRate);

    /// <inheritdoc/>
    public long TotalMediaSize => EndOffset - StartOffset;

    /// <inheritdoc/>
    public int MaxFrameSpacingLength { get; set; }

    /// <summary>Gets the parsed Musepack stream header, or <c>null</c> if <see cref="ReadStream"/> has not succeeded.</summary>
    public MpcStreamHeader? Header { get; private set; }

    /// <summary>Gets the bitstream version detected at <see cref="ReadStream"/> time. Defaults to <see cref="MpcStreamVersion.Sv8"/> before a successful read.</summary>
    public MpcStreamVersion Version => Header?.Version ?? MpcStreamVersion.Sv8;

    /// <summary>Gets the list of packets discovered in the container, in file order. SV7 produces a single audio-span packet; SV8 produces one entry per keyed packet.</summary>
    public IReadOnlyList<MpcPacket> Packets => _packets;

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <c>null</c>.</exception>
    public bool ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var start = stream.Position;
        if (stream.Length - start < 4)
        {
            return false;
        }

        Span<byte> magic = stackalloc byte[4];
        if (stream.Read(magic) != 4)
        {
            return false;
        }

        stream.Position = start;
        if (!TryDetectVersion(magic, out var version))
        {
            return false;
        }

        StartOffset = start;
        _source?.Dispose();
        _source = new StreamSourceReader(stream, leaveOpen: true);
        EndOffset = start + _source.Length;

        var ok = version == MpcStreamVersion.Sv7 ? ReadSv7(stream) : ReadSv8(stream);
        stream.Position = EndOffset;
        return ok;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Byte-passthrough: the parsed range is re-emitted verbatim from the live source. Tag
    /// mutations come back through <see cref="ReadStream"/> on a freshly tagged source produced
    /// by <c>AudioTags</c>.
    /// </remarks>
    public void WriteTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_source is null)
        {
            throw new InvalidOperationException(
                "Source stream was detached or never read. WriteTo requires a live source.");
        }

        // Source offsets are relative to the position the stream was at when the source reader
        // was constructed (i.e. StartOffset is offset 0 within the source view). Mirror the
        // Mp4Stream / MatroskaStream full-passthrough pattern.
        _source.CopyTo(0, _source.Length, destination);
    }

    /// <summary>
    /// Releases the underlying <see cref="ISourceReader"/>. Does not close the user's source
    /// <see cref="Stream"/>; the caller still owns that.
    /// </summary>
    public void Dispose()
    {
        _source?.Dispose();
        _source = null;
    }

    private static bool TryDetectVersion(ReadOnlySpan<byte> magic, out MpcStreamVersion version)
    {
        if (magic[0] == (byte)'M' && magic[1] == (byte)'P' && magic[2] == (byte)'C' && magic[3] == (byte)'K')
        {
            version = MpcStreamVersion.Sv8;
            return true;
        }

        // SV7: 'M','P','+', then a byte whose low nibble is 7. A 3-byte 'MP+'
        // check is NOT sufficient (spec §4.1).
        if (magic[0] == (byte)'M' && magic[1] == (byte)'P' && magic[2] == (byte)'+'
            && (magic[3] & 0x0F) == 0x07)
        {
            version = MpcStreamVersion.Sv7;
            return true;
        }

        version = MpcStreamVersion.Sv8;
        return false;
    }

    /// <summary>
    /// Parses the SV7 24-byte bit-packed header following the 4-byte <c>'MP+',0x?7</c> magic.
    /// Port of <c>streaminfo_read_header_sv7</c>
    /// (<c>3rdparty/musepack_src_r475/libmpcdec/streaminfo.c:109</c>).
    /// </summary>
    /// <remarks>
    /// The C implementation reads bytes through <c>mpc_bits_read</c> on a buffer that has been
    /// 32-bit byte-swapped on load (<c>MPC_BUFFER_SWAP</c>). The net effect is consumption of
    /// bits MSB-first from the original big-endian byte stream — exactly what the standard
    /// <see cref="BitStream"/> helper produces, so we read the raw 24 bytes and pull bit-fields
    /// in the same order as the C source.
    /// </remarks>
    private bool ReadSv7(Stream stream)
    {
        // Skip the 4-byte 'MP+',0x?7 magic.
        stream.Position = StartOffset + 4;

        Span<byte> hdr = stackalloc byte[24];
        if (stream.Read(hdr) != 24)
        {
            return false;
        }

        // Port: streaminfo_read_header_sv7 (streaminfo.c:115..132). Decode bit-fields in the
        // order the C source does.
        using var reader = new BitStream(hdr.ToArray());

        // frames = (read16 << 16) | read16.
        var framesHi = (uint)reader.ReadInt32(16);
        var framesLo = (uint)reader.ReadInt32(16);
        var frames = (framesHi << 16) | framesLo;

        _ = reader.ReadInt32(1); // intensity stereo (should be 0)
        _ = reader.ReadInt32(1); // ms
        _ = reader.ReadInt32(6); // max_band
        var profileRaw = (uint)reader.ReadInt32(4);
        _ = reader.ReadInt32(2); // Link
        var sampleFreqIdx = (uint)reader.ReadInt32(2);

        // ReplayGain block — the C source discards the first 16-bit "Estimatedpeak_title".
        _ = (ushort)reader.ReadInt32(16); // Estimatedpeak_title (discarded)
        var titleGain = (ushort)reader.ReadInt32(16);
        var titlePeak = (ushort)reader.ReadInt32(16);
        var albumGain = (ushort)reader.ReadInt32(16);
        var albumPeak = (ushort)reader.ReadInt32(16);

        var trueGapless = reader.ReadInt32(1) != 0;
        var lastFrameSamples = (uint)reader.ReadInt32(11);
        _ = reader.ReadInt32(1); // fast_seek
        _ = reader.ReadInt32(19); // unused
        var encoderVersion = (uint)reader.ReadInt32(8);

        // Mirror C source clamp: last_frame_samples == 0 → MPC_FRAME_LENGTH (1152);
        // values > MPC_FRAME_LENGTH are invalid.
        const uint MpcFrameLength = 1152;
        const uint MpcDecoderSynthDelay = 481;
        if (lastFrameSamples == 0)
        {
            lastFrameSamples = MpcFrameLength;
        }
        else if (lastFrameSamples > MpcFrameLength)
        {
            return false;
        }

        ReadOnlySpan<uint> freqTable = [44100, 48000, 37800, 32000];
        var sampleRate = freqTable[(int)(sampleFreqIdx & 3)];

        // si->samples = frames * MPC_FRAME_LENGTH, then trim per gapless flag.
        var totalSamples = (ulong)frames * MpcFrameLength;
        if (frames > 0)
        {
            totalSamples -= trueGapless
                ? (MpcFrameLength - lastFrameSamples)
                : MpcDecoderSynthDelay;
        }

        Header = new MpcStreamHeader(
            MpcStreamVersion.Sv7,
            sampleRate,
            channels: 2,
            totalSamples,
            beginningSilence: 0,
            isTrueGapless: trueGapless,
            profile: profileRaw,
            profileName: ProfileName(profileRaw),
            encoderVersion: encoderVersion,
            encoderName: string.Empty,
            rgTitleGain: titleGain,
            rgTitlePeak: titlePeak,
            rgAlbumGain: albumGain,
            rgAlbumPeak: albumPeak);

        // One audio-span packet covering [post-header, EndOffset). Per-frame sub-division
        // is not required for inspection-only callers.
        var audioStart = stream.Position;
        _packets.Add(new MpcPacket(
            key: null,
            startOffset: audioStart,
            length: EndOffset - audioStart,
            sampleCount: totalSamples));
        return true;
    }

    /// <summary>
    /// Maps an SV7 profile index to its human-readable name. Port of
    /// <c>mpc_get_version_string</c> (<c>streaminfo.c:56</c>).
    /// </summary>
    /// <param name="profile">The 4-bit profile index (0..15); only 7..13 are meaningfully named.</param>
    /// <returns>The profile name, or <c>"Unknown"</c> for indices outside the named range.</returns>
    private static string ProfileName(float profile) => profile switch
    {
        7 => "Telephone",
        8 => "Thumb",
        9 => "Radio",
        10 => "Standard",
        11 => "Xtreme",
        12 => "Insane",
        13 => "BrainDead",
        _ => "Unknown",
    };

    /// <summary>
    /// Reads an SV8 size-encoded varint. Mirrors <c>mpc_bits_get_size</c> in
    /// <c>3rdparty/musepack_src_r475/libmpcdec/mpc_bits_reader.c</c>.
    /// </summary>
    private static long ReadSv8VarInt(Stream stream, out int bytesConsumed)
    {
        bytesConsumed = 0;
        long value = 0;
        while (true)
        {
            var b = stream.ReadByte();
            if (b < 0)
            {
                return -1;
            }

            bytesConsumed++;
            value = (value << 7) | (uint)(b & 0x7F);
            if ((b & 0x80) == 0)
            {
                return value;
            }

            if (bytesConsumed > 8)
            {
                return -1;
            }
        }
    }

    /// <summary>
    /// Reads an SV8 size-encoded varint from an in-memory span, advancing <paramref name="pos"/>.
    /// </summary>
    private static long ReadVarIntFromSpan(ReadOnlySpan<byte> buf, ref int pos)
    {
        long value = 0;
        while (pos < buf.Length)
        {
            var b = buf[pos++];
            value = (value << 7) | (uint)(b & 0x7F);
            if ((b & 0x80) == 0)
            {
                return value;
            }
        }

        return -1;
    }

    /// <summary>
    /// Walks the SV8 keyed-packet stream past the 4-byte <c>MPCK</c> magic. Port of the
    /// dispatch loop in <c>mpc_demux_header</c>
    /// (<c>3rdparty/musepack_src_r475/libmpcdec/mpc_demux.c:477</c>).
    /// </summary>
    /// <remarks>
    /// Each packet is a 2-byte ASCII key followed by a varint total length (covering the
    /// key, the size field, and the payload). <c>SH</c>, <c>RG</c>, and <c>EI</c> are parsed
    /// into <see cref="Header"/>; <c>SO</c>, <c>ST</c>, <c>AP</c>, <c>CT</c> are recorded
    /// opaquely; <c>SE</c> terminates the walk.
    /// </remarks>
    private bool ReadSv8(Stream stream)
    {
        stream.Position = StartOffset + 4; // past 'MPCK'
        var sawSh = false;
        var sawSe = false;
        Span<byte> keyBuf = stackalloc byte[2];

        while (stream.Position + 3 <= EndOffset && !sawSe)
        {
            var pktStart = stream.Position;
            if (stream.Read(keyBuf) != 2)
            {
                break;
            }

            var key = $"{(char)keyBuf[0]}{(char)keyBuf[1]}";
            var size = ReadSv8VarInt(stream, out _);
            if (size < 0 || pktStart + size > EndOffset)
            {
                return false;
            }

            var payloadStart = stream.Position;
            var payloadLen = pktStart + size - payloadStart;

            switch (key)
            {
                case "SH":
                    ReadSv8StreamHeader(stream, payloadStart, payloadLen);
                    sawSh = true;
                    break;
                case "RG":
                    ReadSv8ReplayGain(stream, payloadStart);
                    break;
                case "EI":
                    ReadSv8EncoderInfo(stream, payloadStart);
                    break;
                case "SE":
                    sawSe = true;
                    break;
                default:
                    // ST / SO / CT / AP recorded but not parsed for inspection.
                    break;
            }

            _packets.Add(new MpcPacket(key, pktStart, size, sampleCount: 0));
            stream.Position = pktStart + size;
        }

        return sawSh && Header is not null;
    }

    /// <summary>
    /// Parses an SV8 <c>SH</c> stream-header packet payload. Port of
    /// <c>streaminfo_read_header_sv8</c>
    /// (<c>3rdparty/musepack_src_r475/libmpcdec/streaminfo.c:187</c>).
    /// </summary>
    /// <remarks>
    /// Layout: <c>crc32(4) stream_version(1) sample_count(varint) beg_silence(varint)</c>
    /// then bit-packed <c>sample_freq_idx(3) max_used_band(5) channels(4)+1 ms(1) block_pwr(3)</c>.
    /// </remarks>
    private void ReadSv8StreamHeader(Stream stream, long payloadStart, long payloadLen)
    {
        stream.Position = payloadStart;
        Span<byte> buf = stackalloc byte[(int)Math.Min(payloadLen, 64)];
        var read = stream.Read(buf);
        if (read < 8)
        {
            return;
        }

        var pos = 5; // skip 4-byte CRC + 1-byte stream_version
        var totalSamples = ReadVarIntFromSpan(buf[..read], ref pos);
        var begSilence = ReadVarIntFromSpan(buf[..read], ref pos);
        if (totalSamples < 0 || begSilence < 0 || pos + 2 > read)
        {
            return;
        }

        var b0 = buf[pos++];
        var sampleFreqIdx = (b0 >> 5) & 0x07;
        var b1 = buf[pos++];
        var channels = (uint)((b1 >> 4) & 0x0F) + 1u;

        ReadOnlySpan<uint> freqTable = [44100, 48000, 37800, 32000, 0, 0, 0, 0];
        _shTotalSamples = (ulong)totalSamples;
        _shBegSilence = (ulong)begSilence;
        _shSampleRate = freqTable[sampleFreqIdx];
        _shChannels = channels;
        EnsureHeader();
    }

    /// <summary>
    /// Parses an SV8 <c>RG</c> ReplayGain packet payload. Port of <c>streaminfo_gain</c>
    /// (<c>3rdparty/musepack_src_r475/libmpcdec/streaminfo.c:172</c>).
    /// </summary>
    /// <remarks>
    /// Layout: <c>version(1) title_gain(2) title_peak(2) album_gain(2) album_peak(2)</c>.
    /// </remarks>
    private void ReadSv8ReplayGain(Stream stream, long payloadStart)
    {
        stream.Position = payloadStart;
        Span<byte> buf = stackalloc byte[9];
        if (stream.Read(buf) < 9)
        {
            return;
        }

        if (buf[0] != 1)
        {
            return;
        }

        _rgTitleGain = BinaryPrimitives.ReadUInt16BigEndian(buf[1..3]);
        _rgTitlePeak = BinaryPrimitives.ReadUInt16BigEndian(buf[3..5]);
        _rgAlbumGain = BinaryPrimitives.ReadUInt16BigEndian(buf[5..7]);
        _rgAlbumPeak = BinaryPrimitives.ReadUInt16BigEndian(buf[7..9]);
        EnsureHeader();
    }

    /// <summary>
    /// Parses an SV8 <c>EI</c> encoder-info packet payload. Port of
    /// <c>streaminfo_encoder_info</c>
    /// (<c>3rdparty/musepack_src_r475/libmpcdec/streaminfo.c:221</c>).
    /// </summary>
    /// <remarks>
    /// Layout: profile-byte (7 bits profile fixed-point + 1 bit pns), version(3 bytes).
    /// </remarks>
    private void ReadSv8EncoderInfo(Stream stream, long payloadStart)
    {
        stream.Position = payloadStart;
        Span<byte> buf = stackalloc byte[4];
        if (stream.Read(buf) < 4)
        {
            return;
        }

        _eiProfile = (buf[0] >> 1) / 8.0f;
        _eiEncoderVersion = ((uint)buf[1] << 16) | ((uint)buf[2] << 8) | buf[3];
        EnsureHeader();
    }

    private ulong _shTotalSamples;
    private ulong _shBegSilence;
    private uint _shSampleRate;
    private uint _shChannels = 2;
    private ushort _rgTitleGain;
    private ushort _rgTitlePeak;
    private ushort _rgAlbumGain;
    private ushort _rgAlbumPeak;
    private float _eiProfile;
    private uint _eiEncoderVersion;

    /// <summary>
    /// Folds the latest parsed <c>SH</c> / <c>RG</c> / <c>EI</c> field values into
    /// <see cref="Header"/>. Each per-key parser calls this so any subset of packets
    /// produces a populated header.
    /// </summary>
    private void EnsureHeader() =>
        Header = new MpcStreamHeader(
            MpcStreamVersion.Sv8,
            _shSampleRate,
            _shChannels,
            _shTotalSamples,
            _shBegSilence,
            isTrueGapless: true,
            profile: _eiProfile,
            profileName: ProfileName(_eiProfile),
            encoderVersion: _eiEncoderVersion,
            encoderName: string.Empty,
            rgTitleGain: _rgTitleGain,
            rgTitlePeak: _rgTitlePeak,
            rgAlbumGain: _rgAlbumGain,
            rgAlbumPeak: _rgAlbumPeak);
}
