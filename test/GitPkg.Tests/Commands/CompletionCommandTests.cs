using System.CommandLine;
using GitPkg.Commands;

namespace GitPkg.Tests.Commands;

/// <summary>
/// completion 命令单元测试。
/// 验证各 Shell 的自动补全脚本输出和错误处理。
/// </summary>
[Collection("ConsoleCapture")]
public partial class CompletionCommandTests
{
    /// <summary>构建 completion 命令并捕获 stdout/stderr 和退出码。</summary>
    private static async Task<(int ExitCode, string Stdout, string Stderr)> InvokeCompletionAsync(string shell)
    {
        var cmd = CompletionCommand.Create();
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
            var parseResult = root.Parse(["completion", shell]);
            var exitCode = await parseResult.InvokeAsync();
            return (exitCode, stdoutWriter.ToString(), stderrWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    /// <summary>非法 shell 名应返回退出码 1 并将错误信息输出到 stderr。</summary>
    [Fact]
    public async Task Completion_InvalidShell_ReturnsError()
    {
        var (exitCode, stdout, stderr) = await InvokeCompletionAsync("invalid");

        Assert.Equal(1, exitCode);
        Assert.Empty(stdout);
        Assert.Contains("不支持的 shell", stderr);
    }
}
