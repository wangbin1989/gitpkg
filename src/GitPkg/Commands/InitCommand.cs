using System.CommandLine;

namespace GitPkg.Commands;

/// <summary>
/// init 命令：输出 shell 初始化脚本到标准输出，用于 eval 集成。
/// 设置 GITPKG_HOME 环境变量并将其 bin 目录加入 PATH，用法：eval "$(gitpkg init zsh)"。
/// 如需自动补全，请单独使用 completion 命令。
/// </summary>
public static class InitCommand
{
    /// <summary>创建 init 命令。</summary>
    public static Command Create()
    {
        var cmd = new Command("init", "输出 shell 初始化脚本（用于 eval）");

        var shellArg = new Argument<string>("shell") { Description = "目标 shell: zsh, bash, fish, powershell, cmd" };
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
                    "cmd" => CmdInit(binDir),
                    _ => throw new ArgumentException(
                        $"不支持的 shell: '{shell}'。支持: zsh, bash, fish, powershell, cmd")
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

    /// <summary>
    /// 对路径中的单引号进行 POSIX Shell 兼容转义。
    /// 用单引号包裹全文，内部的 ' 替换为 '\''（结束引号 → 转义单引号 → 恢复引号）。
    /// 在 zsh / bash / fish 中通用。
    /// </summary>
    internal static string EscapeForPosixShell(string path)
        => $"'{path.Replace("'", "'\\''")}'";

    /// <summary>
    /// 对路径进行 PowerShell 单引号字符串转义。
    /// PowerShell 中两个连续单引号 '' 表示一个字面单引号。
    /// 返回不带外层引号的内容，由调用方拼接。
    /// </summary>
    internal static string EscapeForPowershell(string path)
        => path.Replace("'", "''");

    /// <summary>生成 POSIX Shell（zsh / bash）的初始化脚本。设置 GITPKG_HOME 环境变量并将 bin 目录加入 PATH。</summary>
    private static string PosixInit(string shell, string binDir)
        => $"# gitpkg shell init for {shell}\n"
         + $"export GITPKG_HOME={EscapeForPosixShell(Path.GetDirectoryName(binDir)!)}\n"
         + $"export PATH=\"$GITPKG_HOME/bin\":$PATH\n";

    /// <summary>生成 Fish Shell 的初始化脚本。设置 GITPKG_HOME 环境变量并将 bin 目录加入 PATH。</summary>
    private static string FishInit(string binDir)
        => $"# gitpkg shell init for fish\n"
         + $"set -gx GITPKG_HOME {EscapeForPosixShell(Path.GetDirectoryName(binDir)!)}\n"
         + $"fish_add_path \"$GITPKG_HOME/bin\"\n";

    /// <summary>生成 PowerShell 的初始化脚本。设置 GITPKG_HOME 环境变量并将 bin 目录加入 PATH。</summary>
    private static string PowershellInit(string binDir)
        => $"# gitpkg shell init for powershell\n"
         + $"$env:GITPKG_HOME = '{EscapeForPowershell(Path.GetDirectoryName(binDir)!)}'\n"
         + $"$env:Path = \"$env:GITPKG_HOME\\bin;\" + $env:Path\n";

    /// <summary>
    /// 生成 cmd.exe 的初始化脚本。设置 GITPKG_HOME 环境变量并将 bin 目录加入 PATH。
    /// 使用 setx 永久设置 GITPKG_HOME，用 set 临时设置 PATH。
    /// </summary>
    private static string CmdInit(string binDir)
    {
        string gitpkgHome = Path.GetDirectoryName(binDir)!;
        string escapedHome = EscapeForCmd(gitpkgHome);
        return $"@echo off\n"
             + $"REM gitpkg shell init for cmd\n"
             + $"set GITPKG_HOME={escapedHome}\n"
             + $"set PATH=%GITPKG_HOME%\\bin;%PATH%\n";
    }

    /// <summary>
    /// 对路径进行 cmd.exe 转义。
    /// cmd.exe 的 set 命令中，% 需要转义为 %%。
    /// </summary>
    internal static string EscapeForCmd(string path)
        => path.Replace("%", "%%");
}
