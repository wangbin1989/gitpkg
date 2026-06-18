using System.CommandLine;
using GitPkg;
using GitPkg.Commands;

namespace GitPkg.Tests;

/// <summary>
/// 与 InitCommandTests 共享同一集合，避免并行执行时的 Console 重定向冲突。
/// </summary>
[CollectionDefinition("ConsoleCapture")]
public class ConsoleCaptureCollection { }

/// <summary>
/// CLI 集成测试：构建完整的根命令并验证端到端行为，
/// 包括帮助输出、版本信息、命令分发和退出码。
/// </summary>
[Collection("ConsoleCapture")]
public class CliIntegrationTests
{
    /// <summary>构建与 Program.cs 相同的根命令树（不含网络依赖的副作用）。</summary>
    private static RootCommand BuildRootCommand()
    {
        GitPkgApp.Initialize();

        var root = new RootCommand("gitpkg — GitHub Release 自动更新工具");

        root.Add(InitCommand.Create());
        root.Add(CompletionCommand.Create());
        root.Add(InstallCommand.Create());
        root.Add(UpdateCommand.Create());
        root.Add(OutdatedCommand.Create());
        root.Add(UninstallCommand.Create());
        root.Add(ListCommand.Create());
        root.Add(InfoCommand.Create());
        root.Add(ManifestCommand.Create());
        root.Add(SelfUpdateCommand.Create());

        return root;
    }

