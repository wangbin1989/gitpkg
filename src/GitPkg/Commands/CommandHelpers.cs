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

    /// <summary>去除文件名中的平台和架构信息，保留基础名称和扩展名。</summary>
    /// <remarks>
    /// 示例：my-tool-windows-amd64.exe → my-tool.exe，my-tool_linux_arm64 → my-tool
    /// </remarks>
    public static string StripPlatformSuffix(string fileName)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);

        // 常见平台和架构关键词（全小写匹配）
        var tokens = new[]
        {
            // 平台
            "windows", "win32", "win64", "linux", "darwin", "macos", "osx", "freebsd", "android",
            // 架构
            "amd64", "x86_64", "x86", "x64", "arm64", "aarch64", "arm", "386", "i386", "i686",
            // 变体
            "musl", "gnu", "static", "portable"
        };

        // 匹配分隔符 + 关键词的模式（单词边界），从右向左逐步裁剪
        var result = nameWithoutExt;
        var lowered = result.ToLowerInvariant();

        foreach (var token in tokens)
        {
            // 尝试匹配 -token、_token 或 token 在末尾的情况
            var patterns = new[] { $"-{token}", $"_{token}", $".{token}" };
            foreach (var pattern in patterns)
            {
                int idx;
                while ((idx = lowered.LastIndexOf(pattern, StringComparison.Ordinal)) >= 0)
                {
                    // 裁剪到该位置
                    result = result[..idx];
                    lowered = lowered[..idx];
                }
            }

            // 文件名以 token 开头的情况（如 amd64-my-tool）
            if (lowered.StartsWith(token + "-") || lowered.StartsWith(token + "_"))
            {
                result = result[(token.Length + 1)..];
                lowered = lowered[(token.Length + 1)..];
            }
        }

        // 避免返回空字符串
        if (string.IsNullOrWhiteSpace(result))
            return fileName;

        return result + ext;
    }
}
