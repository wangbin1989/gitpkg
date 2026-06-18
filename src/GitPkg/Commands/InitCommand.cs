using System.CommandLine;

namespace GitPkg.Commands;

/// <summary>
/// init 命令：输出 shell 初始化脚本到标准输出，用于 eval 集成。
/// 包含两个部分：1) PATH 设置（指向 gitpkg 安装目录） 2) 自动补全注册。
/// 用法：eval "$(gitpkg init zsh)"
/// </summary>
public static class InitCommand
{
    /// <summary>创建 init 命令。</summary>
    public static Command Create()
    {
        var cmd = new Command("init", "输出 shell 初始化脚本（含 PATH 和自动补全）");

        var shellArg = new Argument<string>("shell") { Description = "目标 shell: zsh, bash, fish, powershell" };
        cmd.Add(shellArg);

        cmd.SetAction((parseResult, ct) =>
        {
            try
            {
                var shell = (parseResult.GetValue(shellArg) ?? "").ToLowerInvariant();

                var binDir = Path.GetDirectoryName(Environment.ProcessPath)
                             ?? throw new InvalidOperationException("无法确定 gitpkg 安装目录");

                // PATH 设置 + 自动补全注册
                var script = shell switch
                {
                    "zsh" => PosixInit("zsh", binDir) + ZshCompletion,
                    "bash" => PosixInit("bash", binDir) + BashCompletion,
                    "fish" => FishInit(binDir) + FishCompletion,
                    "powershell" or "pwsh" => PowershellInit(binDir) + PowershellCompletion,
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

    /// <summary>生成 POSIX Shell（zsh / bash）的 export PATH 命令。使用单引号包裹路径防止命令注入。</summary>
    private static string PosixInit(string shell, string binDir)
        => $"# gitpkg shell init for {shell}\n"
         + $"export PATH={EscapeForPosixShell(binDir)}:$PATH\n";

    /// <summary>生成 Fish Shell 的 fish_add_path 命令。Fish 与 POSIX Shell 共享相同的单引号转义策略。</summary>
    private static string FishInit(string binDir)
        => $"# gitpkg shell init for fish\n"
         + $"fish_add_path {EscapeForPosixShell(binDir)}\n";

    /// <summary>生成 PowerShell 的 $env:Path 设置命令。路径放入单引号，分号后拼接原 $env:Path。</summary>
    private static string PowershellInit(string binDir)
        => $"# gitpkg shell init for powershell\n"
         + $"$env:Path = '{EscapeForPowershell(binDir)};' + $env:Path\n";

    // ---- 自动补全脚本 ----

    /// <summary>zsh 补全：通过 compdef 注册，由 [suggest] 指令获取候选项。</summary>
    private const string ZshCompletion = """


        # gitpkg zsh completion
        #compdef gitpkg

        _gitpkg() {
            local -a completions
            completions=("${(@f)$("${words[@]:0:1}" "[suggest]" "${words[@]:1}" 2>/dev/null)}")
            _describe '' completions
        }

        _gitpkg

        """;

    /// <summary>bash 补全：通过 complete -F 注册。</summary>
    private const string BashCompletion = """


        # gitpkg bash completion
        _gitpkg_completion() {
            local IFS=$'\n'
            COMPREPLY=($("${COMP_WORDS[0]}" "[suggest]" "${COMP_WORDS[@]:1}" 2>/dev/null))
        }

        complete -F _gitpkg_completion gitpkg

        """;

    /// <summary>fish 补全：定义补全函数并通过 complete -c 注册。</summary>
    private const string FishCompletion = """


        # gitpkg fish completion
        function _gitpkg_completion
            command gitpkg "[suggest]" (commandline -opc)[2..-1] 2>/dev/null
        end

        complete -c gitpkg -f -a '(_gitpkg_completion)'

        """;

    /// <summary>PowerShell 补全：通过 Register-ArgumentCompleter 注册。</summary>
    private const string PowershellCompletion = """


        # gitpkg powershell completion
        Register-ArgumentCompleter -Native -CommandName gitpkg -ScriptBlock {
            param($wordToComplete, $commandAst, $cursorPosition)
            gitpkg "[suggest]" ($commandAst.ToString() -split '\s+')[1..-1] 2>$null | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
            }
        }

        """;
}
