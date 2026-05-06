namespace AudioVideoLib.Studio.Tests.Editors;

using System;
using AudioVideoLib.Studio.Editors;
using Xunit;

public class TagItemEditorRegistryTests
{
    public class SampleItem { }
    public sealed class OtherItem { }

    [SampleItemEditor(typeof(SampleItem), MenuLabel = "Sample")]
    public sealed class SampleEditor : ITagItemEditor<SampleItem>
    {
        public SampleItem CreateNew(object tag) => new();
        public bool Edit(System.Windows.Window owner, SampleItem item) => true;
    }

    public sealed class SampleItemEditorAttribute(Type itemType) : TagItemEditorAttribute(itemType)
    {
    }

    [Fact]
    public void RegisterFromAssembly_ScopedFilter_FindsExpectedEditor()
    {
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(SampleEditor).Assembly, t => t == typeof(SampleEditor));
        Assert.True(r.TryResolve(typeof(SampleItem), out var editor));
        Assert.Equal(typeof(SampleItem), editor.ItemType);
    }

    [Fact]
    public void TryResolve_UnknownType_ReturnsFalse()
    {
        var r = new TagItemEditorRegistry();
        Assert.False(r.TryResolve(typeof(string), out _));
    }

    [Fact]
    public void TryResolve_DerivedType_FallsBackToBase()
    {
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(SampleEditor).Assembly, t => t == typeof(SampleEditor));

        // Derive a runtime type from SampleItem
        var derivedItem = new SampleItemDerived();
        Assert.True(r.TryResolve(derivedItem.GetType(), out var editor));
        Assert.Equal(typeof(SampleItem), editor.ItemType);
    }

    public sealed class SampleItemDerived : SampleItem { }
}
