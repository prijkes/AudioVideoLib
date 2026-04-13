namespace AudioVideoLib.Tags;

using System;
using System.IO;
using System.Text;

using AudioVideoLib.IO;

/// <summary>
/// The header for an <see cref="ApeItem"/>.
/// </summary>
public partial class ApeItem
{

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the frame version of the frame.
    /// </summary>
    /// <value>
    /// The version.
    /// </value>
    public ApeVersion Version { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="ApeItem"/> is read only.
    /// </summary>
    /// <value>
    /// <c>true</c> if the <see cref="ApeItem"/> is read only; otherwise, <c>false</c>.
    /// </value>
    public bool IsReadOnly
    {
        get
        {
            return (Flags & HeaderFlags.ReadOnly) == 0;
        }

        set
        {
            if (value)
            {
                Flags |= HeaderFlags.ReadOnly;
            }
            else
            {
                Flags &= ~HeaderFlags.ReadOnly;
            }
        }
    }

    /// <summary>
    /// Gets the type of the item.
    /// </summary>
    /// <value>
    /// The type of the item.
    /// </value>
    /// <remarks>
    /// See <see cref="ApeItemType" /> for possible item types.
    /// </remarks>
    public virtual ApeItemType ItemType
    {
        // Read directly from the raw backing field, not via the Flags property — the
        // Flags getter re-encodes the item-type bits from this very property, so going
        // through it would recurse forever for base-class instances that don't
        // override ItemType (e.g. ApeItemType.Reserved from ApeItemFactory).
        get
        {
            return GetItemType(_flags);
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the key of this item.
    /// </summary>
    /// <value>
    /// The key of the item.
    /// </value>
    public string Key
    {
        get;

        private set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.Length < MinKeyLengthCharacters)
            {
                throw new InvalidDataException(string.Format("value needs to be at least {0} characters long.", MinKeyLengthCharacters));
            }

            field = GetTruncatedEncodedString(value, MaxKeyLengthBytes);
        }
    } = null!;

    private int _flags;

    /// <summary>
    /// Gets or sets the flags of this frame.
    /// </summary>
    /// The flags value is stored as short (2 bytes) for all versions.
    private int Flags
    {
        get
        {
            return (_flags & ~HeaderFlags.ItemType) | (((int)ItemType) << 1);
        }

        set
        {
            _flags = value;
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------

    private static string GetTruncatedEncodedString(string value, int maxBytesAllowed)
    {
        return StreamBuffer.GetTruncatedEncodedString(value, Encoding.UTF8, maxBytesAllowed);
    }
}
