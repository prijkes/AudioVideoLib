namespace AudioVideoLib.Studio.Tests;

using System.Linq;

using AudioVideoLib.Studio;
using AudioVideoLib.Tags;

using Xunit;

[Collection("Studio")]
public class ApeTabViewModelTests
{
    [Fact]
    public void AddTextItem_NewKey_AppendsRowAndItem()
    {
        var tag = new ApeTag(ApeVersion.Version2);
        var vm = new ApeTabViewModel(tag);

        vm.AddTextItem("Title", "Hello");

        Assert.Single(vm.Items);
        Assert.Equal("Title", vm.Items[0].Key);
        Assert.Equal("UTF-8", vm.Items[0].Type);
        var stored = Assert.IsType<ApeUtf8Item>(tag.GetItem("Title"));
        Assert.Equal("Hello", stored.Values.Single());
    }

    [Fact]
    public void AddBinaryItem_StoresBytesAsBinaryItem()
    {
        var tag = new ApeTag(ApeVersion.Version2);
        var vm = new ApeTabViewModel(tag);
        var data = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        vm.AddBinaryItem("Cover Art (Front)", data);

        Assert.Equal("Binary", vm.Items.Single().Type);
        var stored = Assert.IsType<ApeBinaryItem>(tag.GetItem("Cover Art (Front)"));
        Assert.Equal(data, stored.Data);
    }

    [Fact]
    public void AddLocatorItem_StoresUriAsLocatorItem()
    {
        var tag = new ApeTag(ApeVersion.Version2);
        var vm = new ApeTabViewModel(tag);

        vm.AddLocatorItem("Related", "https://example.com/track");

        Assert.Equal("Locator", vm.Items.Single().Type);
        var stored = Assert.IsType<ApeLocatorItem>(tag.GetItem("Related"));
        Assert.Equal("https://example.com/track", stored.Values.Single());
    }

    [Fact]
    public void AddItem_DuplicateKey_ReplacesRowInsteadOfAppending()
    {
        // Regression: ApeTag.SetItem replaces an existing same-key item (case-insensitive).
        // The row collection must follow the same semantics, otherwise duplicate rows pile
        // up while the underlying tag holds only one item — the two go out of sync.
        var tag = new ApeTag(ApeVersion.Version2);
        var vm = new ApeTabViewModel(tag);

        vm.AddTextItem("Title", "First");
        vm.AddTextItem("title", "Second");

        Assert.Single(vm.Items);
        Assert.Equal("Second", vm.Items[0].Value);
        var stored = Assert.IsType<ApeUtf8Item>(tag.GetItem("Title"));
        Assert.Equal("Second", stored.Values.Single());
    }

    [Fact]
    public void AddItem_DuplicateKey_DifferentType_ReplacesRow()
    {
        var tag = new ApeTag(ApeVersion.Version2);
        var vm = new ApeTabViewModel(tag);

        vm.AddTextItem("Cover", "placeholder");
        vm.AddBinaryItem("Cover", [1, 2, 3]);

        Assert.Single(vm.Items);
        Assert.Equal("Binary", vm.Items[0].Type);
        Assert.IsType<ApeBinaryItem>(tag.GetItem("Cover"));
    }

    [Fact]
    public void RefreshValueDisplay_AfterExternalMutation_ReflectsAllValues()
    {
        // The edit modal mutates ApeUtf8Item.Values directly. The row must pick
        // up the new joined display ("a / b / c") without going through the
        // inline-edit setter (which would collapse it back to a single value).
        var tag = new ApeTag(ApeVersion.Version2);
        var vm = new ApeTabViewModel(tag);
        var row = vm.AddTextItem("Artist", "solo");

        var item = (ApeUtf8Item)row.Item;
        item.Values.Clear();
        item.Values.Add("First");
        item.Values.Add("Second");
        item.Values.Add("Third");

        row.RefreshValueDisplay();

        Assert.Equal("First / Second / Third", row.Value);
        Assert.Equal(["First", "Second", "Third"], item.Values);
    }

    [Fact]
    public void Item_ExposesUnderlyingApeItem()
    {
        // The right-click and double-click handlers reach through ApeItemRow.Item
        // to dispatch on the runtime type. Pin that the property is exposed.
        var tag = new ApeTag(ApeVersion.Version2);
        var vm = new ApeTabViewModel(tag);

        var textRow = vm.AddTextItem("Title", "v");
        var locatorRow = vm.AddLocatorItem("Related", "https://example.com/");
        var binaryRow = vm.AddBinaryItem("Cover Art (Front)", [1]);

        Assert.IsType<ApeUtf8Item>(textRow.Item);
        Assert.IsType<ApeLocatorItem>(locatorRow.Item);
        Assert.IsType<ApeBinaryItem>(binaryRow.Item);
    }
}
