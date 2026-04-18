namespace AudioVideoLib.Tags;

using System;
using System.IO;

using AudioVideoLib.IO;

/// <summary>
/// Class for storing user defined text information.
/// </summary>
/// <remarks>
/// This frame is intended for one-string text information concerning the audio file in a similar way to an <see cref="Id3v2TextFrame"/>.
/// <para />
/// This frame supports <see cref="Id3v2Version"/> up to and including <see cref="Id3v2Version.Id3v240"/>.
/// </remarks>
public sealed class Id3v2UserDefinedTextInformationFrame : Id3v2Frame
{
    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2UserDefinedTextInformationFrame"/> class with version <see cref="Id3v2Version.Id3v230"/>.
    /// </summary>
    public Id3v2UserDefinedTextInformationFrame() : base(Id3v2Version.Id3v230)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Id3v2UserDefinedTextInformationFrame"/> class.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <exception cref="InvalidVersionException">Thrown if <paramref name="version"/> is not supported by this frame.</exception>
    public Id3v2UserDefinedTextInformationFrame(Id3v2Version version) : base(version)
    {
        if (!IsVersionSupported(version))
        {
            throw new InvalidVersionException($"Version {version} not supported by this frame.");
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the text encoding, see <see cref="Id3v2FrameEncodingType"/> for possible values.
    /// </summary>
    /// <value>
    /// The text encoding.
    /// </value>
    /// <remarks>
    /// An <see cref="InvalidDataException"/> will be thrown when the <see cref="Description"/> or the <see cref="Value"/>
    /// is not valid in the new <see cref="Id3v2FrameEncodingType"/>.
    /// </remarks>
    public Id3v2FrameEncodingType TextEncoding
    {
        get => field;

        set
        {
            if (!string.IsNullOrEmpty(Description) && !IsValidTextString(Description, value, false))
            {
                throw new InvalidDataException("Description contains one or more invalid characters for the specified frame encoding type.");
            }

            if (!string.IsNullOrEmpty(Value) && !IsValidTextString(Value, value, false))
            {
                throw new InvalidDataException("Value contains one or more invalid characters for the specified frame encoding type.");
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the description of the string.
    /// </summary>
    /// <value>
    /// The description.
    /// </value>
    /// <remarks>
    /// New lines are not allowed.
    /// </remarks>
    public string Description
    {
        get => field;

        set
        {
            if (!string.IsNullOrEmpty(value) && !IsValidTextString(value, TextEncoding, false))
            {
                throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");
            }

            field = value;
        }
    } = null!;

    /// <summary>
    /// Gets or sets the actual value.
    /// </summary>
    /// <value>
    /// The value.
    /// </value>
    /// <remarks>
    /// New lines are not allowed.
    /// </remarks>
    public string Value
    {
        get => field;

        set
        {
            if (!string.IsNullOrEmpty(value) && !IsValidTextString(value, TextEncoding, false))
            {
                throw new InvalidDataException("value contains one or more invalid characters for the current frame encoding type.");
            }

            field = value;
        }
    } = null!;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override byte[] Data
    {
        get
        {
            var encoding = Id3v2FrameEncoding.GetEncoding(TextEncoding);
            var stream = new StreamBuffer();
            var preamble = Id3v2FrameEncoding.GetEncodingPreamble(TextEncoding);

            // Text encoding
            stream.WriteByte(Id3v2FrameEncoding.GetEncodingTypeValue(TextEncoding));

            // Preamble
            stream.Write(preamble);

            // Description
            if (Description != null)
            {
                stream.WriteString(Description, encoding);
            }

            // String terminator (0x00 in encoding)
            stream.Write(encoding.GetBytes("\0"));

            // Preamble
            stream.Write(preamble);

            // Value
            if (Value != null)
            {
                stream.WriteString(Value, encoding);
            }

            return stream.ToByteArray();
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            var stream = new StreamBuffer(value);
            TextEncoding = Id3v2FrameEncoding.ReadEncodingTypeFromStream(stream);
            var encoding = Id3v2FrameEncoding.GetEncoding(TextEncoding);
            Description = stream.ReadString(encoding);
            Value = stream.ReadString(encoding);
        }
    }

    /// <inheritdoc />
    public override string Identifier => (Version < Id3v2Version.Id3v230) ? "TXX" : "TXXX";

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <inheritdoc/>
    public override bool Equals(Id3v2Frame? frame) => Equals(frame as Id3v2UserDefinedTextInformationFrame);

    /// <summary>
    /// Equals the specified <see cref="Id3v2UserDefinedTextInformationFrame"/>.
    /// </summary>
    /// <param name="udti">The <see cref="Id3v2UserDefinedTextInformationFrame"/>.</param>
    /// <returns>true if equal; false otherwise.</returns>
    /// <remarks>
    /// Both instances are equal when their <see cref="Version"/> and <see cref="Description"/> properties are equal (case-insensitive).
    /// </remarks>
    public bool Equals(Id3v2UserDefinedTextInformationFrame? udti)
    {
        return udti is not null && (ReferenceEquals(this, udti) || ((udti.Version == Version) && string.Equals(udti.Description, Description, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Determines whether the specified version is supported by the frame.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>
    ///   <c>true</c> if the specified version is supported; otherwise, <c>false</c>.
    /// </returns>
    public override bool IsVersionSupported(Id3v2Version version) => true;
}
