using GitPkg.Commands;
using Shouldly;

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

        exitCode.ShouldBe(0);
        stdout.ShouldContain("-- gitpkg shell init for cmd (requires clink)");
        stdout.ShouldContain("os.setenv(\"GITPKG_HOME\",");
        stdout.ShouldContain("os.setenv(\"PATH\",");
    }

    /// <summary>Lua 转义：普通路径中的反斜杠应转义为 \\。</summary>
    [Fact]
    public void EscapeForLua_NormalPath_EscapesBackslashes()
    {
        var result = InitCommand.EscapeForLua("C:\\Program Files\\GitPkg");
        result.ShouldBe("C:\\\\Program Files\\\\GitPkg");
    }

    /// <summary>Lua 转义：含双引号的路径应转义为 \"。</summary>
    [Fact]
    public void EscapeForLua_WithQuote_Escapes()
    {
        var result = InitCommand.EscapeForLua("C:\\path\\\"foo\"");
        result.ShouldBe("C:\\\\path\\\\\\\"foo\\\"");
    }

    /// <summary>Lua 转义：含反斜杠的路径应转义为 \\。</summary>
    [Fact]
    public void EscapeForLua_WithBackslash_Escapes()
    {
        var result = InitCommand.EscapeForLua("C:\\path\\to\\dir");
        result.ShouldBe("C:\\\\path\\\\to\\\\dir");
    }
}
