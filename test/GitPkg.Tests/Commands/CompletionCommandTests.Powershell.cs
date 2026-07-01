using Shouldly;

namespace GitPkg.Tests.Commands;

/// <summary>
/// completion 命令的 PowerShell 补全测试。
/// </summary>
public partial class CompletionCommandTests
{
    /// <summary>powershell：输出含 Register-ArgumentCompleter 注册的补全脚本。</summary>
    [Fact]
    public async Task Completion_Powershell_HasRegister()
    {
        var (exitCode, stdout, _) = await InvokeCompletionAsync("powershell");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("Register-ArgumentCompleter -Native -CommandName gitpkg");
    }

    /// <summary>powershell：补全脚本包含所有根命令。</summary>
    [Fact]
    public async Task Completion_Powershell_ContainsAllSubcommands()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("powershell");

        foreach (string cmd in new[] { "install", "update", "uninstall", "outdated", "list", "info", "init", "completion", "manifest", "self-update" })
            stdout.ShouldContain($"\"{cmd}\"");
    }

    /// <summary>powershell：init/completion 子命令补全包含 cmd。</summary>
    [Fact]
    public async Task Completion_Powershell_InitIncludesCmd()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("powershell");

        stdout.ShouldContain("'cmd'");
    }

    /// <summary>powershell：install 子命令补全包含 --from 选项。</summary>
    [Fact]
    public async Task Completion_Powershell_InstallHasFromFlag()
    {
        var (_, stdout, _) = await InvokeCompletionAsync("powershell");

        stdout.ShouldContain("'--from'");
    }

    /// <summary>pwsh 别名输出应与 powershell 一致。</summary>
    [Fact]
    public async Task Completion_Pwsh_Alias_MatchesPowershell()
    {
        var (_, stdoutPwsh, _) = await InvokeCompletionAsync("pwsh");
        var (_, stdoutPs, _) = await InvokeCompletionAsync("powershell");

        stdoutPwsh.ShouldBe(stdoutPs);
    }
}
