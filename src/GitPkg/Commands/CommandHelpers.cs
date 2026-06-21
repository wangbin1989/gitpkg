using GitPkg.Models;
using Spectre.Console;

namespace GitPkg.Commands;

/// <summary>
/// 命令通用辅助方法。提供交互式资产选择和格式化输出。
/// </summary>
public static class CommandHelpers
{
    /// <summary>
    /// 显示交互式选择面板，让用户从资产列表中手动选择一项。
    /// 在非交互式终端中自动选择第一项。
    /// </summary>
    public static GitHubAsset PromptAssetSelection(List<GitHubAsset> assets)
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

    /// <summary>将字节数格式化为人类可读的大小字符串（B / KB / MB / GB）。</summary>
    public static string FormatSize(long bytes) => bytes switch
    {
        >= 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F1} GB",
        >= 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        >= 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes} B"
    };

    /// <summary>
    /// 统一的资产选择逻辑：优先使用已记录的资产名称匹配，
    /// 其次按平台匹配，最后回退到手动选择。
    /// </summary>
    /// <param name="assets">Release 的全部资产。</param>
    /// <param name="matches">平台匹配的资产子集。</param>
    /// <param name="platform">当前平台信息。</param>
    /// <param name="savedAssetName">已记录的资产名称（可为 null）。</param>
    /// <returns>选中的资产。</returns>
    public static GitHubAsset SelectAsset(
        List<GitHubAsset> assets, List<GitHubAsset> matches,
        PlatformInfo platform, string? savedAssetName)
    {
        // 优先使用已记录的资产名称（必须同时通过平台匹配）
        if (savedAssetName != null)
        {
            var saved = matches.FirstOrDefault(a =>
                a.Name.Equals(savedAssetName, StringComparison.OrdinalIgnoreCase));
            if (saved != null)
            {
                AnsiConsole.MarkupLine($"[grey]  自动选择已记录的资产: {saved.Name}[/]");
                return saved;
            }
        }

        // 平台匹配
        if (matches.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠ 未找到匹配 {platform} 的资产[/]");
            AnsiConsole.MarkupLine("[grey]以下为全部可用资产，请手动选择:[/]");
            return PromptAssetSelection(assets);
        }

        if (matches.Count == 1)
            return matches[0];

        AnsiConsole.MarkupLine($"[yellow]发现 {matches.Count} 个匹配的资产，请选择:[/]");
        return PromptAssetSelection(matches);
    }

    /// <summary>
    /// 从 Release 资产列表中查找 SHA256 校验文件。
    /// 支持 .sha256、checksums.txt、sha256sums 等常见命名。
    /// </summary>
    /// <returns>校验文件资产，未找到时返回 null。</returns>
    public static GitHubAsset? FindChecksumAsset(List<GitHubAsset> assets)
    {
        return assets.FirstOrDefault(a =>
        {
            var n = a.Name.ToLowerInvariant();
            return n.EndsWith(".sha256") || n == "checksums.txt"
                || n == "sha256sums" || n == "sha256sums.txt";
        });
    }
}
