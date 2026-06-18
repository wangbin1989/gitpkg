using System.CommandLine;
using GitPkg.Commands;

namespace GitPkg.Tests;

/// <summary>
/// init 命令单元测试。
/// 验证各 Shell（zsh/bash/fish/powershell）的初始化脚本输出格式和错误处理。
/// 与 CliIntegrationTests 共享 "ConsoleCapture" 集合，避免并行 Console 重定向冲突。
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

    /// <summary>zsh 应输出含 export PATH 的初始化脚本。</summary>
    [Fact]
    public async Task Init_Zsh_WritesExportPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("zsh");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for zsh", stdout);
        Assert.Contains("export PATH=", stdout);
    }

    /// <summary>bash 应输出含 export PATH 的初始化脚本。</summary>
    [Fact]
    public async Task Init_Bash_WritesExportPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("bash");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for bash", stdout);
        Assert.Contains("export PATH=", stdout);
    }

    /// <summary>fish 应输出含 fish_add_path 的初始化脚本。</summary>
    [Fact]
    public async Task Init_Fish_WritesFishAddPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("fish");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for fish", stdout);
        Assert.Contains("fish_add_path", stdout);
    }

    /// <summary>powershell 应输出含 $env:Path 的初始化脚本。</summary>
    [Fact]
    public async Task Init_Powershell_WritesEnvPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("powershell");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for powershell", stdout);
        Assert.Contains("$env:Path", stdout);
    }

    /// <summary>pwsh 别名应输出与 powershell 相同的初始化脚本。</summary>
    [Fact]
    public async Task Init_Pwsh_Alias_WritesEnvPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("pwsh");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for powershell", stdout);
        Assert.Contains("$env:Path", stdout);
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
}
