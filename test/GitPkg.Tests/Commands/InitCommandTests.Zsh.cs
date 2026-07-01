using Shouldly;

namespace GitPkg.Tests.Commands;

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

        exitCode.ShouldBe(0);
        stdout.ShouldStartWith("# gitpkg shell init for zsh");
        stdout.ShouldContain("export GITPKG_HOME=");
        stdout.ShouldContain("export PATH=\"$GITPKG_HOME/bin\":$PATH");
        stdout.ShouldNotContain("[suggest]");
    }
}
