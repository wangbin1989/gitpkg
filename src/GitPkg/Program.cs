using System.CommandLine;
using GitPkg;
using GitPkg.Commands;

GitPkgApp.Initialize();

var root = new RootCommand("gitpkg — GitHub Release 自动更新工具");

root.AddCommand(InstallCommand.Create());
root.AddCommand(UpdateCommand.Create());
root.AddCommand(OutdatedCommand.Create());
root.AddCommand(UninstallCommand.Create());
root.AddCommand(ListCommand.Create());
root.AddCommand(ManifestCommand.Create());

return await root.InvokeAsync(args);
