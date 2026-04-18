namespace AudioVideoLib.Tags;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AudioVideoLib.IO;

/// <summary>
/// Class for storing an URL link.
/// <para />
/// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
/// </summary>
public sealed partial class Id3v2UrlLinkFrame : Id3v2Frame
{
    private readonly string _identifier = null!;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2UrlLinkFrame"/> class.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="identifier">The identifier of the frame type for the <see cref="Id3v2Version"/> supplied.</param>
    /// <remarks>
    /// When the <paramref name="identifier"/> is not a valid/known identifier for the <paramref name="version"/>, it will look through all the known identifiers
    /// and see if a known identifier partly matches the <paramref name="identifier"/>. If found, it will get the proper identifier for the <paramref name="version"/>;
    /// otherwise, an <exception cref="InvalidDataException">invalid identifier exception</exception> is thrown.
    /// </remarks>
    public Id3v2UrlLinkFrame(Id3v2Version version, string identifier) : base(version)
    {
        if (!IsValidUrlLinkIdentifier(version, identifier))
        {
            // Maybe the identifier is actually a valid identifier but for the wrong version; try to find the 'real' identifier here.
            KeyValuePair<Id3v2UrlLinkFrameIdentifier, Dictionary<string, Id3v2Version[]>>[] pairs =
                [.. Identifiers.Where(
                    urlLinkFramePair =>
                    urlLinkFramePair.Value.OrderByDescending(f => f.Key).Any(f => f.Key.IndexOf(identifier, StringComparison.OrdinalIgnoreCase) >= 0))];

            // Grab the 'real' identifier for the version supplied.
            var resolved = pairs.Length != 0 ? pairs[0].Value.Where(t => t.Value.Contains(version)).Select(t => t.Key).FirstOrDefault() : null;

            if (string.IsNullOrEmpty(resolved))
            {
                throw new InvalidDataException("identifier is not a valid identifier.");
            }

            identifier = resolved!;
        }
        _identifier = identifier;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override byte[] Data
    {
        get => Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default).GetBytes(Url);

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            var stream = new StreamBuffer(value);
            var defaultEncoding = Id3v2FrameEncoding.GetEncoding(Id3v2FrameEncodingType.Default);
            Url = stream.ReadString(defaultEncoding, true);
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the actual URL.
    /// </summary>
    /// <value>
    /// The URL value.
    /// </value>
    /// <remarks>
    /// The URL must be a valid RFC 1738 URL, use <see cref="Id3v2Frame.IsValidUrl"/> to check if a value is a valid URL.
    /// </remarks>
    public string Url
    {
        get => field;

        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (!IsValidDefaultTextString(value, false))
                {
                    throw new InvalidDataException("value contains one or more invalid characters.");
                }

                if (!IsValidUrl(value))
                {
                    throw new InvalidDataException("value is not a valid RFC 1738 URL.");
                }
            }
            field = value;
        }
    } = null!;

    /// <inheritdoc />
    public override string? Identifier
    {
        get
        {
            // Grab the version depending on the version
            var entry =
                Identifiers.Where(
                    i => i.Value != null && i.Value.Any(f => string.Equals(f.Key, base.Identifier, StringComparison.OrdinalIgnoreCase)))
                    .Select(i => i.Value)
                    .FirstOrDefault();

            return (entry != null)
                       ? entry.Where(d => d.Value != null && d.Value.Contains(Version)).Select(d => d.Key).FirstOrDefault()
                       : _identifier;
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the identifier as string.
    /// </summary>
    /// <param name="version">The <see cref="Id3v2Version"/>.</param>
    /// <param name="identifier">The <see cref="Id3v2TextFrameIdentifier"/>.</param>
    /// <returns>
    /// The identifier as string for the specified <see cref="Id3v2TextFrameIdentifier"/>, or null if not found.
    /// </returns>
    public static string? GetIdentifier(Id3v2Version version, Id3v2UrlLinkFrameIdentifier identifier)
    {
        return Identifiers.TryGetValue(identifier, out var identifiers)
                   ? identifiers.Where(v => v.Value != null && v.Value.Contains(version)).Select(i => i.Key).FirstOrDefault()
                   : null;
    }

    /// <summary>
    /// Determines whether the url link identifier is valid for the specified version.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="identifier">The identifier.</param>
    /// <returns>
    ///   <c>true</c> if the identifier is valid for the specified version; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsValidUrlLinkIdentifier(Id3v2Version version, string identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        return IsValidIdentifier(version, identifier) && identifier.StartsWith('W');
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(Id3v2Frame? frame) => Equals(frame as Id3v2UrlLinkFrame);

    /// <summary>
    /// Equals the specified <see cref="Id3v2UrlLinkFrame"/>.
    /// </summary>
    /// <param name="udti">The <see cref="Id3v2UrlLinkFrame"/>.</param>
    /// <returns>true if equal; false otherwise.</returns>
    /// <remarks>
    /// There may be more than one <see cref="Id3v2UrlLinkFrame"/> frame of its kind in an <see cref="Id3v2Tag"/>.
    /// </remarks>
    public bool Equals(Id3v2UrlLinkFrame? udti)
    {
        return udti is not null && (ReferenceEquals(this, udti) || ((udti.Version == Version) && string.Equals(udti.Identifier, Identifier, StringComparison.OrdinalIgnoreCase)
               && string.Equals(udti.Url, Url, StringComparison.OrdinalIgnoreCase)));
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
            return (Version.GetHashCode() * 397) ^ ((Identifier?.GetHashCode() ?? 0) * 397);
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
        // See if the identifier is a known identifier.
        var entry =
            Identifiers.Where(i => i.Value != null && i.Value.Any(f => string.Equals(f.Key, base.Identifier, StringComparison.OrdinalIgnoreCase)))
                .Select(i => i.Value)
                .FirstOrDefault();

        // If the identifier is known, see if it exists in the given version.
        // If the identifier isn't known, see if the supplied version can support it.
        return (entry != null) ? entry.Any(d => d.Value != null && d.Value.Contains(version)) : base.IsVersionSupported(version);
    }
}
