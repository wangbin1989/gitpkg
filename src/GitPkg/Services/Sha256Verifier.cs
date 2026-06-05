using System.Security.Cryptography;

namespace GitPkg.Services;

public class Sha256Verifier
{
    public async Task<string> ComputeHashAsync(string filePath, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexStringLower(hash);
    }

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
