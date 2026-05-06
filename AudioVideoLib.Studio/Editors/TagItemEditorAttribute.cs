namespace AudioVideoLib.Studio.Editors;

using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public abstract class TagItemEditorAttribute : Attribute
{
    public Type ItemType { get; }
    public string MenuLabel { get; init; } = string.Empty;
    public int Order { get; init; }

    protected TagItemEditorAttribute(Type itemType)
    {
        ArgumentNullException.ThrowIfNull(itemType);
        ItemType = itemType;
    }
}
