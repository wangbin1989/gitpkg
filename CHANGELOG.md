# Changelog

## [2.5.1] - 2026-07-21

### Fixed
- `self-update` 命令修复版本号包含 `-scd` 后缀时比较失败的问题，现在正确识别 SCD 版本
- `self-update` 命令根据当前构建类型（AOT/SCD）自动选择对应版本的资产进行更新

### Changed
- 单文件版本资产名称格式从 `gitpkg-{version}-{platform}-singlefile` 调整为 `gitpkg-{version}-scd-{platform}`
- 安装脚本（install.sh / install.ps1）新增 `-scd` 开关，支持选择安装单文件版本

## [2.5.0] - 2026-07-21

### Added
- 新增内置清单（inner-manifest.json）支持，可为特定仓库预配置自定义工具名称和平台特定的可执行文件路径
- 新增 `InnerManifestService` 服务，读取嵌入的 inner-manifest.json 资源
- 新增 `InnerManifest`、`InnerManifestTool`、`InnerManifestPlatform` 数据模型
- 新增 `schema.json`，为 inner-manifest.json 提供 JSON Schema 校验

### Changed
- `install` 命令安装时读取内置清单：已配置的仓库使用自定义名称和指定 bin 文件链接
- `update` 命令更新时读取内置清单：已配置的仓库使用指定 bin 文件链接
- NuGet 依赖升级：`System.CommandLine` 2.0.9 → 2.0.10、`Spectre.Console` 0.57.1 → 0.57.2、`Microsoft.NET.Test.Sdk` 18.7.0 → 18.8.1

## [2.4.5] - 2026-07-09

### Added
- 新增单文件非 AOT 发布版本（`-singlefile` 后缀），适用于不支持 AOT 的环境

## [2.4.4] - 2026-07-02

### Fixed
- `AssetMatcher` Windows 关键词增加 `win`，修复 `win-` 前缀资产无法匹配 Windows 平台的问题
- 关键词匹配改为词边界匹配，避免 `win` 误匹配 `darwin`

## [2.4.3] - 2026-07-02

### Fixed
- `FindExecutableDir` 支持 `usr/bin` 和 `usr/local/bin` 子目录，修复 podman 等 Linux 风格打包的可执行文件链接问题
- `SelectAsset` 通过替换版本号匹配资产，解决更新时版本号变化（如 `b9803` → `b9859`）导致匹配失败的问题

## [2.4.2] - 2026-07-02

### Fixed
- `SelectAsset` 提前过滤辅助资产（校验、签名、源码归档、安装包等），修复提示数量与选择列表不一致的问题
- `update`/`self-update` 命令 Ctrl+C 取消时终端光标消失：注册 `Console.CancelKeyPress` 处理器恢复光标，补充 `OperationCanceledException` 专门捕获
- `update` 命令补充 `HttpRequestException` 分类捕获（资源不存在 / 网络错误），与 `install` 命令保持一致

## [2.4.1] - 2026-07-02

### Fixed
- `update`/`uninstall`/`info` 命令 `--help` 输出不再显示具体的已安装工具名称，改为通用的 `<name>` 占位符
- `PromptAssetSelection` 排除校验文件（`.sha256`/`.sha512`）、签名文件（`.sig`/`.asc`/`.minisig`）、源码归档和安装包（`.msi`/`.deb`/`.rpm`/`.pkg`/`.dmg`/`.appimage`/`.snap`/`.flatpak`/`.apk`）

## [2.4.0] - 2026-07-01

### Added
- `update`/`uninstall`/`info` 命令支持已安装工具名称动态补全（zsh/bash/powershell/clink）
- 新增 `CompletionHelper` 共享类，提供从 manifest 读取工具名称的动态补全源
- 命令参数通过 `Argument.CompletionSources.Add` 注册动态补全委托

### Changed
- 所有命令从 `static class` + `Create()` 工厂方法改为继承 `Command` 的实例类
- `ManifestCommand` 的 `export` 子命令改为私有嵌套类 `ExportCommand`
- 测试断言从 xunit `Assert` 统一改为 `Shouldly`（17 个测试文件，196 处断言）
- `Program.cs` 命令注册改为 `new XxxCommand()` 实例化方式

## [2.3.1] - 2026-07-01

### Changed
- `init cmd` 改为输出 clink Lua 脚本（使用 `os.setenv` 设置环境变量），通过 `load(io.popen(...))` 动态加载
- 新增 `EscapeForLua` 路径转义方法（`\` → `\\`，`"` → `\"`），替代原 `EscapeForCmd`

## [2.3.0] - 2026-07-01

### Added
- `init` 命令新增 `cmd` shell 支持，输出 `set GITPKG_HOME` 和 `set PATH` 初始化脚本
- `completion` 命令新增 `cmd` shell 支持，生成 clink Lua 补全脚本（需安装 clink）
- 所有 shell 补全脚本（zsh/bash/powershell）的 `init`/`completion` 子命令补全列表中增加 `cmd` 选项
- `list` 命令输出增加资产名称列（显示已记录的资产文件名）
- 新增 `EscapeForCmd` 路径转义方法（`%` → `%%`）
- 新增 `CompletionCommandTests` 分部类（按 shell 拆分）和 `InitCommandTests.Cmd.cs`

### Changed
- 更新 NuGet 依赖：`Spectre.Console` 0.57.1、`Microsoft.NET.Test.Sdk` 18.7.0
- 新增测试依赖：`Shouldly` 4.3.0

### Removed
- 移除 fish shell 支持（init 和 completion 命令不再支持 fish）

## [2.2.3] - 2026-06-24

### Changed
- 可执行文件链接名去除平台和架构信息（如 `-windows-amd64`），保留文件扩展名，修复 Windows 下链接丢失 `.exe` 后缀的问题
- `StripPlatformSuffix` 方法从 `InstallCommand` 提取到公共 `CommandHelpers` 类
- `uninstall` 命令清理链接时与 `LinkToBinDir` 保持一致的命名逻辑
- `list` 命令按名称排序输出（不区分大小写）

## [2.2.2] - 2026-06-24

### Fixed
- 重复安装时移除旧版本遗留的符号链接，避免可执行文件列表变化后 bin 目录中残留无效链接

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
