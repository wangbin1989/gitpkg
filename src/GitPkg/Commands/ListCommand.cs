using System.CommandLine;
using Spectre.Console;
using GitPkg.Services;

namespace GitPkg.Commands;

/// <summary>
/// list 命令：以表格形式列出所有已安装的工具及其版本、仓库和安装时间。
/// </summary>
public static class ListCommand
{
    /// <summary>创建 list 命令。</summary>
    public static Command Create()
    {
        var cmd = new Command("list", "列出已安装的工具");

        cmd.SetAction(async (parseResult, ct) =>
        {
            try
            {
                await HandleAsync(ct);
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ 错误: {ex.Message}[/]");
                return 1;
            }
        });

        return cmd;
    }

    private static async Task HandleAsync(CancellationToken ct)
    {
        var manifest = new ManifestService();
        var tools = await manifest.LoadAsync(ct);

        if (tools.Tools.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]暂无已安装工具，使用 gitpkg install <owner/repo> 安装[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("名称");
        table.AddColumn("版本");
        table.AddColumn("仓库");
        table.AddColumn("安装时间");

        foreach (var tool in tools.Tools)
        {
            table.AddRow(
                $"[bold]{tool.Name}[/]",
                tool.Version,
                tool.Repo,
                tool.InstalledAt.ToLocalTime().ToString("yyyy-MM-dd"));
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[grey]共 {tools.Tools.Count} 个工具[/]");
    }
}
