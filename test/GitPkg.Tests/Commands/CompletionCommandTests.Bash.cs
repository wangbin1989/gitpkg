namespace GitPkg.Tests.Commands;

/// <summary>
/// completion 命令的 bash 补全测试。
/// </summary>
public partial class CompletionCommandTests
{
    /// <summary>bash：输出含 complete -F 注册的补全脚本。</summary>
    [Fact]
    public async Task Completion_Bash_HasCompleteF()
    {
        var (exitCode, stdout, _) = await InvokeCompletionAsync("bash");

        Assert.Equal(0, exitCode);
        Assert.Contains("complete -F _gitpkg_completion gitpkg", stdout);
    }

    /// <summary>bash：补全脚本包含所有根命令。</summary>
    [Fact]
    public async Task Completion_Bash_ContainsAllSubcommands()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("bash");

        foreach (string cmd in new[] { "install", "update", "uninstall", "outdated", "list", "info", "init", "completion", "manifest", "self-update" })
            Assert.Contains(cmd, stdout);
    }

    /// <summary>bash：init/completion 子命令补全包含 cmd。</summary>
    [Fact]
    public async Task Completion_Bash_InitIncludesCmd()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("bash");

        Assert.Contains("zsh bash powershell cmd", stdout);
    }

    /// <summary>bash：install 子命令补全包含 --from 选项。</summary>
    [Fact]
    public async Task Completion_Bash_InstallHasFromFlag()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("bash");

        Assert.Contains("--from", stdout);
    }
}
