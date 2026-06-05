using System.Runtime.InteropServices;

namespace GitPkg.Models;

public record PlatformInfo
{
    public string OS { get; init; }
    public string Arch { get; init; }

    public PlatformInfo(string os, string arch)
    {
        OS = os;
        Arch = arch;
    }

    public static PlatformInfo Current()
    {
        var os = GetOS();
        var arch = GetArch();
        return new PlatformInfo(os, arch);
    }

    private static string GetOS()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macos";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
        return RuntimeInformation.OSDescription.ToLowerInvariant();
    }

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

    public override string ToString() => $"{OS}/{Arch}";
}
