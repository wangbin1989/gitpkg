using System.Security.Cryptography;

namespace GitPkg.Services;

/// <summary>
/// SHA256 校验器，提供文件哈希计算、校验和验证以及校验文件解析功能。
/// </summary>
public class Sha256Verifier
{
    /// <summary>异步计算文件的 SHA256 哈希值（小写十六进制）。</summary>
    public async Task<string> ComputeHashAsync(string filePath, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>验证文件哈希是否与期望值匹配（忽略大小写）。</summary>
    public bool Verify(string filePath, string expectedHash)
    {
        var actual = ComputeHash(filePath);
        return string.Equals(actual, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// 解析校验文件内容，提取指定文件的期望哈希值。
    /// 支持 "hash  filename" 和 "hash *filename" 两种常见格式。
    /// </summary>
    /// <returns>匹配的哈希值（小写），未找到则返回 null。</returns>
    public static string? ParseChecksum(string checksumFileContent, string targetFileName)
    {
        var lines = checksumFileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            // Format: "<hash>  <filename>" or "<hash> *<filename>"
            var parts = trimmed.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var fileName = parts[1].TrimStart('*').Trim();
                if (fileName.Equals(targetFileName, StringComparison.OrdinalIgnoreCase))
                    return parts[0].Trim().ToLowerInvariant();
            }
        }
        return null;
    }
}
