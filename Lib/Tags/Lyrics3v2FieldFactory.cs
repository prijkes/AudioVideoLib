/*
 * Date: 2013-10-26
 * Sources used: 
 *  http://id3.org/Lyrics3v2
 */
using System.Linq;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store a Lyrics3v2 field.
    /// </summary>
    public partial class Lyrics3v2Field
    {
        private static readonly Lyrics3v2FieldFactoryItem[] FieldFactories = 
        {
            // IND: Indications field
            new Lyrics3v2FieldFactoryItem
            {
                Type = typeof(Lyrics3v2IndicationsField),
                Identifiers = new [] { "IND" },
                Factory = identifier => new Lyrics3v2IndicationsField()
            },

            // LYR: Lyrics multi line text
            new Lyrics3v2FieldFactoryItem
            {
                Type = typeof(Lyrics3v2LyricsField),
                Identifiers = new[] { "LYR" },
                Factory = identifier => new Lyrics3v2LyricsField()
            },

            // INF: Additional information multi line text
            // AUT: Lyrics/Music Author Name
            // EAL: Extended Album name
            // EAR: Extended Artist name
            // ETT: Extended Track Title
            // GRE: Genre - see http://www.mpx.cz/mp3manager/tags.htm
            new Lyrics3v2FieldFactoryItem
            {
                Type = typeof(Lyrics3v2TextField),
                Identifiers = new[] { "INF", "AUT", "EAL", "EAR", "ETT", "GRE" },
                Factory = identifier => new Lyrics3v2TextField(identifier)
            },

            // IMG: Link to image files
            new Lyrics3v2FieldFactoryItem
            {
                Type = typeof(Lyrics3v2ImageFileField),
                Identifiers = new[] { "IMG" },
                Factory = identifier => new Lyrics3v2ImageFileField()
            }
        };

        ////------------------------------------------------------------------------------------------------------------------------------

        private static Lyrics3v2Field GetField(string identifier)
        {
            return FieldFactories.Where(f => f.IsMatch(identifier)).Select(f => f.Factory(identifier)).FirstOrDefault()
                   ?? new Lyrics3v2Field(identifier);
        }
    }
}
