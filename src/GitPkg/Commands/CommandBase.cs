using System.CommandLine;

namespace GitPkg.Commands;

/// <summary>
/// 命令基类，所有 CLI 命令继承此类。
/// </summary>
public abstract class CommandBase : Command
{
    protected CommandBase(string name, string description) : base(name, description) { }
}
