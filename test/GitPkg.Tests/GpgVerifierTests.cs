using GitPkg.Models;
using GitPkg.Services;

namespace GitPkg.Tests;

/// <summary>
/// GPG 签名查找单元测试。
/// 验证从 Release 资产列表中正确查找配套签名文件（.asc / .sig / .gpg）。
/// </summary>
public class GpgVerifierTests
{
    /// <summary>应找到与目标文件同名的 .asc 签名文件。</summary>
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

    /// <summary>应找到与目标文件同名的 .sig 签名文件。</summary>
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

    /// <summary>无配套签名文件时应返回 null。</summary>
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
