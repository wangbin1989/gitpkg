using System.Text.Json.Serialization;

namespace GitPkg.Models;

public record ToolManifest
{
    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;

    [JsonPropertyName("tools")]
    public List<ToolEntry> Tools { get; init; } = [];
}

public record ToolEntry
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("repo")]
    public string Repo { get; init; } = "";

    [JsonPropertyName("version")]
    public string Version { get; init; } = "";

    [JsonPropertyName("installPath")]
    public string InstallPath { get; init; } = "";

    [JsonPropertyName("installedAt")]
    public DateTime InstalledAt { get; init; }
}
