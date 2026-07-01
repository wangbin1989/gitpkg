using System.CommandLine;
using GitPkg;
using GitPkg.Commands;
using Shouldly;

namespace GitPkg.Tests.Integration;

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

        root.Add(new InitCommand());
        root.Add(new CompletionCommand());
        root.Add(new InstallCommand());
        root.Add(new UpdateCommand());
        root.Add(new OutdatedCommand());
        root.Add(new UninstallCommand());
        root.Add(new ListCommand());
        root.Add(new InfoCommand());
        root.Add(new ManifestCommand());
        root.Add(new SelfUpdateCommand());

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

        exitCode.ShouldBe(0);
        // 验证几个关键命令是否出现在帮助输出中
        stdout.ShouldContain("init");
        stdout.ShouldContain("install");
        stdout.ShouldContain("update");
        stdout.ShouldContain("uninstall");
        stdout.ShouldContain("list");
        stdout.ShouldContain("completion");
    }

    /// <summary>--version 应输出版本号。</summary>
    [Fact]
    public async Task Version_PrintsVersion()
    {
        var (exitCode, stdout, _) = await InvokeAsync(["--version"]);

        exitCode.ShouldBe(0);
        stdout.Trim().ShouldNotBeEmpty();
    }

    /// <summary>未知命令应返回非零退出码。</summary>
    [Fact]
    public async Task UnknownCommand_ReturnsError()
    {
        var (exitCode, _, stderr) = await InvokeAsync(["nonexistent-command"]);

        exitCode.ShouldNotBe(0);
        (stderr + Console.Out.ToString()).ShouldNotBeEmpty();
    }

    /// <summary>init 命令缺少 shell 参数应报错。</summary>
    [Fact]
    public async Task Init_MissingArgument_ReturnsError()
    {
        var (exitCode, _, _) = await InvokeAsync(["init"]);

        exitCode.ShouldNotBe(0);
    }

    /// <summary>init zsh 应返回退出码 0 并输出 PATH 设置脚本（不含补全）。</summary>
    [Fact]
    public async Task Init_Zsh_ReturnsScript()
    {
        var (exitCode, stdout, _) = await InvokeAsync(["init", "zsh"]);

        exitCode.ShouldBe(0);
        stdout.ShouldContain("export PATH=");
        stdout.ShouldNotContain("[suggest]");
    }

    /// <summary>init 对非法 shell 名应返回错误并输出到 stderr。</summary>
    [Fact]
    public async Task Init_InvalidShell_ReturnsError()
    {
        var (exitCode, stdout, stderr) = await InvokeAsync(["init", "invalid_shell"]);

        exitCode.ShouldBe(1);
        stdout.ShouldBeEmpty();
        stderr.ShouldContain("不支持的 shell");
    }

    /// <summary>init 对所有合法 shell 均应返回退出码 0。</summary>
    [Theory]
    [InlineData("zsh")]
    [InlineData("bash")]
    [InlineData("powershell")]
    [InlineData("pwsh")]
    [InlineData("cmd")]
    public async Task Init_AllValidShells_Succeed(string shell)
    {
        var (exitCode, stdout, _) = await InvokeAsync(["init", shell]);

        exitCode.ShouldBe(0);
        stdout.ShouldNotBeEmpty();
    }

    /// <summary>manifest export 应输出包含 version 和 tools 字段的合法 JSON。</summary>
    [Fact]
    public async Task ManifestExport_ProducesValidJson()
    {
        var (exitCode, stdout, _) = await InvokeAsync(["manifest", "export"]);

        exitCode.ShouldBe(0);
        stdout.ShouldContain("\"version\"");
        stdout.ShouldContain("\"tools\"");
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

        exitCode.ShouldBe(0);
        stdout.Trim().ShouldNotBeEmpty();
        stdout.Length.ShouldBeGreaterThan(10, $"Help for '{command}' is too short: {stdout}");
    }

    /// <summary>uninstall 缺少参数时应返回非零退出码。</summary>
    [Fact]
    public async Task Uninstall_MissingArgument_ReturnsError()
    {
        var (exitCode, _, _) = await InvokeAsync(["uninstall"]);

        exitCode.ShouldNotBe(0);
    }

    /// <summary>info 缺少参数时应返回非零退出码。</summary>
    [Fact]
    public async Task Info_MissingArgument_ReturnsError()
    {
        var (exitCode, _, _) = await InvokeAsync(["info"]);

        exitCode.ShouldNotBe(0);
    }

    /// <summary>install 缺少 repo 参数且无 --from 时应返回非零退出码。</summary>
    [Fact]
    public async Task Install_MissingArgument_ReturnsError()
    {
        var (exitCode, _, _) = await InvokeAsync(["install"]);

        exitCode.ShouldNotBe(0);
    }

    /// <summary>install --help 不应包含已移除的 --add-path 选项。</summary>
    [Fact]
    public async Task Install_Help_NoAddPath()
    {
        var (exitCode, stdout, _) = await InvokeAsync(["install", "--help"]);

        exitCode.ShouldBe(0);
        stdout.ShouldNotContain("add-path");
    }

    /// <summary>update --help 应显示命令说明。</summary>
    [Fact]
    public async Task Update_Help_ShowsDescription()
    {
        var (exitCode, stdout, _) = await InvokeAsync(["update", "--help"]);

        exitCode.ShouldBe(0);
        stdout.ToLowerInvariant().ShouldContain("update");
        stdout.Length.ShouldBeGreaterThan(20);
    }
}
