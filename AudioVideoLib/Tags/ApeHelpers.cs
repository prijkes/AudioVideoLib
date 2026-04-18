namespace AudioVideoLib.Tags;

using System;

/// <summary>
/// Class to store an APE tag.
/// </summary>
public partial class ApeTag
{
    /// <summary>
    /// Determines whether the value is a valid ISBN-10 number.
    /// </summary>
    /// <param name="isbn10">The ISBN-10.</param>
    /// <returns>
    ///   <c>true</c> if the value is valid a ISBN-10 number; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsValidIsbn10(string isbn10)
    {
        ArgumentNullException.ThrowIfNull(isbn10);

        // 3-341-00488-X
        if (isbn10.Length == 13)
        {
            isbn10 = isbn10.Replace("-", string.Empty);
        }

        // 3341004807
        if (isbn10.Length != 10)
        {
            return false;
        }

        int checksum = isbn10[^1];
        int i, position, sum = 0;
        for (i = 0, position = 1; i < isbn10.Length - 1; i++, position++)
        {
            sum += isbn10[i] * position;
        }

        return (sum % 11) == checksum;
    }
}
