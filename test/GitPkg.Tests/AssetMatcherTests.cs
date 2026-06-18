using GitPkg.Models;
using GitPkg.Services;

namespace GitPkg.Tests;

/// <summary>
/// 资产匹配器单元测试。
/// 验证根据平台信息（macOS/Linux/Windows × x64/arm64）从文件名中正确筛选匹配的 GitHub Release 资产。
/// </summary>
public class AssetMatcherTests
{
    /// <summary>macOS ARM64 平台应匹配 aarch64-apple-darwin 文件。</summary>
    [Fact]
    public void Match_ExactMacOSArm64_ReturnsMatch()
    {
        var matcher = new AssetMatcher();
        var assets = new List<GitHubAsset>
        {
            new() { Name = "tool-1.0.0-x86_64-unknown-linux-gnu.tar.gz", DownloadUrl = "" },
            new() { Name = "tool-1.0.0-aarch64-apple-darwin.tar.gz", DownloadUrl = "" },
            new() { Name = "tool-1.0.0-x86_64-pc-windows-msvc.zip", DownloadUrl = "" },
        };
        var platform = new PlatformInfo("macos", "arm64");

        var result = matcher.Match(assets, platform);

        Assert.Single(result);
        Assert.Equal("tool-1.0.0-aarch64-apple-darwin.tar.gz", result[0].Name);
    }

    /// <summary>Linux x64 平台应匹配 x86_64-unknown-linux-gnu 文件。</summary>
    [Fact]
    public void Match_ExactLinuxX64_ReturnsMatch()
    {
        var matcher = new AssetMatcher();
        var assets = new List<GitHubAsset>
        {
            new() { Name = "tool-1.0.0-x86_64-apple-darwin.tar.gz", DownloadUrl = "" },
            new() { Name = "tool-1.0.0-x86_64-unknown-linux-gnu.tar.gz", DownloadUrl = "" },
            new() { Name = "tool-1.0.0-x86_64-pc-windows-msvc.zip", DownloadUrl = "" },
        };
        var platform = new PlatformInfo("linux", "x64");

        var result = matcher.Match(assets, platform);

        Assert.Single(result);
        Assert.Equal("tool-1.0.0-x86_64-unknown-linux-gnu.tar.gz", result[0].Name);
    }

    /// <summary>不匹配的平台应返回空列表。</summary>
    [Fact]
    public void Match_NoMatch_ReturnsEmpty()
    {
        var matcher = new AssetMatcher();
        var assets = new List<GitHubAsset>
        {
            new() { Name = "tool-1.0.0-x86_64-unknown-linux-gnu.tar.gz", DownloadUrl = "" },
        };
        var platform = new PlatformInfo("windows", "x64");

        var result = matcher.Match(assets, platform);

        Assert.Empty(result);
    }

    /// <summary>多个文件名同时匹配时应全部返回。</summary>
    [Fact]
    public void Match_MultipleMatches_ReturnsAll()
    {
        var matcher = new AssetMatcher();
        var assets = new List<GitHubAsset>
        {
            new() { Name = "tool-1.0.0-x86_64-apple-darwin.tar.gz", DownloadUrl = "" },
            new() { Name = "tool-1.0.0-amd64-macos.tar.gz", DownloadUrl = "" },
        };
        var platform = new PlatformInfo("macos", "x64");

        var result = matcher.Match(assets, platform);

        Assert.Equal(2, result.Count);
    }
}
