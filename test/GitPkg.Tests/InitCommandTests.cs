using System.CommandLine;
using GitPkg.Commands;

namespace GitPkg.Tests;

/// <summary>
/// init 命令单元测试。
/// 验证各 Shell 的初始化脚本包含 PATH 设置和自动补全两部分。
/// 与 CliIntegrationTests 共享 "ConsoleCapture" 集合。
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

    /// <summary>zsh：PATH 设置 + compdef 补全注册。</summary>
    [Fact]
    public async Task Init_Zsh_WritesExportPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("zsh");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for zsh", stdout);
        Assert.Contains("export PATH=", stdout);
        Assert.Contains("#compdef gitpkg", stdout);
        Assert.Contains("[suggest]", stdout);
    }

    /// <summary>bash：PATH 设置 + complete -F 补全注册。</summary>
    [Fact]
    public async Task Init_Bash_WritesExportPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("bash");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for bash", stdout);
        Assert.Contains("export PATH=", stdout);
        Assert.Contains("complete -F", stdout);
        Assert.Contains("[suggest]", stdout);
    }

    /// <summary>fish：PATH 设置 + complete -c 补全注册。</summary>
    [Fact]
    public async Task Init_Fish_WritesFishAddPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("fish");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for fish", stdout);
        Assert.Contains("fish_add_path", stdout);
        Assert.Contains("complete -c", stdout);
        Assert.Contains("[suggest]", stdout);
    }

    /// <summary>powershell：PATH 设置 + Register-ArgumentCompleter 注册。</summary>
    [Fact]
    public async Task Init_Powershell_WritesEnvPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("powershell");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for powershell", stdout);
        Assert.Contains("$env:Path", stdout);
        Assert.Contains("Register-ArgumentCompleter", stdout);
        Assert.Contains("[suggest]", stdout);
    }

    /// <summary>pwsh 别名输出应与 powershell 一致。</summary>
    [Fact]
    public async Task Init_Pwsh_Alias_WritesEnvPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("pwsh");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for powershell", stdout);
        Assert.Contains("$env:Path", stdout);
        Assert.Contains("Register-ArgumentCompleter", stdout);
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
