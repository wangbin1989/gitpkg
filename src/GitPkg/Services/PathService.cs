namespace GitPkg.Services;

public static class PathService
{
    public static string? DetectShell()
    {
        // Check SHELL environment variable (macOS/Linux)
        var shell = Environment.GetEnvironmentVariable("SHELL");
        if (!string.IsNullOrEmpty(shell))
        {
            var name = Path.GetFileName(shell).ToLowerInvariant();
            if (name == "zsh") return "zsh";
            if (name == "bash") return "bash";
            if (name == "fish") return "fish";
        }

        // Windows: detect PowerShell
        if (OperatingSystem.IsWindows())
        {
            var psModulePath = Environment.GetEnvironmentVariable("PSModulePath");
            if (!string.IsNullOrEmpty(psModulePath)) return "powershell";
            return "cmd";
        }

        return "bash"; // default fallback
    }

    public static string GetConfigFilePath(string shell)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return shell.ToLowerInvariant() switch
        {
            "zsh" => Path.Combine(home, ".zshrc"),
            "bash" => Path.Combine(home, ".bashrc"),
            "fish" => Path.Combine(home, ".config", "fish", "config.fish"),
            "powershell" => Environment.GetEnvironmentVariable("PROFILE") ?? Path.Combine(home, "Documents", "PowerShell", "Microsoft.PowerShell_profile.ps1"),
            _ => Path.Combine(home, ".bashrc")
        };
    }

    public static bool AddToPath(string installDir, string shell)
    {
        var configFile = GetConfigFilePath(shell);
        var line = shell switch
        {
            "fish" => $"fish_add_path \"{installDir}\"",
            "powershell" => $"$env:Path += \";{installDir}\"",
            _ => $"export PATH=\"{installDir}:$PATH\""
        };

        // Read existing content
        string content;
        if (File.Exists(configFile))
        {
            content = File.ReadAllText(configFile);
            if (content.Contains(installDir))
                return false; // Already in PATH
        }
        else
        {
            content = "";
        }

        // Ensure file ends with newline before appending
        if (content.Length > 0 && !content.EndsWith('\n'))
            content += "\n";

        content += $"\n# Added by gitpkg\n{line}\n";

        // Ensure directory exists
        var dir = Path.GetDirectoryName(configFile);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(configFile, content);
        return true;
    }
}
