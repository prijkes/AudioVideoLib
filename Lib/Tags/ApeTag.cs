/*
 * Date: 2010-02-12
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://en.wikipedia.org/wiki/APE_tag
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1_specification
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2_specification
 *  http://www.monkeysaudio.com/developers.html
 */
using System;
using System.Collections.Generic;
using System.Linq;

using AudioVideoLib.Collections;
using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /*
        APEv2 is a tagging format derived from APEv1 originally developed for MPC audio files, and is now also used in Monkey's Audio, WavPack and OptimFROG.
        It can also be used with other formats when using programs like foobar2000 or Tag (a program that can create and read tags).
    */

    /// <summary>
    /// Class to store an APE tag.
    /// </summary>
    public sealed partial class ApeTag : IAudioTag
    {
        /// <summary>
        /// Max number of allowed <see cref="ApeItem"/>s in a tag.
        /// </summary>
        public const int MaxAllowedFields = 0xFFFF;

        /// <summary>
        /// Max allowed size for a <see cref="ApeTag"/>.
        /// </summary>
        public const int MaxAllowedSize = 1024 * 1024 * 16;

        private readonly EventList<ApeItem> _items = new EventList<ApeItem>();

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="ApeTag"/> class.
        /// </summary>
        public ApeTag()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApeTag"/> class.
        /// </summary>
        /// <param name="version">The tag version.</param>
        public ApeTag(ApeVersion version)
        {
            if (!IsValidVersion(version))
                throw new ArgumentOutOfRangeException("version");

            Version = version;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApeTag" /> class.
        /// </summary>
        /// <param name="version">The tag version.</param>
        /// <param name="flags">The flags.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">version</exception>
        public ApeTag(ApeVersion version, int flags)
        {
            if (!IsValidVersion(version))
                throw new ArgumentOutOfRangeException("version");

            Version = version;

            Flags = flags;
        }


        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the <see cref="ApeItem"/>s in the <see cref="ApeTag"/>.
        /// </summary>
        /// <value>
        /// A list of <see cref="ApeItem"/>s in the tag.
        /// </value>
        public IEnumerable<ApeItem> Items
        {
            get
            {
                return _items.AsReadOnly();
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as ApeTag);
        }

        /// <inheritdoc/>
        public bool Equals(IAudioTag other)
        {
            return Equals(other as ApeTag);
        }

        /// <summary>
        /// Equals the specified <see cref="ApeTag"/>.
        /// </summary>
        /// <param name="tag">The <see cref="ApeTag"/>.</param>
        /// <returns>true if equal; false otherwise.</returns>
        public bool Equals(ApeTag tag)
        {
            if (ReferenceEquals(null, tag))
                return false;

            if (ReferenceEquals(this, tag))
                return true;

            return (tag.Version == Version) && (tag.Flags == Flags) && tag.Items.SequenceEqual(Items);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// The value should be calculated on immutable fields only.
        public override int GetHashCode()
        {
            unchecked
            {
                return (Version.GetHashCode() * 397);
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the first item of type T.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <returns>
        /// The first item of type T if found; otherwise, null.
        /// </returns>
        public T GetItem<T>() where T : ApeItem
        {
            return _items.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Gets the <see cref="ApeItem"/>.
        /// </summary>
        /// <param name="key">The key of the <see cref="ApeItem"/>.</param>
        /// <returns>
        /// The <see cref="ApeItem"/> when found, otherwise null.
        /// </returns>
        public ApeItem GetItem(ApeItemKey key)
        {
            IEnumerable<string> keys = ApeItem.GetItemKeys(key);
            return _items.FirstOrDefault(f => keys.Contains(f.Key));
        }

        /// <summary>
        /// Gets the <see cref="ApeItem"/>.
        /// </summary>
        /// <param name="key">The key of the <see cref="ApeItem"/>.</param>
        /// <returns>
        /// The <see cref="ApeItem"/> when found, otherwise null.
        /// </returns>
        public ApeItem GetItem(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            return _items.FirstOrDefault(f => String.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all items of type T.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <returns>
        /// A list of items of type T.
        /// </returns>
        public IEnumerable<T> GetItems<T>() where T : ApeItem
        {
            return _items.OfType<T>();
        }

        /// <summary>
        /// Updates the first item with a matching key if found; else, adds a new item.
        /// </summary>
        /// <param name="item">Item to add to the <see cref="ApeTag"/>.</param>
        /// <remarks>
        /// The item needs to have the same version set as the tag, otherwise an <see cref="InvalidVersionException"/> will be thrown.
        /// </remarks>
        public void SetItem(ApeItem item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            if (item.Version != Version)
                throw new InvalidVersionException("The version of the item needs to be matching the version of the tag.");

            int i, itemCount = _items.Count;
            for (i = 0; i < itemCount; i++)
            {
                if (!ReferenceEquals(_items[i], item) && !String.Equals(_items[i].Key, item.Key, StringComparison.OrdinalIgnoreCase))
                    continue;

                _items[i] = item;
                break;
            }

            if (i == itemCount)
                _items.Add(item);
        }

        /// <summary>
        /// Updates a list of items if found; else, adds it.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <remarks>
        /// The items needs to have the same version set as the tag, otherwise an <see cref="InvalidVersionException"/> will be thrown.
        /// </remarks>
        public void SetItems(IEnumerable<ApeItem> items)
        {
            if (items == null)
                throw new ArgumentNullException("items");

            foreach (ApeItem item in items)
                SetItem(item);
        }

        /// <summary>
        /// Removes the first item with the same key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if key is null.</exception>
        public void RemoveItem(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            ApeItem item = _items.FirstOrDefault(i => String.Equals(i.Key, key, StringComparison.OrdinalIgnoreCase));
            if (item != null)
                _items.Remove(item);
        }

        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if item is null.</exception>
        public void RemoveItem(ApeItem item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            _items.RemoveAll(i => ReferenceEquals(i, item));
        }

        /// <summary>
        /// Removes all items with the same key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if key is null.</exception>
        public void RemoveItems(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            _items.RemoveAll(i => String.Equals(i.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Removes all items of type T.
        /// </summary>
        /// <typeparam name="T">A class of type <see cref="ApeItem" />.</typeparam>
        public void RemoveItems<T>() where T : ApeItem
        {
            _items.RemoveAll(i => i is T);
        }

        /// <summary>
        /// Removes the items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if items is null.</exception>
        public void RemoveItems(IEnumerable<ApeItem> items)
        {
            if (items == null)
                throw new ArgumentNullException("items");

            foreach (ApeItem item in items)
                RemoveItem(item);
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public byte[] ToByteArray()
        {
            using (StreamBuffer fullBuffer = new StreamBuffer())
            {
                // Save flags so we can restore it later
                bool useHeader = UseHeader, isHeader = IsHeader, useFooter = UseFooter;

                int dataSize;
                using (StreamBuffer data = new StreamBuffer())
                {
                    foreach (byte[] field in _items.Select(frame => frame.ToByteArray()))
                        data.Write(field, field.Length);

                    dataSize = (int)(data.Length + (UseFooter ? HeaderSize : 0));

                    if (UseHeader)
                    {
                        fullBuffer.Write(TagIdentifierBytes);
                        fullBuffer.WriteInt((int)Version * 1000);
                        fullBuffer.WriteInt(dataSize);
                        fullBuffer.WriteInt(_items.Count);
                        SetUseHeaderFlag(false, false);
                        IsHeader = true;
                        fullBuffer.WriteInt(Flags);
                        fullBuffer.Write(Reserved, Reserved.Length);
                    }

                    // Data.
                    fullBuffer.Write(data.ToByteArray());
                }

                // Reset flags
                SetUseHeaderFlag(useHeader, false);
                IsHeader = isHeader;
                SetUseFooterFlag(useFooter, false);

                if (UseFooter)
                {
                    fullBuffer.Write(TagIdentifierBytes);
                    fullBuffer.WriteInt((int)Version * 1000);
                    fullBuffer.WriteInt(dataSize);
                    fullBuffer.WriteInt(_items.Count);
                    SetUseFooterFlag(false, false);
                    IsHeader = false;                               // Footer flags shouldn't have this flag set
                    SetUseHeaderFlag(useHeader, false);       // If useHeader, write header flag, 
                    fullBuffer.WriteInt(Flags);
                    fullBuffer.Write(Reserved, Reserved.Length);
                }

                // Reset flags
                SetUseHeaderFlag(useHeader, false);
                IsHeader = isHeader;
                SetUseFooterFlag(useFooter, false);

                // Return data
                return fullBuffer.ToByteArray();
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "APEv" + Version.ToString().Last();
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static bool IsValidVersion(ApeVersion version)
        {
            return Enum.TryParse(version.ToString(), true, out version);
        }
    }
}
