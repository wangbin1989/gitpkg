using GitPkg.Commands;

namespace GitPkg.Tests;

/// <summary>
/// init 命令的 CMD / clink 相关测试。
/// </summary>
public partial class InitCommandTests
{
    /// <summary>cmd：含 GITPKG_HOME 和基于变量的 PATH 初始化脚本。</summary>
    [Fact]
    public async Task Init_Cmd_WritesSetPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("cmd");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("@echo off", stdout);
        Assert.Contains("set GITPKG_HOME=", stdout);
        Assert.Contains("%GITPKG_HOME%\\bin;%PATH%", stdout);
    }

    /// <summary>cmd 转义：普通路径保持不变。</summary>
    [Fact]
    public void EscapeForCmd_NormalPath_Unchanged()
    {
        var result = InitCommand.EscapeForCmd("C:\\Program Files\\GitPkg");
        Assert.Equal("C:\\Program Files\\GitPkg", result);
    }

    /// <summary>cmd 转义：含 % 的路径应转义为 %%。</summary>
    [Fact]
    public void EscapeForCmd_WithPercent_Doubles()
    {
        var result = InitCommand.EscapeForCmd("C:\\path\\%foo%");
        Assert.Equal("C:\\path\\%%foo%%", result);
    }
}
