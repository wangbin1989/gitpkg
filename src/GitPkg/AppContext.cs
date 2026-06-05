namespace GitPkg;

public static class GitPkgApp
{
    public static HttpClient Http { get; private set; } = null!;

    public static void Initialize()
    {
        Http = new HttpClient();
        Http.DefaultRequestHeaders.Add("User-Agent", "gitpkg");
        Http.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        Http.Timeout = TimeSpan.FromMinutes(10);
    }
}
