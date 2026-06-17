using System.CommandLine;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Spectre.Console;
using GitPkg.Models;
using GitPkg.Services;

namespace GitPkg.Commands;

public static class SelfUpdateCommand
{
    private const string Owner = "wangbin1989";
    private const string Repo = "gitpkg";

    public static Command Create()
    {
        var cmd = new Command("self-update", "更新 GitPkg 自身到最新版本");

        cmd.SetAction(async (parseResult, ct) =>
        {
            try
            {
                await HandleAsync(ct);
                return 0;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("Not Found") || ex.Message.Contains("资源不存在"))
            {
                AnsiConsole.MarkupLine($"[red]✗ 未找到 GitPkg 的最新 Release[/]");
                return 1;
            }
            catch (HttpRequestException ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ 网络错误: {ex.Message}[/]");
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

    private static async Task HandleAsync(CancellationToken ct)
    {
        var gitHub = new GitHubService(GitPkgApp.Http);
        var matcher = new AssetMatcher();
        var extractor = new ArchiveExtractor();
        var verifier = new Sha256Verifier();

        var currentVersion = GetCurrentVersion();
        AnsiConsole.MarkupLine($"[grey]当前版本: {currentVersion}[/]");

        var release = await gitHub.GetLatestReleaseAsync(Owner, Repo, ct);
        var latestVersion = release.TagName;
        AnsiConsole.MarkupLine($"[grey]最新版本: {latestVersion}[/]");

        if (currentVersion == latestVersion)
        {
            AnsiConsole.MarkupLine($"[green]✓ GitPkg 已是最新版本[/]");
            return;
        }

        if (!IsNewer(latestVersion, currentVersion))
        {
            AnsiConsole.MarkupLine($"[green]✓ GitPkg 已是最新版本 (当前 {currentVersion} > 最新 {latestVersion})[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[blue]发现新版本: {currentVersion} → {latestVersion}[/]");

        var platform = PlatformInfo.Current();
        var matches = matcher.Match(release.Assets, platform);

        GitHubAsset selected;

        if (matches.Count == 0)
        {
            if (release.Assets.Count == 0)
                throw new InvalidOperationException("Release 中无可用资产");

            AnsiConsole.MarkupLine($"[yellow]⚠ 未找到匹配 {platform} 的资产，手动选择:[/]");
            selected = CommandHelpers.PromptAssetSelection(release.Assets);
        }
        else if (matches.Count == 1)
        {
            selected = matches[0];
        }
        else
        {
            selected = matches[0];
            AnsiConsole.MarkupLine($"[grey]多个匹配，自动选择: {selected.Name}[/]");
        }

        AnsiConsole.MarkupLine($"[grey]下载 {selected.Name} ({CommandHelpers.FormatSize(selected.Size)})...[/]");
        var tmpDir = ManifestService.GetTmpDir();
        Directory.CreateDirectory(tmpDir);
        var archivePath = Path.Combine(tmpDir, selected.Name);

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
                AnsiConsole.Markup("[grey]校验 SHA256...[/]");
                if (!verifier.Verify(archivePath, expectedHash))
                {
                    File.Delete(archivePath);
                    throw new InvalidOperationException("SHA256 校验失败");
                }
                AnsiConsole.MarkupLine("[green] ✓[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]⚠ 未找到校验文件，跳过完整性校验[/]");
        }

        var extractDir = Path.Combine(tmpDir, "self-update");
        if (Directory.Exists(extractDir))
            Directory.Delete(extractDir, recursive: true);
        Directory.CreateDirectory(extractDir);

        AnsiConsole.MarkupLine("[grey]解压...[/]");
        await extractor.ExtractAsync(archivePath, extractDir, ct);

        File.Delete(archivePath);

        var currentExePath = Environment.ProcessPath;
        if (currentExePath == null)
            throw new InvalidOperationException("无法确定当前程序路径");

        var exeName = Path.GetFileName(currentExePath);
        var newBinary = FindBinary(extractDir, exeName);
        if (newBinary == null)
            throw new InvalidOperationException($"在归档文件中未找到 {exeName}");

        AnsiConsole.MarkupLine("[grey]替换二进制文件...[/]");

        if (OperatingSystem.IsWindows())
            ReplaceOnWindows(newBinary, currentExePath);
        else
            ReplaceOnUnix(newBinary, currentExePath);

        Directory.Delete(extractDir, recursive: true);

        AnsiConsole.MarkupLine($"[green]✓ GitPkg 已更新到 {latestVersion}[/]");
    }

    private static string GetCurrentVersion()
    {
        var attr = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attr != null && !string.IsNullOrEmpty(attr.InformationalVersion))
        {
            var v = attr.InformationalVersion.Split('+')[0];
            if (!v.StartsWith('v'))
                v = "v" + v;
            return v;
        }

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
            return $"v{version.Major}.{version.Minor}.{version.Build}";

        return "v0.0.0";
    }

    private static bool IsNewer(string latest, string current)
    {
        var l = ParseVersion(latest);
        var c = ParseVersion(current);
        if (l == null || c == null) return latest != current;

        for (int i = 0; i < Math.Min(l.Length, c.Length); i++)
        {
            if (l[i] > c[i]) return true;
            if (l[i] < c[i]) return false;
        }

        return l.Length > c.Length;
    }

    private static int[]? ParseVersion(string v)
    {
        if (v.StartsWith('v') || v.StartsWith('V'))
            v = v[1..];

        var parts = v.Split('.');
        var result = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out var n))
                return null;
            result[i] = n;
        }

        return result;
    }

    private static string? FindBinary(string extractDir, string exeName)
    {
        var direct = Path.Combine(extractDir, exeName);
        if (File.Exists(direct)) return direct;

        foreach (var f in Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(f).Equals(exeName, StringComparison.OrdinalIgnoreCase))
                return f;
        }

        return null;
    }

    private static void ReplaceOnUnix(string newBinary, string currentPath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            File.SetUnixFileMode(newBinary,
                UnixFileMode.UserRead | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }

        var oldPath = currentPath + ".old";
        if (File.Exists(oldPath))
            File.Delete(oldPath);

        File.Move(currentPath, oldPath);
        File.Move(newBinary, currentPath);

        try { File.Delete(oldPath); } catch { /* best effort cleanup */ }
    }

    private static void ReplaceOnWindows(string newBinary, string currentPath)
    {
        var dir = Path.GetDirectoryName(currentPath)!;
        var exeName = Path.GetFileName(currentPath);
        var newPath = currentPath + ".new";

        File.Move(newBinary, newPath, overwrite: true);

        var batchPath = Path.Combine(dir, "gitpkg-update.bat");
        File.WriteAllText(batchPath, $"""
@echo off
timeout /t 2 /nobreak >nul
move /Y "{currentPath}" "{currentPath}.old" 2>nul
move /Y "{newPath}" "{currentPath}" 2>nul
del "{currentPath}.old" 2>nul
del "{batchPath}" 2>nul
""");

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{batchPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }

}
