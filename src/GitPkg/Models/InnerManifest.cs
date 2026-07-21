using System.Text.Json.Serialization;

namespace GitPkg.Models;

/// <summary>
/// 内置清单（inner-manifest.json）的顶层结构。
/// </summary>
public record InnerManifest
{
    /// <summary>工具配置列表。</summary>
    [JsonPropertyName("tools")]
    public List<InnerManifestTool> Tools { get; init; } = [];
}

/// <summary>
/// 单个工具的配置条目。
/// </summary>
public record InnerManifestTool
{
    /// <summary>GitHub 仓库，格式为 owner/repo。</summary>
    [JsonPropertyName("repo")]
    public string Repo { get; init; } = "";

    /// <summary>自定义工具名称，覆盖默认的仓库名。</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>平台特定的可执行文件配置。</summary>
    [JsonPropertyName("platforms")]
    public List<InnerManifestPlatform>? Platforms { get; init; }
}

/// <summary>
/// 特定平台架构下的可执行文件配置。
/// </summary>
public record InnerManifestPlatform
{
    /// <summary>平台架构标识符（如 osx-arm64、linux-x64）。</summary>
    [JsonPropertyName("target")]
    public string Target { get; init; } = "";

    /// <summary>需要链接到 bin 目录的可执行文件路径列表（相对于安装目录）。</summary>
    [JsonPropertyName("bin")]
    public List<string> Bin { get; init; } = [];
}
