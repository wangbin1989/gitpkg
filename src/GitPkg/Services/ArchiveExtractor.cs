using System.Formats.Tar;
using System.IO.Compression;

namespace GitPkg.Services;

public class ArchiveExtractor
{
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

    private static async Task ExtractTarGzAsync(string archivePath, string destDir, CancellationToken ct)
    {
        await using var fileStream = File.OpenRead(archivePath);
        await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        await TarFile.ExtractToDirectoryAsync(gzipStream, destDir, overwriteFiles: true, cancellationToken: ct);
    }

    private static async Task ExtractTarAsync(string archivePath, string destDir, CancellationToken ct)
    {
        await TarFile.ExtractToDirectoryAsync(archivePath, destDir, overwriteFiles: true, cancellationToken: ct);
    }
}
