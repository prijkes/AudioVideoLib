namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;
using Xunit;

public class Id3v2VersionMaskTests
{
    [Theory]
    [InlineData(Id3v2Version.Id3v220, Id3v2VersionMask.V220)]
    [InlineData(Id3v2Version.Id3v221, Id3v2VersionMask.V221)]
    [InlineData(Id3v2Version.Id3v230, Id3v2VersionMask.V230)]
    [InlineData(Id3v2Version.Id3v240, Id3v2VersionMask.V240)]
    public void ToMask_RoundTrip(Id3v2Version version, Id3v2VersionMask expected)
        => Assert.Equal(expected, version.ToMask());

    [Theory]
    [InlineData(Id3v2VersionMask.All, Id3v2Version.Id3v230, true)]
    [InlineData(Id3v2VersionMask.V230 | Id3v2VersionMask.V240, Id3v2Version.Id3v220, false)]
    [InlineData(Id3v2VersionMask.V240, Id3v2Version.Id3v240, true)]
    [InlineData(Id3v2VersionMask.None, Id3v2Version.Id3v230, false)]
    public void Contains_Matches(Id3v2VersionMask mask, Id3v2Version version, bool expected)
        => Assert.Equal(expected, mask.Contains(version));
}
