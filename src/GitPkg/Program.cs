using System.CommandLine;
using GitPkg;
using GitPkg.Commands;

// 初始化全局 HttpClient（User-Agent、超时等）
GitPkgApp.Initialize();

var root = new RootCommand("gitpkg — GitHub Release 自动更新工具");

// 注册所有子命令
root.Add(InitCommand.Create());
root.Add(InstallCommand.Create());
root.Add(UpdateCommand.Create());
root.Add(OutdatedCommand.Create());
root.Add(UninstallCommand.Create());
root.Add(ListCommand.Create());
root.Add(InfoCommand.Create());
root.Add(ManifestCommand.Create());
root.Add(SelfUpdateCommand.Create());

// 解析参数并执行对应的命令处理函数
var parseResult = root.Parse(args);
return await parseResult.InvokeAsync();
