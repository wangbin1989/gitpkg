using System.Formats.Tar;
using System.IO.Compression;

namespace GitPkg.Services;

/// <summary>
/// 归档文件解压器，支持 .tar.gz、.tar、.zip 格式以及裸二进制文件。
/// </summary>
public class ArchiveExtractor
{
    /// <summary>
    /// 将归档文件解压到目标目录，自动根据扩展名选择解压方式。
    /// 非归档文件（裸二进制）将被直接复制到目标目录。
    /// 在 Unix 系统上，自动为解压/复制的文件添加可执行权限。
    /// </summary>
    /// <param name="archivePath">归档文件路径。</param>
    /// <param name="destDir">目标目录（自动创建）。</param>
    public async Task ExtractAsync(string archivePath, string destDir, CancellationToken ct = default)
    {
        Directory.CreateDirectory(destDir);

        var ext = Path.GetExtension(archivePath).ToLowerInvariant();
        var name = Path.GetFileNameWithoutExtension(archivePath).ToLowerInvariant();

        if (ext == ".zip")
        {
            ZipFile.ExtractToDirectory(archivePath, destDir, overwriteFiles: true);
            EnsureExecutablePermissions(destDir);
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
            // 非归档文件视为裸二进制，直接复制到目标目录
            var destPath = Path.Combine(destDir, Path.GetFileName(archivePath));
            File.Copy(archivePath, destPath, overwrite: true);
            EnsureExecutablePermission(destPath);
        }
    }

    /// <summary>在 Unix 系统上为文件添加可执行权限（chmod +x）。</summary>
    private static void EnsureExecutablePermission(string filePath)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(filePath, File.GetUnixFileMode(filePath) | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
        }
    }

    /// <summary>在 Unix 系统上为目录中的所有文件添加可执行权限。</summary>
    private static void EnsureExecutablePermissions(string dirPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            foreach (var file in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
            {
                EnsureExecutablePermission(file);
            }
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
