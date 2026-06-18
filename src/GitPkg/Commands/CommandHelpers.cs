using GitPkg.Models;
using Spectre.Console;

namespace GitPkg.Commands;

/// <summary>
/// 命令通用辅助方法。提供交互式资产选择和格式化输出。
/// </summary>
public static class CommandHelpers
{
    /// <summary>
    /// 显示交互式选择面板，让用户从资产列表中手动选择一项。
    /// 在非交互式终端中自动选择第一项。
    /// </summary>
    public static GitHubAsset PromptAssetSelection(List<GitHubAsset> assets)
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            var first = assets[0];
            AnsiConsole.MarkupLine($"[grey]非交互模式，自动选择 [bold]{first.Name}[/][/]");
            return first;
        }

        var choices = assets.Select(a => $"{a.Name} ({FormatSize(a.Size)})").ToArray();
        var prompt = new SelectionPrompt<string>()
            .Title("选择要安装的资产")
            .AddChoices(choices);
        var chosen = AnsiConsole.Prompt(prompt);
        return assets[Array.IndexOf(choices, chosen)];
    }

    /// <summary>将字节数格式化为人类可读的大小字符串（B / KB / MB / GB）。</summary>
    public static string FormatSize(long bytes) => bytes switch
    {
        >= 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F1} GB",
        >= 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        >= 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes} B"
    };
}
