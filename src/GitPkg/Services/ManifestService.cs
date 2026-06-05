using System.Text.Json;
using GitPkg.Models;

namespace GitPkg.Services;

public class ManifestService
{
    private static readonly string DefaultBaseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".gitpkg");

    private readonly string _baseDir;
    private readonly string _manifestPath;
    private readonly AppJsonContext _jsonContext = new();

    public ManifestService(string? baseDir = null)
    {
        _baseDir = baseDir ?? DefaultBaseDir;
        _manifestPath = Path.Combine(_baseDir, "manifest.json");
    }

    public async Task<ToolManifest> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_manifestPath))
            return new ToolManifest();

        await using var stream = File.OpenRead(_manifestPath);
        return await JsonSerializer.DeserializeAsync(stream, _jsonContext.ToolManifest, ct)
               ?? new ToolManifest();
    }

    public async Task SaveAsync(ToolManifest manifest, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_baseDir);
        await using var stream = File.Create(_manifestPath);
        await JsonSerializer.SerializeAsync(stream, manifest, _jsonContext.ToolManifest, ct);
    }

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

    public async Task<bool> RemoveToolAsync(string name, CancellationToken ct = default)
    {
        var manifest = await LoadAsync(ct);
        var removed = manifest.Tools.RemoveAll(t =>
            t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) > 0;

        if (removed)
            await SaveAsync(manifest, ct);

        return removed;
    }

    public async Task<ToolEntry?> FindToolAsync(string name, CancellationToken ct = default)
    {
        var manifest = await LoadAsync(ct);
        return manifest.Tools.Find(t =>
            t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetToolDir(string toolName)
        => Path.Combine(DefaultBaseDir, "tools", toolName);

    public static string GetTmpDir()
        => Path.Combine(DefaultBaseDir, "tmp");

    public static string GetRepoName(string ownerRepo)
    {
        var parts = ownerRepo.Split('/');
        return parts.Length >= 2 ? parts[1] : ownerRepo;
    }
}
