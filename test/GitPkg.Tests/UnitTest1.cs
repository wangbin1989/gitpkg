using System.CommandLine;
using GitPkg.Commands;
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

public class ManifestServiceTests
{
    private static string GetTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"gitpkg_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task LoadAsync_EmptyWhenNoFile()
    {
        var dir = GetTempDir();
        try
        {
            var service = new ManifestService(dir);
            var manifest = await service.LoadAsync();
            Assert.NotNull(manifest);
            Assert.Empty(manifest.Tools);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task FindToolAsync_ReturnsNullWhenNotFound()
    {
        var dir = GetTempDir();
        try
        {
            var service = new ManifestService(dir);
            var tool = await service.FindToolAsync("nonexistent");
            Assert.Null(tool);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task AddAndFindTool_RoundTrip()
    {
        var dir = GetTempDir();
        try
        {
            var service = new ManifestService(dir);
            var entry = new ToolEntry
            {
                Name = "testtool",
                Repo = "owner/testtool",
                Version = "v1.0.0",
                InstallPath = "/tmp/testtool",
                InstalledAt = DateTime.UtcNow
            };

            await service.AddToolAsync(entry);
            var found = await service.FindToolAsync("testtool");

            Assert.NotNull(found);
            Assert.Equal("testtool", found!.Name);
            Assert.Equal("owner/testtool", found.Repo);
            Assert.Equal("v1.0.0", found.Version);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task AddTool_UpdatesExisting()
    {
        var dir = GetTempDir();
        try
        {
            var service = new ManifestService(dir);
            await service.AddToolAsync(new ToolEntry
            {
                Name = "testtool", Repo = "a/b", Version = "v1.0.0",
                InstallPath = "/tmp", InstalledAt = DateTime.UtcNow
            });
            await service.AddToolAsync(new ToolEntry
            {
                Name = "testtool", Repo = "a/b", Version = "v2.0.0",
                InstallPath = "/tmp", InstalledAt = DateTime.UtcNow
            });

            var manifest = await service.LoadAsync();
            Assert.Single(manifest.Tools);
            Assert.Equal("v2.0.0", manifest.Tools[0].Version);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task RemoveTool_RemovesFromManifest()
    {
        var dir = GetTempDir();
        try
        {
            var service = new ManifestService(dir);
            await service.AddToolAsync(new ToolEntry
            {
                Name = "testtool", Repo = "a/b", Version = "v1.0.0",
                InstallPath = "/tmp", InstalledAt = DateTime.UtcNow
            });

            var removed = await service.RemoveToolAsync("testtool");
            Assert.True(removed);

            var found = await service.FindToolAsync("testtool");
            Assert.Null(found);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task RemoveTool_Nonexistent_ReturnsFalse()
    {
        var dir = GetTempDir();
        try
        {
            var service = new ManifestService(dir);
            var removed = await service.RemoveToolAsync("nonexistent");
            Assert.False(removed);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetToolDir_ReturnsCorrectPath()
    {
        var dir = ManifestService.GetToolDir("mytool");
        Assert.EndsWith(Path.Combine(".gitpkg", "tools", "mytool"), dir);
    }

    [Fact]
    public void GetRepoName_ParsesOwnerRepo()
    {
        Assert.Equal("repo", ManifestService.GetRepoName("owner/repo"));
        Assert.Equal("repo", ManifestService.GetRepoName("repo"));
    }
}

public class PathServiceTests
{
    [Fact]
    public void GetConfigFilePath_ReturnsCorrectPaths()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        Assert.Equal(Path.Combine(home, ".zshrc"), PathService.GetConfigFilePath("zsh"));
        Assert.Equal(Path.Combine(home, ".bashrc"), PathService.GetConfigFilePath("bash"));
        Assert.Equal(Path.Combine(home, ".config", "fish", "config.fish"),
            PathService.GetConfigFilePath("fish"));
    }

    [Fact]
    public void DetectShell_ReturnsNonNullOnUnix()
    {
        var shell = PathService.DetectShell();
        if (!OperatingSystem.IsWindows())
        {
            Assert.NotNull(shell);
        }
    }
}

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

public class InitCommandTests
{
    private static async Task<(int ExitCode, string Stdout, string Stderr)> InvokeInitAsync(string shell)
    {
        var cmd = InitCommand.Create();
        var root = new RootCommand();
        root.Add(cmd);

        using var stdoutWriter = new StringWriter();
        using var stderrWriter = new StringWriter();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(stdoutWriter);
            Console.SetError(stderrWriter);
            var parseResult = root.Parse(["init", shell]);
            var exitCode = await parseResult.InvokeAsync();
            return (exitCode, stdoutWriter.ToString(), stderrWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public async Task Init_Zsh_WritesExportPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("zsh");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for zsh", stdout);
        Assert.Contains("export PATH=", stdout);
    }

    [Fact]
    public async Task Init_Bash_WritesExportPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("bash");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for bash", stdout);
        Assert.Contains("export PATH=", stdout);
    }

    [Fact]
    public async Task Init_Fish_WritesFishAddPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("fish");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for fish", stdout);
        Assert.Contains("fish_add_path", stdout);
    }

    [Fact]
    public async Task Init_Powershell_WritesEnvPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("powershell");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for powershell", stdout);
        Assert.Contains("$env:Path", stdout);
    }

    [Fact]
    public async Task Init_Pwsh_Alias_WritesEnvPath()
    {
        var (exitCode, stdout, _) = await InvokeInitAsync("pwsh");

        Assert.Equal(0, exitCode);
        Assert.StartsWith("# gitpkg shell init for powershell", stdout);
        Assert.Contains("$env:Path", stdout);
    }

    [Fact]
    public async Task Init_InvalidShell_WritesError()
    {
        var (exitCode, stdout, stderr) = await InvokeInitAsync("invalid");

        Assert.Equal(1, exitCode);
        Assert.Empty(stdout);
        Assert.Contains("不支持的 shell", stderr);
    }
}
