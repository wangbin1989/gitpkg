using GitPkg.Models;

namespace GitPkg.Tests;

/// <summary>
/// 平台信息检测单元测试。
/// 验证运行时平台信息（OS + 架构）的获取。
/// </summary>
public class PlatformInfoTests
{
    /// <summary>当前平台信息应有非空的 OS 和 Arch。</summary>
    [Fact]
    public void Current_ReturnsValidPlatform()
    {
        var platform = PlatformInfo.Current();

        Assert.NotNull(platform.OS);
        Assert.NotNull(platform.Arch);
        Assert.NotEmpty(platform.OS);
        Assert.NotEmpty(platform.Arch);
    }
}
