using System.Text.Json.Serialization;

namespace GitPkg.Models;

/// <summary>
/// AOT 源生成 JSON 序列化上下文。
/// 为 NativeAOT 发布提供裁剪安全的序列化支持，避免运行时反射。
/// 每新增一个需要 JSON 序列化的类型，需在此添加 <see cref="JsonSerializableAttribute"/>。
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(GitHubRelease))]
[JsonSerializable(typeof(GitHubRepo))]
[JsonSerializable(typeof(ToolManifest))]
[JsonSerializable(typeof(ToolEntry))]
[JsonSerializable(typeof(List<ToolEntry>))]
[JsonSerializable(typeof(InnerManifest))]
[JsonSerializable(typeof(InnerManifestTool))]
[JsonSerializable(typeof(InnerManifestPlatform))]
public partial class AppJsonContext : JsonSerializerContext
{
}
