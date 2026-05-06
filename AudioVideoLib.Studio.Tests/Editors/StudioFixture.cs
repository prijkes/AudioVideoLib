namespace AudioVideoLib.Studio.Tests.Editors;

using AudioVideoLib.Studio.Editors;

using Xunit;

public sealed class StudioFixture
{
    private static readonly object InitLock = new();
    private static bool initialised;

    public StudioFixture()
    {
        // Populate Shared once. The lock prevents a TOCTOU race if two collections
        // ever instantiate this fixture concurrently (xUnit collections run in parallel).
        lock (InitLock)
        {
            if (initialised)
            {
                return;
            }
            TagItemEditorRegistry.Shared.RegisterFromAssembly(typeof(MainWindow).Assembly);
            initialised = true;
        }
    }
}

[CollectionDefinition("Studio")]
public class StudioCollection : ICollectionFixture<StudioFixture>
{
}
