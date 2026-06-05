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

public class Sha256VerifierTests
{
    [Fact]
    public void ParseChecksum_StandardFormat_ReturnsHash()
    {
        var content = """
            2e3c6b6f5acbe576b6e6cae68044d75a6be3d67c3f4bfdff4f1b172e3549d1e0  tool-v1.0.0-linux-amd64.tar.gz
            a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2  tool-v1.0.0-darwin-amd64.tar.gz
            """;

        var hash = Sha256Verifier.ParseChecksum(content, "tool-v1.0.0-linux-amd64.tar.gz");

        Assert.Equal("2e3c6b6f5acbe576b6e6cae68044d75a6be3d67c3f4bfdff4f1b172e3549d1e0", hash);
    }

    [Fact]
    public void ParseChecksum_BinaryFormat_ReturnsHash()
    {
        var content = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2 *tool-v1.0.0-darwin-amd64.tar.gz";

        var hash = Sha256Verifier.ParseChecksum(content, "tool-v1.0.0-darwin-amd64.tar.gz");

        Assert.Equal("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2", hash);
    }

    [Fact]
    public void ParseChecksum_NotFound_ReturnsNull()
    {
        var content = "hash1  tool-v1.0.0-linux-amd64.tar.gz";

        var hash = Sha256Verifier.ParseChecksum(content, "other-tool.tar.gz");

        Assert.Null(hash);
    }

    [Fact]
    public async Task ComputeHash_ReturnsValidHexString()
    {
        var verifier = new Sha256Verifier();
        var tmpFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tmpFile, "hello world");

            var hash = await verifier.ComputeHashAsync(tmpFile);

            Assert.Equal(64, hash.Length);
            Assert.Equal("b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9", hash);
        }
        finally
        {
            if (File.Exists(tmpFile))
                File.Delete(tmpFile);
        }
    }
}

public class PlatformInfoTests
{
    [Fact]
    public void Current_ReturnsValidPlatform()
    {
        var platform = PlatformInfo.Current();

        Assert.NotNull(platform.OS);
        Assert.NotNull(platform.Arch);
        Assert.NotEmpty(platform.OS);
        Assert.NotEmpty(platform.Arch);
    }
}
