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
        var filtered = assets.Where(a => !IsAuxiliaryAsset(a.Name)).ToList();
        if (filtered.Count == 0) filtered = assets;

        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            var first = filtered[0];
            AnsiConsole.MarkupLine($"[grey]非交互模式，自动选择 [bold]{first.Name}[/][/]");
            return first;
        }

        var choices = filtered.Select(a => $"{a.Name} ({FormatSize(a.Size)})").ToArray();
        var prompt = new SelectionPrompt<string>()
            .Title("选择要安装的资产")
            .AddChoices(choices);
        var chosen = AnsiConsole.Prompt(prompt);
        return filtered[Array.IndexOf(choices, chosen)];
    }

    /// <summary>判断是否为辅助文件（校验、签名、源码归档、安装包等），不应出现在安装选择中。</summary>
    private static bool IsAuxiliaryAsset(string name)
    {
        var n = name.ToLowerInvariant();
        // 校验文件
        if (n.EndsWith(".sha256") || n.EndsWith(".sha512")
            || n is "checksums.txt" or "sha256sums" or "sha256sums.txt")
            return true;
        // 签名文件
        if (n.EndsWith(".sig") || n.EndsWith(".asc") || n.EndsWith(".minisig"))
            return true;
        // 源码归档
        if (n.Contains("source code") || n.Contains("source-code"))
            return true;
        // 安装包
        if (n.EndsWith(".msi") || n.EndsWith(".deb") || n.EndsWith(".rpm")
            || n.EndsWith(".pkg") || n.EndsWith(".dmg") || n.EndsWith(".appimage")
            || n.EndsWith(".snap") || n.EndsWith(".flatpak") || n.EndsWith(".apk"))
            return true;
        return false;
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
    /// 统一的资产选择逻辑：优先使用已记录的资产名称匹配（替换版本号），
    /// 其次按平台匹配，最后回退到手动选择。
    /// </summary>
    /// <param name="assets">Release 的全部资产。</param>
    /// <param name="matches">平台匹配的资产子集。</param>
    /// <param name="platform">当前平台信息。</param>
    /// <param name="savedAssetName">已记录的资产名称（可为 null）。</param>
    /// <param name="oldVersion">旧版本标签（可为 null）。</param>
    /// <param name="newVersion">新版本标签（可为 null）。</param>
    /// <returns>选中的资产。</returns>
    public static GitHubAsset SelectAsset(
        List<GitHubAsset> assets, List<GitHubAsset> matches,
        PlatformInfo platform, string? savedAssetName,
        string? oldVersion = null, string? newVersion = null)
    {
        // 优先使用已记录的资产名称（替换版本号后匹配）
        if (savedAssetName != null)
        {
            var expectedName = oldVersion != null && newVersion != null
                ? savedAssetName.Replace(oldVersion, newVersion, StringComparison.OrdinalIgnoreCase)
                : savedAssetName;
            var saved = matches.FirstOrDefault(a =>
                a.Name.Equals(expectedName, StringComparison.OrdinalIgnoreCase));
            if (saved != null)
            {
                AnsiConsole.MarkupLine($"[grey]  自动选择已记录的资产: {saved.Name}[/]");
                return saved;
            }
        }

        // 过滤辅助文件（校验、签名、源码归档、安装包等）
        var filtered = matches.Where(a => !IsAuxiliaryAsset(a.Name)).ToList();

        // 平台匹配
        if (filtered.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠ 未找到匹配 {platform} 的资产[/]");
            AnsiConsole.MarkupLine("[grey]以下为全部可用资产，请手动选择:[/]");
            return PromptAssetSelection(assets);
        }

        if (filtered.Count == 1)
            return filtered[0];

        AnsiConsole.MarkupLine($"[yellow]发现 {filtered.Count} 个匹配的资产，请选择:[/]");
        return PromptAssetSelection(filtered);
    }

    /// <summary>去除文件名中的平台和架构信息，保留基础名称和扩展名。</summary>
    /// <remarks>
    /// 示例：my-tool-windows-amd64.exe → my-tool.exe，my-tool_linux_arm64 → my-tool
    /// </remarks>
    public static string StripPlatformSuffix(string fileName)
    {
        // 提取已知扩展名，避免版本号中的点号被误识别为扩展名分隔符
        var ext = GetKnownExtension(fileName);
        var nameWithoutExt = ext.Length > 0
            ? fileName[..^ext.Length]
            : fileName;

        // 常见平台和架构关键词（全小写匹配，长变体在前避免子串误匹配）
        var tokens = new[]
        {
            // 平台
            "windows", "win32", "win64", "linux", "darwin", "macos", "osx", "freebsd", "android",
            // 架构（长变体优先：x86_64 > x86, aarch64 > arm64 > arm, i686 > i386 > 386）
            "x86_64", "amd64", "aarch64", "arm64", "arm", "x64", "i686", "i386", "386", "x86",
            // 变体
            "musl", "gnu", "static", "portable"
        };

        // 匹配分隔符 + 关键词的模式，从右向左逐步裁剪
        var result = nameWithoutExt;
        var lowered = result.ToLowerInvariant();

        foreach (var token in tokens)
        {
            // 尝试匹配 -token 或 _token
            var patterns = new[] { $"-{token}", $"_{token}" };
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

    /// <summary>提取已知文件扩展名，避免将版本号中的点号误识别为扩展名分隔符。</summary>
    private static string GetKnownExtension(string fileName)
    {
        var knownExtensions = new[]
        {
            ".tar.gz", ".tar.xz", ".tar.bz2", ".tar.zst",
            ".exe", ".msi", ".bat", ".cmd", ".ps1",
            ".zip", ".gz", ".xz", ".bz2", ".zst", ".7z", ".rar",
            ".sh", ".bash", ".zsh", ".fish",
            ".deb", ".rpm", ".apk", ".dmg", ".pkg", ".appimage",
            ".dll", ".so", ".dylib", ".bin",
            ".txt", ".md", ".json", ".yaml", ".yml", ".toml", ".xml",
            ".sha256", ".sha512"
        };

        var lowered = fileName.ToLowerInvariant();
        foreach (var ext in knownExtensions)
        {
            if (lowered.EndsWith(ext))
                return fileName[^ext.Length..];
        }

        return "";
    }
}
