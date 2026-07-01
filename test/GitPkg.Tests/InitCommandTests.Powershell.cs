namespace GitPkg.Tests;

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

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for powershell", stdout);
        Assert.Contains("$env:GITPKG_HOME", stdout);
        Assert.Contains("$env:GITPKG_HOME\\bin", stdout);
    }

    /// <summary>pwsh 别名输出应与 powershell 一致。</summary>
    [Fact]
    public async Task Init_Pwsh_Alias_WritesEnvPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("pwsh");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for powershell", stdout);
        Assert.Contains("$env:GITPKG_HOME", stdout);
        Assert.Contains("$env:GITPKG_HOME\\bin", stdout);
    }
}
