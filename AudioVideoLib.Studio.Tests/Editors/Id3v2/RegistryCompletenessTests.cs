namespace AudioVideoLib.Studio.Tests.Editors.Id3v2;

using System.Linq;

using AudioVideoLib.Studio.Editors;
using AudioVideoLib.Tags;

using Xunit;
using Xunit.Abstractions;

[Collection("Studio")]
public class RegistryCompletenessTests(ITestOutputHelper output)
{
    /// <summary>Flipped to true in Phase 2.1 (after all wave editors are registered).</summary>
    public static bool RegistrationComplete => false;

    [Fact]
    public void EveryConcreteId3v2Frame_HasRegisteredEditor()
    {
        var libAsm = typeof(Id3v2Frame).Assembly;
        var allFrames = libAsm.GetTypes()
            .Where(t => !t.IsAbstract && typeof(Id3v2Frame).IsAssignableFrom(t))
            .ToArray();

        var missing = allFrames
            .Where(t => !TagItemEditorRegistry.Shared.TryResolve(t, out _))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToArray();

        if (missing.Length > 0)
        {
            output.WriteLine($"Missing editors for ({missing.Length}):");
            foreach (var name in missing)
            {
                output.WriteLine($"  - {name}");
            }
        }

        if (RegistrationComplete)
        {
            Assert.Empty(missing);
        }
    }

    [Fact]
    public void EveryEditor_HasMatchingTestClass()
    {
        var testAsm = typeof(RegistryCompletenessTests).Assembly;
        var testClassNames = testAsm.GetTypes()
            .Where(t => !t.IsAbstract)
            .Select(t => t.Name)
            .ToHashSet();

        var missingTests = TagItemEditorRegistry.Shared.Entries
            .Select(e => e.EditorType.Name + "Tests")
            .Where(n => !testClassNames.Contains(n))
            .OrderBy(n => n)
            .ToArray();

        if (missingTests.Length > 0)
        {
            output.WriteLine($"Editors without matching XxxTests class ({missingTests.Length}):");
            foreach (var name in missingTests)
            {
                output.WriteLine($"  - {name}");
            }
        }

        if (RegistrationComplete)
        {
            Assert.Empty(missingTests);
        }
    }
}
