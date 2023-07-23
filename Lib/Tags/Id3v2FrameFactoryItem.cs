/*
 * Date: 2013-10-26
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class used to store an <see cref="Id3v2Tag"/> frame.
    /// </summary>
    /// <remarks>
    /// A frame is a block of information in an <see cref="Id3v2Tag"/>.
    /// </remarks>
    public partial class Id3v2Frame
    {
        private class Id3v2FrameFactoryItem
        {
            /// <summary>
            /// Gets or sets the <see cref="Id3v2Frame"/> type.
            /// </summary>
            /// <value>
            /// The <see cref="Id3v2Frame"/> type.
            /// </value>
            public Type Type { get; set; }

            /// <summary>
            /// Gets or sets the identifiers of the <see cref="Id3v2Frame"/>.
            /// </summary>
            /// <value>
            /// The <see cref="Id3v2Frame"/> identifiers.
            /// </value>
            /// <remarks>
            /// Each identifier is used for a <see cref="Id3v2Version"/>; multiple <see cref="Id3v2Version"/>s can use the same identifier.
            /// </remarks>
            public Dictionary<string, Id3v2Version[]> Identifiers { get; set; }

            /// <summary>
            /// Gets or sets the factory for creating the <see cref="Id3v2Frame"/>.
            /// </summary>
            /// <value>
            /// The factory for creating the <see cref="Id3v2Frame"/>.
            /// </value>
            /// <remarks>
            /// The factory creates new <see cref="Id3v2Frame"/> items depending on the <see cref="Id3v2Version"/> and the identifier.
            /// </remarks>
            public Func<Id3v2Version, string, Id3v2Frame> Factory { get; set; }

            /// <summary>
            /// Gets or sets the partial comparer.
            /// </summary>
            /// <value>
            /// The partial comparer.
            /// </value>
            /// <remarks>
            /// The partial comparer is used for comparing <see cref="Id3v2Frame"/>s when the <see cref="Id3v2Frame"/> does not have pre-defined <see cref="Identifiers"/>.
            /// It is used to test if the <see cref="Factory"/> can create the given <see cref="Id3v2Frame"/> based on the identifier and <see cref="Id3v2Version"/>.
            /// </remarks>
            public Func<Id3v2Version, string, bool> PartialComparer { get; set; }

            ////------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Determines whether the specified version and identifier matches any of the <see cref="Identifiers"/>.
            /// </summary>
            /// <param name="version">The version.</param>
            /// <param name="identifier">The identifier.</param>
            /// <returns>
            /// true if the <paramref name="version"/> and <paramref name="identifier"/> were found; otherwise, false.
            /// </returns>
            /// <remarks>
            /// A match is found when either the <see cref="Identifiers"/> is set and contains the <paramref name="identifier"/> for the given <paramref name="version"/>
            /// (or no <see cref="Id3v2Version"/> is specified for the <paramref name="identifier"/> in <see cref="Identifiers"/>),
            /// or when <see cref="PartialComparer"/> is set and returns <c>true</c>.
            /// </remarks>
            public bool IsMatch(Id3v2Version version, string identifier)
            {
                return (Identifiers != null
                        && Identifiers.Any(
                            i =>
                            String.Equals(i.Key, identifier, StringComparison.OrdinalIgnoreCase) && ((i.Value == null) || i.Value.Contains(version))))
                       || ((PartialComparer != null) && PartialComparer(version, identifier));
            }
        }
    }
}
