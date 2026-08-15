using System.CommandLine;
using Spectre.Console;
using GitPkg.Models;
using GitPkg.Services;

namespace GitPkg.Commands;

/// <summary>
/// link 命令：重新为已安装工具创建符号链接。
/// </summary>
public class LinkCommand : Command
{
    /// <summary>创建 link 命令。</summary>
    public LinkCommand() : base("link", "重新为已安装工具创建符号链接")
    {
        var nameArg = new Argument<string>("name") { Description = "工具名称" };
        Add(nameArg);

        SetAction(async (parseResult, ct) =>
        {
            var name = parseResult.GetValue(nameArg);

            try
            {
                await HandleAsync(name!, ct);
                return 0;
            }
            catch (Exception ex)
            {
                CommandHelpers.WriteError(ex);
                return 1;
            }
        });
    }

    private static async Task HandleAsync(string name, CancellationToken ct)
    {
        var manifest = new ManifestService();
        var tool = await manifest.FindToolAsync(name, ct);

        if (tool == null)
        {
            AnsiConsole.MarkupLine($"[red]✗ {name} 未安装[/]");
            return;
        }

        if (!Directory.Exists(tool.InstallPath))
        {
            AnsiConsole.MarkupLine($"[red]✗ {name} 安装目录不存在: {tool.InstallPath}[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[grey]重新链接 {name} {tool.Version}...[/]");

        var innerManifest = new InnerManifestService();
        var innerEntry = innerManifest.FindEntry(tool.Repo);
        var platform = PlatformInfo.Current();

        var innerLinkPaths = InnerManifestService.GetLinkPaths(innerEntry, platform);
        if (innerLinkPaths != null)
            InstallCommand.LinkPaths(tool.InstallPath, name, innerLinkPaths);
        else
            InstallCommand.LinkToBinDir(tool.InstallPath, name);

        AnsiConsole.MarkupLine($"[green]✓ {name} 链接已重建[/]");
    }
}
