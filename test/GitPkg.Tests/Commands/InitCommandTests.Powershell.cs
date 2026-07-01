using Shouldly;

namespace GitPkg.Tests.Commands;

/// <summary>
/// init 命令的 PowerShell 初始化测试。
/// </summary>
public partial class InitCommandTests
{
    /// <summary>powershell：含 GITPKG_HOME 和基于变量的 $env:Path 初始化脚本。</summary>
    [Fact]
    public async Task Init_Powershell_WritesEnvPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("powershell");

        exitCode.ShouldBe(0);
        stdout.ShouldStartWith("# gitpkg shell init for powershell");
        stdout.ShouldContain("$env:GITPKG_HOME");
        stdout.ShouldContain("$env:GITPKG_HOME\\bin");
    }

    /// <summary>pwsh 别名输出应与 powershell 一致。</summary>
    [Fact]
    public async Task Init_Pwsh_Alias_WritesEnvPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("pwsh");

        exitCode.ShouldBe(0);
        stdout.ShouldStartWith("# gitpkg shell init for powershell");
        stdout.ShouldContain("$env:GITPKG_HOME");
        stdout.ShouldContain("$env:GITPKG_HOME\\bin");
    }
}
