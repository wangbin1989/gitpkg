using System.Text.Json;
using GitPkg.Models;

namespace GitPkg.Services;

/// <summary>
/// 工具清单管理器，负责 manifest.json 的读/写/增/删/查，
/// 以及工具安装目录和临时目录路径的计算。
/// </summary>
public class ManifestService
{
    /// <summary>默认的基础目录 ~/.gitpkg。</summary>
    private static readonly string DefaultBaseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".gitpkg");

    private readonly string _baseDir;
    private readonly string _manifestPath;
    private readonly AppJsonContext _jsonContext = new();

    /// <summary>可指定自定义基础目录，主要用于单元测试隔离。</summary>
    public ManifestService(string? baseDir = null)
    {
        _baseDir = baseDir ?? DefaultBaseDir;
        _manifestPath = Path.Combine(_baseDir, "manifest.json");
    }

    /// <summary>加载 manifest.json，不存在时返回空清单。</summary>
    public async Task<ToolManifest> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_manifestPath))
            return new ToolManifest();

        await using var stream = File.OpenRead(_manifestPath);
        return await JsonSerializer.DeserializeAsync(stream, _jsonContext.ToolManifest, ct)
               ?? new ToolManifest();
    }

    /// <summary>将清单写入 manifest.json，自动创建目录。</summary>
    public async Task SaveAsync(ToolManifest manifest, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_baseDir);
        await using var stream = File.Create(_manifestPath);
        await JsonSerializer.SerializeAsync(stream, manifest, _jsonContext.ToolManifest, ct);
    }

    /// <summary>添加或更新工具条目（按名称匹配，已存在则覆盖）。</summary>
    public async Task AddToolAsync(ToolEntry entry, CancellationToken ct = default)
    {
        var manifest = await LoadAsync(ct);
        var existing = manifest.Tools.FindIndex(t =>
            t.Name.Equals(entry.Name, StringComparison.OrdinalIgnoreCase));

        if (existing >= 0)
            manifest.Tools[existing] = entry;
        else
            manifest.Tools.Add(entry);

        await SaveAsync(manifest, ct);
    }

    /// <summary>按名称移除工具条目，返回是否实际移除了条目。</summary>
    public async Task<bool> RemoveToolAsync(string name, CancellationToken ct = default)
    {
        var manifest = await LoadAsync(ct);
        var removed = manifest.Tools.RemoveAll(t =>
            t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) > 0;

        if (removed)
            await SaveAsync(manifest, ct);

        return removed;
    }

    /// <summary>按名称查找工具条目，不存在则返回 null。</summary>
    public async Task<ToolEntry?> FindToolAsync(string name, CancellationToken ct = default)
    {
        var manifest = await LoadAsync(ct);
        return manifest.Tools.Find(t =>
            t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>获取工具的安装目录路径（~/.gitpkg/tools/{name}）。</summary>
    public static string GetToolDir(string toolName)
        => Path.Combine(DefaultBaseDir, "tools", toolName);

    /// <summary>获取下载临时目录路径（~/.gitpkg/tmp）。</summary>
    public static string GetTmpDir()
        => Path.Combine(DefaultBaseDir, "tmp");

    /// <summary>从 owner/repo 中提取仓库名。</summary>
    public static string GetRepoName(string ownerRepo)
    {
        var parts = ownerRepo.Split('/');
        return parts.Length >= 2 ? parts[1] : ownerRepo;
    }
}
