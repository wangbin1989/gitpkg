using GitPkg.Models;
using GitPkg.Services;

namespace GitPkg.Tests;

public class AssetMatcherTests
{
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
