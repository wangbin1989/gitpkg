using System.Text.Json;
using GitPkg.Models;

namespace GitPkg.Tests.Data;

/// <summary>
/// 测试数据加载器，从 Data/ 目录读取 JSON 文件。
/// </summary>
public static class TestDataLoader
{
    private static string DataDir =>
        Path.Combine(AppContext.BaseDirectory, "Data");

    /// <summary>加载 llama.cpp b9859 版本的完整资产名称列表。</summary>
    public static List<string> LoadLlamaB9859AssetNames()
    {
        var path = Path.Combine(DataDir, "llama-b9859-assets.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<string>>(json) ?? [];
    }

    /// <summary>加载 llama.cpp b9859 版本的资产列表，转换为 GitHubAsset 列表。</summary>
    public static List<GitHubAsset> LoadLlamaB9859Assets() =>
        LoadLlamaB9859AssetNames()
            .Select(name => new GitHubAsset { Name = name, DownloadUrl = $"https://example.com/{name}" })
            .ToList();
}
