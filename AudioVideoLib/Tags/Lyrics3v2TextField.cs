namespace AudioVideoLib.Tags;

using System;
using System.IO;
using System.Text;

/// <summary>
/// Class to store a Lyrics3v2 text field.
/// </summary>
public sealed partial class Lyrics3v2TextField : Lyrics3v2Field
{
    private string _value = null!;
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="Lyrics3v2TextField"/> class.
    /// </summary>
    /// <param name="identifier">The identifier of the field.</param>
    public Lyrics3v2TextField(Lyrics3v2TextFieldIdentifier identifier) : base(GetIdentifier(identifier) ?? throw new ArgumentException("Unknown identifier", nameof(identifier)))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Lyrics3v2TextField"/> class.
    /// </summary>
    /// <param name="identifier">The identifier of the field.</param>
    public Lyrics3v2TextField(string identifier) : base(identifier)
    {
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override byte[]? Data
    {
        get
        {
            return (_value != null) ? Encoding.ASCII.GetBytes(_value) : null;
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            _value = Encoding.ASCII.GetString(value);
        }
    }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    /// <value>
    /// The value.
    /// </value>
    public string Value
    {
        get
        {
            return _value;
        }

        set
        {
            if (!string.IsNullOrEmpty(value) && !IsValidString(value))
            {
                throw new InvalidDataException("Value contains one or more invalid characters.");
            }

            _value = value;
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the identifier as string.
    /// </summary>
    /// <param name="identifier">The <see cref="Lyrics3v2TextFieldIdentifier"/>.</param>
    /// <returns>
    /// The identifier as string for the specified <see cref="Lyrics3v2TextFieldIdentifier"/>, or null if not found.
    /// </returns>
    public static string? GetIdentifier(Lyrics3v2TextFieldIdentifier identifier)
    {
        string? id;
        return Identifiers.TryGetValue(identifier, out id) ? id : null;
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(Lyrics3v2Field? audioFrame)
    {
        return Equals(audioFrame as Lyrics3v2TextField);
    }

    /// <summary>
    /// Equals the specified <see cref="Lyrics3v2TextField"/>.
    /// </summary>
    /// <param name="field">The <see cref="Lyrics3v2TextField"/>.</param>
    /// <returns>
    /// true if equal; false otherwise.
    /// </returns>
    public bool Equals(Lyrics3v2TextField? field)
    {
        return field is not null && (ReferenceEquals(this, field) || (string.Equals(field.Identifier, Identifier, StringComparison.OrdinalIgnoreCase)
               && string.Equals(field.Value, Value, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Serves as a hash function for a particular type.
    /// </summary>
    /// <returns>
    /// A hash code for the current <see cref="T:System.Object"/>.
    /// </returns>
    /// <filterpriority>2</filterpriority>
    /// The value should be calculated on immutable fields only.
    public override int GetHashCode()
    {
        unchecked
        {
            return Identifier.GetHashCode() * 397;
        }
    }
}
