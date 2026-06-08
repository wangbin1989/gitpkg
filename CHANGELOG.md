# Changelog

## [1.1.2] - 2026-06-07

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
