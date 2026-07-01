using Shouldly;

namespace GitPkg.Tests.Commands;

/// <summary>
/// init 命令的 bash 初始化测试。
/// </summary>
public partial class InitCommandTests
{
    /// <summary>bash：含 GITPKG_HOME 和基于变量的 PATH 初始化脚本。</summary>
    [Fact]
    public async Task Init_Bash_WritesExportPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("bash");

        exitCode.ShouldBe(0);
        stdout.ShouldStartWith("# gitpkg shell init for bash");
        stdout.ShouldContain("export GITPKG_HOME=");
        stdout.ShouldContain("export PATH=\"$GITPKG_HOME/bin\":$PATH");
    }
}
