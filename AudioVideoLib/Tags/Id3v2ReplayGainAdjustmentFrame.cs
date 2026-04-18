namespace AudioVideoLib.Tags;

using System;

using AudioVideoLib.IO;

/// <summary>
/// Class for storing a relative volume adjustment.
/// </summary>
/// <remarks>
/// The Replay Gain represents a suggested gain adjustment for each track.
/// Players can scale the audio data by this replay gain in order to achieve a consistent perceived loudness across all tracks.
/// <para />
/// The reference gain is 83dB SPL, as defined in the SMPTE RP 200 standard.
/// If the Replay Gain for a given track is -12dB, this means that the track is relatively loud, and the gain should be reduced by 12dB, ideally to 71dB.
/// <para />
/// This frame supports <see cref="Id3v2Version"/> <see cref="Id3v2Version.Id3v230"/> and later.
/// </remarks>
public sealed class Id3v2ReplayGainAdjustmentFrame : Id3v2Frame
{
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2ReplayGainAdjustmentFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
    /// </summary>
    /// <remarks>
    /// This frame is not an official frame but has been seen since <see cref="Id3v2Version.Id3v230"/> and not earlier.
    /// </remarks>
    public Id3v2ReplayGainAdjustmentFrame() : base(Id3v2Version.Id3v230)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2ReplayGainAdjustmentFrame"/> class.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
    public Id3v2ReplayGainAdjustmentFrame(Id3v2Version version) : base(version)
    {
        if (!IsVersionSupported(version))
        {
            throw new InvalidVersionException($"Version {version} not supported by this frame.");
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the peak amplitude.
    /// </summary>
    /// <value>
    /// The peak amplitude.
    /// </value>
    /// <remarks>
    /// Peak signal amplitude.
    /// </remarks>
    public int PeakAmplitude { get; set; }

    /// <summary>
    /// Gets or sets the radio replay gain adjustment.
    /// </summary>
    /// <value>
    /// The radio replay gain adjustment.
    /// </value>
    /// <remarks>
    /// Replay Gain adjustment required to make all tracks equal loudness.
    /// </remarks>
    public Id3v2ReplayGain RadioAdjustment
    {
        get;

        set
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    } = new();

    /// <summary>
    /// Gets or sets the audiophile replay gain adjustment.
    /// </summary>
    /// <value>
    /// The audiophile replay gain adjustment.
    /// </value>
    /// <remarks>
    /// Replay Gain adjustment required to give ideal listening loudness.
    /// </remarks>
    public Id3v2ReplayGain AudiophileAdjustment
    {
        get;

        set
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    } = new();

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override byte[] Data
    {
        get
        {
            var stream = new StreamBuffer();
            stream.WriteInt(PeakAmplitude);
            stream.Write(RadioAdjustment.ToByteArray());
            stream.Write(AudiophileAdjustment.ToByteArray());
            return stream.ToByteArray();
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            var stream = new StreamBuffer(value);
            PeakAmplitude = stream.ReadInt32();

            int radioAdjustment = stream.ReadInt16();
            RadioAdjustment = new Id3v2ReplayGain
            {
                Adjustment = (short)(radioAdjustment & Id3v2ReplayGain.AdjustmentMask),
                Sign = (Id3v2ReplayGainSign)(radioAdjustment & Id3v2ReplayGain.SignMask),
                OriginatorCode = (Id3v2OriginatorCode)(radioAdjustment & Id3v2ReplayGain.OriginatorCodeMask),
                NameCode = (Id3v2NameCode)(radioAdjustment & Id3v2ReplayGain.NameCodeMask)
            };

            int audiophileAdjustment = stream.ReadInt16();
            AudiophileAdjustment = new Id3v2ReplayGain
            {
                Adjustment = (short)(audiophileAdjustment & Id3v2ReplayGain.AdjustmentMask),
                Sign = (Id3v2ReplayGainSign)(audiophileAdjustment & Id3v2ReplayGain.SignMask),
                OriginatorCode = (Id3v2OriginatorCode)(audiophileAdjustment & Id3v2ReplayGain.OriginatorCodeMask),
                NameCode = (Id3v2NameCode)(audiophileAdjustment & Id3v2ReplayGain.NameCodeMask)
            };
        }
    }

    /// <inheritdoc />
    public override string Identifier => "RGAD";

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(Id3v2Frame? frame)
    {
        return Equals(frame as Id3v2ReplayGainAdjustmentFrame);
    }

    /// <summary>
    /// Equals the specified <see cref="Id3v2ReplayGainAdjustmentFrame"/>.
    /// </summary>
    /// <param name="rgad">The <see cref="Id3v2ReplayGainAdjustmentFrame"/>.</param>
    /// <returns>true if equal; false otherwise.</returns>
    /// <remarks>
    ///  There may only be one <see cref="Id3v2ReplayGainAdjustmentFrame"/> frame in each <see cref="Id3v2Tag"/>.
    /// </remarks>
    public bool Equals(Id3v2ReplayGainAdjustmentFrame? rgad)
    {
        return rgad is not null && (ReferenceEquals(this, rgad) || rgad.Version == Version);
    }

    /// <summary>
    /// Serves as a hash function for a particular type.
    /// </summary>
    /// <returns>
    /// A hash code for the current <see cref="T:System.Object"/>.
    /// </returns>
    /// <filterpriority>2</filterpriority>
    public override int GetHashCode()
    {
        unchecked
        {
            return (Version.GetHashCode() * 397) ^ (Identifier.GetHashCode() * 397);
        }
    }

    /// <summary>
    /// Determines whether the specified version is supported by the frame.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>
    ///   <c>true</c> if the specified version is supported; otherwise, <c>false</c>.
    /// </returns>
    public override bool IsVersionSupported(Id3v2Version version)
    {
        return version < Id3v2Version.Id3v230;
    }
}
