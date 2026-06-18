using System.Text.Json.Serialization;

namespace GitPkg.Models;

/// <summary>GitHub Release 响应记录，对应 API /repos/{owner}/{repo}/releases 的 JSON。</summary>
public record GitHubRelease
{
    /// <summary>版本标签（如 v1.0.0）。</summary>
    [JsonPropertyName("tag_name")]
    public string TagName { get; init; } = "";

    /// <summary>Release 标题（可为 null）。</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>发布时间（UTC）。</summary>
    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; init; }

    /// <summary>Release 正文（Markdown）。</summary>
    [JsonPropertyName("body")]
    public string? Body { get; init; }

    /// <summary>附加的资产文件列表。</summary>
    [JsonPropertyName("assets")]
    public List<GitHubAsset> Assets { get; init; } = [];
}

/// <summary>GitHub Release 资产（附件）记录。</summary>
public record GitHubAsset
{
    /// <summary>文件名（如 tool-v1.0.0-linux-amd64.tar.gz）。</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    /// <summary>浏览器下载 URL。</summary>
    [JsonPropertyName("browser_download_url")]
    public string DownloadUrl { get; init; } = "";

    /// <summary>文件大小（字节）。</summary>
    [JsonPropertyName("size")]
    public long Size { get; init; }

    /// <summary>MIME 类型。</summary>
    [JsonPropertyName("content_type")]
    public string ContentType { get; init; } = "";
}

/// <summary>GitHub 仓库信息响应记录。</summary>
public record GitHubRepo
{
    /// <summary>仓库描述。</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>Star 数量。</summary>
    [JsonPropertyName("stargazers_count")]
    public int Stars { get; init; }
}
