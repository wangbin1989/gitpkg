using GitPkg.Services;

namespace GitPkg.Tests;

/// <summary>
/// Shell 路径服务单元测试。
/// 验证各 Shell 配置文件路径的正确性和 Shell 检测逻辑。
/// </summary>
public class PathServiceTests
{
    /// <summary>zsh/bash/fish 各自的配置文件路径应正确。</summary>
    [Fact]
    public void GetConfigFilePath_ReturnsCorrectPaths()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        Assert.Equal(Path.Combine(home, ".zshrc"), PathService.GetConfigFilePath("zsh"));
        Assert.Equal(Path.Combine(home, ".bashrc"), PathService.GetConfigFilePath("bash"));
        Assert.Equal(Path.Combine(home, ".config", "fish", "config.fish"),
            PathService.GetConfigFilePath("fish"));
    }

    /// <summary>Unix 系统上应能检测到非空 Shell。</summary>
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
