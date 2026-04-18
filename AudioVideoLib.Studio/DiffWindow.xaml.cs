namespace AudioVideoLib.Studio;

using System.Collections.Generic;
using System.Linq;
using System.Windows;

using AudioVideoLib.Studio.Diff;
using AudioVideoLib.Tags;

public partial class DiffWindow : Window
{
    public DiffWindow(FileDossier left, FileDossier right)
    {
        InitializeComponent();

        HeaderText.Text =
            $"Left:  {left.FilePath}  ({left.FileSize:N0} bytes)\n" +
            $"Right: {right.FilePath}  ({right.FileSize:N0} bytes)";

        var leftTags = ExtractTags(left);
        var rightTags = ExtractTags(right);
        var leftVorbis = ExtractVorbis(left);
        var rightVorbis = ExtractVorbis(right);

        var tagRows = TagDiff.Compare(leftTags, leftVorbis, rightTags, rightVorbis);
        var structRows = StructureDiff.Compare(left.InspectorRoot, right.InspectorRoot);

        TagGrid.ItemsSource = tagRows;
        StructureGrid.ItemsSource = structRows;

        var tagAdds = tagRows.Count(r => r.Kind == DiffKind.Added);
        var tagRems = tagRows.Count(r => r.Kind == DiffKind.Removed);
        var tagChg = tagRows.Count(r => r.Kind == DiffKind.Changed);
        var structChg = structRows.Count(r => r.Kind != DiffKind.Same);
        SummaryText.Text = $"Tags: {tagAdds} added, {tagRems} removed, {tagChg} changed.   Structure: {structChg} differing nodes.";
    }

    private static IReadOnlyList<IAudioTag> ExtractTags(FileDossier dossier)
    {
        var list = new List<IAudioTag>();
        foreach (var tab in dossier.TagTabs)
        {
            IAudioTag? tag = tab switch
            {
                Id3v2TabViewModel v2 => v2.Tag,
                Id3v1TabViewModel v1 => v1.Tag,
                ApeTabViewModel ape => ape.Tag,
                Lyrics3v1TabViewModel l1 => l1.Tag,
                Lyrics3v2TabViewModel l2 => l2.Tag,
                MusicMatchTabViewModel mm => mm.Tag,
                _ => null,
            };

            if (tag != null)
            {
                list.Add(tag);
            }
        }

        return list;
    }

    private static VorbisComments? ExtractVorbis(FileDossier dossier)
    {
        return dossier.TagTabs.OfType<VorbisTabViewModel>().FirstOrDefault()?.Comments;
    }
}
