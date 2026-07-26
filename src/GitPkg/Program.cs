using System.CommandLine;
using GitPkg;
using GitPkg.Commands;

// 注册 Ctrl+C 处理，确保进程退出前恢复终端光标（Spectre.Console 进度条会隐藏光标）
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true; // 阻止进程立即终止，让异常处理流程正常清理
    Console.Write("\x1b[?25h"); // 恢复光标可见
};

// 初始化全局 HttpClient（User-Agent、超时等）
GitPkgApp.Initialize();

var root = new RootCommand("gitpkg — GitHub Release 自动更新工具")
{
    new InitCommand(),
    new CompletionCommand(),
    new InstallCommand(),
    new UpdateCommand(),
    new OutdatedCommand(),
    new UninstallCommand(),
    new ListCommand(),
    new InfoCommand(),
    new ManifestCommand(),
    new SelfUpdateCommand(),
};

// 解析参数并执行对应的命令处理函数
var parseResult = root.Parse(args);
return await parseResult.InvokeAsync();
