using GitPkg.Models;

namespace GitPkg.Services;

/// <summary>
/// 资产匹配器。根据平台信息（OS + 架构）从一组 GitHub Release 资产中筛选匹配的文件。
/// 通过文件名中的关键词识别目标平台（如 darwin→macOS、x86_64→x64）。
/// </summary>
public class AssetMatcher
{
    /// <summary>操作系统关键词映射表，将内部标识符映射到文件名中可能出现的关键词变体。</summary>
    private static readonly Dictionary<string, string[]> OsKeywords = new()
    {
        ["macos"] = ["darwin", "macos", "osx", "apple", "mac"],
        ["windows"] = ["windows", "win64", "win32", "msvc"],
        ["linux"] = ["linux", "ubuntu", "debian", "rhel", "gnu", "musl"],
    };

    /// <summary>CPU 架构关键词映射表。</summary>
    private static readonly Dictionary<string, string[]> ArchKeywords = new()
    {
        ["x64"] = ["x86_64", "amd64", "x64", "x86-64"],
        ["arm64"] = ["arm64", "aarch64", "armv8"],
    };

    /// <summary>
    /// 从资产列表中筛选出匹配指定平台的文件。
    /// </summary>
    /// <param name="assets">Release 的全部资产。</param>
    /// <param name="platform">目标平台信息。</param>
    /// <returns>匹配的资产列表（可能为空）。</returns>
    public List<GitHubAsset> Match(List<GitHubAsset> assets, PlatformInfo platform)
    {
        var osPatterns = GetKeywords(OsKeywords, platform.OS);
        var archPatterns = GetKeywords(ArchKeywords, platform.Arch);

        var matches = assets
            .Where(a => Matches(a.Name.ToLowerInvariant(), osPatterns, archPatterns))
            .ToList();

        return matches;
    }

    private static string[] GetKeywords(Dictionary<string, string[]> dict, string key)
    {
        return dict.TryGetValue(key.ToLowerInvariant(), out var v) ? v : [key.ToLowerInvariant()];
    }

    private static bool Matches(string assetName, string[] osPatterns, string[] archPatterns)
    {
        var hasOS = osPatterns.Any(p => assetName.Contains(p));
        var hasArch = archPatterns.Any(p => assetName.Contains(p));
        return hasOS && hasArch;
    }
}
