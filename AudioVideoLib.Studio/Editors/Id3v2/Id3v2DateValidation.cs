namespace AudioVideoLib.Studio.Editors.Id3v2;

internal static class Id3v2DateValidation
{
    /// <summary>
    /// Returns true when <paramref name="value"/> is exactly 8 ASCII digits
    /// (YYYYMMDD). Doesn't validate calendar correctness — just the shape.
    /// </summary>
    public static bool IsYyyyMmDd(string? value)
    {
        if (value is null || value.Length != 8)
        {
            return false;
        }
        foreach (var c in value)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }
        }
        return true;
    }
}
