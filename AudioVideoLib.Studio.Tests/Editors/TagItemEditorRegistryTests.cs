namespace AudioVideoLib.Studio.Tests.Editors;

using System;
using AudioVideoLib.Studio.Editors;
using Xunit;

[Collection("Studio")]
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

    [Fact]
    public void TryFindEntry_ExactType_ReturnsTrue()
    {
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(SampleEditor).Assembly, t => t == typeof(SampleEditor));
        Assert.True(r.TryFindEntry(typeof(SampleItem), out var entry));
        Assert.Equal(typeof(SampleEditor), entry.EditorType);
    }

    [Fact]
    public void TryFindEntry_DerivedType_WalksHierarchy()
    {
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(SampleEditor).Assembly, t => t == typeof(SampleEditor));
        Assert.True(r.TryFindEntry(typeof(SampleItemDerived), out var entry));
        Assert.Equal(typeof(SampleItem), entry.Adapter.ItemType);
    }

    [Fact]
    public void TryFindEntry_RegisteredAtBase_ReturnsBaseEntry()
    {
        // Pins "first match wins on the way up": a derived-type query should yield
        // the base registration's editor, not just any entry.
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(SampleEditor).Assembly, t => t == typeof(SampleEditor));
        Assert.True(r.TryFindEntry(typeof(SampleItemDerived), out var entry));
        Assert.Equal(typeof(SampleEditor), entry.EditorType);
    }

    [Fact]
    public void TryFindEntry_Unknown_ReturnsFalse()
    {
        var r = new TagItemEditorRegistry();
        Assert.False(r.TryFindEntry(typeof(string), out var entry));
        Assert.Equal(default, entry);
    }

    [Fact]
    public void TryFindEntry_ItemTypeIsObject_ReturnsFalse()
    {
        // Guards the t != typeof(object) loop terminator: even if some pathological
        // registration referenced object, the walk should not return it for an
        // object-typed query.
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(SampleEditor).Assembly, t => t == typeof(SampleEditor));
        Assert.False(r.TryFindEntry(typeof(object), out _));
    }

    [Fact]
    public void TryFindEntry_InterfaceType_ReturnsFalse()
    {
        // The walk follows BaseType, not implemented interfaces.
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(SampleEditor).Assembly, t => t == typeof(SampleEditor));
        Assert.False(r.TryFindEntry(typeof(IDisposable), out _));
    }

    [Fact]
    public void TryFindEntry_Null_Throws()
    {
        var r = new TagItemEditorRegistry();
        Assert.Throws<ArgumentNullException>(() => r.TryFindEntry(null!, out _));
    }

    [Fact]
    public void TryResolve_StillForwardsThroughTryFindEntry()
    {
        // Sanity check that the existing API contract holds after refactoring.
        var r = new TagItemEditorRegistry();
        r.RegisterFromAssembly(typeof(SampleEditor).Assembly, t => t == typeof(SampleEditor));
        Assert.True(r.TryResolve(typeof(SampleItemDerived), out var adapter));
        Assert.Equal(typeof(SampleItem), adapter.ItemType);
    }
}
