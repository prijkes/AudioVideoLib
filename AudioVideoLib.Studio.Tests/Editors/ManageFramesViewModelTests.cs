namespace AudioVideoLib.Studio.Tests.Editors;

using System.Linq;
using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;
using Xunit;

[Collection("Studio")]
public class ManageFramesViewModelTests
{
    [Fact]
    public void All_PopulatesFromRegistry_FilteredByVersion()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var vm = new ManageFramesViewModel(TagItemEditorRegistry.Shared, tag);
        Assert.Contains(vm.All, e => e.Identifier == "APIC");
        Assert.DoesNotContain(vm.All, e => e.Identifier == "CDM"); // v2.2-only
    }

    [Theory]
    [InlineData("apic")]
    [InlineData("APIC")]
    [InlineData("attached")]
    [InlineData("Attachments")]
    public void ApplyFilter_MatchesIdNameCategory_CaseInsensitive(string query)
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var vm = new ManageFramesViewModel(TagItemEditorRegistry.Shared, tag);
        Assert.Contains(vm.ApplyFilter(query), e => e.Identifier == "APIC");
    }

    [Fact]
    public void GetActionLabel_UniqueAlreadyInTag_ReturnsEdit()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var trck = new Id3v2TextFrame(Id3v2Version.Id3v240, "TRCK");
        trck.Values.Add("1");
        tag.SetFrame(trck);
        tag.SetFrame(new Id3v2MusicCdIdentifierFrame(Id3v2Version.Id3v240) { TableOfContents = new byte[8] });
        var vm = new ManageFramesViewModel(TagItemEditorRegistry.Shared, tag);
        var mcdi = vm.All.Single(e => e.Identifier == "MCDI");
        Assert.Equal("Edit", vm.GetActionLabel(mcdi));
    }

    [Fact]
    public void GetActionLabel_NotInTag_ReturnsAdd()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var vm = new ManageFramesViewModel(TagItemEditorRegistry.Shared, tag);
        var apic = vm.All.Single(e => e.Identifier == "APIC");
        Assert.Equal("Add", vm.GetActionLabel(apic));
    }

    [Fact]
    public void ExistsInTag_ReflectsCurrentTagState()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var trck = new Id3v2TextFrame(Id3v2Version.Id3v240, "TRCK");
        trck.Values.Add("1");
        tag.SetFrame(trck);
        tag.SetFrame(new Id3v2MusicCdIdentifierFrame(Id3v2Version.Id3v240) { TableOfContents = new byte[8] });
        var vm = new ManageFramesViewModel(TagItemEditorRegistry.Shared, tag);
        Assert.True(vm.All.Single(r => r.Identifier == "MCDI").ExistsInTag);
        Assert.False(vm.All.Single(r => r.Identifier == "APIC").ExistsInTag);
    }

    [Fact]
    public void All_IncludesTextFrameFamilyEntries()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var vm = new ManageFramesViewModel(TagItemEditorRegistry.Shared, tag);
        Assert.Contains(vm.All, r => r.Identifier == "TIT2" && r.Name.Contains("Title"));
        Assert.Contains(vm.All, r => r.Identifier == "TPE1");
        Assert.Contains(vm.All, r => r.Identifier == "TALB");
    }

    [Fact]
    public void All_IncludesUrlFrameFamilyEntries()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var vm = new ManageFramesViewModel(TagItemEditorRegistry.Shared, tag);
        Assert.Contains(vm.All, r => r.Identifier == "WCOM");
        Assert.Contains(vm.All, r => r.Identifier == "WOAR");
    }

    [Fact]
    public void All_V220_TextFrameFamilyUsesV220Identifier()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v220);
        var vm = new ManageFramesViewModel(TagItemEditorRegistry.Shared, tag);
        Assert.Contains(vm.All, r => r.Identifier == "TT2");
        Assert.DoesNotContain(vm.All, r => r.Identifier == "TIT2");
    }

    [Fact]
    public void ExistsInTag_TextFrame_ReflectsActualPresence()
    {
        var tag = new Id3v2Tag(Id3v2Version.Id3v240);
        var tit2 = new Id3v2TextFrame(Id3v2Version.Id3v240, "TIT2");
        tit2.Values.Add("Test");
        tag.SetFrame(tit2);
        var vm = new ManageFramesViewModel(TagItemEditorRegistry.Shared, tag);
        Assert.True(vm.All.Single(r => r.Identifier == "TIT2").ExistsInTag);
        Assert.False(vm.All.Single(r => r.Identifier == "TPE1").ExistsInTag);
    }
}
