using System.Text.Json.Serialization;

namespace GitPkg.Models;

/// <summary>
/// 工具清单文件（~/.gitpkg/manifest.json）的顶层记录。
/// 持久化所有已安装工具的元数据，支持 JSON 序列化。
/// </summary>
public record ToolManifest
{
    /// <summary>清单格式版本号，用于未来兼容性。</summary>
    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;

    /// <summary>已安装的工具条目列表。</summary>
    [JsonPropertyName("tools")]
    public List<ToolEntry> Tools { get; init; } = [];
}

/// <summary>
/// 单个已安装工具的条目，包含名称、来源仓库、版本和安装路径。
/// </summary>
public record ToolEntry
{
    /// <summary>工具名称（如 ripgrep、fd）。</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    /// <summary>GitHub 仓库（owner/repo 格式）。</summary>
    [JsonPropertyName("repo")]
    public string Repo { get; init; } = "";

    /// <summary>已安装的版本标签（如 v14.1.0）。</summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = "";

    /// <summary>工具在本地磁盘的安装目录。</summary>
    [JsonPropertyName("installPath")]
    public string InstallPath { get; init; } = "";

    /// <summary>安装时间（UTC）。</summary>
    [JsonPropertyName("installedAt")]
    public DateTime InstalledAt { get; init; }

    /// <summary>上次选择的资产文件名，用于更新时自动选择相同文件。</summary>
    [JsonPropertyName("assetName")]
    public string? AssetName { get; init; }
}
