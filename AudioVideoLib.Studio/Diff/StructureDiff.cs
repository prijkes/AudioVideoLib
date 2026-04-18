namespace AudioVideoLib.Studio.Diff;

using System;
using System.Collections.Generic;
using System.Linq;

public sealed record StructureDiffRow(int Depth, string Path, DiffKind Kind, string? LeftLabel, long LeftSize, string? RightLabel, long RightSize);

public static class StructureDiff
{
    public static IReadOnlyList<StructureDiffRow> Compare(InspectorNode? left, InspectorNode? right)
    {
        var rows = new List<StructureDiffRow>();
        Walk(left, right, depth: 0, path: string.Empty, rows);
        return rows;
    }

    private static void Walk(InspectorNode? left, InspectorNode? right, int depth, string path, List<StructureDiffRow> rows)
    {
        if (left == null && right == null)
        {
            return;
        }

        if (left == null)
        {
            rows.Add(new(depth, path + right!.Label, DiffKind.Added, null, 0, right.Label, right.Size));
            AddTree(right, depth + 1, path + right.Label + "/", DiffKind.Added, isLeft: false, rows);
            return;
        }

        if (right == null)
        {
            rows.Add(new(depth, path + left.Label, DiffKind.Removed, left.Label, left.Size, null, 0));
            AddTree(left, depth + 1, path + left.Label + "/", DiffKind.Removed, isLeft: true, rows);
            return;
        }

        var kind = left.Size == right.Size && string.Equals(left.Label, right.Label, StringComparison.Ordinal)
            ? DiffKind.Same
            : DiffKind.Changed;

        rows.Add(new(depth, path + left.Label, kind, left.Label, left.Size, right.Label, right.Size));

        // Pair children by position — structural trees tend to be stable within a section.
        var leftKids = left.Children.ToList();
        var rightKids = right.Children.ToList();
        var max = Math.Max(leftKids.Count, rightKids.Count);
        for (var i = 0; i < max; i++)
        {
            var lc = i < leftKids.Count ? leftKids[i] : null;
            var rc = i < rightKids.Count ? rightKids[i] : null;
            Walk(lc, rc, depth + 1, path + left.Label + "/", rows);
        }
    }

    private static void AddTree(InspectorNode node, int depth, string path, DiffKind kind, bool isLeft, List<StructureDiffRow> rows)
    {
        foreach (var child in node.Children)
        {
            rows.Add(new(
                depth,
                path + child.Label,
                kind,
                isLeft ? child.Label : null,
                isLeft ? child.Size : 0,
                isLeft ? null : child.Label,
                isLeft ? 0 : child.Size));
            AddTree(child, depth + 1, path + child.Label + "/", kind, isLeft, rows);
        }
    }
}
