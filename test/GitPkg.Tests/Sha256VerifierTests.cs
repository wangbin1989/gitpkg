using GitPkg.Services;

namespace GitPkg.Tests;

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
