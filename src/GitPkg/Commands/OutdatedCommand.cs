using System.CommandLine;
using Spectre.Console;
using GitPkg.Services;

namespace GitPkg.Commands;

public static class OutdatedCommand
{
    public static Command Create()
    {
        var cmd = new Command("outdated", "检查已安装工具的更新");

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
            }
        }

        if (outdatedCount == 0)
        {
            AnsiConsole.MarkupLine("[green]所有工具均为最新版本[/]");
            return;
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[grey]共 {outdatedCount} 个工具可更新，使用 gitpkg update 更新[/]");
    }
}
