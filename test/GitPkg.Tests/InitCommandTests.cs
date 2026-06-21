using System.CommandLine;
using GitPkg.Commands;

namespace GitPkg.Tests;

/// <summary>
/// init 命令单元测试。
/// 验证各 Shell 的 PATH 初始化脚本输出、路径转义和错误处理。
/// </summary>
[Collection("ConsoleCapture")]
public class InitCommandTests
{
    /// <summary>构建 init 命令并捕获 stdout/stderr 和退出码。</summary>
    private static async Task<(int ExitCode, string Stdout, string Stderr)> InvokeInitAsync(string shell)
    {
        var cmd = InitCommand.Create();
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

    /// <summary>zsh：含 GITPKG_HOME 和基于变量的 PATH 初始化脚本。</summary>
    [Fact]
    public async Task Init_Zsh_WritesExportPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("zsh");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for zsh", stdout);
        Assert.Contains("export GITPKG_HOME=", stdout);
        Assert.Contains("export PATH=\"$GITPKG_HOME/bin\":$PATH", stdout);
        Assert.DoesNotContain("[suggest]", stdout); // init 不含补全
    }

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

    /// <summary>fish：含 GITPKG_HOME 和基于变量的 fish_add_path 初始化脚本。</summary>
    [Fact]
    public async Task Init_Fish_WritesFishAddPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("fish");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for fish", stdout);
        Assert.Contains("set -gx GITPKG_HOME", stdout);
        Assert.Contains("fish_add_path \"$GITPKG_HOME/bin\"", stdout);
    }

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
        // 单引号内 $ 是字面量，不会被展开
        Assert.Equal("'/path/$foo'", result);
    }

    /// <summary>POSIX 转义：含反引号的路径不应被 shell 执行。</summary>
    [Fact]
    public void EscapeForPosixShell_WithBacktick_PreventsExecution()
    {
        var result = InitCommand.EscapeForPosixShell("/path/`id`");
        // 单引号内反引号是字面量，不会被当作命令替换
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
        // $ 在 PowerShell 单引号内是字面量
        Assert.Equal("C:\\path\\$foo", result);
    }
}
