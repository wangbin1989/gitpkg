using GitPkg.Models;

namespace GitPkg.Services;

public class AssetMatcher
{
    private static readonly Dictionary<string, string[]> OsKeywords = new()
    {
        ["macos"] = ["darwin", "macos", "osx", "apple", "mac"],
        ["windows"] = ["windows", "win64", "win32", "msvc"],
        ["linux"] = ["linux", "ubuntu", "debian", "rhel", "gnu", "musl"],
    };

    private static readonly Dictionary<string, string[]> ArchKeywords = new()
    {
        ["x64"] = ["x86_64", "amd64", "x64", "x86-64"],
        ["arm64"] = ["arm64", "aarch64", "armv8"],
    };

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
