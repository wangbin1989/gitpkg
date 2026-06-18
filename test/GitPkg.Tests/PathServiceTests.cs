using GitPkg.Services;

namespace GitPkg.Tests;

public class PathServiceTests
{
    [Fact]
    public void GetConfigFilePath_ReturnsCorrectPaths()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        Assert.Equal(Path.Combine(home, ".zshrc"), PathService.GetConfigFilePath("zsh"));
        Assert.Equal(Path.Combine(home, ".bashrc"), PathService.GetConfigFilePath("bash"));
        Assert.Equal(Path.Combine(home, ".config", "fish", "config.fish"),
            PathService.GetConfigFilePath("fish"));
    }

    [Fact]
    public void DetectShell_ReturnsNonNullOnUnix()
    {
        var shell = PathService.DetectShell();
        if (!OperatingSystem.IsWindows())
        {
            Assert.NotNull(shell);
        }
    }
}
