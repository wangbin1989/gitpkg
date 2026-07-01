namespace GitPkg.Tests;

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

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for bash", stdout);
        Assert.Contains("export GITPKG_HOME=", stdout);
        Assert.Contains("export PATH=\"$GITPKG_HOME/bin\":$PATH", stdout);
    }
}
