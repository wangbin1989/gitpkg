using System.CommandLine;
using System.Text.Json;
using Spectre.Console;
using GitPkg.Models;
using GitPkg.Services;

namespace GitPkg.Commands;

public static class ManifestCommand
{
    public static Command Create()
    {
        var cmd = new Command("manifest", "清单管理");

        cmd.AddCommand(CreateExportCommand());

        return cmd;
    }

    private static Command CreateExportCommand()
    {
        var cmd = new Command("export", "导出清单文件到标准输出");

        cmd.SetHandler(async context =>
        {
            var ct = context.GetCancellationToken();

            try
            {
                var manifest = new ManifestService();
                var tools = await manifest.LoadAsync(ct);

                var jsonContext = new AppJsonContext();
                var json = JsonSerializer.Serialize(tools, jsonContext.ToolManifest);
                Console.WriteLine(json);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ 错误: {ex.Message}[/]");
                context.ExitCode = 1;
            }
        });

        return cmd;
    }
}
