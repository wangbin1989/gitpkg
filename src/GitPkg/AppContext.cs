namespace GitPkg;

/// <summary>
/// 应用程序全局上下文，持有共享的 <see cref="HttpClient"/> 实例。
/// 在程序启动时调用 <see cref="Initialize"/> 完成配置。
/// </summary>
public static class GitPkgApp
{
    /// <summary>全局共享的 HttpClient 实例，已预设 User-Agent 和超时时间。</summary>
    public static HttpClient Http { get; private set; } = null!;

    /// <summary>初始化 HttpClient（设置默认请求头、超时等）。</summary>
    public static void Initialize()
    {
        Http = new HttpClient();
        Http.DefaultRequestHeaders.Add("User-Agent", "gitpkg");
        Http.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        Http.Timeout = TimeSpan.FromMinutes(10);
    }
}
