namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using System.Collections.Generic;

using AudioVideoLib.Studio.Editors.Id3v2;

using Xunit;

/// <summary>
/// D14 regression tests: PropertyChanged plumbing inherited from EditorBase
/// must still fire as expected, and the Data → DataInfo cascade through
/// BinaryDataEditorBase must continue to work.
/// </summary>
public class EditorBaseTests
{
    // PropertyChanged fires for the property name when value changes.
    [Fact]
    public void Set_FiresPropertyChanged_OnChange()
    {
        var e = new PcntEditor();
        var fired = new List<string?>();
        e.PropertyChanged += (_, args) => fired.Add(args.PropertyName);

        e.Counter = 42;
        Assert.Contains(nameof(PcntEditor.Counter), fired);
    }

    // PropertyChanged does NOT fire when value is unchanged.
    [Fact]
    public void Set_SuppressesPropertyChanged_OnEqualValue()
    {
        var e = new PcntEditor { Counter = 5 };
        var fired = new List<string?>();
        e.PropertyChanged += (_, args) => fired.Add(args.PropertyName);

        e.Counter = 5;
        Assert.DoesNotContain(nameof(PcntEditor.Counter), fired);
    }

    // BinaryDataEditorBase: setting Data fires PropertyChanged for both Data and DataInfo.
    [Fact]
    public void BinaryDataEditor_DataToDataInfoCascade()
    {
        var e = new AencEditor();
        var fired = new List<string?>();
        e.PropertyChanged += (_, args) => fired.Add(args.PropertyName);

        e.Data = [0x01, 0x02, 0x03];

        Assert.Contains(nameof(AencEditor.Data), fired);
        Assert.Contains(nameof(AencEditor.DataInfo), fired);
    }

    // EncrEditor preserves its "No data." wording (overrides DataInfo on the base).
    [Fact]
    public void EncrEditor_DataInfoEmpty_PreservesNoDataWording()
    {
        var e = new EncrEditor();
        Assert.Equal("No data.", e.DataInfo);
    }

    // GridEditor preserves its "No data." wording.
    [Fact]
    public void GridEditor_DataInfoEmpty_PreservesNoDataWording()
    {
        var e = new GridEditor();
        Assert.Equal("No data.", e.DataInfo);
    }

    // AencEditor uses the default "(no data)" wording from BinaryDataEditorBase.
    [Fact]
    public void AencEditor_DataInfoEmpty_UsesDefaultWording()
    {
        var e = new AencEditor();
        Assert.Equal("(no data)", e.DataInfo);
    }
}
