using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using GitPkg.Models;

namespace GitPkg.Services;

public class GitHubService
{
    private readonly HttpClient _http;
    private readonly AppJsonContext _jsonContext = new();

    public GitHubService(HttpClient http)
    {
        _http = http;
    }

    public async Task<GitHubRelease> GetLatestReleaseAsync(string owner, string repo, CancellationToken ct = default)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
        return await GetAsync(url, _jsonContext.GitHubRelease, ct);
    }

    public async Task<GitHubRelease> GetReleaseByTagAsync(string owner, string repo, string tag, CancellationToken ct = default)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/releases/tags/{tag}";
        return await GetAsync(url, _jsonContext.GitHubRelease, ct);
    }

    public async Task<GitHubRepo> GetRepoAsync(string owner, string repo, CancellationToken ct = default)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}";
        return await GetAsync(url, _jsonContext.GitHubRepo, ct);
    }

    public async Task<string> DownloadStringAsync(string url, CancellationToken ct = default)
    {
        return await _http.GetStringAsync(url, ct);
    }

    public async Task<Stream> GetStreamAsync(string url, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(ct);
    }

    public async Task DownloadFileAsync(string url, string destPath, Action<long, long>? onProgress = null, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var fileStream = File.Create(destPath);

        var buffer = new byte[8192];
        var downloaded = 0L;
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
            downloaded += read;
            onProgress?.Invoke(downloaded, totalBytes);
        }
    }

    private async Task<T> GetAsync<T>(string url, JsonTypeInfo<T> typeInfo, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "gitpkg");
        request.Headers.Add("Accept", "application/vnd.github+json");

        using var response = await _http.SendAsync(request, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new HttpRequestException($"资源不存在: {url}");

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize(json, typeInfo)
               ?? throw new InvalidOperationException("API 返回空响应");
    }
}