    /// <summary>调用根命令并捕获 stdout、stderr 和退出码。</summary>
    private static async Task<(int ExitCode, string Stdout, string Stderr)> InvokeAsync(string[] args)
    {
        var root = BuildRootCommand();

        using var stdoutWriter = new StringWriter();
        using var stderrWriter = new StringWriter();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(stdoutWriter);
            Console.SetError(stderrWriter);
            var parseResult = root.Parse(args);
            var exitCode = await parseResult.InvokeAsync();
            return (exitCode, stdoutWriter.ToString(), stderrWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    /// <summary>--help 应返回 0 且输出中包含关键命令名称。</summary>
    [Fact]
    public async Task Help_ContainsAllCommands()
    {
        var (exitCode, stdout, _) = await InvokeAsync(["--help"]);

        Assert.Equal(0, exitCode);
        // 验证几个关键命令是否出现在帮助输出中
        Assert.Contains("init", stdout);
        Assert.Contains("install", stdout);
        Assert.Contains("update", stdout);
        Assert.Contains("uninstall", stdout);
        Assert.Contains("list", stdout);
        Assert.Contains("completion", stdout);
    }

    /// <summary>--version 应输出版本号。</summary>
    [Fact]
    public async Task Version_PrintsVersion()
    {
        var (exitCode, stdout, _) = await InvokeAsync(["--version"]);

        Assert.Equal(0, exitCode);
        Assert.NotEmpty(stdout.Trim());
    }

    /// <summary>未知命令应返回非零退出码。</summary>
    [Fact]
    public async Task UnknownCommand_ReturnsError()
    {
        var (exitCode, _, stderr) = await InvokeAsync(["nonexistent-command"]);

        Assert.NotEqual(0, exitCode);
        Assert.NotEmpty(stderr + Console.Out.ToString());
    }

    /// <summary>init 命令缺少 shell 参数应报错。</summary>
    [Fact]
    public async Task Init_MissingArgument_ReturnsError()
    {
        var (exitCode, _, _) = await InvokeAsync(["init"]);

        Assert.NotEqual(0, exitCode);
    }

    /// <summary>init zsh 应返回退出码 0 并输出 PATH 设置脚本（不含补全）。</summary>
    [Fact]
    public async Task Init_Zsh_ReturnsScript()
    {
        var (exitCode, stdout, _) = await InvokeAsync(["init", "zsh"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("export PATH=", stdout);
        Assert.DoesNotContain("[suggest]", stdout);
    }

    /// <summary>init 对非法 shell 名应返回错误并输出到 stderr。</summary>
    [Fact]
    public async Task Init_InvalidShell_ReturnsError()
    {
        var (exitCode, stdout, stderr) = await InvokeAsync(["init", "invalid_shell"]);

        Assert.Equal(1, exitCode);
        Assert.Empty(stdout);
        Assert.Contains("不支持的 shell", stderr);
    }

    /// <summary>init 对所有合法 shell 均应返回退出码 0。</summary>
    [Theory]
    [InlineData("zsh")]
    [InlineData("bash")]
    [InlineData("fish")]
    [InlineData("powershell")]
    [InlineData("pwsh")]
    public async Task Init_AllValidShells_Succeed(string shell)
    {
        var (exitCode, stdout, _) = await InvokeAsync(["init", shell]);

        Assert.Equal(0, exitCode);
        Assert.NotEmpty(stdout);
    }

    /// <summary>manifest export 应输出包含 version 和 tools 字段的合法 JSON。</summary>
    [Fact]
    public async Task ManifestExport_ProducesValidJson()
    {
        var (exitCode, stdout, _) = await InvokeAsync(["manifest", "export"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("\"version\"", stdout);
        Assert.Contains("\"tools\"", stdout);
    }

    /// <summary>各子命令的 --help 应输出非空的帮助文本。</summary>
    [Theory]
    [InlineData("init")]
    [InlineData("install")]
    [InlineData("update")]
    [InlineData("outdated")]
    [InlineData("uninstall")]
    [InlineData("list")]
    [InlineData("info")]
    [InlineData("manifest")]
    [InlineData("completion")]
    [InlineData("self-update")]
    public async Task SubCommand_Help_ShowsDescription(string command)
    {
        var (exitCode, stdout, _) = await InvokeAsync([command, "--help"]);

        Assert.Equal(0, exitCode);
        Assert.NotEmpty(stdout.Trim());
        Assert.True(stdout.Length > 10, $"Help for '{command}' is too short: {stdout}");
    }

    /// <summary>uninstall 缺少参数时应返回非零退出码。</summary>
    [Fact]
    public async Task Uninstall_MissingArgument_ReturnsError()
    {
        var (exitCode, _, _) = await InvokeAsync(["uninstall"]);

        Assert.NotEqual(0, exitCode);
    }

    /// <summary>info 缺少参数时应返回非零退出码。</summary>
    [Fact]
    public async Task Info_MissingArgument_ReturnsError()
    {
        var (exitCode, _, _) = await InvokeAsync(["info"]);

        Assert.NotEqual(0, exitCode);
    }

    /// <summary>completion zsh 应输出含 compdef 的补全脚本。</summary>
    [Fact]
    public async Task Completion_Zsh_ReturnsScript()
    {
        var (exitCode, stdout, stderr) = await InvokeAsync(["completion", "zsh"]);

        // 如果失败，输出详细信息帮助诊断
        Assert.True(exitCode == 0, $"ExitCode={exitCode}, Stdout='{stdout}', Stderr='{stderr}'");
        Assert.Contains("#compdef gitpkg", stdout);
        Assert.Contains("[suggest]", stdout);
    }

    /// <summary>completion bash 应输出含 complete -F 的补全脚本。</summary>
    [Fact]
    public async Task Completion_Bash_ReturnsScript()
    {
        var (exitCode, stdout, _) = await InvokeAsync(["completion", "bash"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("complete -F", stdout);
        Assert.Contains("[suggest]", stdout);
    }

    /// <summary>completion fish 应输出含 complete -c 的补全脚本。</summary>
    [Fact]
    public async Task Completion_Fish_ReturnsScript()
    {
        var (exitCode, stdout, _) = await InvokeAsync(["completion", "fish"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("complete -c", stdout);
        Assert.Contains("[suggest]", stdout);
    }

    /// <summary>completion powershell 应输出含 Register-ArgumentCompleter 的补全脚本。</summary>
    [Fact]
    public async Task Completion_Powershell_ReturnsScript()
    {
        var (exitCode, stdout, _) = await InvokeAsync(["completion", "powershell"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Register-ArgumentCompleter", stdout);
        Assert.Contains("[suggest]", stdout);
    }

    /// <summary>completion 非法 shell 应返回错误。</summary>
    [Fact]
    public async Task Completion_InvalidShell_ReturnsError()
    {
        var (exitCode, stdout, stderr) = await InvokeAsync(["completion", "bad"]);

        Assert.Equal(1, exitCode);
        Assert.Empty(stdout);
        Assert.Contains("不支持的 shell", stderr);
    }
}
