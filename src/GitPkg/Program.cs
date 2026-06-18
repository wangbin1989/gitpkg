using System.CommandLine;
using GitPkg;
using GitPkg.Commands;

GitPkgApp.Initialize();

var root = new RootCommand("gitpkg — GitHub Release 自动更新工具");

root.Add(InitCommand.Create());
root.Add(InstallCommand.Create());
root.Add(UpdateCommand.Create());
root.Add(OutdatedCommand.Create());
root.Add(UninstallCommand.Create());
root.Add(ListCommand.Create());
root.Add(InfoCommand.Create());
root.Add(ManifestCommand.Create());
root.Add(SelfUpdateCommand.Create());

var parseResult = root.Parse(args);
return await parseResult.InvokeAsync();
