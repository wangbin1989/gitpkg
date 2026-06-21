using System.CommandLine;

namespace GitPkg.Commands;

/// <summary>
/// completion 命令：输出 shell 自动补全脚本，用于 eval 集成。
/// 底层使用静态定义的子命令和选项列表提供补全。
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
    /// 使用 compdef 注册补全函数，通过静态定义的子命令和选项列表提供补全。
    /// </summary>
    private static string ZshCompletion() => """
        # gitpkg zsh completion — 添加到 ~/.zshrc 后 source 即可
        #   eval "$(gitpkg completion zsh)"

        _gitpkg() {
            local -a completions
            local cur="${words[CURRENT]}"
            local prev="${words[CURRENT-1]}"
            local cmd="${words[2]}"

            case "$cmd" in
                install)
                    completions=(--from --help)
                    ;;
                update|uninstall|info)
                    completions=(--help)
                    ;;
                init|completion)
                    completions=(zsh bash fish powershell)
                    ;;
                manifest)
                    completions=(export --help)
                    ;;
                list|outdated|self-update)
                    completions=(--help)
                    ;;
                *)
                    completions=(install update uninstall outdated list info init completion manifest self-update --help --version)
                    ;;
            esac

            _describe '' completions
        }

        compdef _gitpkg gitpkg

        """;

    /// <summary>
    /// bash 补全脚本。
    /// 使用 complete -F 注册补全函数，通过 compgen 匹配静态定义的选项列表。
    /// </summary>
    private static string BashCompletion() => """
        # gitpkg bash completion — 添加到 ~/.bashrc 后 source 即可
        #   eval "$(gitpkg completion bash)"
        _gitpkg_completion() {
            local cur="${COMP_WORDS[COMP_CWORD]}"
            local cmd="${COMP_WORDS[1]}"
            local opts=""

            case "$cmd" in
                install)
                    opts="--from --help"
                    ;;
                update|uninstall|info)
                    opts="--help"
                    ;;
                init|completion)
                    opts="zsh bash fish powershell"
                    ;;
                manifest)
                    opts="export --help"
                    ;;
                list|outdated|self-update)
                    opts="--help"
                    ;;
                *)
                    opts="install update uninstall outdated list info init completion manifest self-update --help --version"
                    ;;
            esac

            COMPREPLY=($(compgen -W "$opts" -- "$cur"))
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

        # 根命令补全
        complete -c gitpkg -f -n '__fish_use_subcommand' -a 'install' -d '安装工具'
        complete -c gitpkg -f -n '__fish_use_subcommand' -a 'update' -d '更新工具'
        complete -c gitpkg -f -n '__fish_use_subcommand' -a 'uninstall' -d '卸载工具'
        complete -c gitpkg -f -n '__fish_use_subcommand' -a 'outdated' -d '检查更新'
        complete -c gitpkg -f -n '__fish_use_subcommand' -a 'list' -d '列出已安装工具'
        complete -c gitpkg -f -n '__fish_use_subcommand' -a 'info' -d '查看工具详情'
        complete -c gitpkg -f -n '__fish_use_subcommand' -a 'init' -d '输出 shell 初始化脚本'
        complete -c gitpkg -f -n '__fish_use_subcommand' -a 'completion' -d '输出 shell 补全脚本'
        complete -c gitpkg -f -n '__fish_use_subcommand' -a 'manifest' -d '清单管理'
        complete -c gitpkg -f -n '__fish_use_subcommand' -a 'self-update' -d '更新 gitpkg 自身'

        # install 子命令选项
        complete -c gitpkg -f -n '__fish_seen_subcommand_from install' -l from -d '从清单文件批量安装'
        complete -c gitpkg -f -n '__fish_seen_subcommand_from install' -l help -d '帮助'

        # manifest 子命令补全
        complete -c gitpkg -f -n '__fish_seen_subcommand_from manifest' -a 'export' -d '导出清单文件'

        # init / completion 子命令补全
        complete -c gitpkg -f -n '__fish_seen_subcommand_from init completion' -a 'zsh bash fish powershell'

        # update / uninstall / info 子命令选项
        complete -c gitpkg -f -n '__fish_seen_subcommand_from update uninstall info' -l help -d '帮助'

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
            $tokens = $commandAst.ToString() -split '\s+'
            $cmd = if ($tokens.Length -ge 2) { $tokens[1] } else { "" }

            $completions = switch ($cmd) {
                "install"   { @('--from', '--help') }
                "update"    { @('--help') }
                "uninstall" { @('--help') }
                "info"      { @('--help') }
                "init"      { @('zsh', 'bash', 'fish', 'powershell') }
                "completion"{ @('zsh', 'bash', 'fish', 'powershell') }
                "manifest"  { @('export', '--help') }
                "list"      { @('--help') }
                "outdated"  { @('--help') }
                "self-update" { @('--help') }
                default     { @('install', 'update', 'uninstall', 'outdated', 'list', 'info', 'init', 'completion', 'manifest', 'self-update', '--help', '--version') }
            }

            $completions | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
            }
        }

        """;
}
