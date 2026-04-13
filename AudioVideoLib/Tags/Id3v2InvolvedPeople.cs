namespace AudioVideoLib.Tags;

using System;

/// <summary>
/// Class for storing those involved and how they were involved.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Id3v2InvolvedPeople"/> class.
/// </remarks>
/// <param name="involvement">The involvement of the involvee.</param>
/// <param name="involvee">The involvee.</param>
public class Id3v2InvolvedPeople(string involvement, string involvee)
{

    /// <summary>
    /// Gets the involvement.
    /// </summary>
    /// <value>
    /// The involvement of the involvee.
    /// </value>
    public string Involvement { get; private set; } = involvement ?? throw new ArgumentNullException("involvement");

    /// <summary>
    /// Gets the involvee.
    /// </summary>
    /// <value>
    /// The involvee.
    /// </value>
    public string Involvee { get; private set; } = involvee ?? throw new ArgumentNullException("involvee");
}
