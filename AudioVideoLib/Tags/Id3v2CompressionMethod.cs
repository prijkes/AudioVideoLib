/*
 * Date: 2012-12-03
 * Sources used:
 *  http://id3lib.sourceforge.net/api/tag__parse_8cpp-source.html
 *  http://nedbatchelder.com/code/modules/id3reader.py
 *  https://gnunet.org/svn/Extractor/test/id3v2/README.txt
 *  http://id3lib.sourceforge.net/id3lib-manual.php
 */

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Id3v2 compression method.
    /// </summary>
    public enum Id3v2CompressionMethod
    {
        /// <summary>
        /// ZLib compression.
        /// </summary>
        ZLib = 'z'
    }
}
