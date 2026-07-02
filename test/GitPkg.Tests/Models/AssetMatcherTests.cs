using GitPkg.Models;
using GitPkg.Services;
using GitPkg.Tests.Data;
using Shouldly;

namespace GitPkg.Tests.Models;

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

        result.ShouldHaveSingleItem();
        result[0].Name.ShouldBe("tool-1.0.0-aarch64-apple-darwin.tar.gz");
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

        result.ShouldHaveSingleItem();
        result[0].Name.ShouldBe("tool-1.0.0-x86_64-unknown-linux-gnu.tar.gz");
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

        result.ShouldBeEmpty();
    }

    /// <summary>Windows x64 应匹配 win- 前缀的资产（词边界匹配，不误匹配 darwin）。</summary>
    [Fact]
    public void Match_WindowsX64_WinPrefix_ReturnsMatch()
    {
        var matcher = new AssetMatcher();
        var assets = TestDataLoader.LoadLlamaB9859Assets();
        var platform = new PlatformInfo("windows", "x64");

        var result = matcher.Match(assets, platform);

        result.ShouldContain(a => a.Name == "llama-b9859-bin-win-cuda-13.3-x64.zip");
        result.ShouldContain(a => a.Name == "llama-b9859-bin-win-cpu-x64.zip");
        result.ShouldNotContain(a => a.Name == "llama-b9859-bin-win-cpu-arm64.zip");
        result.ShouldNotContain(a => a.Name == "llama-b9859-bin-macos-arm64.tar.gz");
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

        result.Count.ShouldBe(2);
    }
}
