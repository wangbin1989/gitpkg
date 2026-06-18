using GitPkg.Models;
using GitPkg.Services;

namespace GitPkg.Tests;

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
