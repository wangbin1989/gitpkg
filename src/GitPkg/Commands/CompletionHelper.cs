using System.CommandLine.Completions;
using GitPkg.Services;

namespace GitPkg.Commands;

/// <summary>
/// 补全辅助方法。提供从 manifest 读取已安装工具名称的动态补全源。
/// </summary>
internal static class CompletionHelper
{
    /// <summary>
    /// 从 manifest 读取已安装工具名称，用于动态补全。
    /// 出错时静默返回空集合（如 manifest 文件不存在）。
    /// </summary>
    internal static IEnumerable<string> GetInstalledToolNames(CompletionContext context)
    {
        try
        {
            var manifest = new ManifestService();
            var tools = manifest.LoadAsync(CancellationToken.None)
                .GetAwaiter().GetResult();
            return tools.Tools
                .Select(t => t.Name)
                .Where(n => n.StartsWith(context.WordToComplete ?? "",
                    StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }
}
