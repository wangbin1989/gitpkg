# Changelog

## [2.2.1] - 2026-06-23

### Changed
- 可执行文件链接时使用仓库名称作为链接名（单个可执行文件场景），而非原始文件名

### Fixed
- Unix 系统上安装后自动为二进制文件添加可执行权限（chmod +x），解决部分 Release 资产缺少可执行属性的问题

## [2.2.0] - 2026-06-23

### Added
- 支持裸二进制文件仓库：当 GitHub Release 只发布可执行文件（无压缩包）时，直接复制到安装目录

## [2.1.0] - 2026-06-21

### Added
- 安装时记录选择的资产文件名（`assetName`），更新时自动选择相同文件，无需重复手动选择
- `info` 命令展示已记录的资产名称
- 新增 `AssetName` 相关单元测试（往返、默认 null、更新覆盖）

## [2.0.3] - 2026-06-21

### Added
- 新增命令设计文档 `docs/命令设计文档.md`

### Changed
- 更新使用说明书：补充 `init`、`completion`、`self-update` 命令说明，更新安装指南
- 更新 README：补充 bash/fish/PowerShell 手动配置说明

### Removed
- 移除 `--verify-gpg` 选项及 GPG 签名校验逻辑（`GpgVerifier` 类）
- 移除 `--dir` 选项（安装目录固定为 `~/.gitpkg/tools/<name>/`）
- 移除 `--dry-run` 选项

### Fixed
- 补全脚本移除已废弃的 `--verify-gpg`、`--dir`、`--dry-run` 补全项
- zsh 补全脚本 `compdef` 改为显式注册（`compdef _gitpkg gitpkg`）
- 修正 `CompletionCommand` 中过时的 `[suggest]` 文档注释

## [2.0.2] - 2026-06-21

### Added
- 补全脚本支持所有子命令和选项补全（替换 `[suggest]` 机制为手动补全定义）

### Fixed
- `install --from ~/.gitpkg/manifest.json` 文件占用错误：`HandleBatchAsync` 中 stream 生命周期过长，改用 `File.ReadAllTextAsync` 一次性读取
- zsh 补全脚本 `eval` 时报 `_tags can only be called from completion function`：将 `_gitpkg` 直接调用改为 `compdef` 注册

## [2.0.1] - 2026-06-21

### Added
- `init` 命令输出中新增 `GITPKG_HOME` 环境变量设置，PATH 基于 `GITPKG_HOME` 变量引用
- 安装脚本（install.sh / install.ps1）安装完成后提示自动补全命令

### Changed
- 安装脚本不再自动修改 shell 配置文件，改为输出 `eval "$(gitpkg init <shell>)"` 供用户手动执行
- 安装脚本 PATH 已配置时也建议运行 init 命令（移除 `source ~/.zshrc` / `. $PROFILE` 的建议）
- 安装脚本提示路径使用 `${INSTALL_DIR}` / `$env:USERPROFILE` 变量替代硬编码

## [2.0.0] - 2026-06-18

### Added
- 安装后将可执行文件自动链接到 `~/.gitpkg/bin/`，用户只需将该目录加入 PATH 一次即可使用所有已安装工具
- 新增 `ExecutableFinder` 共享辅助类，提供跨平台可执行文件查找与目录探针
- 新增 `ManifestService.GetBinDir()` 方法

### Changed
- **移除 `--add-path` 选项**：安装时不再直接修改 shell 配置文件，改为统一 bin 目录方案
- `update` 命令在更新完成后自动刷新 `~/.gitpkg/bin/` 中的符号链接
- `uninstall` 命令自动清理 `~/.gitpkg/bin/` 中对应的符号链接
- `FindExecutables` / `FindExecutableDir` 从 `InstallCommand` 提取到共享 `ExecutableFinder` 类

### Removed
- 移除 `PathService` 及其测试（仅服务于已移除的 `--add-path` 功能）

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
