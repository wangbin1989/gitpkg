using System.Text;
using GitPkg.Commands;
using GitPkg.Models;
using Shouldly;
using Spectre.Console;

namespace GitPkg.Tests.Models;

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
        CommandHelpers.FormatSize(0).ShouldBe("0 B");
    }

    /// <summary>小于 1KB 应显示为 B。</summary>
    [Fact]
    public void FormatSize_SmallBytes_ReturnsBytes()
    {
        CommandHelpers.FormatSize(512).ShouldBe("512 B");
    }

    /// <summary>1023 字节仍应显示为 B（未到 1KB）。</summary>
    [Fact]
    public void FormatSize_Below1KB_ReturnsBytes()
    {
        CommandHelpers.FormatSize(1023).ShouldBe("1023 B");
    }

    /// <summary>恰好 1KB 应显示为 1.0 KB。</summary>
    [Fact]
    public void FormatSize_Exactly1KB_ReturnsKB()
    {
        CommandHelpers.FormatSize(1024).ShouldBe("1.0 KB");
    }

    /// <summary>1.5 KB 边界值。</summary>
    [Fact]
    public void FormatSize_1536Bytes_ReturnsFractionalKB()
    {
        CommandHelpers.FormatSize(1536).ShouldBe("1.5 KB");
    }

    /// <summary>恰好 1MB 应显示为 1.0 MB。</summary>
    [Fact]
    public void FormatSize_Exactly1MB_ReturnsMB()
    {
        CommandHelpers.FormatSize(1024 * 1024).ShouldBe("1.0 MB");
    }

    /// <summary>2.5 MB 边界值。</summary>
    [Fact]
    public void FormatSize_2_5MB_ReturnsMB()
    {
        CommandHelpers.FormatSize((long)(2.5 * 1024 * 1024)).ShouldBe("2.5 MB");
    }

    /// <summary>恰好 1GB 应显示为 1.0 GB。</summary>
    [Fact]
    public void FormatSize_Exactly1GB_ReturnsGB()
    {
        CommandHelpers.FormatSize(1024L * 1024 * 1024).ShouldBe("1.0 GB");
    }

    /// <summary>大型 GB 值。</summary>
    [Fact]
    public void FormatSize_LargeGB_ReturnsGB()
    {
        CommandHelpers.FormatSize(4L * 1024 * 1024 * 1024).ShouldBe("4.0 GB");
    }

    /// <summary>空资产列表应抛出异常。</summary>
    [Fact]
    public void PromptAssetSelection_EmptyList_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
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

        result.Name.ShouldBe("tool-darwin-arm64.tar.gz");
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

        result.Name.ShouldBe("tool-darwin-arm64.tar.gz");
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

        result.Name.ShouldBe("tool-darwin-arm64.tar.gz");
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

        result.Name.ShouldBe("tool-darwin-arm64-v2.tar.gz");
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

        result.Name.ShouldBe("tool-darwin-arm64.tar.gz");
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

        result.Name.ShouldBe("tool-darwin-arm64.tar.gz");
    }

    /// <summary>匹配列表含辅助文件时，应过滤后计数，避免提示数与选择列表不一致。</summary>
    [Fact]
    public void SelectAsset_AuxiliaryAssetFiltered_CountMatchesList()
    {
        var all = new List<GitHubAsset>
        {
            Asset("tool-darwin-arm64.tar.gz"),
            Asset("tool-darwin-arm64.tar.gz.sha256"),
        };
        var matches = new List<GitHubAsset> { all[0], all[1] };

        // sha256 为辅助文件，过滤后仅 1 个，应自动选中而非提示选择
        var result = CommandHelpers.SelectAsset(all, matches, Platform(), null);

        result.Name.ShouldBe("tool-darwin-arm64.tar.gz");
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

        result.Name.ShouldBe("tool-linux-amd64.tar.gz");
    }

    /// <summary>版本号变化时（v5.0.0 → v5.1.0），替换后应匹配新资产。</summary>
    [Fact]
    public void SelectAsset_SavedNameDifferentVersion_ReplacesAndMatches()
    {
        var all = new List<GitHubAsset>
        {
            Asset("podman-v5.1.0-darwin-arm64.tar.gz"),
            Asset("podman-v5.1.0-linux-amd64.tar.gz"),
        };
        var matches = new List<GitHubAsset> { all[0] };

        var result = CommandHelpers.SelectAsset(all, matches, Platform(),
            "podman-v5.0.0-darwin-arm64.tar.gz", "v5.0.0", "v5.1.0");

        result.Name.ShouldBe("podman-v5.1.0-darwin-arm64.tar.gz");
    }

    /// <summary>构建号变化时（b9803 → b9859），替换后应匹配新资产。</summary>
    [Fact]
    public void SelectAsset_SavedNameBuildNumber_ReplacesAndMatches()
    {
        var all = new List<GitHubAsset>
        {
            Asset("tool-b9859-darwin-arm64.tar.gz"),
            Asset("tool-b9859-linux-amd64.tar.gz"),
        };
        var matches = new List<GitHubAsset> { all[0] };

        var result = CommandHelpers.SelectAsset(all, matches, Platform(),
            "tool-b9803-darwin-arm64.tar.gz", "b9803", "b9859");

        result.Name.ShouldBe("tool-b9859-darwin-arm64.tar.gz");
    }

    /// <summary>替换版本号后仍无匹配时，应回退到平台匹配。</summary>
    [Fact]
    public void SelectAsset_SavedNameReplaceNoMatch_FallsBackToPlatformMatch()
    {
        var all = new List<GitHubAsset>
        {
            Asset("tool-v2.0.0-darwin-arm64.tar.gz"),
            Asset("tool-v2.0.0-linux-amd64.tar.gz"),
        };
        var matches = new List<GitHubAsset> { all[0] };

        // saved 为 linux 资产，替换版本后仍不在平台匹配列表中
        var result = CommandHelpers.SelectAsset(all, matches, Platform(),
            "tool-v1.0.0-linux-amd64.tar.gz", "v1.0.0", "v2.0.0");

        result.Name.ShouldBe("tool-v2.0.0-darwin-arm64.tar.gz");
    }

    /// <summary>未传入版本号时，应回退到精确匹配。</summary>
    [Fact]
    public void SelectAsset_SavedNameNoVersion_ExactMatch()
    {
        var all = new List<GitHubAsset>
        {
            Asset("tool-darwin-arm64.tar.gz"),
            Asset("tool-linux-amd64.tar.gz"),
        };
        var matches = new List<GitHubAsset> { all[0] };

        var result = CommandHelpers.SelectAsset(all, matches, Platform(), "tool-darwin-arm64.tar.gz");

        result.Name.ShouldBe("tool-darwin-arm64.tar.gz");
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

        result.ShouldNotBeNull();
        result!.Name.ShouldBe("tool.tar.gz.sha256");
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

        result.ShouldNotBeNull();
        result!.Name.ShouldBe("checksums.txt");
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

        result.ShouldNotBeNull();
        result!.Name.ShouldBe("sha256sums");
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

        result.ShouldNotBeNull();
        result!.Name.ShouldBe("sha256sums.txt");
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

        result.ShouldBeNull();
    }

    /// <summary>空资产列表应返回 null。</summary>
    [Fact]
    public void FindChecksumAsset_EmptyList_ReturnsNull()
    {
        var result = CommandHelpers.FindChecksumAsset([]);

        result.ShouldBeNull();
    }

    // ---- StripPlatformSuffix 测试 ----

    /// <summary>Windows 平台 + amd64 架构 + .exe 扩展名。</summary>
    [Fact]
    public void StripPlatformSuffix_WindowsAmd64Exe_PreservesExtension()
    {
        CommandHelpers.StripPlatformSuffix("my-tool-windows-amd64.exe").ShouldBe("my-tool.exe");
    }

    /// <summary>Linux 平台 + amd64 架构，无扩展名。</summary>
    [Fact]
    public void StripPlatformSuffix_LinuxAmd64_NoExtension()
    {
        CommandHelpers.StripPlatformSuffix("my-tool-linux-amd64").ShouldBe("my-tool");
    }

    /// <summary>Darwin 平台 + arm64 架构。</summary>
    [Fact]
    public void StripPlatformSuffix_DarwinArm64()
    {
        CommandHelpers.StripPlatformSuffix("my-tool-darwin-arm64").ShouldBe("my-tool");
    }

    /// <summary>下划线分隔符。</summary>
    [Fact]
    public void StripPlatformSuffix_UnderscoreSeparator()
    {
        CommandHelpers.StripPlatformSuffix("my-tool_linux_arm64").ShouldBe("my-tool");
    }

    /// <summary>混合分隔符（连字符和下划线）。</summary>
    [Fact]
    public void StripPlatformSuffix_MixedSeparators()
    {
        CommandHelpers.StripPlatformSuffix("my-tool-linux_amd64").ShouldBe("my-tool");
    }

    /// <summary>无平台后缀时保持原样。</summary>
    [Fact]
    public void StripPlatformSuffix_NoSuffix_Unchanged()
    {
        CommandHelpers.StripPlatformSuffix("my-tool").ShouldBe("my-tool");
    }

    /// <summary>无平台后缀但有扩展名时保持原样。</summary>
    [Fact]
    public void StripPlatformSuffix_NoSuffixWithExt_Unchanged()
    {
        CommandHelpers.StripPlatformSuffix("my-tool.exe").ShouldBe("my-tool.exe");
    }

    /// <summary>macOS 变体。</summary>
    [Fact]
    public void StripPlatformSuffix_MacOS()
    {
        CommandHelpers.StripPlatformSuffix("tool-macos-arm64").ShouldBe("tool");
    }

    /// <summary>OSX 变体。</summary>
    [Fact]
    public void StripPlatformSuffix_OSX()
    {
        CommandHelpers.StripPlatformSuffix("tool-osx-x64").ShouldBe("tool");
    }

    /// <summary>x86_64 架构（含下划线）。</summary>
    [Fact]
    public void StripPlatformSuffix_X86_64()
    {
        CommandHelpers.StripPlatformSuffix("tool-linux-x86_64").ShouldBe("tool");
    }

    /// <summary>aarch64 架构。</summary>
    [Fact]
    public void StripPlatformSuffix_AArch64()
    {
        CommandHelpers.StripPlatformSuffix("tool-linux-aarch64").ShouldBe("tool");
    }

    /// <summary>Windows + x64 架构。</summary>
    [Fact]
    public void StripPlatformSuffix_WindowsX64()
    {
        CommandHelpers.StripPlatformSuffix("tool-windows-x64").ShouldBe("tool");
    }

    /// <summary>musl 变体后缀。</summary>
    [Fact]
    public void StripPlatformSuffix_MuslVariant()
    {
        CommandHelpers.StripPlatformSuffix("tool-linux-amd64-musl").ShouldBe("tool");
    }

    /// <summary>gnu 变体后缀。</summary>
    [Fact]
    public void StripPlatformSuffix_GnuVariant()
    {
        CommandHelpers.StripPlatformSuffix("tool-linux-amd64-gnu").ShouldBe("tool");
    }

    /// <summary>多个后缀组合：平台 + 架构 + 变体。</summary>
    [Fact]
    public void StripPlatformSuffix_PlatformArchVariant()
    {
        CommandHelpers.StripPlatformSuffix("tool-linux-amd64-musl-static").ShouldBe("tool");
    }

    /// <summary>Windows + x86 架构 + .exe。</summary>
    [Fact]
    public void StripPlatformSuffix_WindowsX86Exe()
    {
        CommandHelpers.StripPlatformSuffix("tool-windows-x86.exe").ShouldBe("tool.exe");
    }

    /// <summary>Win32 平台变体。</summary>
    [Fact]
    public void StripPlatformSuffix_Win32()
    {
        CommandHelpers.StripPlatformSuffix("tool-win32-x64").ShouldBe("tool");
    }

    /// <summary>Win64 平台变体。</summary>
    [Fact]
    public void StripPlatformSuffix_Win64()
    {
        CommandHelpers.StripPlatformSuffix("tool-win64-amd64").ShouldBe("tool");
    }

    /// <summary>FreeBSD 平台。</summary>
    [Fact]
    public void StripPlatformSuffix_FreeBSD()
    {
        CommandHelpers.StripPlatformSuffix("tool-freebsd-amd64").ShouldBe("tool");
    }

    /// <summary>Android 平台。</summary>
    [Fact]
    public void StripPlatformSuffix_Android()
    {
        CommandHelpers.StripPlatformSuffix("tool-android-arm64").ShouldBe("tool");
    }

    /// <summary>i686 架构。</summary>
    [Fact]
    public void StripPlatformSuffix_I686()
    {
        CommandHelpers.StripPlatformSuffix("tool-linux-i686").ShouldBe("tool");
    }

    /// <summary>386 架构。</summary>
    [Fact]
    public void StripPlatformSuffix_386()
    {
        CommandHelpers.StripPlatformSuffix("tool-linux-386").ShouldBe("tool");
    }

    /// <summary>real-world 示例：ripgrep 风格（版本号不在剥离范围）。</summary>
    [Fact]
    public void StripPlatformSuffix_RipgrepStyle()
    {
        CommandHelpers.StripPlatformSuffix("rg-14.1.1-x86_64-linux-musl").ShouldBe("rg-14.1.1");
    }

    /// <summary>real-world 示例：fd 风格（版本号保留，.tar.gz 作为复合扩展名保留）。</summary>
    [Fact]
    public void StripPlatformSuffix_FdStyle()
    {
        CommandHelpers.StripPlatformSuffix("fd-v10.2.0-x86_64-linux-gnu.tar.gz").ShouldBe("fd-v10.2.0.tar.gz");
    }

    /// <summary>real-world 示例：bat 风格 Windows（版本号不在剥离范围）。</summary>
    [Fact]
    public void StripPlatformSuffix_BatWindowsStyle()
    {
        CommandHelpers.StripPlatformSuffix("bat-v0.25.0-x86_64-windows-msvc").ShouldBe("bat-v0.25.0");
    }

    /// <summary>real-world 示例：带版本号的 Windows exe（版本号保留）。</summary>
    [Fact]
    public void StripPlatformSuffix_VersionedWindowsExe()
    {
        CommandHelpers.StripPlatformSuffix("tool-1.0.0-windows-amd64.exe").ShouldBe("tool-1.0.0.exe");
    }

    /// <summary>结果为空时应返回原始文件名。</summary>
    [Fact]
    public void StripPlatformSuffix_AllStripped_ReturnsOriginal()
    {
        // 文件名只有平台/架构信息时，避免返回空字符串
        CommandHelpers.StripPlatformSuffix("amd64").ShouldBe("amd64");
    }
}
