using System.CommandLine;
using Spectre.Console;
using GitPkg.Models;
using GitPkg.Services;

namespace GitPkg.Commands;

public static class InstallCommand
{
    public static Command Create()
    {
        var cmd = new Command("install", "从 GitHub Release 安装工具");

        var repoArg = new Argument<string>("owner/repo", "GitHub 仓库 (owner/repo[@version])");
        cmd.AddArgument(repoArg);

        var dirOpt = new Option<string?>("--dir", "自定义安装目录");
        dirOpt.AddAlias("-d");
        cmd.AddOption(dirOpt);

        var addPathOpt = new Option<bool>("--add-path", "将工具目录加入 PATH 环境变量");
        cmd.AddOption(addPathOpt);

        cmd.SetHandler(async context =>
        {
            var repo = context.ParseResult.GetValueForArgument(repoArg);
            var dir = context.ParseResult.GetValueForOption(dirOpt);
            var addPath = context.ParseResult.GetValueForOption(addPathOpt);
            var ct = context.GetCancellationToken();

            try
            {
                await HandleAsync(repo, dir, addPath, ct);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("Not Found") || ex.Message.Contains("资源不存在"))
            {
                AnsiConsole.MarkupLine($"[red]✗ 仓库 {repo} 不存在[/]");
                context.ExitCode = 1;
            }
            catch (HttpRequestException ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ 网络错误: {ex.Message}[/]");
                context.ExitCode = 1;
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[yellow]操作已取消[/]");
                context.ExitCode = 1;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ 错误: {ex.Message}[/]");
                context.ExitCode = 1;
            }
        });

        return cmd;
    }

    private static async Task HandleAsync(string repo, string? dir, bool addPath, CancellationToken ct)
    {
        var gitHub = new GitHubService(GitPkgApp.Http);
        var matcher = new AssetMatcher();
        var extractor = new ArchiveExtractor();
        var verifier = new Sha256Verifier();
        var manifest = new ManifestService();

        // 1. Parse owner/repo[@version]
        var (owner, repoName, version) = ParseRepo(repo);
        var toolName = repoName;

        // 2. Get release
        GitHubRelease release;
        if (version != null)
        {
            release = await gitHub.GetReleaseByTagAsync(owner, repoName, version, ct);
        }
        else
        {
            release = await gitHub.GetLatestReleaseAsync(owner, repoName, ct);
        }

        // 3. Get platform info and match assets
        var platform = PlatformInfo.Current();
        var matches = matcher.Match(release.Assets, platform);

        if (matches.Count == 0)
        {
            AnsiConsole.MarkupLine($"[red]✗ 未找到适用于 {platform} 的资产[/]");
            AnsiConsole.MarkupLine($"[grey]  Release 包含 {release.Assets.Count} 个资产:[/]");
            foreach (var a in release.Assets)
                AnsiConsole.MarkupLine($"    [grey]- {a.Name}[/]");
            return;
        }

        // 4. Select asset
        GitHubAsset selected;
        if (matches.Count == 1)
        {
            selected = matches[0];
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]发现 {matches.Count} 个匹配的资产，请选择:[/]");
            var choices = matches.Select(m => $"{m.Name} ({FormatSize(m.Size)})").ToArray();
            var prompt = new SelectionPrompt<string>()
                .Title("选择要安装的资产")
                .AddChoices(choices);
            var chosen = AnsiConsole.Prompt(prompt);
            selected = matches[Array.IndexOf(choices, chosen)];
        }

        var installDir = dir ?? ManifestService.GetToolDir(toolName);
        var tmpDir = ManifestService.GetTmpDir();
        Directory.CreateDirectory(tmpDir);

        var archivePath = Path.Combine(tmpDir, selected.Name);

        // 5. Download
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[green]下载 {selected.Name}[/]", maxValue: selected.Size > 0 ? selected.Size : 100);
                var lastUpdate = DateTime.MinValue;

                await gitHub.DownloadFileAsync(selected.DownloadUrl, archivePath,
                    onProgress: (downloaded, total) =>
                    {
                        var now = DateTime.UtcNow;
                        if ((now - lastUpdate).TotalMilliseconds < 100) return;
                        lastUpdate = now;

                        if (total > 0)
                        {
                            task.MaxValue(total);
                            task.Value(downloaded);
                        }
                        else
                        {
                            task.MaxValue(downloaded + 8192);
                            task.Value(downloaded);
                        }
                    }, ct: ct);

