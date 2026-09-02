using System.CommandLine;

namespace GitPkg.Commands;

/// <summary>
/// 命令基类，所有 CLI 命令继承此类。
/// </summary>
public abstract class CommandBase : Command
{
    protected CommandBase(string name, string description) : base(name, description) { }

    /// <summary>添加工具名称参数（必需）。</summary>
    protected Argument<string> AddNameArg()
    {
        var arg = new Argument<string>("name") { Description = "工具名称" };
        Add(arg);
        return arg;
    }

    /// <summary>添加 shell 参数。</summary>
    protected Argument<string> AddShellArg()
    {
        var arg = new Argument<string>("shell") { Description = "目标 shell: zsh, bash, powershell (pwsh), cmd" };
        Add(arg);
        return arg;
    }

    /// <summary>添加 --prerelease 选项。</summary>
    protected Option<bool> AddPrereleaseOption()
    {
        var opt = new Option<bool>("--prerelease") { Description = "包含预发布版本" };
        Add(opt);
        return opt;
    }
}
