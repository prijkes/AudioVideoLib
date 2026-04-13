namespace AudioVideoLib.Tags;

using System;
using System.Collections.Generic;
using System.IO;

using AudioVideoLib.Collections;
using AudioVideoLib.IO;

/// <summary>
/// Class for storing the equalisation.
/// </summary>
/// <remarks>
/// This frame allows the user to predefine an equalisation curve within the audio file.
/// <para />
/// This frame has been replaced by the <see cref="Id3v2Equalisation2Frame"/> frame as of <see cref="Id3v2Version.Id3v240"/>.
/// <para />
/// This frame supports <see cref="Id3v2Version"/> up to but not including <see cref="Id3v2Version.Id3v240"/>.
/// </remarks>
public sealed class Id3v2EqualisationFrame : Id3v2Frame
{
    private readonly NotifyingList<Id3v2EqualisationBand> _equalisationBands = [];

    private byte _adjustmentBits;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2EqualisationFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
    /// </summary>
    public Id3v2EqualisationFrame() : base(Id3v2Version.Id3v230)
    {
        BindEqualisationBandsListEvents();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2EqualisationFrame"/> class.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
    public Id3v2EqualisationFrame(Id3v2Version version) : base(version)
    {
        if (!IsVersionSupported(version))
        {
            throw new InvalidVersionException(string.Format("Version {0} not supported by this frame.", version));
        }

        BindEqualisationBandsListEvents();
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the adjustment bits.
    /// </summary>
    /// <value>
    /// The adjustment bits.
    /// </value>
    /// <remarks>
    /// The adjustment bits field defines the number of bits used for representation of the adjustment.
    /// This is normally 0x10 (16 bits) for MPEG 2 layer I, II and III [MPEG] and MPEG 2.5.
    /// This value may not be 0x00.
    /// </remarks>
    public byte AdjustmentBits
    {
        get
        {
            return _adjustmentBits;
        }

        set
        {
            if (value == 0)
            {
                throw new InvalidDataException("AdjustmentBits may not be 0.");
            }

            _adjustmentBits = value;
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="Id3v2EqualisationBand"/> equalization bands.
    /// </summary>
    /// <value>
    /// The <see cref="Id3v2EqualisationBand"/> equalization bands.
    /// </value>
    /// <remarks>
    /// When adding items, the equalization bands will be ordered increasingly with reference to frequency.
    /// </remarks>
    public ICollection<Id3v2EqualisationBand> EqualisationBands
    {
        get
        {
            return _equalisationBands;
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override byte[] Data
    {
        get
        {
            var stream = new StreamBuffer();
            stream.WriteByte(AdjustmentBits);
            var bytesEntry = (int)Math.Ceiling((double)AdjustmentBits / 8);
            foreach (var band in EqualisationBands)
            {
                stream.WriteBigEndianInt16((short)(band.Frequency | (short)(band.Increment ? 0x8FFF : 0x0000)));
                stream.WriteBigEndianBytes(band.Adjustment, bytesEntry);
            }
            return stream.ToByteArray();
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            var stream = new StreamBuffer(value);
            _adjustmentBits = (byte)stream.ReadByte();
            var bytesEntry = (int)Math.Ceiling((double)AdjustmentBits / 8);

            // Read the equalisation bands.
            _equalisationBands.Clear();
            while (stream.Position < stream.Length)
            {
                var frequency = stream.ReadBigEndianInt16() & 0xFFFFFF;
                _equalisationBands.Add(
                    new Id3v2EqualisationBand(
                        (frequency & 0x8000) != 0,
                        (short)((frequency & 0x7FFF) * (((frequency & 0x8000) != 0) ? 1 : -1)),
                        stream.ReadBigEndianInt(bytesEntry)));
            }
        }
    }

    /// <inheritdoc />
    public override string Identifier
    {
        get { return (Version < Id3v2Version.Id3v230) ? "EQU" : "EQUA"; }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(Id3v2Frame? frame)
    {
        return Equals(frame as Id3v2EqualisationFrame);
    }

    /// <summary>
    /// Equals the specified <see cref="Id3v2EqualisationFrame"/>.
    /// </summary>
    /// <param name="equalisation">The <see cref="Id3v2EqualisationFrame"/>.</param>
    /// <returns>true if equal; false otherwise.</returns>
    /// <remarks>
    /// Both instances are equal when their <see cref="Version"/> property is equal.
    /// </remarks>
    public bool Equals(Id3v2EqualisationFrame? equalisation)
    {
        return equalisation is not null && (ReferenceEquals(this, equalisation) || equalisation.Version == Version);
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
        return version < Id3v2Version.Id3v240;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private void BindEqualisationBandsListEvents()
    {
        _equalisationBands.ItemAdd += EqualisationBandAdd;

        _equalisationBands.ItemReplace += EqualisationBandReplace;
    }

    private void EqualisationBandAdd(object? sender, ListItemAddEventArgs<Id3v2EqualisationBand> e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (e.Item == null)
        {
            throw new NullReferenceException("e.Item may not be null");
        }

        for (var i = 0; i < _equalisationBands.Count; i++)
        {
            var equalisationBand = _equalisationBands[i];
            if (equalisationBand.Frequency >= e.Item.Frequency)
            {
                e.Index = i;
                break;
            }
        }
    }

    private void EqualisationBandReplace(object? sender, ListItemReplaceEventArgs<Id3v2EqualisationBand> e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (e.NewItem == null)
        {
            throw new NullReferenceException("e.NewItem may not be null");
        }

        _equalisationBands.RemoveAt(e.Index);
        e.Cancel = true;
        _equalisationBands.Add(e.NewItem);
    }
}
