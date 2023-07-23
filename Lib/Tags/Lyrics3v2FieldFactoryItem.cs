/*
 * Date: 2013-10-26
 * Sources used: 
 *  http://id3.org/Lyrics3v2
 */
using System;
using System.Linq;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class to store a Lyrics3v2 field.
    /// </summary>
    public partial class Lyrics3v2Field
    {
        private class Lyrics3v2FieldFactoryItem
        {
            /// <summary>
            /// Gets or sets the <see cref="Lyrics3v2Field"/> type.
            /// </summary>
            /// <value>
            /// The <see cref="Lyrics3v2Field"/> type.
            /// </value>
            public Type Type { get; set; }

            /// <summary>
            /// Gets or sets the identifiers of the <see cref="Lyrics3v2Field"/>.
            /// </summary>
            /// <value>
            /// The <see cref="Lyrics3v2Field"/> identifiers.
            /// </value>
            public string[] Identifiers { get; set; }

            /// <summary>
            /// Gets or sets the factory for creating the <see cref="Lyrics3v2Field"/>.
            /// </summary>
            /// <value>
            /// The factory for creating the <see cref="Lyrics3v2Field"/>.
            /// </value>
            public Func<string, Lyrics3v2Field> Factory { get; set; }

            /// <summary>
            /// Gets or sets the partial comparer.
            /// </summary>
            /// <value>
            /// The partial comparer.
            /// </value>
            /// <remarks>
            /// The partial comparer is used for comparing <see cref="Lyrics3v2Field"/>s when the <see cref="Lyrics3v2Field"/> does not have pre-defined <see cref="Identifiers"/>.
            /// It is used to test if the <see cref="Factory"/> can create the given <see cref="Lyrics3v2Field"/> based on the identifier.
            /// </remarks>
            public Func<string, bool> PartialComparer { get; set; }

            ////------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Determines whether the specified identifier matches any of the <see cref="Identifiers"/>.
            /// </summary>
            /// <param name="identifier">The identifier.</param>
            /// <returns>
            /// true if the <paramref name="identifier"/> was found; otherwise, false.
            /// </returns>
            /// <remarks>
            /// A match is found when either the <see cref="Identifiers"/> is set and contains the <paramref name="identifier"/>,
            /// or when <see cref="PartialComparer"/> is set and returns <c>true</c>.
            /// </remarks>
            public bool IsMatch(string identifier)
            {
                return (Identifiers != null && Identifiers.Any(i => String.Equals(i, identifier, StringComparison.OrdinalIgnoreCase)))
                       || ((PartialComparer != null) && PartialComparer(identifier));
            }
        }
    }
}
