using System.Runtime.InteropServices;

namespace GitPkg.Models;

/// <summary>
/// 当前运行平台的操作系统和架构信息，用于匹配 GitHub Release 中对应平台的资产。
/// </summary>
/// <param name="Os">操作系统标识符（macos / windows / linux）</param>
/// <param name="Arch">CPU 架构标识符（x64 / arm64）</param>
public record PlatformInfo(string Os, string Arch)
{
    /// <summary>获取当前运行时的平台信息。</summary>
    public static PlatformInfo Current()
    {
        var os = GetOs();
        var arch = GetArch();
        return new PlatformInfo(os, arch);
    }

    /// <summary>通过 <see cref="RuntimeInformation"/> 检测操作系统。</summary>
    private static string GetOs()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macos";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
        return RuntimeInformation.OSDescription.ToLowerInvariant();
    }

    /// <summary>通过 <see cref="RuntimeInformation.ProcessArchitecture"/> 检测 CPU 架构。</summary>
    private static string GetArch()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()
        };
    }

    public override string ToString() => $"{Os}/{Arch}";
}
