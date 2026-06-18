using GitPkg.Models;

namespace GitPkg.Tests;

public class PlatformInfoTests
{
    [Fact]
    public void Current_ReturnsValidPlatform()
    {
        var platform = PlatformInfo.Current();

        Assert.NotNull(platform.OS);
        Assert.NotNull(platform.Arch);
        Assert.NotEmpty(platform.OS);
        Assert.NotEmpty(platform.Arch);
    }
}
