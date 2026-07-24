using System.CommandLine;
using Spectre.Console;
using GitPkg.Services;

namespace GitPkg.Commands;

/// <summary>
/// outdated 命令：对比已安装工具与 GitHub 最新 Release，列出可更新的工具。
/// </summary>
public class OutdatedCommand : Command
{
    /// <summary>创建 outdated 命令。</summary>
    public OutdatedCommand() : base("outdated", "检查已安装工具的更新")
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

    /// <summary>逐个查询已安装工具的最新版本，生成对比表格。</summary>
    private static async Task HandleAsync(CancellationToken ct)
    {
        var gitHub = new GitHubService(GitPkgApp.Http);
        var manifest = new ManifestService();
        var tools = await manifest.LoadAsync(ct);

        if (tools.Tools.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]暂无已安装工具[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("名称");
        table.AddColumn("当前版本");
        table.AddColumn("最新版本");
        table.AddColumn("仓库");

        var outdatedCount = 0;
        var failedCount = 0;

        foreach (var tool in tools.Tools)
        {
            try
            {
                var parts = tool.Repo.Split('/');
                if (parts.Length != 2) continue;

                var release = await gitHub.GetLatestReleaseAsync(parts[0], parts[1], ct);
                var latest = release.TagName;

                if (latest != tool.Version)
                {
                    table.AddRow(
                        $"[yellow]{tool.Name}[/]",
                        tool.Version,
                        $"[green]{latest}[/]",
                        tool.Repo);
                    outdatedCount++;
                }
            }
            catch
            {
                table.AddRow(
                    $"[grey]{tool.Name}[/]",
                    tool.Version,
                    "[red]查询失败[/]",
                    tool.Repo);
                failedCount++;
            }
        }

        if (failedCount == tools.Tools.Count)
        {
            AnsiConsole.MarkupLine("[red]✗ 所有工具查询失败，可能是 GitHub API 限流，请稍后重试[/]");
            return;
        }

        if (outdatedCount == 0 && failedCount == 0)
        {
            AnsiConsole.MarkupLine("[green]所有工具均为最新版本[/]");
            return;
        }

        AnsiConsole.Write(table);

        if (outdatedCount > 0)
            AnsiConsole.MarkupLine($"[grey]共 {outdatedCount} 个工具可更新，使用 gitpkg update 更新[/]");

        if (failedCount > 0)
            AnsiConsole.MarkupLine($"[yellow]⚠ {failedCount} 个工具查询失败，可能是 GitHub API 限流，请稍后重试[/]");
    }
}
