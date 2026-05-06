namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using System.IO;
using System.Reflection;
using System.Text;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Studio.Editors.Id3v2;
using AudioVideoLib.Tags;

using Xunit;

/// <summary>
/// Baseline snapshot tests for the Add-Frame menu structure per ID3v2 version.
///
/// <para>
/// Intent: a baseline snapshot for future-PR regression detection — a generated golden
/// cannot validate current correctness (the generator and the test agree by construction).
/// Current correctness is covered by the per-version BuildModel expected-set tests in
/// <see cref="Id3v2AddMenuBuilderTests"/>. The goldens catch <em>future drift</em> in
/// menu structure, ordering, or category membership.
/// </para>
/// </summary>
[Collection("Studio")]
public class Id3v2MenuGoldenTests
{
    [Theory]
    [InlineData(Id3v2Version.Id3v220, "menu-v220.txt")]
    [InlineData(Id3v2Version.Id3v221, "menu-v221.txt")]
    [InlineData(Id3v2Version.Id3v230, "menu-v230.txt")]
    [InlineData(Id3v2Version.Id3v240, "menu-v240.txt")]
    public void MenuBuilder_MatchesGoldenSnapshot(Id3v2Version version, string goldenName)
    {
        var tag = new Id3v2Tag(version);
        var model = Id3v2AddMenuBuilder.BuildModel(TagItemEditorRegistry.Shared, tag);
        var actual = SerializeMenu(model);

        var goldenPath = Path.Combine(GoldenDir, goldenName);
        if (!File.Exists(goldenPath))
        {
            Directory.CreateDirectory(GoldenDir);
            File.WriteAllText(goldenPath, actual);
            Assert.Fail($"Golden file generated at {goldenPath}; review and re-run to lock baseline.");
        }

        var expected = File.ReadAllText(goldenPath).ReplaceLineEndings();
        Assert.Equal(expected, actual.ReplaceLineEndings());
    }

    private static string SerializeMenu(Id3v2MenuModel model)
    {
        var sb = new StringBuilder();
        foreach (var cat in model.Categories)
        {
            sb.Append(cat.Category).Append(" | ").AppendLine(cat.Header);
            foreach (var entry in cat.Entries)
            {
                sb.Append("  ").Append(entry.FrameIdentifier).Append(" | ").AppendLine(entry.Label);
            }
        }
        return sb.ToString();
    }

    private static string GoldenDir
        => Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "Goldens");
}
