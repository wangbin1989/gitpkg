using GitPkg.Models;
using GitPkg.Services;

namespace GitPkg.Tests;

public class GpgVerifierTests
{
    [Fact]
    public void FindSignatureAsset_FindsAscFile()
    {
        var assets = new List<GitHubAsset>
        {
            new() { Name = "tool-v1.0.0-linux-amd64.tar.gz", DownloadUrl = "" },
            new() { Name = "tool-v1.0.0-linux-amd64.tar.gz.asc", DownloadUrl = "" },
            new() { Name = "checksums.txt", DownloadUrl = "" },
        };

        var result = GpgVerifier.FindSignatureAsset(assets, "tool-v1.0.0-linux-amd64.tar.gz");

        Assert.NotNull(result);
        Assert.Equal("tool-v1.0.0-linux-amd64.tar.gz.asc", result!.Name);
    }

    [Fact]
    public void FindSignatureAsset_FindsSigFile()
    {
        var assets = new List<GitHubAsset>
        {
            new() { Name = "tool-v1.0.0-x64.tar.gz", DownloadUrl = "" },
            new() { Name = "tool-v1.0.0-x64.tar.gz.sig", DownloadUrl = "" },
        };

        var result = GpgVerifier.FindSignatureAsset(assets, "tool-v1.0.0-x64.tar.gz");

        Assert.NotNull(result);
        Assert.Equal("tool-v1.0.0-x64.tar.gz.sig", result!.Name);
    }

    [Fact]
    public void FindSignatureAsset_NotFound_ReturnsNull()
    {
        var assets = new List<GitHubAsset>
        {
            new() { Name = "tool-v1.0.0.tar.gz", DownloadUrl = "" },
        };

        var result = GpgVerifier.FindSignatureAsset(assets, "tool-v1.0.0.tar.gz");

        Assert.Null(result);
    }
}
