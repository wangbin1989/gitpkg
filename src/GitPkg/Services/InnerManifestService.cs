using System.Text.Json;
using GitPkg.Models;

namespace GitPkg.Services;

/// <summary>
/// 内置清单服务，负责读取嵌入的 inner-manifest.json 并查询仓库配置。
/// </summary>
public class InnerManifestService
{
    private readonly List<InnerManifestTool>? _tools;

    /// <summary>初始化服务并加载嵌入资源。</summary>
    public InnerManifestService()
    {
        _tools = LoadEmbedded();
    }

    /// <summary>按 owner/repo 查找配置条目，未找到返回 null。</summary>
    public InnerManifestTool? FindEntry(string ownerRepo)
    {
        return _tools?.Find(t =>
            t.Repo.Equals(ownerRepo, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>获取工具名称：有 name 配置则使用 name，否则使用 repoName。</summary>
    public static string GetToolName(InnerManifestTool? entry, string repoName)
    {
        return !string.IsNullOrWhiteSpace(entry?.Name) ? entry.Name : repoName;
    }

    /// <summary>
    /// 获取当前平台架构对应的 bin 文件列表。
    /// 未匹配到平台或无 bin 配置时返回 null。
    /// </summary>
    public static List<string>? GetBinPaths(InnerManifestTool? entry, PlatformInfo platform)
    {
        if (entry?.Platforms == null) return null;

        var platformKey = ToInnerManifestKey(platform);
        if (platformKey == null) return null;

        var match = entry.Platforms.Find(p =>
            p.Target.Equals(platformKey, StringComparison.OrdinalIgnoreCase));

        return match?.Bin.Count > 0 ? match.Bin : null;
    }

    /// <summary>将 PlatformInfo 转换为 inner-manifest 平台键格式（如 macos/arm64 → osx-arm64）。</summary>
    private static string? ToInnerManifestKey(PlatformInfo platform)
    {
        var os = platform.OS.ToLowerInvariant() switch
        {
            "macos" => "osx",
            "linux" => "linux",
            "windows" => "windows",
            _ => null
        };

        var arch = platform.Arch.ToLowerInvariant() switch
        {
            "x64" or "amd64" => "x64",
            "arm64" or "aarch64" => "arm64",
            _ => null
        };

        return os != null && arch != null ? $"{os}-{arch}" : null;
    }

    /// <summary>从程序集嵌入资源中加载 inner-manifest.json。</summary>
    private static List<InnerManifestTool>? LoadEmbedded()
    {
        var assembly = typeof(InnerManifestService).Assembly;
        using var stream = assembly.GetManifestResourceStream("GitPkg.inner-manifest.json");
        if (stream == null) return null;

        var manifest = JsonSerializer.Deserialize(stream, AppJsonContext.Default.InnerManifest);
        return manifest?.Tools;
    }
}
