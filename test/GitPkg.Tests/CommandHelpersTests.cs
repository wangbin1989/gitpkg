using System.Text;
using GitPkg.Commands;
using GitPkg.Models;
using Spectre.Console;

namespace GitPkg.Tests;

/// <summary>
/// 命令辅助方法单元测试。
/// 覆盖 FormatSize、PromptAssetSelection、SelectAsset、FindChecksumAsset。
/// </summary>
public class CommandHelpersTests : IDisposable
{
    private readonly StringWriter _consoleOut;
    private readonly IAnsiConsole _originalConsole;

    public CommandHelpersTests()
    {
        // 重定向 AnsiConsole 输出到 StringWriter，避免测试中 TextWriter 已关闭的问题
        _consoleOut = new StringWriter();
        _originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new TestableAnsiConsoleOutput(_consoleOut),
        });
    }

    public void Dispose()
    {
        AnsiConsole.Console = _originalConsole;
        _consoleOut.Dispose();
    }

    /// <summary>测试用 AnsiConsole 输出，将内容写入 TextWriter。</summary>
    private sealed class TestableAnsiConsoleOutput(TextWriter writer) : IAnsiConsoleOutput
    {
        public TextWriter Writer { get; } = writer;
        public void SetCursorPosition(int left, int top) { }
        public void SetEncoding(Encoding encoding) { }
        public bool IsTerminal => false;
        public int Width => 80;
        public int Height => 24;
    }
    /// <summary>0 字节应显示为 "0 B"。</summary>
    [Fact]
    public void FormatSize_Zero_ReturnsBytes()
    {
        Assert.Equal("0 B", CommandHelpers.FormatSize(0));
    }

    /// <summary>小于 1KB 应显示为 B。</summary>
    [Fact]
    public void FormatSize_SmallBytes_ReturnsBytes()
    {
        Assert.Equal("512 B", CommandHelpers.FormatSize(512));
    }

    /// <summary>1023 字节仍应显示为 B（未到 1KB）。</summary>
    [Fact]
    public void FormatSize_Below1KB_ReturnsBytes()
    {
        Assert.Equal("1023 B", CommandHelpers.FormatSize(1023));
    }

    /// <summary>恰好 1KB 应显示为 1.0 KB。</summary>
    [Fact]
    public void FormatSize_Exactly1KB_ReturnsKB()
    {
        Assert.Equal("1.0 KB", CommandHelpers.FormatSize(1024));
    }

    /// <summary>1.5 KB 边界值。</summary>
    [Fact]
    public void FormatSize_1536Bytes_ReturnsFractionalKB()
    {
        Assert.Equal("1.5 KB", CommandHelpers.FormatSize(1536));
    }

    /// <summary>恰好 1MB 应显示为 1.0 MB。</summary>
    [Fact]
    public void FormatSize_Exactly1MB_ReturnsMB()
    {
        Assert.Equal("1.0 MB", CommandHelpers.FormatSize(1024 * 1024));
    }

    /// <summary>2.5 MB 边界值。</summary>
    [Fact]
    public void FormatSize_2_5MB_ReturnsMB()
    {
        Assert.Equal("2.5 MB", CommandHelpers.FormatSize((long)(2.5 * 1024 * 1024)));
    }

    /// <summary>恰好 1GB 应显示为 1.0 GB。</summary>
    [Fact]
    public void FormatSize_Exactly1GB_ReturnsGB()
    {
        Assert.Equal("1.0 GB", CommandHelpers.FormatSize(1024L * 1024 * 1024));
    }

    /// <summary>大型 GB 值。</summary>
    [Fact]
    public void FormatSize_LargeGB_ReturnsGB()
    {
        Assert.Equal("4.0 GB", CommandHelpers.FormatSize(4L * 1024 * 1024 * 1024));
    }

    /// <summary>空资产列表应抛出异常。</summary>
    [Fact]
    public void PromptAssetSelection_EmptyList_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CommandHelpers.PromptAssetSelection([]));
    }

    // ---- SelectAsset 测试 ----

    private static GitHubAsset Asset(string name, long size = 0)
        => new() { Name = name, DownloadUrl = $"https://example.com/{name}", Size = size };

    private static PlatformInfo Platform(string os = "macos", string arch = "arm64")
        => new(os, arch);

    /// <summary>savedAssetName 匹配且在平台匹配列表中时，应自动选择。</summary>
    [Fact]
    public void SelectAsset_SavedNameInMatches_AutoSelects()
    {
        var all = new List<GitHubAsset>
        {
            Asset("tool-darwin-arm64.tar.gz"),
            Asset("tool-linux-amd64.tar.gz"),
        };
        var matches = new List<GitHubAsset> { all[0] };

        var result = CommandHelpers.SelectAsset(all, matches, Platform(), "tool-darwin-arm64.tar.gz");

        Assert.Equal("tool-darwin-arm64.tar.gz", result.Name);
    }

    /// <summary>savedAssetName 匹配但不在平台匹配列表中时，应回退到平台匹配。</summary>
    [Fact]
    public void SelectAsset_SavedNameNotInMatches_FallsBackToPlatformMatch()
    {
        var all = new List<GitHubAsset>
        {
            Asset("tool-darwin-arm64.tar.gz"),
            Asset("tool-linux-amd64.tar.gz"),
        };
        var matches = new List<GitHubAsset> { all[0] };

        // saved 是 linux 资产，但平台匹配只有 darwin
        var result = CommandHelpers.SelectAsset(all, matches, Platform(), "tool-linux-amd64.tar.gz");

        Assert.Equal("tool-darwin-arm64.tar.gz", result.Name);
    }

    /// <summary>savedAssetName 为 null 时，应回退到平台匹配（单个匹配自动选）。</summary>
    [Fact]
    public void SelectAsset_NullSavedName_UsesPlatformMatch()
    {
        var all = new List<GitHubAsset>
        {
            Asset("tool-darwin-arm64.tar.gz"),
            Asset("tool-linux-amd64.tar.gz"),
        };
        var matches = new List<GitHubAsset> { all[0] };

        var result = CommandHelpers.SelectAsset(all, matches, Platform(), null);

        Assert.Equal("tool-darwin-arm64.tar.gz", result.Name);
    }

    /// <summary>savedAssetName 在新版本中不存在时，应回退到平台匹配。</summary>
    [Fact]
    public void SelectAsset_SavedNameNotFound_FallsBackToPlatformMatch()
    {
        var all = new List<GitHubAsset>
        {
            Asset("tool-darwin-arm64-v2.tar.gz"),
        };
        var matches = new List<GitHubAsset> { all[0] };

        var result = CommandHelpers.SelectAsset(all, matches, Platform(), "tool-darwin-arm64-v1.tar.gz");

        Assert.Equal("tool-darwin-arm64-v2.tar.gz", result.Name);
    }

    /// <summary>单个平台匹配时自动选中。</summary>
    [Fact]
    public void SelectAsset_SingleMatch_AutoSelects()
    {
        var all = new List<GitHubAsset>
        {
            Asset("tool-darwin-arm64.tar.gz"),
            Asset("tool-linux-amd64.tar.gz"),
        };
        var matches = new List<GitHubAsset> { all[0] };

        var result = CommandHelpers.SelectAsset(all, matches, Platform(), null);

        Assert.Equal("tool-darwin-arm64.tar.gz", result.Name);
    }

    /// <summary>多个平台匹配时，在非交互模式下自动选第一个。</summary>
    [Fact]
    public void SelectAsset_MultipleMatches_NonInteractive_SelectsFirst()
    {
        var all = new List<GitHubAsset>
        {
            Asset("tool-darwin-arm64.tar.gz"),
            Asset("tool-darwin-arm64-musl.tar.gz"),
        };
        var matches = new List<GitHubAsset> { all[0], all[1] };

        // 测试环境为非交互模式，PromptAssetSelection 自动选第一个
        var result = CommandHelpers.SelectAsset(all, matches, Platform(), null);

        Assert.Equal("tool-darwin-arm64.tar.gz", result.Name);
    }

    /// <summary>无平台匹配时，在非交互模式下从全部资产中选第一个。</summary>
    [Fact]
    public void SelectAsset_NoMatches_NonInteractive_SelectsFirstFromAll()
    {
        var all = new List<GitHubAsset>
        {
            Asset("tool-linux-amd64.tar.gz"),
            Asset("tool-windows-x64.zip"),
        };

        // 测试环境为非交互模式，PromptAssetSelection 自动选第一个
        var result = CommandHelpers.SelectAsset(all, [], Platform(), null);

        Assert.Equal("tool-linux-amd64.tar.gz", result.Name);
    }

    // ---- FindChecksumAsset 测试 ----

    /// <summary>应识别 .sha256 后缀的校验文件。</summary>
    [Fact]
    public void FindChecksumAsset_Sha256Extension_Found()
    {
        var assets = new List<GitHubAsset>
        {
            Asset("tool.tar.gz"),
            Asset("tool.tar.gz.sha256"),
        };

        var result = CommandHelpers.FindChecksumAsset(assets);

        Assert.NotNull(result);
        Assert.Equal("tool.tar.gz.sha256", result.Name);
    }

    /// <summary>应识别 checksums.txt 文件。</summary>
    [Fact]
    public void FindChecksumAsset_ChecksumsTxt_Found()
    {
        var assets = new List<GitHubAsset>
        {
            Asset("tool.tar.gz"),
            Asset("checksums.txt"),
        };

        var result = CommandHelpers.FindChecksumAsset(assets);

        Assert.NotNull(result);
        Assert.Equal("checksums.txt", result.Name);
    }

    /// <summary>应识别 sha256sums 文件。</summary>
    [Fact]
    public void FindChecksumAsset_Sha256Sums_Found()
    {
        var assets = new List<GitHubAsset>
        {
            Asset("tool.tar.gz"),
            Asset("sha256sums"),
        };

        var result = CommandHelpers.FindChecksumAsset(assets);

        Assert.NotNull(result);
        Assert.Equal("sha256sums", result.Name);
    }

    /// <summary>应识别 sha256sums.txt 文件。</summary>
    [Fact]
    public void FindChecksumAsset_Sha256SumsTxt_Found()
    {
        var assets = new List<GitHubAsset>
        {
            Asset("tool.tar.gz"),
            Asset("sha256sums.txt"),
        };

        var result = CommandHelpers.FindChecksumAsset(assets);

        Assert.NotNull(result);
        Assert.Equal("sha256sums.txt", result.Name);
    }

    /// <summary>无校验文件时应返回 null。</summary>
    [Fact]
    public void FindChecksumAsset_NoChecksum_ReturnsNull()
    {
        var assets = new List<GitHubAsset>
        {
            Asset("tool.tar.gz"),
            Asset("tool.zip"),
        };

        var result = CommandHelpers.FindChecksumAsset(assets);

        Assert.Null(result);
    }

    /// <summary>空资产列表应返回 null。</summary>
    [Fact]
    public void FindChecksumAsset_EmptyList_ReturnsNull()
    {
        var result = CommandHelpers.FindChecksumAsset([]);

        Assert.Null(result);
    }
}
