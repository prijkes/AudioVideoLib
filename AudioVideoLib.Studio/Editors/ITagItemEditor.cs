namespace AudioVideoLib.Studio.Editors;

using System;
using System.Windows;

public interface ITagItemEditor<TItem>
{
    TItem CreateNew(object tag);
    bool Edit(Window owner, TItem item);
}

public interface ITagItemEditorAdapter
{
    Type ItemType { get; }
    object Inner { get; }
    object CreateNew(object tag);
    bool Edit(Window owner, object item);
}

internal sealed class TagItemEditorAdapter<TItem>(ITagItemEditor<TItem> inner) : ITagItemEditorAdapter
{
    private readonly ITagItemEditor<TItem> _inner = inner;

    public Type ItemType => typeof(TItem);
    public object Inner => _inner!;
    public object CreateNew(object tag) => _inner.CreateNew(tag)!;
    public bool Edit(Window owner, object item) => _inner.Edit(owner, (TItem)item);
}
