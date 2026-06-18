namespace GitPkg.Services;

/// <summary>
/// 可执行文件查找工具，提供跨平台的二进制文件识别和目录探针。
/// </summary>
public static class ExecutableFinder
{
    /// <summary>在目录中查找可执行文件（排除文档类文件如 LICENSE、README）。</summary>
    public static List<string> FindExecutables(string dir)
    {
        if (!Directory.Exists(dir)) return [];

        var bins = new List<string>();
        foreach (var file in Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
            if (name is "license" or "readme" or "changelog" or "copying")
                continue;

            if (OperatingSystem.IsWindows())
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is ".exe" or ".bat" or ".cmd")
                    bins.Add(file);
            }
            else
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext == "" || ext == ".sh")
                    bins.Add(file);
            }
        }

        return bins;
    }

    /// <summary>查找包含可执行文件的目录（优先 bin 子目录）。</summary>
    public static string FindExecutableDir(string installDir)
    {
        if (FindExecutables(installDir).Count > 0)
            return installDir;

        foreach (var sub in new[] { "bin" })
        {
            var path = Path.Combine(installDir, sub);
            if (Directory.Exists(path) && FindExecutables(path).Count > 0)
                return path;
        }

        return installDir;
    }
}
