using System.Text.Json.Serialization;

namespace GitPkg.Models;

[JsonSerializable(typeof(GitHubRelease))]
[JsonSerializable(typeof(GitHubRepo))]
[JsonSerializable(typeof(ToolManifest))]
[JsonSerializable(typeof(ToolEntry))]
[JsonSerializable(typeof(List<ToolEntry>))]
public partial class AppJsonContext : JsonSerializerContext
{
}
