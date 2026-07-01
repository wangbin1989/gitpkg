using GitPkg.Models;
using GitPkg.Services;
using Shouldly;

namespace GitPkg.Tests.Services;

/// <summary>
/// 清单服务单元测试。
/// 覆盖 manifest.json 的增/删/查操作及路径计算。
/// </summary>
public class ManifestServiceTests
{
    /// <summary>创建临时隔离目录用于测试。</summary>
    private static string GetTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"gitpkg_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>清单文件不存在时 LoadAsync 应返回空清单。</summary>
    [Fact]
    public async Task LoadAsync_EmptyWhenNoFile()
    {
        var dir = GetTempDir();
        try
        {
            var service = new ManifestService(dir);
            var manifest = await service.LoadAsync();
            manifest.ShouldNotBeNull();
            manifest.Tools.ShouldBeEmpty();
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>查找不存在的工具应返回 null。</summary>
    [Fact]
    public async Task FindToolAsync_ReturnsNullWhenNotFound()
    {
        var dir = GetTempDir();
        try
        {
            var service = new ManifestService(dir);
            var tool = await service.FindToolAsync("nonexistent");
            tool.ShouldBeNull();
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>添加后立即可查询到相同数据（往返测试）。</summary>
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

            found.ShouldNotBeNull();
            found!.Name.ShouldBe("testtool");
            found.Repo.ShouldBe("owner/testtool");
            found.Version.ShouldBe("v1.0.0");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>AssetName 应正确持久化和读取。</summary>
    [Fact]
    public async Task AddAndFindTool_AssetNameRoundTrip()
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
                InstalledAt = DateTime.UtcNow,
                AssetName = "testtool-x86_64-apple-darwin.tar.gz"
            };

            await service.AddToolAsync(entry);
            var found = await service.FindToolAsync("testtool");

            found.ShouldNotBeNull();
            found!.AssetName.ShouldBe("testtool-x86_64-apple-darwin.tar.gz");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>未设置 AssetName 时应为 null（向后兼容）。</summary>
    [Fact]
    public async Task AddAndFindTool_AssetNameNullByDefault()
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

            found.ShouldNotBeNull();
            found!.AssetName.ShouldBeNull();
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>更新工具时 AssetName 应随之更新。</summary>
    [Fact]
    public async Task AddTool_UpdatesAssetName()
    {
        var dir = GetTempDir();
        try
        {
            var service = new ManifestService(dir);
            await service.AddToolAsync(new ToolEntry
            {
                Name = "testtool", Repo = "a/b", Version = "v1.0.0",
                InstallPath = "/tmp", InstalledAt = DateTime.UtcNow,
                AssetName = "testtool-v1.tar.gz"
            });
            await service.AddToolAsync(new ToolEntry
            {
                Name = "testtool", Repo = "a/b", Version = "v2.0.0",
                InstallPath = "/tmp", InstalledAt = DateTime.UtcNow,
                AssetName = "testtool-v2.tar.gz"
            });

            var found = await service.FindToolAsync("testtool");
            found.ShouldNotBeNull();
            found!.Version.ShouldBe("v2.0.0");
            found.AssetName.ShouldBe("testtool-v2.tar.gz");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>重复添加同名工具应更新版本而非新增条目。</summary>
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
            manifest.Tools.ShouldHaveSingleItem();
            manifest.Tools[0].Version.ShouldBe("v2.0.0");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>移除已存在的工具应返回 true 且无法再查到。</summary>
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
            removed.ShouldBeTrue();

            var found = await service.FindToolAsync("testtool");
            found.ShouldBeNull();
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>移除不存在的工具应返回 false。</summary>
    [Fact]
    public async Task RemoveTool_Nonexistent_ReturnsFalse()
    {
        var dir = GetTempDir();
        try
        {
            var service = new ManifestService(dir);
            var removed = await service.RemoveToolAsync("nonexistent");
            removed.ShouldBeFalse();
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>工具目录路径应以 .gitpkg/tools/{name} 结尾。</summary>
    [Fact]
    public void GetToolDir_ReturnsCorrectPath()
    {
        var dir = ManifestService.GetToolDir("mytool");
        dir.ShouldEndWith(Path.Combine(".gitpkg", "tools", "mytool"));
    }

    /// <summary>owner/repo 应正确提取 repo 部分。</summary>
    [Fact]
    public void GetRepoName_ParsesOwnerRepo()
    {
        ManifestService.GetRepoName("owner/repo").ShouldBe("repo");
        ManifestService.GetRepoName("repo").ShouldBe("repo");
    }

    /// <summary>bin 目录路径应以 .gitpkg/bin 结尾。</summary>
    [Fact]
    public void GetBinDir_ReturnsCorrectPath()
    {
        var dir = ManifestService.GetBinDir();
        dir.ShouldEndWith(Path.Combine(".gitpkg", "bin"));
    }
}
