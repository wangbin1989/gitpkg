using System.CommandLine;
using Spectre.Console;
using GitPkg.Models;
using GitPkg.Services;

namespace GitPkg.Commands;

public static class UpdateCommand
{
    public static Command Create()
    {
        var cmd = new Command("update", "更新已安装的工具");

        var nameArg = new Argument<string?>("name", () => null, "工具名称（不指定则更新全部）");
        nameArg.Arity = ArgumentArity.ZeroOrOne;
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

    private static async Task HandleAsync(string? name, CancellationToken ct)
    {
        var gitHub = new GitHubService(GitPkgApp.Http);
        var matcher = new AssetMatcher();
        var extractor = new ArchiveExtractor();
        var verifier = new Sha256Verifier();
        var manifest = new ManifestService();

        var tools = await manifest.LoadAsync(ct);

        if (tools.Tools.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]暂无已安装工具[/]");
            return;
        }

        var toUpdate = name != null
            ? tools.Tools.Where(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList()
            : tools.Tools;

        if (name != null && toUpdate.Count == 0)
        {
            AnsiConsole.MarkupLine($"[red]✗ {name} 未安装[/]");
            return;
        }

        var updated = 0;
        var currentCount = 0;
        var failed = 0;

        foreach (var tool in toUpdate)
        {
            try
            {
                var parts = tool.Repo.Split('/');
                if (parts.Length != 2)
                {
                    AnsiConsole.MarkupLine($"[red]✗ {tool.Name}: 无效的仓库格式[/]");
                    failed++;
                    continue;
                }

                var release = await gitHub.GetLatestReleaseAsync(parts[0], parts[1], ct);

                if (release.TagName == tool.Version)
                {
                    AnsiConsole.MarkupLine($"[grey]= {tool.Name} {tool.Version} (已是最新)[/]");
                    currentCount++;
                    continue;
                }

                var platform = PlatformInfo.Current();
                var matches = matcher.Match(release.Assets, platform);

                GitHubAsset selected;

                if (matches.Count == 0)
                {
                    if (release.Assets.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[yellow]= {tool.Name}: 新版本 {release.TagName} 无可用资产，跳过[/]");
                        currentCount++;
                        continue;
                    }

                    AnsiConsole.MarkupLine($"[yellow]⚠ {tool.Name}: 未找到匹配 {platform} 的资产，手动选择:[/]");
                    selected = PromptAssetSelection(release.Assets);
                }
                else if (matches.Count == 1)
                {
                    selected = matches[0];
                }
                else
                {
                    selected = matches[0];
                }
                var tmpDir = ManifestService.GetTmpDir();
                Directory.CreateDirectory(tmpDir);
                var archivePath = Path.Combine(tmpDir, selected.Name);

                // Download
                await gitHub.DownloadFileAsync(selected.DownloadUrl, archivePath, ct: ct);

                // Verify
                var checksumAsset = release.Assets.FirstOrDefault(a =>
                {
                    var n = a.Name.ToLowerInvariant();
                    return n.EndsWith(".sha256") || n == "checksums.txt" || n == "sha256sums" || n == "sha256sums.txt";
                });
                if (checksumAsset != null)
                {
                    var content = await gitHub.DownloadStringAsync(checksumAsset.DownloadUrl, ct);
                    var expectedHash = Sha256Verifier.ParseChecksum(content, selected.Name);
                    if (expectedHash != null)
                    {
                        if (!verifier.Verify(archivePath, expectedHash))
                        {
                            if (File.Exists(archivePath))
                                File.Delete(archivePath);
                            throw new InvalidOperationException("SHA256 校验失败");
                        }
                    }
                }

                // Keep old dir as backup during extraction
                var backupDir = tool.InstallPath + ".bak";
                if (Directory.Exists(backupDir))
                    Directory.Delete(backupDir, recursive: true);

                if (Directory.Exists(tool.InstallPath))
                    Directory.Move(tool.InstallPath, backupDir);

                try
                {
                    await extractor.ExtractAsync(archivePath, tool.InstallPath, ct);

                    // Handle nested directory
                    var subDirs = Directory.GetDirectories(tool.InstallPath);
                    if (subDirs.Length == 1)
                    {
                        var inner = subDirs[0];
                        foreach (var f in Directory.GetFiles(inner))
                            File.Move(f, Path.Combine(tool.InstallPath, Path.GetFileName(f)));
                        foreach (var d in Directory.GetDirectories(inner))
                            Directory.Move(d, Path.Combine(tool.InstallPath, Path.GetFileName(d)));
                        Directory.Delete(inner);
                    }

                    if (Directory.Exists(backupDir))
                        Directory.Delete(backupDir, recursive: true);
                }
                catch
                {
                    // Restore backup on failure
                    if (Directory.Exists(tool.InstallPath))
                        Directory.Delete(tool.InstallPath, recursive: true);
                    if (Directory.Exists(backupDir))
                        Directory.Move(backupDir, tool.InstallPath);
                    throw;
                }

                // Clean up archive
                if (File.Exists(archivePath))
                    File.Delete(archivePath);

                // Update manifest
                await manifest.AddToolAsync(tool with
                {
                    Version = release.TagName,
                    InstalledAt = DateTime.UtcNow
                }, ct);

                var versionDisplay = release.Name ?? release.TagName;
                AnsiConsole.MarkupLine($"[green]✓ {tool.Name} {tool.Version} → {versionDisplay}[/]");
                updated++;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ {tool.Name} 更新失败: {ex.Message}[/]");
                failed++;
            }
        }

        // Summary
        var summary = $"更新: {updated} | 已是最新: {currentCount}";
        if (failed > 0) summary += $" | 失败: {failed}";
        AnsiConsole.MarkupLine($"[bold]{summary}[/]");
    }

    private static GitHubAsset PromptAssetSelection(List<GitHubAsset> assets)
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            var first = assets[0];
            AnsiConsole.MarkupLine($"[grey]非交互模式，自动选择 [bold]{first.Name}[/][/]");
            return first;
        }

        var choices = assets.Select(a => $"{a.Name} ({FormatSize(a.Size)})").ToArray();
        var prompt = new SelectionPrompt<string>()
            .Title("选择要安装的资产")
            .AddChoices(choices);
        var chosen = AnsiConsole.Prompt(prompt);
        return assets[Array.IndexOf(choices, chosen)];
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F1} GB",
        >= 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        >= 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes} B"
    };
}
