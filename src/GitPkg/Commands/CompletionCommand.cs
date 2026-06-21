using System.CommandLine;

namespace GitPkg.Commands;

/// <summary>
/// completion 命令：输出 shell 自动补全脚本，用于 eval 集成。
/// 底层利用 System.CommandLine 内置的 [suggest] 指令获取补全候选项。
/// </summary>
public static class CompletionCommand
{
    /// <summary>创建 completion 命令。</summary>
    public static Command Create()
    {
        var cmd = new Command("completion", "输出 shell 自动补全脚本（用于 eval）");

        var shellArg = new Argument<string>("shell") { Description = "目标 shell: zsh, bash, fish, powershell" };
        cmd.Add(shellArg);

        cmd.SetAction((parseResult, ct) =>
        {
            try
            {
                var shell = (parseResult.GetValue(shellArg) ?? "").ToLowerInvariant();

                var script = shell switch
                {
                    "zsh" => ZshCompletion(),
                    "bash" => BashCompletion(),
                    "fish" => FishCompletion(),
                    "powershell" or "pwsh" => PowershellCompletion(),
                    _ => throw new ArgumentException(
                        $"不支持的 shell: '{shell}'。支持: zsh, bash, fish, powershell")
                };

                Console.Write(script);
                return Task.FromResult(0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"gitpkg completion: {ex.Message}");
                return Task.FromResult(1);
            }
        });

        return cmd;
    }

    /// <summary>
    /// zsh 补全脚本。
    /// 使用 compdef 注册补全函数，通过 [suggest] 指令获取候选项。
    /// </summary>
    private static string ZshCompletion() => """
        # gitpkg zsh completion — 添加到 ~/.zshrc 后 source 即可
        #   eval "$(gitpkg completion zsh)"

        _gitpkg() {
            local -a completions
            completions=("${(@f)$("${words[@]:0:1}" "[suggest]" "${words[@]:1}" 2>/dev/null)}")
            _describe '' completions
        }

        compdef _gitpkg gitpkg

        """;

    /// <summary>
    /// bash 补全脚本。
    /// 使用 complete -F 注册补全函数，通过 [suggest] 指令获取候选项。
    /// </summary>
    private static string BashCompletion() => """
        # gitpkg bash completion — 添加到 ~/.bashrc 后 source 即可
        #   eval "$(gitpkg completion bash)"
        _gitpkg_completion() {
            local IFS=$'\n'
            COMPREPLY=($("${COMP_WORDS[0]}" "[suggest]" "${COMP_WORDS[@]:1}" 2>/dev/null))
        }

        complete -F _gitpkg_completion gitpkg

        """;

    /// <summary>
    /// fish 补全脚本。
    /// 定义补全函数并通过 complete -c 注册。
    /// </summary>
    private static string FishCompletion() => """
        # gitpkg fish completion — 保存到 ~/.config/fish/completions/gitpkg.fish
        #   gitpkg completion fish > ~/.config/fish/completions/gitpkg.fish
        function _gitpkg_completion
            command gitpkg "[suggest]" (commandline -opc)[2..-1] 2>/dev/null
        end

        complete -c gitpkg -f -a '(_gitpkg_completion)'

        """;

    /// <summary>
    /// PowerShell 补全脚本。
    /// 使用 Register-ArgumentCompleter 注册 Native 补全。
    /// </summary>
    private static string PowershellCompletion() => """
        # gitpkg powershell completion — 添加到 $PROFILE 后重启终端
        #   gitpkg completion powershell >> $PROFILE
        Register-ArgumentCompleter -Native -CommandName gitpkg -ScriptBlock {
            param($wordToComplete, $commandAst, $cursorPosition)
            gitpkg "[suggest]" ($commandAst.ToString() -split '\s+')[1..-1] 2>$null | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
            }
        }

        """;
}
