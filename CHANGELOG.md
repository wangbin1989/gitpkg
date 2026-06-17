# Changelog

## [1.1.4] - 2026-06-17

### Changed
- 程序集名称由 `gpkg` 变更为 `gitpkg`
- 发布包名称增加版本号（`gitpkg-v1.1.4-darwin-arm64.tar.gz`）
- 移除单独 `.sha256` 文件，保留 `checksums.txt`
- 升级 GitHub Actions 到最新稳定版（checkout@v6, setup-dotnet@v5, upload-artifact@v7, download-artifact@v8, action-gh-release@v3）

## [1.1.3] - 2026-06-13

### Changed
- 迁移 `System.CommandLine` API（beta → 2.0.9）：`AddArgument/AddOption/AddCommand` → `Add`，`SetHandler` → `SetAction`，`AddAlias` → Option 构造函数别名
- 更新 NuGet 依赖：`System.CommandLine` 2.0.9、`Spectre.Console` 0.57.0
- 更新测试依赖：`Microsoft.NET.Test.Sdk` 18.6.0、`coverlet.collector` 10.0.1、`xunit.runner.visualstudio` 3.1.5

### Fixed
- 修复 Windows 下 `install --add-path` 提示 `source` 命令不可用的问题，PowerShell 改为 `. $PROFILE`，cmd 仅提示重启终端

## [1.1.2] - 2026-06-07

### Changed
- 提取 `FormatSize` / `PromptAssetSelection` 到 `CommandHelpers`，消除重复代码

### Fixed
- 修复 `update` 命令在无自动匹配资产时直接跳过的问题，改为回退到手动选择或非交互模式自动选择

## [1.1.1] - 2026-06-07

### Added
- 新增 `self-update` 命令，支持从 GitHub Release 自动更新 GitPkg 自身

### Fixed
- 修复 `--add-path` 对可执行文件在 `bin/` 子目录的工具（如 cli/cli）PATH 设置不正确的问题

## [1.1.0] - 2026-06-05

### Added
- 新增 `install --from manifest.json` 批量安装功能
- 新增 `manifest export` 导出清单命令
- 新增 `--add-path` 选项自动将工具目录加入 PATH
- 新增 `--verify-gpg` GPG 签名校验

### Changed
- 移除 Intel macOS 构建，增加 Windows ARM64 支持

## [1.0.0] - 2026-06-03

### Added
- 初始版本，支持从 GitHub Release 安装、更新、卸载、列出二进制工具
