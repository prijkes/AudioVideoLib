namespace AudioVideoLib.Tags;

using System;
using System.Text;

/// <summary>
/// Per-field text storage for an ID3v1 tag. The source of truth is the raw byte
/// sequence captured at parse time; a per-field <see cref="Encoding"/> override
/// controls how those bytes are decoded and (on save) how new string values are
/// re-encoded. This lets callers display a tag written with one encoding (e.g.
/// UTF-8) even when the tag-level default is something else (e.g. Latin-1).
/// </summary>
public partial class Id3v1Tag
{
    private byte[] _titleBytes = [];
    private byte[] _artistBytes = [];
    private byte[] _albumTitleBytes = [];
    private byte[] _albumYearBytes = [];
    private byte[] _trackCommentBytes = [];
    private byte[] _extendedTrackGenreBytes = [];

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Tag-level fallback <see cref="Encoding"/>. Used for any field whose own
    /// per-field <c>Encoding</c> property is <c>null</c>.
    /// </summary>
    public Encoding Encoding
    {
        get;
        set
        {
            field = value ?? throw new ArgumentNullException(nameof(value));
        }
    } = Encoding.Default;

    /// <summary>Per-field encoding override for <see cref="TrackTitle"/>; <c>null</c> falls back to <see cref="Encoding"/>.</summary>
    public Encoding? TrackTitleEncoding { get; set; }

    /// <summary>Per-field encoding override for <see cref="Artist"/>; <c>null</c> falls back to <see cref="Encoding"/>.</summary>
    public Encoding? ArtistEncoding { get; set; }

    /// <summary>Per-field encoding override for <see cref="AlbumTitle"/>; <c>null</c> falls back to <see cref="Encoding"/>.</summary>
    public Encoding? AlbumTitleEncoding { get; set; }

    /// <summary>Per-field encoding override for <see cref="AlbumYear"/>; <c>null</c> falls back to <see cref="Encoding"/>.</summary>
    public Encoding? AlbumYearEncoding { get; set; }

    /// <summary>Per-field encoding override for <see cref="TrackComment"/>; <c>null</c> falls back to <see cref="Encoding"/>.</summary>
    public Encoding? TrackCommentEncoding { get; set; }

    /// <summary>Per-field encoding override for <see cref="ExtendedTrackGenre"/>; <c>null</c> falls back to <see cref="Encoding"/>.</summary>
    public Encoding? ExtendedTrackGenreEncoding { get; set; }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>Effective encoding used when decoding/encoding <see cref="TrackTitle"/>.</summary>
    public Encoding EffectiveTrackTitleEncoding => TrackTitleEncoding ?? Encoding;

    /// <summary>Effective encoding used when decoding/encoding <see cref="Artist"/>.</summary>
    public Encoding EffectiveArtistEncoding => ArtistEncoding ?? Encoding;

    /// <summary>Effective encoding used when decoding/encoding <see cref="AlbumTitle"/>.</summary>
    public Encoding EffectiveAlbumTitleEncoding => AlbumTitleEncoding ?? Encoding;

    /// <summary>Effective encoding used when decoding/encoding <see cref="AlbumYear"/>.</summary>
    public Encoding EffectiveAlbumYearEncoding => AlbumYearEncoding ?? Encoding;

    /// <summary>Effective encoding used when decoding/encoding <see cref="TrackComment"/>.</summary>
    public Encoding EffectiveTrackCommentEncoding => TrackCommentEncoding ?? Encoding;

    /// <summary>Effective encoding used when decoding/encoding <see cref="ExtendedTrackGenre"/>.</summary>
    public Encoding EffectiveExtendedTrackGenreEncoding => ExtendedTrackGenreEncoding ?? Encoding;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>Raw bytes for the title field — combined standard (30) + extended (60) regions if extended is in use.</summary>
    public byte[] TrackTitleRawBytes
    {
        get => (byte[])_titleBytes.Clone();
        set => _titleBytes = TrimTrailingZeros(value ?? []);
    }

    /// <summary>Raw bytes for the artist field — combined standard (30) + extended (60) regions if extended is in use.</summary>
    public byte[] ArtistRawBytes
    {
        get => (byte[])_artistBytes.Clone();
        set => _artistBytes = TrimTrailingZeros(value ?? []);
    }

    /// <summary>Raw bytes for the album field — combined standard (30) + extended (60) regions if extended is in use.</summary>
    public byte[] AlbumTitleRawBytes
    {
        get => (byte[])_albumTitleBytes.Clone();
        set => _albumTitleBytes = TrimTrailingZeros(value ?? []);
    }

    /// <summary>Raw bytes for the year field (4 bytes max).</summary>
    public byte[] AlbumYearRawBytes
    {
        get => (byte[])_albumYearBytes.Clone();
        set => _albumYearBytes = TrimTrailingZeros(value ?? []);
    }

    /// <summary>Raw bytes for the comment field (28 bytes for v1.1, 30 bytes for v1.0).</summary>
    public byte[] TrackCommentRawBytes
    {
        get => (byte[])_trackCommentBytes.Clone();
        set => _trackCommentBytes = TrimTrailingZeros(value ?? []);
    }

