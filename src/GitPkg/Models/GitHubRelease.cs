using System.Text.Json.Serialization;

namespace GitPkg.Models;

public record GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; init; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; init; }

    [JsonPropertyName("body")]
    public string? Body { get; init; }

    [JsonPropertyName("assets")]
    public List<GitHubAsset> Assets { get; init; } = [];
}

public record GitHubAsset
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("browser_download_url")]
    public string DownloadUrl { get; init; } = "";

    [JsonPropertyName("size")]
    public long Size { get; init; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; init; } = "";
}

public record GitHubRepo
{
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("stargazers_count")]
    public int Stars { get; init; }
}
