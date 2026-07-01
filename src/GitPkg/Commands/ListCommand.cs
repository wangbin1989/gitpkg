using System.CommandLine;
using Spectre.Console;
using GitPkg.Services;

namespace GitPkg.Commands;

/// <summary>
/// list 命令：以表格形式列出所有已安装的工具及其版本、仓库和安装时间。
/// </summary>
public class ListCommand : Command
{
    /// <summary>创建 list 命令。</summary>
    public ListCommand() : base("list", "列出已安装的工具")
    {
        SetAction(async (parseResult, ct) =>
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

        var sorted = tools.Tools.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();

        var table = new Table();
        table.AddColumn("名称");
        table.AddColumn("版本");
        table.AddColumn("仓库");
        table.AddColumn("资产");
        table.AddColumn("安装时间");

        foreach (var tool in sorted)
        {
            table.AddRow(
                $"[bold]{tool.Name}[/]",
                tool.Version,
                tool.Repo,
                tool.AssetName ?? "[grey]-[/]",
                tool.InstalledAt.ToLocalTime().ToString("yyyy-MM-dd"));
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[grey]共 {sorted.Count} 个工具[/]");
    }
}
