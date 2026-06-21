using System.CommandLine;
using Spectre.Console;
using GitPkg.Services;

namespace GitPkg.Commands;

/// <summary>
/// info 命令：查看工具的详细信息。
/// 优先按已安装工具名查找，其次按 owner/repo 格式远程查询。
/// </summary>
public static class InfoCommand
{
    /// <summary>创建 info 命令。</summary>
    public static Command Create()
    {
        var cmd = new Command("info", "查看工具详情");

        var nameArg = new Argument<string>("name") { Description = "工具名称或 owner/repo" };
        cmd.Add(nameArg);

        cmd.SetAction(async (parseResult, ct) =>
        {
            var name = parseResult.GetValue(nameArg);

            try
            {
                await HandleAsync(name!, ct);
                return 0;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("Not Found") || ex.Message.Contains("资源不存在"))
            {
                AnsiConsole.MarkupLine($"[red]✗ 仓库 {name} 不存在[/]");
                return 1;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ 错误: {ex.Message}[/]");
                return 1;
            }
        });

        return cmd;
    }

    /// <summary>
    /// 解析输入参数，支持已安装工具名和 owner/repo 两种格式。
    /// 展示仓库描述、已安装版本、最新版本和可用资产列表。
    /// </summary>
    private static async Task HandleAsync(string input, CancellationToken ct)
    {
        var gitHub = new GitHubService(GitPkgApp.Http);
        var manifest = new ManifestService();

        // Determine if input is installed tool name or owner/repo
        string owner, repoName;
        Models.ToolEntry? installed = null;

        var tool = await manifest.FindToolAsync(input, ct);
        if (tool != null)
        {
            installed = tool;
            var parts = tool.Repo.Split('/');
            owner = parts[0];
            repoName = parts[1];
        }
        else if (input.Contains('/'))
        {
            var parts = input.Split('/');
            if (parts.Length != 2)
                throw new ArgumentException("无效格式，应为 owner/repo");
            owner = parts[0];
            repoName = parts[1];
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗ {input} 未找到已安装的工具，请使用 owner/repo 格式[/]");
            return;
        }

        // Get repo info
        var repo = await gitHub.GetRepoAsync(owner, repoName, ct);

        // Get latest release
        Models.GitHubRelease release;
        try
        {
            release = await gitHub.GetLatestReleaseAsync(owner, repoName, ct);
        }
        catch
        {
            release = new Models.GitHubRelease { TagName = "(无 Release)", Assets = [] };
        }

        // Build panel
        var content = new List<string>();

        content.Add($"仓库:     {owner}/{repoName}");
        if (repo.Description != null)
            content.Add($"描述:     {repo.Description}");

        if (installed != null)
        {
            content.Add($"已安装:   {installed.Version} ({installed.InstalledAt.ToLocalTime():yyyy-MM-dd})");
            if (installed.AssetName != null)
                content.Add($"记录资产: {installed.AssetName}");
        }

        var releaseDisplay = release.Name ?? release.TagName;
        var releaseDate = release.PublishedAt != default
            ? release.PublishedAt.ToLocalTime().ToString("yyyy-MM-dd")
            : "-";
        content.Add($"最新版本: {releaseDisplay} ({releaseDate})");

        if (release.Assets.Count > 0)
        {
            content.Add("可用资产:");
            foreach (var a in release.Assets)
                content.Add($"  - {a.Name} ({CommandHelpers.FormatSize(a.Size)})");
        }

        var panel = new Panel(string.Join("\n", content))
        {
            Header = new PanelHeader($" {input} "),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);
    }

}
