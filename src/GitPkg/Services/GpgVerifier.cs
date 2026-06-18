using System.Diagnostics;
using GitPkg.Models;

namespace GitPkg.Services;

/// <summary>
/// GPG 签名验证器，通过调用系统 gpg 命令行工具验证文件的 GPG 签名。
/// </summary>
public class GpgVerifier
{
    /// <summary>
    /// 使用 GPG 验证归档文件的签名。
    /// </summary>
    /// <param name="archivePath">待验证的归档文件路径。</param>
    /// <param name="signaturePath">签名文件路径（.asc / .sig）。</param>
    /// <param name="keyId">可选的 GPG 密钥 ID。</param>
    /// <returns>验证通过返回 true。</returns>
    /// <exception cref="InvalidOperationException">系统中未安装 gpg 命令。</exception>
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

    /// <summary>
    /// 在 Release 资产列表中查找与目标文件配套的签名文件（.asc / .sig / .gpg）。
    /// </summary>
    /// <returns>匹配的签名资产，未找到则返回 null。</returns>
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
