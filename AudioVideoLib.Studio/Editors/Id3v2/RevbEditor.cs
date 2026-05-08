namespace AudioVideoLib.Studio.Editors.Id3v2;

using System.Windows;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Mvvm;
using AudioVideoLib.Tags;

[Id3v2FrameEditor(typeof(Id3v2ReverbFrame),
    Category = Id3v2FrameCategory.AudioAdjustment,
    MenuLabel = "Reverb (REVB)",
    Order = 24,
    SupportedVersions = Id3v2VersionMask.All,
    IsUniqueInstance = true)]
public sealed class RevbEditor : ObservableObject, ITagItemEditor<Id3v2ReverbFrame>, IValidatedEditor
{
    public int ReverbLeftMilliseconds { get => field; set => Set(ref field, value); }
    public int ReverbRightMilliseconds { get => field; set => Set(ref field, value); }
    public int ReverbBouncesLeft { get => field; set => Set(ref field, value); }
    public int ReverbBouncesRight { get => field; set => Set(ref field, value); }
    public int ReverbFeedbackLeftToLeft { get => field; set => Set(ref field, value); }
    public int ReverbFeedbackLeftToRight { get => field; set => Set(ref field, value); }
    public int ReverbFeedbackRightToRight { get => field; set => Set(ref field, value); }
    public int ReverbFeedbackRightToLeft { get => field; set => Set(ref field, value); }
    public int PremixLeftToRight { get => field; set => Set(ref field, value); }
    public int PremixRightToLeft { get => field; set => Set(ref field, value); }

    public Id3v2ReverbFrame CreateNew(object tag) => new(((Id3v2Tag)tag).Version);

    public bool Edit(Window owner, Id3v2ReverbFrame frame)
        => EditorDialog.Run<RevbEditorDialog, Id3v2ReverbFrame>(
            owner, frame, this, Load, Save);

    public void Load(Id3v2ReverbFrame f)
    {
        ReverbLeftMilliseconds = f.ReverbLeftMilliseconds;
        ReverbRightMilliseconds = f.ReverbRightMilliseconds;
        ReverbBouncesLeft = f.ReverbBouncesLeft;
        ReverbBouncesRight = f.ReverbBouncesRight;
        ReverbFeedbackLeftToLeft = f.ReverbFeedbackLeftToLeft;
        ReverbFeedbackLeftToRight = f.ReverbFeedbackLeftToRight;
        ReverbFeedbackRightToRight = f.ReverbFeedbackRightToRight;
        ReverbFeedbackRightToLeft = f.ReverbFeedbackRightToLeft;
        PremixLeftToRight = f.PremixLeftToRight;
        PremixRightToLeft = f.PremixRightToLeft;
    }

    public void Save(Id3v2ReverbFrame f)
    {
        f.ReverbLeftMilliseconds = (short)ReverbLeftMilliseconds;
        f.ReverbRightMilliseconds = (short)ReverbRightMilliseconds;
        f.ReverbBouncesLeft = (byte)ReverbBouncesLeft;
        f.ReverbBouncesRight = (byte)ReverbBouncesRight;
        f.ReverbFeedbackLeftToLeft = (byte)ReverbFeedbackLeftToLeft;
        f.ReverbFeedbackLeftToRight = (byte)ReverbFeedbackLeftToRight;
        f.ReverbFeedbackRightToRight = (byte)ReverbFeedbackRightToRight;
        f.ReverbFeedbackRightToLeft = (byte)ReverbFeedbackRightToLeft;
        f.PremixLeftToRight = (byte)PremixLeftToRight;
        f.PremixRightToLeft = (byte)PremixRightToLeft;
    }

    public bool Validate(out string? error)
    {
        if (ReverbLeftMilliseconds < 0 || ReverbRightMilliseconds < 0)
        {
            error = "Reverb time must be non-negative.";
            return false;
        }
        if (!IsByte(ReverbBouncesLeft) || !IsByte(ReverbBouncesRight))
        {
            error = "Bounces must be between 0 and 255.";
            return false;
        }
        if (!IsByte(ReverbFeedbackLeftToLeft) || !IsByte(ReverbFeedbackLeftToRight)
            || !IsByte(ReverbFeedbackRightToRight) || !IsByte(ReverbFeedbackRightToLeft))
        {
            error = "Feedback values must be between 0 and 255.";
            return false;
        }
        if (!IsByte(PremixLeftToRight) || !IsByte(PremixRightToLeft))
        {
            error = "Premix values must be between 0 and 255.";
            return false;
        }
        error = null;
        return true;
    }

    private static bool IsByte(int v) => v is >= 0 and <= 255;

}
