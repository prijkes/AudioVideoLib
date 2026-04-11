/*
 * Date: 2011-10-28
 * Sources used: 
 *  http://en.wikipedia.org/wiki/APE_tag
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv1_specification
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2
 *  http://wiki.hydrogenaudio.org/index.php?title=APEv2_specification
 *  http://www.monkeysaudio.com/developers.html
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Defines the type of an <see cref="ApeItem"/>.
    /// </summary>
    public enum ApeItemType
    {
        /// <summary>
        /// Item contains text information coded in UTF-8.
        /// </summary>
        CodedUTF8 = 0,

        /// <summary>
        /// Item contains binary information.
        /// </summary>
        /// <remarks>
        /// Binary information: Information which should not be edited by a text editor, because:
        /// * Information is not a text;
        /// * Contains control characters;
        /// * Contains internal restrictions which can't be handled by a normal text editor;
        /// * Can't be easily interpreted by humans;
        /// </remarks>
        ContainsBinary = 1,

        /// <summary>
        /// Item is a locator of external stored information.
        /// </summary>
        /// <remarks>
        /// Allowed formats:
        /// * http://host/directory/filename.ext
        /// * ftp://host/directory/filename.ext
        /// * filename.ext
        /// * /directory/filename.ext
        /// * DRIVE:/directory/filename.ext
        /// <para />
        /// Note: Locators are also UTF-8 encoded. This can especially occur when filenames are encoded.
        /// </remarks>
        IsLocator = 2,

        /// <summary>
        /// Reserved type.
        /// </summary>
        Reserved = 3
    }
}
