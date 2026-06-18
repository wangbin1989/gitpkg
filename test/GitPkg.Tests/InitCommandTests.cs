using System.CommandLine;
using GitPkg.Commands;

namespace GitPkg.Tests;

public class InitCommandTests
{
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

    [Fact]
    public async Task Init_Zsh_WritesExportPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("zsh");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for zsh", stdout);
        Assert.Contains("export PATH=", stdout);
    }

    [Fact]
    public async Task Init_Bash_WritesExportPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("bash");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for bash", stdout);
        Assert.Contains("export PATH=", stdout);
    }

    [Fact]
    public async Task Init_Fish_WritesFishAddPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("fish");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for fish", stdout);
        Assert.Contains("fish_add_path", stdout);
    }

    [Fact]
    public async Task Init_Powershell_WritesEnvPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("powershell");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for powershell", stdout);
        Assert.Contains("$env:Path", stdout);
    }

    [Fact]
    public async Task Init_Pwsh_Alias_WritesEnvPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("pwsh");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for powershell", stdout);
        Assert.Contains("$env:Path", stdout);
    }

    [Fact]
    public async Task Init_InvalidShell_WritesError()
    {
        var (exitCode, stdout, stderr) = await InvokeInitAsync("invalid");

        Assert.Equal(1, exitCode);
        Assert.Empty(stdout);
        Assert.Contains("不支持的 shell", stderr);
    }
}
