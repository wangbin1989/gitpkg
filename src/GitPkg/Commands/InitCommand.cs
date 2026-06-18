using System.CommandLine;

namespace GitPkg.Commands;

/// <summary>
/// init 命令：输出 shell 初始化脚本到标准输出，用于 eval 集成。
/// 自动检测 gitpkg 二进制所在目录，生成对应的 PATH 设置命令。
/// </summary>
public static class InitCommand
{
    /// <summary>创建 init 命令。</summary>
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

    /// <summary>生成 POSIX Shell（zsh / bash）的 export PATH 命令。</summary>
    private static string PosixInit(string shell, string binDir)
        => $"# gitpkg shell init for {shell}\n"
         + $"export PATH=\"{binDir}:$PATH\"\n";

    /// <summary>生成 Fish Shell 的 fish_add_path 命令。</summary>
    private static string FishInit(string binDir)
        => $"# gitpkg shell init for fish\n"
         + $"fish_add_path \"{binDir}\"\n";

    /// <summary>生成 PowerShell 的 $env:Path 设置命令。</summary>
    private static string PowershellInit(string binDir)
        => $"# gitpkg shell init for powershell\n"
         + $"$env:Path = \"{binDir};$env:Path\"\n";
}
