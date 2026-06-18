using System.Formats.Tar;
using System.IO.Compression;

namespace GitPkg.Services;

/// <summary>
/// 归档文件解压器，支持 .tar.gz、.tar 和 .zip 格式。
/// </summary>
public class ArchiveExtractor
{
    /// <summary>
    /// 将归档文件解压到目标目录，自动根据扩展名选择解压方式。
    /// </summary>
    /// <param name="archivePath">归档文件路径。</param>
    /// <param name="destDir">目标目录（自动创建）。</param>
    /// <exception cref="NotSupportedException">不支持的归档格式。</exception>
    public async Task ExtractAsync(string archivePath, string destDir, CancellationToken ct = default)
    {
        Directory.CreateDirectory(destDir);

        var ext = Path.GetExtension(archivePath).ToLowerInvariant();
        var name = Path.GetFileNameWithoutExtension(archivePath).ToLowerInvariant();

        if (ext == ".zip")
        {
            ZipFile.ExtractToDirectory(archivePath, destDir, overwriteFiles: true);
        }
        else if (ext == ".gz" && name.EndsWith(".tar"))
        {
            await ExtractTarGzAsync(archivePath, destDir, ct);
        }
        else if (ext == ".tar")
        {
            await ExtractTarAsync(archivePath, destDir, ct);
        }
        else
        {
            throw new NotSupportedException($"不支持的归档格式: {ext}");
        }
    }

    /// <summary>解压 .tar.gz 归档文件。</summary>
    private static async Task ExtractTarGzAsync(string archivePath, string destDir, CancellationToken ct)
    {
        await using var fileStream = File.OpenRead(archivePath);
        await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        await TarFile.ExtractToDirectoryAsync(gzipStream, destDir, overwriteFiles: true, cancellationToken: ct);
    }

    /// <summary>解压 .tar 归档文件。</summary>
    private static async Task ExtractTarAsync(string archivePath, string destDir, CancellationToken ct)
    {
        await TarFile.ExtractToDirectoryAsync(archivePath, destDir, overwriteFiles: true, cancellationToken: ct);
    }
}
