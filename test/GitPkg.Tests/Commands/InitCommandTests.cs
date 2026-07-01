using System.CommandLine;
using GitPkg.Commands;

namespace GitPkg.Tests.Commands;

/// <summary>
/// init 命令单元测试。
/// 验证各 Shell 的 PATH 初始化脚本输出、路径转义和错误处理。
/// </summary>
[Collection("ConsoleCapture")]
public partial class InitCommandTests
{
    /// <summary>构建 init 命令并捕获 stdout/stderr 和退出码。</summary>
    private static async Task<(int ExitCode, string Stdout, string Stderr)> InvokeInitAsync(string shell)
    {
        var cmd = new InitCommand();
        var root = new RootCommand();
        root.Add(cmd);

        using var stdoutWriter = new StringWriter();
        using var stderrWriter = new StringWriter();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(stdoutWriter);
            Console.SetError(stderrWriter);
            var parseResult = root.Parse(["init", shell]);
            var exitCode = await parseResult.InvokeAsync();
            return (exitCode, stdoutWriter.ToString(), stderrWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    /// <summary>非法 shell 名应返回退出码 1 并将错误信息输出到 stderr。</summary>
    [Fact]
    public async Task Init_InvalidShell_WritesError()
    {
        var (exitCode, stdout, stderr) = await InvokeInitAsync("invalid");

        Assert.Equal(1, exitCode);
        Assert.Empty(stdout);
        Assert.Contains("不支持的 shell", stderr);
    }

    /// <summary>POSIX 转义：普通路径保持不变。</summary>
    [Fact]
    public void EscapeForPosixShell_NormalPath_Unchanged()
    {
        var result = InitCommand.EscapeForPosixShell("/usr/local/bin");
        Assert.Equal("'/usr/local/bin'", result);
    }

    /// <summary>POSIX 转义：含单引号的路径应正确转义为 '\'' 模式。</summary>
    [Fact]
    public void EscapeForPosixShell_WithSingleQuote_EscapesCorrectly()
    {
        var result = InitCommand.EscapeForPosixShell("/path/it's here");
        Assert.Equal("'/path/it'\\''s here'", result);
    }

    /// <summary>POSIX 转义：含 $ 的路径不应被 shell 展开。</summary>
    [Fact]
    public void EscapeForPosixShell_WithDollarSign_PreventsExpansion()
    {
        var result = InitCommand.EscapeForPosixShell("/path/$foo");
        Assert.Equal("'/path/$foo'", result);
    }

    /// <summary>POSIX 转义：含反引号的路径不应被 shell 执行。</summary>
    [Fact]
    public void EscapeForPosixShell_WithBacktick_PreventsExecution()
    {
        var result = InitCommand.EscapeForPosixShell("/path/`id`");
        Assert.Equal("'/path/`id`'", result);
    }

    /// <summary>PowerShell 转义：普通路径保持不变。</summary>
    [Fact]
    public void EscapeForPowershell_NormalPath_Unchanged()
    {
        var result = InitCommand.EscapeForPowershell("C:\\Program Files\\GitPkg");
        Assert.Equal("C:\\Program Files\\GitPkg", result);
    }

    /// <summary>PowerShell 转义：含单引号的路径应双写单引号。</summary>
    [Fact]
    public void EscapeForPowershell_WithSingleQuote_Doubles()
    {
        var result = InitCommand.EscapeForPowershell("C:\\it's here");
        Assert.Equal("C:\\it''s here", result);
    }

    /// <summary>PowerShell 转义：含 $ 的路径不应被展开。</summary>
    [Fact]
    public void EscapeForPowershell_WithDollar_PreventsExpansion()
    {
        var result = InitCommand.EscapeForPowershell("C:\\path\\$foo");
        Assert.Equal("C:\\path\\$foo", result);
    }
}
