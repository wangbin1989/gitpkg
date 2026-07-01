using Shouldly;

namespace GitPkg.Tests.Commands;

/// <summary>
/// completion 命令的 CMD / clink 补全测试。
/// </summary>
public partial class CompletionCommandTests
{
    /// <summary>cmd：输出含 clink.argmatcher 注册的 Lua 补全脚本。</summary>
    [Fact]
    public async Task Completion_Cmd_HasArgmatcher()
    {
        var (exitCode, stdout, _) = await InvokeCompletionAsync("cmd");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("clink.argmatcher");
    }

    /// <summary>cmd：补全脚本包含所有根命令。</summary>
    [Fact]
    public async Task Completion_Cmd_ContainsAllSubcommands()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("cmd");

        foreach (string cmd in new[] { "install", "update", "uninstall", "outdated", "list", "info", "init", "completion", "manifest", "self-update" })
            stdout.ShouldContain($"\"{cmd}\"");
    }

    /// <summary>cmd：init/completion 子命令补全包含 cmd。</summary>
    [Fact]
    public async Task Completion_Cmd_InitIncludesCmd()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("cmd");

        stdout.ShouldContain("\"zsh\", \"bash\", \"powershell\", \"cmd\"");
    }

    /// <summary>cmd：install 子命令补全包含 --from 选项。</summary>
    [Fact]
    public async Task Completion_Cmd_InstallHasFromFlag()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("cmd");

        stdout.ShouldContain("\"--from\"");
    }

    /// <summary>cmd：脚本使用 addarg 函数分派子命令补全。</summary>
    [Fact]
    public async Task Completion_Cmd_HasSubcommandDispatcher()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("cmd");

        stdout.ShouldContain(":addarg(function");
    }

    /// <summary>cmd：脚本注释包含 load(io.popen(...))() 加载方式。</summary>
    [Fact]
    public async Task Completion_Cmd_HasLoadPattern()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("cmd");

        stdout.ShouldContain("load(io.popen('gitpkg completion cmd'):read(\"*a\"))()");
    }
}
