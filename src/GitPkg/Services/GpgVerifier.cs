using System.Diagnostics;
using GitPkg.Models;

namespace GitPkg.Services;

public class GpgVerifier
{
    public async Task<bool> VerifyAsync(
        string archivePath, string signaturePath,
        string? keyId = null, CancellationToken ct = default)
    {
        var args = new List<string> { "--verify" };

        if (keyId != null)
            args.Add($"--keyring {keyId}");

        args.Add(signaturePath);
        args.Add(archivePath);

        var psi = new ProcessStartInfo
        {
            FileName = "gpg",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        try
        {
            using var proc = Process.Start(psi);
            if (proc == null) return false;

            await proc.WaitForExitAsync(ct);
            return proc.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new InvalidOperationException("未安装 gpg 命令行工具，请先安装 GPG 或移除 --verify-gpg 选项");
        }
    }

    public static GitHubAsset? FindSignatureAsset(List<GitHubAsset> assets, string targetFileName)
    {
        var sigNames = new[]
        {
            targetFileName + ".asc",
            targetFileName + ".sig",
            targetFileName + ".gpg"
        };

        return assets.FirstOrDefault(a =>
            sigNames.Any(s => a.Name.Equals(s, StringComparison.OrdinalIgnoreCase)));
    }
}
