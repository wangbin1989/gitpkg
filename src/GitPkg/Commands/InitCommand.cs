using System.CommandLine;

namespace GitPkg.Commands;

public static class InitCommand
{
    public static Command Create()
    {
        var cmd = new Command("init", "输出 shell 初始化脚本（用于 eval）");

        var shellArg = new Argument<string>("shell") { Description = "目标 shell: zsh, bash, fish, powershell" };
        cmd.Add(shellArg);

        cmd.SetAction((parseResult, ct) =>
        {
            try
            {
                var shell = (parseResult.GetValue(shellArg) ?? "").ToLowerInvariant();

                var binDir = Path.GetDirectoryName(Environment.ProcessPath)
                             ?? throw new InvalidOperationException("无法确定 gitpkg 安装目录");

                var script = shell switch
                {
                    "zsh" => PosixInit("zsh", binDir),
                    "bash" => PosixInit("bash", binDir),
                    "fish" => FishInit(binDir),
                    "powershell" or "pwsh" => PowershellInit(binDir),
                    _ => throw new ArgumentException(
                        $"不支持的 shell: '{shell}'。支持: zsh, bash, fish, powershell")
                };

                Console.Write(script);
                return Task.FromResult(0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"gitpkg init: {ex.Message}");
                return Task.FromResult(1);
            }
        });

        return cmd;
    }

    private static string PosixInit(string shell, string binDir)
        => $"# gitpkg shell init for {shell}\n"
         + $"export PATH=\"{binDir}:$PATH\"\n";

    private static string FishInit(string binDir)
        => $"# gitpkg shell init for fish\n"
         + $"fish_add_path \"{binDir}\"\n";

    private static string PowershellInit(string binDir)
        => $"# gitpkg shell init for powershell\n"
         + $"$env:Path = \"{binDir};$env:Path\"\n";
}
