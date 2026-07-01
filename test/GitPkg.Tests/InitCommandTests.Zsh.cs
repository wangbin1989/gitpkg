namespace GitPkg.Tests;

/// <summary>
/// init 命令的 zsh 初始化测试。
/// </summary>
public partial class InitCommandTests
{
    /// <summary>zsh：含 GITPKG_HOME 和基于变量的 PATH 初始化脚本。</summary>
    [Fact]
    public async Task Init_Zsh_WritesExportPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("zsh");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for zsh", stdout);
        Assert.Contains("export GITPKG_HOME=", stdout);
        Assert.Contains("export PATH=\"$GITPKG_HOME/bin\":$PATH", stdout);
        Assert.DoesNotContain("[suggest]", stdout);
    }
}