    /// <summary>Raw bytes for the extended genre field (TAG+, 30 bytes max).</summary>
    public byte[] ExtendedTrackGenreRawBytes
    {
        get => (byte[])_extendedTrackGenreBytes.Clone();
        set => _extendedTrackGenreBytes = TrimTrailingZeros(value ?? []);
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the artist string. Decoded from <see cref="ArtistRawBytes"/> via
    /// <see cref="EffectiveArtistEncoding"/>; assigning re-encodes the raw bytes,
    /// truncated to the spec maximum (30 bytes standard, 90 bytes when <see cref="UseExtendedTag"/>).
    /// </summary>
    public string? Artist
    {
        get => DecodeNullable(_artistBytes, EffectiveArtistEncoding);
        set => _artistBytes = EncodeAndTruncate(value, EffectiveArtistEncoding, UseExtendedTag ? 90 : 30);
    }

    /// <summary>
    /// Gets or sets the album title string. Decoded from <see cref="AlbumTitleRawBytes"/> via
    /// <see cref="EffectiveAlbumTitleEncoding"/>; assigning re-encodes the raw bytes,
    /// truncated to the spec maximum (30 bytes standard, 90 bytes when <see cref="UseExtendedTag"/>).
    /// </summary>
    public string? AlbumTitle
    {
        get => DecodeNullable(_albumTitleBytes, EffectiveAlbumTitleEncoding);
        set => _albumTitleBytes = EncodeAndTruncate(value, EffectiveAlbumTitleEncoding, UseExtendedTag ? 90 : 30);
    }

    /// <summary>
    /// Gets or sets the album year string (4 bytes ASCII). Decoded from <see cref="AlbumYearRawBytes"/>
    /// via <see cref="EffectiveAlbumYearEncoding"/>; assigning truncates to 4 bytes.
    /// </summary>
    public string? AlbumYear
    {
        get => DecodeNullable(_albumYearBytes, EffectiveAlbumYearEncoding);
        set => _albumYearBytes = EncodeAndTruncate(value, EffectiveAlbumYearEncoding, 4);
    }

    /// <summary>
    /// Gets or sets the track comment string. Decoded from <see cref="TrackCommentRawBytes"/>
    /// via <see cref="EffectiveTrackCommentEncoding"/>; assigning truncates to
    /// <see cref="TrackCommentLength"/> bytes.
    /// </summary>
    public string? TrackComment
    {
        get => DecodeNullable(_trackCommentBytes, EffectiveTrackCommentEncoding);
        set => _trackCommentBytes = EncodeAndTruncate(value, EffectiveTrackCommentEncoding, TrackCommentLength);
    }

    /// <summary>
    /// Gets or sets the genre.
    /// </summary>
    public Id3v1Genre Genre
    {
        get;
        set
        {
            if (!IsValidGenre(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the track number (1-byte unsigned). Only meaningful for <see cref="Id3v1Version.Id3v11"/>.
    /// </summary>
    public byte TrackNumber { get; set; }

    /// <summary>
    /// Gets or sets the title string. Decoded from <see cref="TrackTitleRawBytes"/> via
    /// <see cref="EffectiveTrackTitleEncoding"/>; assigning re-encodes the raw bytes,
    /// truncated to the spec maximum (30 bytes standard, 90 bytes when <see cref="UseExtendedTag"/>).
    /// </summary>
    public string? TrackTitle
    {
        get => DecodeNullable(_titleBytes, EffectiveTrackTitleEncoding);
        set => _titleBytes = EncodeAndTruncate(value, EffectiveTrackTitleEncoding, UseExtendedTag ? 90 : 30);
    }

    /// <summary>
    /// Gets the length of the track comment region for the current version (28 for v1.1, 30 for v1.0).
    /// </summary>
    public int TrackCommentLength => Version >= Id3v1Version.Id3v11 ? 28 : 30;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets a value indicating whether to write the extended TAG+ block.
    /// </summary>
    public bool UseExtendedTag { get; set; }

    /// <summary>
    /// Gets or sets the track speed (TAG+ only).
    /// </summary>
    public Id3v1TrackSpeed TrackSpeed
    {
        get;
        set
        {
            if (!IsValidTrackSpeed(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the extended track genre string (TAG+ only). Decoded from
    /// <see cref="ExtendedTrackGenreRawBytes"/> via <see cref="EffectiveExtendedTrackGenreEncoding"/>.
    /// </summary>
    public string? ExtendedTrackGenre
    {
        get => DecodeNullable(_extendedTrackGenreBytes, EffectiveExtendedTrackGenreEncoding);
        set => _extendedTrackGenreBytes = EncodeAndTruncate(value, EffectiveExtendedTrackGenreEncoding, 30);
    }

    /// <summary>
    /// Gets or sets the start time (TAG+ only).
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time (TAG+ only).
    /// </summary>
    public TimeSpan EndTime { get; set; }

    ////------------------------------------------------------------------------------------------------------------------------------

    private static byte[] TrimTrailingZeros(byte[] bytes)
    {
        var len = bytes.Length;
        while (len > 0 && bytes[len - 1] == 0)
        {
            len--;
        }

        if (len == bytes.Length)
        {
            return bytes;
        }

        var trimmed = new byte[len];
        Array.Copy(bytes, trimmed, len);
        return trimmed;
    }

    private static string? DecodeNullable(byte[] bytes, Encoding encoding)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        // Strip trailing zero padding for display.
        var len = bytes.Length;
        while (len > 0 && bytes[len - 1] == 0)
        {
            len--;
        }

        return len == 0 ? string.Empty : encoding.GetString(bytes, 0, len);
    }

    private static byte[] EncodeAndTruncate(string? value, Encoding encoding, int maxBytes)
    {
        if (string.IsNullOrEmpty(value))
        {
            return [];
        }

        var bytes = encoding.GetBytes(value);
        if (bytes.Length <= maxBytes)
        {
            return bytes;
        }

        // Re-encode shorter prefixes until the result fits — handles multi-byte
        // encodings where a naive byte-truncation could split a code point.
        for (var charCount = value.Length - 1; charCount >= 0; charCount--)
        {
            var prefix = encoding.GetBytes(value, 0, charCount);
            if (prefix.Length <= maxBytes)
            {
                return prefix;
            }
        }

        return [];
    }
}
