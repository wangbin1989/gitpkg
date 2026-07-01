using GitPkg.Services;
using Shouldly;

namespace GitPkg.Tests.Models;

/// <summary>
/// 可执行文件查找器单元测试。
/// 覆盖跨平台二进制识别和可执行目录探针逻辑。
/// </summary>
public class ExecutableFinderTests
{
    /// <summary>创建含指定文件的临时目录，测试结束后清理。</summary>
    private static string CreateTempDirWithFiles(params string[] fileNames)
    {
        var dir = Path.Combine(Path.GetTempPath(), $"gitpkg_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        foreach (var name in fileNames)
            File.WriteAllText(Path.Combine(dir, name), "");
        return dir;
    }

    /// <summary>不存在或空目录应返回空列表。</summary>
    [Fact]
    public void FindExecutables_EmptyDir_ReturnsEmpty()
    {
        var dir = CreateTempDirWithFiles();
        try
        {
            var result = ExecutableFinder.FindExecutables(dir);
            result.ShouldBeEmpty();
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>不存在的目录应返回空列表。</summary>
    [Fact]
    public void FindExecutables_NonexistentDir_ReturnsEmpty()
    {
        var result = ExecutableFinder.FindExecutables("/nonexistent/path");
        result.ShouldBeEmpty();
    }

    /// <summary>无扩展名的文件应被识别为可执行文件（Unix）。</summary>
    [Fact]
    public void FindExecutables_NoExtension_ReturnsFile()
    {
        var dir = CreateTempDirWithFiles("mybinary");
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                var result = ExecutableFinder.FindExecutables(dir);
                result.ShouldHaveSingleItem();
                Path.GetFileName(result[0]).ShouldBe("mybinary");
            }
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>.sh 扩展名的文件应被识别为可执行文件。</summary>
    [Fact]
    public void FindExecutables_ShExtension_ReturnsFile()
    {
        var dir = CreateTempDirWithFiles("script.sh");
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                var result = ExecutableFinder.FindExecutables(dir);
                result.ShouldHaveSingleItem();
            }
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>文档类文件（LICENSE、README、CHANGELOG、COPYING）应被排除。</summary>
    [Theory]
    [InlineData("LICENSE")]
    [InlineData("README")]
    [InlineData("CHANGELOG")]
    [InlineData("COPYING")]
    public void FindExecutables_DocFiles_Excluded(string docName)
    {
        var dir = CreateTempDirWithFiles(docName);
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                var result = ExecutableFinder.FindExecutables(dir);
                result.ShouldBeEmpty();
            }
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>Windows 平台：.exe/.bat/.cmd 应被识别。</summary>
    [Fact]
    public void FindExecutables_WindowsExtensions_Recognized()
    {
        var dir = CreateTempDirWithFiles("tool.exe", "setup.bat", "run.cmd", "readme.txt");
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var result = ExecutableFinder.FindExecutables(dir);
                result.Count.ShouldBe(3);
            }
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>根目录有可执行文件时 FindExecutableDir 应直接返回根目录。</summary>
    [Fact]
    public void FindExecutableDir_ExecutablesInRoot_ReturnsRoot()
    {
        var dir = CreateTempDirWithFiles("tool");
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                var result = ExecutableFinder.FindExecutableDir(dir);
                result.ShouldBe(dir);
            }
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>可执行文件在 bin 子目录中时应返回 bin 子目录路径。</summary>
    [Fact]
    public void FindExecutableDir_ExecutablesInBinSubdir_ReturnsBinSubdir()
    {
        var dir = CreateTempDirWithFiles();
        var binDir = Path.Combine(dir, "bin");
        try
        {
            Directory.CreateDirectory(binDir);
            File.WriteAllText(Path.Combine(binDir, "tool"), "");

            if (!OperatingSystem.IsWindows())
            {
                var result = ExecutableFinder.FindExecutableDir(dir);
                result.ShouldBe(binDir);
            }
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>目录中无可执行文件时应返回原始目录。</summary>
    [Fact]
    public void FindExecutableDir_NoExecutables_ReturnsOriginalDir()
    {
        var dir = CreateTempDirWithFiles("readme.txt", "LICENSE");
        try
        {
            var result = ExecutableFinder.FindExecutableDir(dir);
            result.ShouldBe(dir);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}
