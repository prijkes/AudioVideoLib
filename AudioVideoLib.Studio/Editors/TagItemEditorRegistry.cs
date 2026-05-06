namespace AudioVideoLib.Studio.Editors;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public sealed class TagItemEditorRegistry
{
    public static TagItemEditorRegistry Shared { get; } = new();

    private readonly Dictionary<Type, RegistrationEntry> _byItemType = [];
    private readonly List<RegistrationEntry> _entries = [];

    public IReadOnlyList<RegistrationEntry> Entries => _entries;

    public void RegisterFromAssembly(Assembly assembly, Func<Type, bool>? editorTypeFilter = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        Type[] candidates;
        try
        {
            candidates = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            candidates = [.. ex.Types.Where(t => t is not null).Cast<Type>()];
        }

        foreach (var type in candidates
                     .Where(t => !t.IsAbstract && !t.IsInterface)
                     .Where(t => editorTypeFilter is null || editorTypeFilter(t)))
        {
            foreach (var attr in type.GetCustomAttributes<TagItemEditorAttribute>(inherit: false))
            {
                var adapter = CreateAdapter(type, attr.ItemType);
                if (_byItemType.ContainsKey(attr.ItemType))
                {
                    var existing = _byItemType[attr.ItemType].EditorType.Name;
                    throw new InvalidOperationException(
                        $"Duplicate editor for item type {attr.ItemType.FullName}: {existing} and {type.Name}.");
                }
                var entry = new RegistrationEntry(type, attr, adapter);
                _byItemType.Add(attr.ItemType, entry);
                _entries.Add(entry);
            }
        }
    }

    public bool TryResolve(Type itemRuntimeType, out ITagItemEditorAdapter editor)
    {
        ArgumentNullException.ThrowIfNull(itemRuntimeType);
        for (var t = itemRuntimeType; t is not null && t != typeof(object); t = t.BaseType)
        {
            if (_byItemType.TryGetValue(t, out var entry))
            {
                editor = entry.Adapter;
                return true;
            }
        }
        editor = null!;
        return false;
    }

    private static ITagItemEditorAdapter CreateAdapter(Type editorType, Type itemType)
    {
        var iface = typeof(ITagItemEditor<>).MakeGenericType(itemType);
        if (!iface.IsAssignableFrom(editorType))
        {
            throw new InvalidOperationException(
                $"Editor {editorType.FullName} must implement ITagItemEditor<{itemType.Name}>.");
        }
        var instance = Activator.CreateInstance(editorType)!;
        var adapterType = typeof(TagItemEditorAdapter<>).MakeGenericType(itemType);
        return (ITagItemEditorAdapter)Activator.CreateInstance(adapterType, instance)!;
    }

    public readonly record struct RegistrationEntry(
        Type EditorType, TagItemEditorAttribute Attribute, ITagItemEditorAdapter Adapter);
}
