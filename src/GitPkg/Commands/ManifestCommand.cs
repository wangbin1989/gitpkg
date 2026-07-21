using System.CommandLine;
using System.Text.Json;
using Spectre.Console;
using GitPkg.Models;
using GitPkg.Services;

namespace GitPkg.Commands;

/// <summary>
/// manifest 命令组：清单管理（导出等），将 manifest.json 内容序列化为 JSON 输出。
/// </summary>
public class ManifestCommand : Command
{
    /// <summary>创建 manifest 命令组。</summary>
    public ManifestCommand() : base("manifest", "清单管理")
    {
        Add(new ExportCommand());
    }

    /// <summary>
    /// export 子命令：导出清单文件到标准输出。
    /// </summary>
    private class ExportCommand : Command
    {
        public ExportCommand() : base("export", "导出清单文件到标准输出")
        {
            SetAction(async (parseResult, ct) =>
            {
                try
                {
                    var manifest = new ManifestService();
                    var tools = await manifest.LoadAsync(ct);

                    var json = JsonSerializer.Serialize(tools, AppJsonContext.Default.ToolManifest);
                    Console.WriteLine(json);
                    return 0;
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗ 错误: {ex.Message}[/]");
                    return 1;
                }
            });
        }
    }
}
