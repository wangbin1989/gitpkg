# Changelog

## [1.2.0] - 2026-06-18

### Added
- 新增 `init <shell>` 命令：输出 shell 初始化脚本，用于 eval 集成（`eval "$(gitpkg init zsh)"`）
- 新增 `completion <shell>` 命令：输出 shell 自动补全脚本，利用 System.CommandLine 内置 `[suggest]` 指令
- 新增 CLI 集成测试（`CliIntegrationTests`），覆盖 --help、--version、命令分发、退出码等端到端场景
- 单元测试按类别拆分为独立文件（AssetMatcher / Sha256Verifier / ManifestService 等 7 个文件）

### Changed
- 安装脚本（install.sh / install.ps1）改用 `gitpkg init` 生成 PATH 配置，带兜底逻辑
- 更新 README：补充 init / completion 命令用法和命令速查表

### Fixed
- `InitCommand`: binDir 路径使用单引号包裹并转义单引号（`'\\''`），防止 eval 命令注入
- `install.sh`: trap 改用函数封装（`cleanup() { rm -rf "${tmp_dir}"; }`），避免路径中单引号导致引号错乱
- `install.sh`: 兜底路径使用 sed 单引号转义，防止 `$`、`` ` ``、`"`、`\` 被 shell 展开

### Security
- 路径转义策略：POSIX Shell 用 `'\\''` 模式，PowerShell 用 `''` 双写，消除 eval 注入面

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
