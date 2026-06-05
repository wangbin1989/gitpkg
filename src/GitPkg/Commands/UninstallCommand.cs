using System.CommandLine;
using Spectre.Console;
using GitPkg.Services;

namespace GitPkg.Commands;

public static class UninstallCommand
{
    public static Command Create()
    {
        var cmd = new Command("uninstall", "卸载已安装的工具");

        var nameArg = new Argument<string>("name", "工具名称");
        cmd.AddArgument(nameArg);

        cmd.SetHandler(async context =>
        {
            var name = context.ParseResult.GetValueForArgument(nameArg);
            var ct = context.GetCancellationToken();

            try
            {
                await HandleAsync(name, ct);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ 错误: {ex.Message}[/]");
                context.ExitCode = 1;
            }
        });

        return cmd;
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

        AnsiConsole.MarkupLine($"[grey]卸载 {name} {tool.Version} ({tool.InstallPath})...[/]");

        if (Directory.Exists(tool.InstallPath))
        {
            Directory.Delete(tool.InstallPath, recursive: true);
        }

        // Remove from manifest
        await manifest.RemoveToolAsync(name, ct);

        AnsiConsole.MarkupLine($"[green]✓ {name} 已卸载[/]");
        AnsiConsole.MarkupLine("[grey]  提示: 请手动检查 shell 配置文件中的 PATH 条目是否需要清理[/]");
    }
}
