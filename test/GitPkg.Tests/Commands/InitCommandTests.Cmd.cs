using GitPkg.Commands;

namespace GitPkg.Tests.Commands;

/// <summary>
/// init 命令的 CMD / clink 相关测试。
/// </summary>
public partial class InitCommandTests
{
    /// <summary>cmd：含 GITPKG_HOME 和基于变量的 PATH 初始化 Lua 脚本。</summary>
    [Fact]
    public async Task Init_Cmd_WritesLuaScript()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("cmd");

        Assert.Equal(0, exitCode);
        Assert.Contains("-- gitpkg shell init for cmd (requires clink)", stdout);
        Assert.Contains("os.setenv(\"GITPKG_HOME\",", stdout);
        Assert.Contains("os.setenv(\"PATH\",", stdout);
    }

    /// <summary>Lua 转义：普通路径中的反斜杠应转义为 \\。</summary>
    [Fact]
    public void EscapeForLua_NormalPath_EscapesBackslashes()
    {
        var result = InitCommand.EscapeForLua("C:\\Program Files\\GitPkg");
        Assert.Equal("C:\\\\Program Files\\\\GitPkg", result);
    }

    /// <summary>Lua 转义：含双引号的路径应转义为 \"。</summary>
    [Fact]
    public void EscapeForLua_WithQuote_Escapes()
    {
        var result = InitCommand.EscapeForLua("C:\\path\\\"foo\"");
        Assert.Equal("C:\\\\path\\\\\\\"foo\\\"", result);
    }

    /// <summary>Lua 转义：含反斜杠的路径应转义为 \\。</summary>
    [Fact]
    public void EscapeForLua_WithBackslash_Escapes()
    {
        var result = InitCommand.EscapeForLua("C:\\path\\to\\dir");
        Assert.Equal("C:\\\\path\\\\to\\\\dir", result);
    }
}
