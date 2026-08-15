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
    [JsonPropertyName("rid")]
    public string Rid { get; init; } = "";

    /// <summary>资产名称匹配模式（如 codex-aarch64-apple-darwin.tar.gz），用于精确选择 release asset。</summary>
    [JsonPropertyName("asset")]
    public string? Asset { get; init; }

    /// <summary>需要链接到 bin 目录的可执行文件配置列表。</summary>
    [JsonPropertyName("link")]
    public List<InnerManifestLink> Link { get; init; } = [];
}

/// <summary>
/// 内置清单中的链接配置条目。
/// </summary>
public record InnerManifestLink
{
    /// <summary>源文件路径（相对于安装目录）。</summary>
    [JsonPropertyName("source")]
    public string Source { get; init; } = "";

    /// <summary>链接名称，为空时使用 source 的文件名。</summary>
    [JsonPropertyName("target")]
    public string? Target { get; init; }
}
