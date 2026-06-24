using System.CommandLine;
using Spectre.Console;
using GitPkg.Services;

namespace GitPkg.Commands;

/// <summary>
/// uninstall 命令：删除已安装工具的文件并从清单中移除。
/// </summary>
public static class UninstallCommand
{
    /// <summary>创建 uninstall 命令。</summary>
    public static Command Create()
    {
        var cmd = new Command("uninstall", "卸载已安装的工具");

        var nameArg = new Argument<string>("name") { Description = "工具名称" };
        cmd.Add(nameArg);

        cmd.SetAction(async (parseResult, ct) =>
        {
            var name = parseResult.GetValue(nameArg);

            try
            {
                await HandleAsync(name!, ct);
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

        // 在删除目录之前，找出已链接到 bin 的可执行文件名称
        var linkedNames = new List<string>();
        if (Directory.Exists(tool.InstallPath))
        {
            var exeDir = ExecutableFinder.FindExecutableDir(tool.InstallPath);
            var executables = ExecutableFinder.FindExecutables(exeDir);
            // 单个可执行文件时使用去平台后缀的名称（与 LinkToBinDir 一致）
            linkedNames.AddRange(executables.Select(f =>
                executables.Count == 1
                    ? CommandHelpers.StripPlatformSuffix(Path.GetFileName(f)!)
                    : Path.GetFileName(f)!));

            Directory.Delete(tool.InstallPath, recursive: true);
        }

        // 清理 ~/.gitpkg/bin/ 中的符号链接
        var binDir = ManifestService.GetBinDir();
        if (Directory.Exists(binDir))
        {
            foreach (var name2 in linkedNames)
            {
                var linkPath = Path.Combine(binDir, name2);
                if (File.Exists(linkPath))
                    File.Delete(linkPath);
            }
        }

        // Remove from manifest
        await manifest.RemoveToolAsync(name, ct);

        AnsiConsole.MarkupLine($"[green]✓ {name} 已卸载[/]");
    }
}
