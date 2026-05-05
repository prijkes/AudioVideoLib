namespace AudioVideoLib.Formats;

/// <summary>
/// Top-of-file descriptor parsed from a Musepack container. Read-only.
/// Field names follow <c>mpc_streaminfo</c> in
/// <c>3rdparty/musepack_src_r475/include/mpc/streaminfo.h</c>.
/// </summary>
public sealed class MpcStreamHeader
{
    internal MpcStreamHeader(
        MpcStreamVersion version, uint sampleRate, uint channels,
        ulong totalSamples, ulong beginningSilence, bool isTrueGapless,
        float profile, string profileName, uint encoderVersion, string encoderName,
        ushort rgTitleGain, ushort rgTitlePeak, ushort rgAlbumGain, ushort rgAlbumPeak)
    {
        Version = version;
        SampleRate = sampleRate;
        Channels = channels;
        TotalSamples = totalSamples;
        BeginningSilence = beginningSilence;
        IsTrueGapless = isTrueGapless;
        Profile = profile;
        ProfileName = profileName;
        EncoderVersion = encoderVersion;
        EncoderName = encoderName;
        ReplayGainTitleGain = rgTitleGain;
        ReplayGainTitlePeak = rgTitlePeak;
        ReplayGainAlbumGain = rgAlbumGain;
        ReplayGainAlbumPeak = rgAlbumPeak;
    }

    /// <summary>Gets the Musepack bitstream version (SV7 or SV8).</summary>
    public MpcStreamVersion Version { get; }

    /// <summary>Gets the sample rate in Hz.</summary>
    public uint SampleRate { get; }

    /// <summary>Gets the number of audio channels.</summary>
    public uint Channels { get; }

    /// <summary>Gets the total number of inter-channel samples in the stream.</summary>
    public ulong TotalSamples { get; }

    /// <summary>Gets the number of samples of silence skipped at the beginning of the stream.</summary>
    public ulong BeginningSilence { get; }

    /// <summary>Gets a value indicating whether the stream uses true-gapless framing (SV7 last-frame trim or SV8 packet model).</summary>
    public bool IsTrueGapless { get; }

    /// <summary>Gets the encoder profile value (quality level).</summary>
    public float Profile { get; }

    /// <summary>Gets the human-readable encoder profile name (e.g. "Standard", "Insane").</summary>
    public string ProfileName { get; }

    /// <summary>Gets the encoder version number as encoded in the stream.</summary>
    public uint EncoderVersion { get; }

    /// <summary>Gets the human-readable encoder name and version string.</summary>
    public string EncoderName { get; }

    /// <summary>Gets the ReplayGain title gain value as stored in the stream header.</summary>
    public ushort ReplayGainTitleGain { get; }

    /// <summary>Gets the ReplayGain title peak value as stored in the stream header.</summary>
    public ushort ReplayGainTitlePeak { get; }

    /// <summary>Gets the ReplayGain album gain value as stored in the stream header.</summary>
    public ushort ReplayGainAlbumGain { get; }

    /// <summary>Gets the ReplayGain album peak value as stored in the stream header.</summary>
    public ushort ReplayGainAlbumPeak { get; }
}
