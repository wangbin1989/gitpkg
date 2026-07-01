using Shouldly;

namespace GitPkg.Tests.Commands;

/// <summary>
/// completion 命令的 zsh 补全测试。
/// </summary>
public partial class CompletionCommandTests
{
    /// <summary>zsh：输出含 compdef 注册的补全脚本。</summary>
    [Fact]
    public async Task Completion_Zsh_HasCompdef()
    {
        var (exitCode, stdout, _) = await InvokeCompletionAsync("zsh");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("compdef _gitpkg gitpkg");
    }

    /// <summary>zsh：补全脚本包含所有根命令。</summary>
    [Fact]
    public async Task Completion_Zsh_ContainsAllSubcommands()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("zsh");

        foreach (string cmd in new[] { "install", "update", "uninstall", "outdated", "list", "info", "init", "completion", "manifest", "self-update" })
            stdout.ShouldContain(cmd);
    }

    /// <summary>zsh：init/completion 子命令补全包含 cmd。</summary>
    [Fact]
    public async Task Completion_Zsh_InitIncludesCmd()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("zsh");

        stdout.ShouldContain("zsh bash powershell cmd");
    }

    /// <summary>zsh：install 子命令补全包含 --from 选项。</summary>
    [Fact]
    public async Task Completion_Zsh_InstallHasFromFlag()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("zsh");

        stdout.ShouldContain("--from");
    }
}
