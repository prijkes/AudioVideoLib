namespace AudioVideoLib.Studio.Tests.Smoke;

using Xunit;

public class SanityTests
{
    [Fact]
    public void StudioAssembly_IsReferenceable()
        => Assert.Equal("AudioVideoLib.Studio", typeof(MainWindow).Assembly.GetName().Name);
}
