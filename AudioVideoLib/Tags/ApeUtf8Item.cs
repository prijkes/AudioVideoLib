namespace AudioVideoLib.Tags;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AudioVideoLib.Collections;
using AudioVideoLib.IO;

/// <summary>
/// Class used to store an <see cref="ApeUtf8Item"/> item.
/// </summary>
public class ApeUtf8Item : ApeItem
{
    private readonly NotifyingList<string> _values = [];

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="ApeUtf8Item"/> class.
    /// </summary>
    /// <param name="version">The <see cref="ApeVersion"/> of the <see cref="ApeTag"/>.</param>
    /// <param name="key">The name of the item.</param>
    public ApeUtf8Item(ApeVersion version, string key) : base(version, key)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApeUtf8Item"/> class.
    /// </summary>
    /// <param name="version">The <see cref="ApeVersion"/> of the <see cref="ApeTag"/>.</param>
    /// <param name="key">The key.</param>
    public ApeUtf8Item(ApeVersion version, ApeItemKey key) : base(version, key)
    {
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the character used to separate values from each other.
    /// </summary>
    public static char ValueSeparator => '\0';

    /// <summary>
    /// Gets the type of the item.
    /// </summary>
    /// <value>
    /// The type of the item.
    /// </value>
    /// <remarks>
    /// See <see cref="ApeItemType" /> for possible item types.
    /// </remarks>
    public override ApeItemType ItemType => ApeItemType.CodedUTF8;

    /// <inheritdoc/>
    public override byte[] Data
    {
        get
        {
            return Encoding.UTF8.GetBytes(_values.Aggregate(string.Empty, (s, t) => !string.IsNullOrEmpty(s) ? s + ValueSeparator + t : t));
        }

        protected set
        {
            ArgumentNullException.ThrowIfNull(value);

            var stream = new StreamBuffer(value);
            _values.Clear();
            while (stream.Position < stream.Length)
            {
                _values.Add(stream.ReadString(Encoding.UTF8, ValueSeparator));
            }

            // Items are not zero-terminated like in C/C++.
            // If there's a zero character, multiple items are stored under the key and the items are separated by zero characters. 
            // ------------------------------------------------------------------------------------------
            // If the last byte is a ValueSeparator, we'll need to write an additional empty _value string;
            // otherwise the last entry will always be skipped when calling ToByteArray()
            // the string 'nothingness\0' is basically 2 strings, 'nothingness' and '' ('\0' is the ValueSeparator in this case).
            var delimiterBytes = Encoding.UTF8.GetBytes([ValueSeparator]);
            if (value.Length < delimiterBytes.Length)
            {
                return;
            }

            var readBytes = new byte[delimiterBytes.Length];
            Buffer.BlockCopy(value, value.Length - delimiterBytes.Length, readBytes, 0, delimiterBytes.Length);
            if (StreamBuffer.SequenceEqual(readBytes, delimiterBytes))
            {
                _values.Add(string.Empty);
            }
        }
    }

    /// <summary>
    /// Gets or sets a list of one or more UTF-8 string(s).
    /// </summary>
    /// <value>
    /// A list of one ore more UTF-8 string(s).
    /// </value>
    public virtual IList<string> Values => _values;

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Equals the specified <see cref="ApeUtf8Item"/> instance with this one.
    /// </summary>
    /// <param name="item">The <see cref="ApeUtf8Item"/> instance.</param>
    /// <returns>True if both instances match; otherwise, false.</returns>
    public bool Equals(ApeUtf8Item item)
    {
        return Equals(item as ApeItem);
    }
}
