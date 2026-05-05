namespace AudioVideoLib.Formats;

/// <summary>
/// Identifies which Musepack bitstream version a file uses. Set on
/// <see cref="IO.MpcStream.Version"/> after a successful read.
/// </summary>
public enum MpcStreamVersion
{
    /// <summary>Stream version 7 — magic <c>'M','P','+',0x?7</c>. Frame-based bitstream.</summary>
    Sv7,

    /// <summary>Stream version 8 — magic <c>MPCK</c>. Keyed-packet bitstream.</summary>
    Sv8,
}
