using System.CommandLine;
using System.Text.Json;
using Spectre.Console;
using GitPkg.Models;
using GitPkg.Services;

namespace GitPkg.Commands;

/// <summary>
/// install 命令：从 GitHub Release 下载并安装工具。
/// 核心流程：解析仓库 → 获取 Release → 匹配资产 → 下载 → 校验 → 解压 → 链接到 bin → 记录清单。
/// </summary>
public static class InstallCommand
{
    /// <summary>创建 install 命令，支持单工具安装和批量清单安装。</summary>
    public static Command Create()
    {
        var cmd = new Command("install", "从 GitHub Release 安装工具");

        var repoArg = new Argument<string?>("owner/repo") { Description = "GitHub 仓库 (owner/repo[@version])", Arity = ArgumentArity.ZeroOrOne };
        cmd.Add(repoArg);

        var dirOpt = new Option<string?>("--dir", "-d") { Description = "自定义安装目录" };
        cmd.Add(dirOpt);

        var gpgOpt = new Option<string?>("--verify-gpg") { Description = "GPG 密钥 ID，用于签名校验" };
        cmd.Add(gpgOpt);

        var fromOpt = new Option<string?>("--from") { Description = "从清单文件批量安装" };
        cmd.Add(fromOpt);

        var dryRunOpt = new Option<bool>("--dry-run") { Description = "预览批量安装，不实际执行" };
        cmd.Add(dryRunOpt);

        cmd.SetAction(async (parseResult, ct) =>
        {
            var repo = parseResult.GetValue(repoArg);
            var dir = parseResult.GetValue(dirOpt);
            var gpgKey = parseResult.GetValue(gpgOpt);
            var fromFile = parseResult.GetValue(fromOpt);
            var dryRun = parseResult.GetValue(dryRunOpt);

            try
            {
                if (fromFile != null)
                    await HandleBatchAsync(fromFile, dryRun, ct);
                else if (repo != null)
                    await HandleSingleAsync(repo, dir, gpgKey, ct);
                else
                    throw new ArgumentException("请指定 owner/repo 或使用 --from <file>");
                return 0;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("Not Found") || ex.Message.Contains("资源不存在"))
            {
                AnsiConsole.MarkupLine($"[red]✗ 资源不存在: {ex.Message.Split(": ").Last()}[/]");
                return 1;
            }
            catch (HttpRequestException ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ 网络错误: {ex.Message}[/]");
                return 1;
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[yellow]操作已取消[/]");
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

    private static async Task HandleBatchAsync(string fromFile, bool dryRun, CancellationToken ct)
    {
        if (!File.Exists(fromFile))
            throw new FileNotFoundException($"清单文件不存在: {fromFile}");

        var jsonContext = new AppJsonContext();
        await using var stream = File.OpenRead(fromFile);
        var manifest = await JsonSerializer.DeserializeAsync(stream, jsonContext.ToolManifest, ct);
        if (manifest == null || manifest.Tools.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]清单文件为空[/]");
            return;
        }

        AnsiConsole.MarkupLine(dryRun
            ? $"[blue]预览模式 — 将从 {fromFile} 安装 {manifest.Tools.Count} 个工具[/]"
            : $"[blue]将从 {fromFile} 批量安装 {manifest.Tools.Count} 个工具[/]");

        var success = 0;
        var failed = 0;

        foreach (var tool in manifest.Tools)
        {
            var repo = tool.Repo.Contains('@') ? tool.Repo : $"{tool.Repo}@{tool.Version}";

            if (dryRun)
            {
                var installDir = ManifestService.GetToolDir(tool.Name);
                AnsiConsole.MarkupLine($"  [grey]→ {tool.Name} {tool.Version} ({tool.Repo}) → {installDir}[/]");
                success++;
            }
            else
            {
                try
                {
                    await InstallSingleAsync(repo, null, null, ct);
                    success++;
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"  [red]✗ {tool.Name}: {ex.Message}[/]");
                    failed++;
                }
            }
        }

        var summary = dryRun
            ? $"预览: {success} 个工具"
            : $"安装: {success} | 失败: {failed}";
        AnsiConsole.MarkupLine($"[bold]{summary}[/]");
    }

    private static async Task HandleSingleAsync(string repo, string? dir, string? gpgKey, CancellationToken ct)
    {
        await InstallSingleAsync(repo, dir, gpgKey, ct);
    }

    /// <summary>
    /// 安装单个工具的完整流程：
    /// 1. 解析 owner/repo[@version]
    /// 2. 获取 Release 信息
    /// 3. 平台匹配并选择资产
    /// 4. 下载归档文件
    /// 5. 可选的 GPG / SHA256 校验
    /// 6. 解压到安装目录
    /// 7. 链接可执行文件到 ~/.gitpkg/bin/
    /// 8. 更新 manifest.json
    /// </summary>
    private static async Task InstallSingleAsync(string repo, string? dir, string? gpgKey, CancellationToken ct)
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

        // 4. Select asset
        GitHubAsset selected;

        if (matches.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠ 未找到自动匹配 {platform} 的资产[/]");
            AnsiConsole.MarkupLine("[grey]以下为全部可用资产，请手动选择:[/]");
            selected = CommandHelpers.PromptAssetSelection(release.Assets);
        }
        else if (matches.Count == 1)
        {
            selected = matches[0];
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]发现 {matches.Count} 个匹配的资产，请选择:[/]");
            selected = CommandHelpers.PromptAssetSelection(matches);
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

        // 6. GPG verification (optional)
        if (gpgKey != null)
        {
            var sigAsset = GpgVerifier.FindSignatureAsset(release.Assets, selected.Name);
            if (sigAsset == null)
            {
                AnsiConsole.MarkupLine("[yellow]⚠ 未找到 GPG 签名文件，跳过签名校验[/]");
            }
            else
            {
                var sigPath = Path.Combine(tmpDir, sigAsset.Name);
                await gitHub.DownloadFileAsync(sigAsset.DownloadUrl, sigPath, ct: ct);

                AnsiConsole.Markup("[grey]校验 GPG 签名...[/]");
                var gpg = new GpgVerifier();
                var valid = await gpg.VerifyAsync(archivePath, sigPath, gpgKey, ct);

                File.Delete(sigPath);

                if (!valid)
                {
                    File.Delete(archivePath);
                    throw new InvalidOperationException("GPG 签名校验失败");
                }

                AnsiConsole.MarkupLine("[green] ✓[/]");
            }
        }

        // 7. SHA256 verification
        await VerifyChecksumAsync(gitHub, verifier, release.Assets, selected.Name, archivePath, ct);

        // 8. Extract
        AnsiConsole.MarkupLine($"[grey]解压到 {installDir}...[/]");
        if (Directory.Exists(installDir))
            Directory.Delete(installDir, recursive: true);

        await extractor.ExtractAsync(archivePath, installDir, ct);

        // Clean up archive
        if (File.Exists(archivePath))
            File.Delete(archivePath);

        // 9. Handle nested directory
        var bins = ExecutableFinder.FindExecutables(installDir);
        if (bins.Count == 0)
        {
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

        // 10. Link executables to ~/.gitpkg/bin/
        LinkToBinDir(installDir);

        // 11. Update manifest
        await manifest.AddToolAsync(new ToolEntry
        {
            Name = toolName,
            Repo = $"{owner}/{repoName}",
            Version = release.TagName,
            InstallPath = installDir,
            InstalledAt = DateTime.UtcNow
        }, ct);

        // 12. Success
        var versionDisplay = release.Name ?? release.TagName;
        AnsiConsole.MarkupLine($"[green]✓ {toolName} {versionDisplay} 已安装到 {installDir}[/]");
    }

    /// <summary>
    /// 解析仓库输入字符串，支持 owner/repo 和 owner/repo@version 两种格式。
    /// </summary>
    private static (string owner, string repo, string? version) ParseRepo(string input)
    {
        string? version = null;
        var rest = input;

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

    /// <summary>将安装目录中的可执行文件链接到 ~/.gitpkg/bin/。</summary>
    internal static void LinkToBinDir(string installDir)
    {
        var binDir = ManifestService.GetBinDir();
        Directory.CreateDirectory(binDir);

        var exeDir = ExecutableFinder.FindExecutableDir(installDir);
        var executables = ExecutableFinder.FindExecutables(exeDir);

        foreach (var exe in executables)
        {
            var fileName = Path.GetFileName(exe);
            var linkPath = Path.Combine(binDir, fileName);

            // 移除已存在的同名链接/文件（覆盖安装场景）
            if (File.Exists(linkPath))
                File.Delete(linkPath);

            File.CreateSymbolicLink(linkPath, exe);
        }

        if (executables.Count > 0)
            AnsiConsole.MarkupLine($"[grey]  已链接 {executables.Count} 个可执行文件到 {binDir}[/]");
    }

}
