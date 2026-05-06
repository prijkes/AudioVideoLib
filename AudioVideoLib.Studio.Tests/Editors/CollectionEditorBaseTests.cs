namespace AudioVideoLib.Studio.Tests.Editors;

using System.Windows;

using AudioVideoLib.Studio.Editors;

using Xunit;

public class CollectionEditorBaseTests
{
    private sealed class Dialog : CollectionEditorBase<object, int>
    {
        public override object CreateNew(object tag) => new();
        public override bool Edit(Window owner, object frame) => false;
        public override void LoadRows(object frame) { }
        public override void SaveRows(object frame) { }
        public override bool Validate(out string? error)
        {
            error = null;
            return true;
        }
    }

    [Fact]
    public void AddRow_Appends()
    {
        var d = new Dialog();
        d.AddRow(1);
        d.AddRow(2);
        Assert.Equal([1, 2], d.Entries);
    }

    [Fact]
    public void RemoveRow_OutOfRange_NoOp()
    {
        var d = new Dialog();
        d.RemoveRow(0);
        d.AddRow(7);
        d.RemoveRow(5);
        Assert.Single(d.Entries);
    }

    [Fact]
    public void MoveUp_FirstRow_NoOp()
    {
        var d = new Dialog();
        d.AddRow(1);
        d.AddRow(2);
        d.MoveUp(0);
        Assert.Equal([1, 2], d.Entries);
    }

    [Fact]
    public void MoveDown_LastRow_NoOp()
    {
        var d = new Dialog();
        d.AddRow(1);
        d.AddRow(2);
        d.MoveDown(1);
        Assert.Equal([1, 2], d.Entries);
    }

    [Fact]
    public void MoveUp_Swaps()
    {
        var d = new Dialog();
        d.AddRow(1);
        d.AddRow(2);
        d.AddRow(3);
        d.MoveUp(2);
        Assert.Equal([1, 3, 2], d.Entries);
    }

    [Fact]
    public void MoveDown_Swaps()
    {
        var d = new Dialog();
        d.AddRow(1);
        d.AddRow(2);
        d.AddRow(3);
        d.MoveDown(0);
        Assert.Equal([2, 1, 3], d.Entries);
    }
}
