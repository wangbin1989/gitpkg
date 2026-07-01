using System.CommandLine;

namespace GitPkg.Commands;

/// <summary>
/// completion 命令：输出 shell 自动补全脚本。
/// 底层使用静态定义的子命令和选项列表提供补全。
/// update/uninstall/info 子命令支持动态补全已安装工具名称。
/// </summary>
public class CompletionCommand : Command
{
    /// <summary>创建 completion 命令。</summary>
    public CompletionCommand() : base("completion", "输出 shell 自动补全脚本")
    {
        var shellArg = new Argument<string>("shell") { Description = "目标 shell: zsh, bash, powershell (pwsh), cmd" };
        Add(shellArg);

        SetAction((parseResult, ct) =>
        {
            try
            {
                var shell = (parseResult.GetValue(shellArg) ?? "").ToLowerInvariant();

                var script = shell switch
                {
                    "zsh" => ZshCompletion(),
                    "bash" => BashCompletion(),
                    "powershell" or "pwsh" => PowershellCompletion(),
                    "cmd" => ClinkCompletion(),
                    _ => throw new ArgumentException(
                        $"不支持的 shell: '{shell}'。支持: zsh, bash, powershell (pwsh), cmd")
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
    }

    /// <summary>
    /// zsh 补全脚本。
    /// 使用 compdef 注册补全函数，通过静态定义的子命令和选项列表提供补全。
    /// update/uninstall/info 支持动态补全已安装工具名称。
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
                    # 动态补全已安装工具名称
                    local manifest="$HOME/.gitpkg/manifest.json"
                    if [[ -f "$manifest" ]]; then
                        completions=(${(f)"$(grep -o '"name":"[^"]*"' "$manifest" 2>/dev/null | sed 's/"name":"//;s/"//' 2>/dev/null)"})
                    fi
                    completions+=("--help")
                    ;;
                init|completion)
                    completions=(zsh bash powershell cmd)
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
    /// update/uninstall/info 支持动态补全已安装工具名称。
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
                    # 动态补全已安装工具名称
                    local manifest="$HOME/.gitpkg/manifest.json"
                    if [[ -f "$manifest" ]]; then
                        opts="$(grep -o '"name":"[^"]*"' "$manifest" 2>/dev/null | sed 's/"name":"//;s/"//' | tr '\n' ' ')--help"
                    else
                        opts="--help"
                    fi
                    ;;
                init|completion)
                    opts="zsh bash powershell cmd"
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
    /// PowerShell 补全脚本。
    /// 使用 Register-ArgumentCompleter 注册 Native 补全。
    /// update/uninstall/info 支持动态补全已安装工具名称。
    /// </summary>
    private static string PowershellCompletion() => """
        # gitpkg powershell completion — 添加到 $PROFILE 后重启终端
        #   gitpkg completion powershell >> $PROFILE
        Register-ArgumentCompleter -Native -CommandName gitpkg -ScriptBlock {
            param($wordToComplete, $commandAst, $cursorPosition)
            $tokens = $commandAst.ToString() -split '\s+'
            $cmd = if ($tokens.Length -ge 2) { $tokens[1] } else { "" }

            $manifestPath = Join-Path $env:USERPROFILE ".gitpkg" "manifest.json"

            $completions = switch ($cmd) {
                "install"   { @('--from', '--help') }
                "update"    {
                    # 动态补全已安装工具名称
                    $names = @()
                    if (Test-Path $manifestPath) {
                        $json = Get-Content $manifestPath -Raw | ConvertFrom-Json
                        $names = @($json.tools | ForEach-Object { $_.name })
                    }
                    $names + @('--help')
                }
                "uninstall" {
                    $names = @()
                    if (Test-Path $manifestPath) {
                        $json = Get-Content $manifestPath -Raw | ConvertFrom-Json
                        $names = @($json.tools | ForEach-Object { $_.name })
                    }
                    $names + @('--help')
                }
                "info"      {
                    $names = @()
                    if (Test-Path $manifestPath) {
                        $json = Get-Content $manifestPath -Raw | ConvertFrom-Json
                        $names = @($json.tools | ForEach-Object { $_.name })
                    }
                    $names + @('--help')
                }
                "init"      { @('zsh', 'bash', 'powershell', 'cmd') }
                "completion"{ @('zsh', 'bash', 'powershell', 'cmd') }
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

    /// <summary>
    /// Clink (cmd.exe) 补全脚本。
    /// 使用 clink.argmatcher 注册补全，需要安装 clink。
    /// 通过 clink 的 load(io.popen(...)) 模式动态加载。
    /// update/uninstall/info 支持动态补全已安装工具名称。
    /// </summary>
    private static string ClinkCompletion() => """
        -- gitpkg cmd completion (requires clink)
        -- 在 clink 中加载:
        --   load(io.popen('gitpkg completion cmd'):read("*a"))()
        -- clink 脚本目录可通过 clink info 查看（scripts 路径）

        -- 从 manifest.json 读取已安装工具名称
        local function get_tool_names()
            local names = {}
            local home = os.getenv("USERPROFILE") or os.getenv("HOME") or ""
            local manifest_path = home .. "\\.gitpkg\\manifest.json"
            local f = io.open(manifest_path, "r")
            if f then
                local content = f:read("*a")
                f:close()
                -- 简单解析 JSON 中的 "name":"xxx" 字段
                for name in content:gmatch('"name"%s*:%s*"([^"]+)"') do
                    table.insert(names, name)
                end
            end
            return names
        end

        local sub_completions = {
            install = {"--from", "--help"},
            init = {"zsh", "bash", "powershell", "cmd"},
            completion = {"zsh", "bash", "powershell", "cmd"},
            manifest = {"export", "--help"},
            list = {"--help"},
            outdated = {"--help"},
            ["self-update"] = {"--help"},
        }

        local commands = {
            "install", "update", "uninstall", "outdated", "list", "info",
            "init", "completion", "manifest", "self-update",
            "--help", "--version"
        }

        clink.argmatcher("gitpkg")
            :addarg(commands)
            :addarg(function (arg_index, word, word_count, line_state)
                local cmd = line_state:getword(2)
                if cmd then
                    -- update/uninstall/info 动态补全已安装工具名称
                    if cmd == "update" or cmd == "uninstall" or cmd == "info" then
                        local names = get_tool_names()
                        table.insert(names, "--help")
                        return names
                    end
                    if sub_completions[cmd] then
                        return sub_completions[cmd]
                    end
                end
            end)

        """;
}