                task.Value(task.MaxValue);
            });

        // 6. SHA256 verification
        await VerifyChecksumAsync(gitHub, verifier, release.Assets, selected.Name, archivePath, ct);

        // 7. Extract
        AnsiConsole.MarkupLine($"[grey]解压到 {installDir}...[/]");
        if (Directory.Exists(installDir))
            Directory.Delete(installDir, recursive: true);

        await extractor.ExtractAsync(archivePath, installDir, ct);

        // Clean up archive
        if (File.Exists(archivePath))
            File.Delete(archivePath);

        // 8. Handle nested directory (common pattern: archive contains single dir with same name)
        var bins = FindExecutables(installDir);
        if (bins.Count == 0)
        {
            // Try one level of nesting
            var subDirs = Directory.GetDirectories(installDir);
            if (subDirs.Length == 1)
            {
                var inner = subDirs[0];
                foreach (var f in Directory.GetFiles(inner))
                    File.Move(f, Path.Combine(installDir, Path.GetFileName(f)));
                foreach (var d in Directory.GetDirectories(inner))
                    Directory.Move(d, Path.Combine(installDir, Path.GetFileName(d)));
                Directory.Delete(inner);
            }
        }

        // 9. Update manifest
        await manifest.AddToolAsync(new ToolEntry
        {
            Name = toolName,
            Repo = $"{owner}/{repoName}",
            Version = release.TagName,
            InstallPath = installDir,
            InstalledAt = DateTime.UtcNow
        }, ct);

        // 10. PATH setup
        if (addPath)
        {
            var shell = PathService.DetectShell() ?? "bash";
            var added = PathService.AddToPath(installDir, shell);
            if (added)
            {
                var configFile = PathService.GetConfigFilePath(shell);
                AnsiConsole.MarkupLine($"[blue]ℹ PATH 已更新，请执行 source {configFile} 或重新打开终端[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[grey]  PATH 中已存在该目录，跳过[/]");
            }
        }

        // 11. Success
        var versionDisplay = release.Name ?? release.TagName;
        AnsiConsole.MarkupLine($"[green]✓ {toolName} {versionDisplay} 已安装到 {installDir}[/]");
    }

    private static (string owner, string repo, string? version) ParseRepo(string input)
    {
        string? version = null;
        var rest = input;

        // Check for @version suffix
        var atIndex = input.LastIndexOf('@');
        if (atIndex > 0)
        {
            version = input[(atIndex + 1)..];
            rest = input[..atIndex];
        }

        var parts = rest.Split('/');
        if (parts.Length != 2)
            throw new ArgumentException($"无效的仓库格式 '{input}'，应为 owner/repo");

        return (parts[0], parts[1], version);
    }

    private static async Task VerifyChecksumAsync(
        GitHubService gitHub, Sha256Verifier verifier,
        List<GitHubAsset> assets, string targetName, string archivePath,
        CancellationToken ct)
    {
        var checksumAsset = assets.FirstOrDefault(a =>
        {
            var n = a.Name.ToLowerInvariant();
            return n.EndsWith(".sha256") || n == "checksums.txt" || n == "sha256sums" || n == "sha256sums.txt";
        });

        if (checksumAsset == null)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ 未找到 SHA256 校验文件，跳过完整性校验[/]");
            return;
        }

        var content = await gitHub.DownloadStringAsync(checksumAsset.DownloadUrl, ct);
        var expectedHash = Sha256Verifier.ParseChecksum(content, targetName);

        if (expectedHash == null)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ 校验文件中未找到当前资产的校验和[/]");
            return;
        }

        AnsiConsole.Markup("[grey]校验 SHA256...[/]");
        var actualHash = await verifier.ComputeHashAsync(archivePath, ct);

        if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(archivePath))
                File.Delete(archivePath);

            throw new InvalidOperationException(
                $"SHA256 校验失败！\n  期望: {expectedHash}\n  实际: {actualHash}");
        }

        AnsiConsole.MarkupLine("[green] ✓[/]");
    }

    private static List<string> FindExecutables(string dir)
    {
        if (!Directory.Exists(dir)) return [];

        var bins = new List<string>();
        foreach (var file in Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
            // Skip common non-executable files
            if (name is "license" or "readme" or "changelog" or "copying")
                continue;

            // On Windows, check .exe/.bat/.cmd
            if (OperatingSystem.IsWindows())
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is ".exe" or ".bat" or ".cmd")
                    bins.Add(file);
            }
            else
            {
                // Unix: check if file is executable or has no extension (common for Unix binaries)
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext == "" || ext == ".sh")
                    bins.Add(file);
            }
        }

        return bins;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F1} GB",
        >= 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        >= 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes} B"
    };
}
