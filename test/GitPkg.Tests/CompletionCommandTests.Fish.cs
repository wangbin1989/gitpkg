namespace GitPkg.Tests;

/// <summary>
/// completion 命令的 fish 补全测试。
/// </summary>
public partial class CompletionCommandTests
{
    /// <summary>fish：输出含 complete -c 注册的补全脚本。</summary>
    [Fact]
    public async Task Completion_Fish_HasCompleteC()
    {
        var (exitCode, stdout, _) = await InvokeCompletionAsync("fish");

        Assert.Equal(0, exitCode);
        Assert.Contains("complete -c gitpkg", stdout);
    }

    /// <summary>fish：补全脚本包含所有根命令。</summary>
    [Fact]
    public async Task Completion_Fish_ContainsAllSubcommands()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("fish");

        foreach (string cmd in new[] { "install", "update", "uninstall", "outdated", "list", "info", "init", "completion", "manifest", "self-update" })
            Assert.Contains($"-a '{cmd}'", stdout);
    }

    /// <summary>fish：init/completion 子命令补全包含 cmd。</summary>
    [Fact]
    public async Task Completion_Fish_InitIncludesCmd()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("fish");

        Assert.Contains("zsh bash fish powershell cmd", stdout);
    }

    /// <summary>fish：install 子命令补全包含 --from 选项。</summary>
    [Fact]
    public async Task Completion_Fish_InstallHasFromFlag()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("fish");

        Assert.Contains("-l from", stdout);
    }
}
