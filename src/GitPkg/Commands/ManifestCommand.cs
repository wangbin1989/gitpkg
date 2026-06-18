using System.CommandLine;
using System.Text.Json;
using Spectre.Console;
using GitPkg.Models;
using GitPkg.Services;

namespace GitPkg.Commands;

/// <summary>
/// manifest 命令组：清单管理（导出等），将 manifest.json 内容序列化为 JSON 输出。
/// </summary>
public static class ManifestCommand
{
    /// <summary>创建 manifest 命令组。</summary>
    public static Command Create()
    {
        var cmd = new Command("manifest", "清单管理");

        cmd.Add(CreateExportCommand());

        return cmd;
    }

    private static Command CreateExportCommand()
    {
        var cmd = new Command("export", "导出清单文件到标准输出");

        cmd.SetAction(async (parseResult, ct) =>
        {
            try
            {
                var manifest = new ManifestService();
                var tools = await manifest.LoadAsync(ct);

                var jsonContext = new AppJsonContext();
                var json = JsonSerializer.Serialize(tools, jsonContext.ToolManifest);
                Console.WriteLine(json);
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
}
