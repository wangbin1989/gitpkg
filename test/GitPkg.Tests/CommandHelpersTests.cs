using GitPkg.Commands;

namespace GitPkg.Tests;

/// <summary>
/// 命令辅助方法单元测试。
/// 覆盖 FormatSize 格式化逻辑和 PromptAssetSelection 回退行为。
/// </summary>
public class CommandHelpersTests
{
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
}
