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

        Assert.Equal(0, exitCode);
        Assert.Contains("clink.argmatcher", stdout);
    }

    /// <summary>cmd：补全脚本包含所有根命令。</summary>
    [Fact]
    public async Task Completion_Cmd_ContainsAllSubcommands()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("cmd");

        foreach (string cmd in new[] { "install", "update", "uninstall", "outdated", "list", "info", "init", "completion", "manifest", "self-update" })
            Assert.Contains($"\"{cmd}\"", stdout);
    }

    /// <summary>cmd：init/completion 子命令补全包含 cmd。</summary>
    [Fact]
    public async Task Completion_Cmd_InitIncludesCmd()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("cmd");

        Assert.Contains("\"zsh\", \"bash\", \"powershell\", \"cmd\"", stdout);
    }

    /// <summary>cmd：install 子命令补全包含 --from 选项。</summary>
    [Fact]
    public async Task Completion_Cmd_InstallHasFromFlag()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("cmd");

        Assert.Contains("\"--from\"", stdout);
    }

    /// <summary>cmd：脚本使用 .loop 分派子命令补全。</summary>
    [Fact]
    public async Task Completion_Cmd_HasLoopDispatcher()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("cmd");

        Assert.Contains(":loop(function", stdout);
    }

    /// <summary>cmd：脚本注释包含 load(io.popen(...))() 加载方式。</summary>
    [Fact]
    public async Task Completion_Cmd_HasLoadPattern()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("cmd");

        Assert.Contains("load(io.popen('gitpkg completion cmd'):read(\"*a\"))()", stdout);
    }
}
