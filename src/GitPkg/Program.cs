using System.CommandLine;
using GitPkg;
using GitPkg.Commands;

// 初始化全局 HttpClient（User-Agent、超时等）
GitPkgApp.Initialize();

var root = new RootCommand("gitpkg — GitHub Release 自动更新工具");

// 注册所有子命令
root.Add(new InitCommand());
root.Add(new CompletionCommand());
root.Add(new InstallCommand());
root.Add(new UpdateCommand());
root.Add(new OutdatedCommand());
root.Add(new UninstallCommand());
root.Add(new ListCommand());
root.Add(new InfoCommand());
root.Add(new ManifestCommand());
root.Add(new SelfUpdateCommand());

// 解析参数并执行对应的命令处理函数
var parseResult = root.Parse(args);
return await parseResult.InvokeAsync();
