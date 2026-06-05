# GitPkg 软件需求规格说明书

**版本**: 1.0  
**日期**: 2026-06-05  
**状态**: 草案  

---

## 1. 引言

### 1.1 编写目的

本文档旨在定义 GitPkg 的功能需求与非功能需求，作为后续设计、开发与测试的依据。

### 1.2 产品描述

GitPkg 是一款基于 .NET 10 AOT 的命令行工具，用于从 GitHub Release 自动下载、安装和更新二进制工具软件。用户通过简单的命令即可管理多个 GitHub 发布工具，无需手动下载归档、解压和配置环境变量。

### 1.3 术语与缩写

| 术语 | 定义 |
|------|------|
| GitPkg / gpkg | 本产品名称及 CLI 命令入口 |
| 清单文件 (Manifest) | `~/.gitpkg/manifest.json`，记录所有已安装工具的元数据 |
| 资产 (Asset) | GitHub Release 中的可下载文件 |
| 工具 (Tool) | 用户通过 GitPkg 安装的目标软件 |
| AOT | Ahead-of-Time 编译，生成平台原生单文件二进制 |

### 1.4 参考文档

- [GitHub REST API v3 — Releases](https://docs.github.com/en/rest/releases)
- 用户故事文档: `docs/1.需求/user-stories.md`

---

## 2. 总体描述

### 2.1 产品视角

```
┌──────────┐     ┌──────────────┐     ┌─────────────────┐
│  用户     │────▶│   GitPkg CLI │────▶│  GitHub API      │
│ (终端)    │◀────│   (gpkg)     │◀────│  (github.com)    │
└──────────┘     └──────┬───────┘     └─────────────────┘
                        │
                        ▼
               ┌───────────────┐
               │  本地文件系统   │
               │ ~/.gitpkg/    │
               └───────────────┘
```

GitPkg 作为用户和 GitHub Release 之间的中间层，负责发现版本、下载归档、解压安装、管理 PATH 和记录清单。

### 2.2 用户特征

**目标用户**: 使用命令行的开发者、运维人员、技术爱好者。

**能力要求**:
- 熟悉终端操作
- 了解 GitHub 仓库和 Release 的基本概念
- 了解操作系统环境变量 (PATH) 的基本概念

### 2.3 运行环境

| 项目 | 要求 |
|------|------|
| 操作系统 | macOS 12+ / Windows 10+ / Linux (kernel 5.x+) |
| 架构 | x64 / arm64 |
| 运行时 | 无依赖（AOT 编译为单文件二进制） |
| 网络 | 需访问 github.com (HTTPS) |
| 磁盘空间 | 二进制 < 20MB + 已安装工具所占空间 |

### 2.4 约束与假设

**约束**:
- C1: 使用 .NET 10 AOT 编译，不得依赖 JIT 运行时
- C2: 通过 GitHub REST API 获取 Release 信息，不使用 git 命令行
- C3: 对 GitHub API 的未认证请求受速率限制（60 次/小时/IP）

**假设**:
- A1: 被管理的工具通过 GitHub Release 分发
- A2: Release 资产采用常见归档格式（zip / tar.gz / tar.xz）
- A3: 用户具有安装目录的读写权限

---

## 3. 功能需求

### FR-01: 安装工具

**优先级**: P0 (核心)

**描述**: 用户通过 `gpkg install <owner>/<repo>` 从 GitHub Release 安装工具。

**输入**:
- `owner/repo`: 目标 GitHub 仓库
- `@<version>` (可选): 指定版本号或 tag
- `--dir <path>` (可选): 自定义安装目录，默认 `~/.gitpkg/tools/<name>/`

**处理流程**:
1. 解析 `owner/repo[@version]` 参数
2. 调用 GitHub API 获取 Release 信息（有版本则查指定版本，否则查 latest）
3. 获取当前操作系统标识符和 CPU 架构
4. 按规则匹配 Release 资产（见 FR-01a）
5. 若匹配到多个资产，列出选项让用户选择
6. 若无匹配资产，报错退出
7. 下载资产到临时目录
8. 执行 SHA256 校验（见 FR-07）
9. 解压归档到安装目录
10. 若使用 `--add-path`，将工具目录加入 PATH（见 FR-09）
11. 更新清单文件
12. 打印安装成功信息（工具名、版本、路径）

**输出**:
- 成功: 打印 `✓ <name> v<version> installed to <path>`
- 失败: 打印错误信息，退出码非零

**错误处理**:
| 场景 | 行为 |
|------|------|
| 仓库不存在 | `✗ 仓库 <owner/repo> 不存在` |
| 无 Release | `✗ <owner/repo> 没有发布过 Release` |
| 版本不存在 | `✗ 版本 <version> 不存在` |
| 无匹配资产 | `✗ 未找到适用于 <os>-<arch> 的资产` |
| 网络错误 | `✗ 网络错误: <detail>` |

---

### FR-01a: 平台-架构自动匹配规则

**描述**: 将当前系统的 OS 和架构映射到 Release 资产的命名模式。

**OS 标识符**:
| 平台 | 匹配关键词 |
|------|-----------|
| macOS | `darwin`, `macos`, `osx`, `apple`, `mac` |
| Windows | `windows`, `win64`, `win32`, `msvc` |
| Linux | `linux`, `ubuntu`, `debian`, `rhel`, `gnu`, `musl` |

**架构标识符**:
| 架构 | 匹配关键词 |
|------|-----------|
| x64 | `x86_64`, `amd64`, `x64`, `x86-64` |
| arm64 | `arm64`, `aarch64`, `armv8` |

**匹配逻辑**:
1. 遍历 Release 的所有资产
2. 资产名称（小写）同时包含任一 OS 关键词和任一架构关键词时视为匹配
3. 若只匹配到一个，直接使用
4. 若匹配到多个，按优先级排序后列出：精确匹配 > 部分匹配
5. 若无匹配，报错

---

### FR-02: 更新工具

**优先级**: P0 (核心)

**命令格式**:
- `gpkg update <name>` — 更新指定工具
- `gpkg update` — 更新所有已安装工具

**处理流程** (单个工具):
1. 从清单中读取工具信息
2. 查询 GitHub API 获取最新 Release 版本
3. 比较版本：
   - 若当前版本 == 最新版本 → 提示 "已是最新版本"
   - 若存在更新 → 执行安装流程（同 FR-01，SKIP 步骤 11）
4. 更新清单中的版本信息

**处理流程** (全部工具):
1. 遍历清单中所有工具
2. 对每个工具执行上述流程
3. 汇总输出：

```
  ✓ <name1>  v1.0.0 → v1.1.0
  ✓ <name2>  v2.0.0 → v2.1.0
  = <name3>  v3.0.0 (已是最新)
  ✗ <name4>  更新失败: <reason>

  更新: 2 | 已是最新: 1 | 失败: 1
```

**兼容性要求**:
- 更新时保留工具目录下的非二进制文件（如用户配置文件）

---

### FR-03: 检查更新

**优先级**: P1 (重要)

**命令**: `gpkg outdated`

**描述**: 检查所有已安装工具是否有新版本，仅展示，不下载。

**处理流程**:
1. 遍历清单中所有工具
2. 对每个工具查询最新 Release 版本
3. 对比当前版本，有新版本则记录
4. 表格输出：

```
  名称      当前版本    最新版本    仓库
  ─────────────────────────────────────────
  ripgrep   v14.0.0    v14.1.0    BurntSushi/ripgrep
  fd        v9.0.0     v10.0.0    sharkdp/fd
```

- 若全部为最新，输出 "所有工具均为最新版本"

---

### FR-04: 卸载工具

**优先级**: P1 (重要)

**命令**: `gpkg uninstall <name>`

**处理流程**:
1. 从清单查找工具
2. 删除安装目录
3. 从清单移除记录
4. 提示用户检查 PATH 中是否需要手动清理

**输出**: `✓ <name> 已卸载`

**错误处理**:
- 工具未安装: `✗ <name> 未安装`

---

### FR-05: 列出已安装工具

**优先级**: P1 (重要)

**命令**: `gpkg list`

**输出格式** (Spectre.Console 表格):

```
  名称      版本      来源仓库              安装时间
  ───────────────────────────────────────────────────
  ripgrep   v14.0.0   BurntSushi/ripgrep    2026-05-15
  fd        v9.0.0    sharkdp/fd            2026-05-16
```

- 若清单为空: "暂无已安装工具，使用 `gpkg install <owner/repo>` 安装"

---

### FR-06: 查看工具详情

**优先级**: P2 (增强)

**命令**: `gpkg info <name>`

**输出**:
```
  ┌─ ripgrep ──────────────────────────────────
  │ 仓库:     BurntSushi/ripgrep
  │ 描述:     ripgrep recursively searches directories...
  │ 已安装:   v14.0.0 (2026-05-15)
  │ 最新版本: v14.1.0 (2026-06-01)
  │ 可用资产:
  │   - ripgrep-14.1.0-x86_64-apple-darwin.tar.gz
  │   - ripgrep-14.1.0-x86_64-unknown-linux-musl.tar.gz
  │   - ripgrep_14.1.0-1_amd64.deb
  └────────────────────────────────────────────
```

**注意**: 若工具未安装，`<name>` 替换为 `owner/repo` 格式。

---

### FR-07: SHA256 完整性校验

**优先级**: P1 (重要)

**描述**: 下载完成后，自动检测并校验归档文件的 SHA256。

**处理流程**:
1. 下载资产文件到临时目录
2. 检查 Release 中是否存在校验文件（文件名匹配 `.sha256`、`checksums.txt`、`SHA256SUMS`）
3. 若存在校验文件：
   a. 下载校验文件
   b. 计算下载文件的 SHA256
   c. 对比校验和
   d. 不匹配 → 删除临时文件，报错退出
4. 若不存在校验文件：给出警告 `⚠ 未找到 SHA256 校验文件，跳过完整性校验`

**错误处理**:
- 校验失败: `✗ SHA256 校验失败，文件可能已损坏或遭篡改`

---

### FR-08: GPG 签名校验

**优先级**: P2 (增强)

**命令**: `gpkg install owner/repo --verify-gpg <key-id>`

**描述**: 用户可选的 GPG 签名校验。

**处理流程**:
1. 下载 Release 中的 `.asc` 或 `.sig` 签名文件
2. 使用指定 key-id 校验
3. 失败 → 终止安装

**依赖**: 系统需安装 `gpg` 命令行工具，否则提示未安装 GPG。

---

### FR-09: PATH 环境变量管理

**优先级**: P1 (重要)

**命令**: `gpkg install owner/repo --add-path`

**描述**: 安装工具时自动将工具可执行目录加入 PATH。

**支持的 Shell**:
| Shell | 配置文件 |
|-------|---------|
| bash | `~/.bashrc` |
| zsh | `~/.zshrc` |
| fish | `~/.config/fish/config.fish` |
| PowerShell | `$PROFILE` |

**处理流程**:
1. 检测当前 Shell 类型
2. 在对应配置文件末尾追加 `export PATH="<install-dir>:$PATH"`
3. 若已存在相同路径，跳过
4. 输出 `ℹ PATH 已更新，请执行 source <config-file> 或重新打开终端`

**卸载时**: `gpkg uninstall` 提示用户手动检查和清理 PATH 条目（不自动编辑，避免误删）。

---

### FR-10: 清单文件管理

**优先级**: P1 (重要)

**清单文件路径**: `~/.gitpkg/manifest.json`

**数据结构**:
```json
{
  "version": 1,
  "tools": [
    {
      "name": "ripgrep",
      "repo": "BurntSushi/ripgrep",
      "version": "14.0.0",
      "installPath": "/home/user/.gitpkg/tools/ripgrep",
      "installedAt": "2026-05-15T10:30:00Z"
    }
  ]
}
```

**命令**:
- `gpkg manifest export` — 将清单文件内容输出到 stdout，可用 `> manifest.json` 导出

**批量安装**:
- `gpkg install --from manifest.json` — 读取清单文件，逐一安装
- `gpkg install --from manifest.json --dry-run` — 仅预览不执行

---

### FR-11: 自身版本

**命令**: `gpkg --version` 或 `gpkg -v`

**输出**: `gitpkg v1.0.0`

---

## 4. 外部接口

### 4.1 GitHub REST API

| 端点 | 用途 | 调用场景 |
|------|------|---------|
| `GET /repos/{owner}/{repo}/releases/latest` | 获取最新 Release | install (无版本号)、update、outdated |
| `GET /repos/{owner}/{repo}/releases/tags/{tag}` | 获取指定 tag 的 Release | install (指定版本) |
| `GET /repos/{owner}/{repo}` | 获取仓库信息 | info |

**认证**: 无认证（公共访问），受 IP 速率限制。
**速率限制**: 60 requests/hour。GitPkg 应遵守 HTTP 头中的 `X-RateLimit-Remaining` 并在接近限制时向用户发出警告。

### 4.2 文件系统

| 路径 | 用途 |
|------|------|
| `~/.gitpkg/` | GitPkg 主目录 |
| `~/.gitpkg/tools/<name>/` | 工具安装目录 |
| `~/.gitpkg/tmp/` | 下载临时目录 |
| `~/.gitpkg/manifest.json` | 清单文件 |

### 4.3 命令行接口

```
gpkg install <owner/repo>[@<version>] [--dir <path>] [--add-path] [--verify-gpg <key>]
gpkg update [<name>]
gpkg outdated
gpkg uninstall <name>
gpkg list
gpkg info <owner/repo> | <name>
gpkg manifest export
gpkg --version | -v
gpkg --help | -h
```

---

## 5. 非功能需求

### NFR-01: 跨平台兼容性

| 要求 | 说明 |
|------|------|
| NFR-01.1 | 支持 macOS 12+、Windows 10+、Linux kernel 5.x+ |
| NFR-01.2 | 支持 x64 和 arm64 架构 |
| NFR-01.3 | AOT 编译为单文件可执行文件，无运行时依赖 |
| NFR-01.4 | 文件路径处理使用 `Path.Combine`、`Path.DirectorySeparatorChar` 等跨平台 API |

### NFR-02: 性能

| 要求 | 说明 |
|------|------|
| NFR-02.1 | 启动时间 < 500ms（不含首次 JIT 预热，AOT 保证） |
| NFR-02.2 | `outdated` 命令（仅检查，不下载）在 3s 内完成（5 个工具以内） |
| NFR-02.3 | 下载速度依赖用户网络带宽，不设限制 |

### NFR-03: 资源占用

| 要求 | 说明 |
|------|------|
| NFR-03.1 | AOT 编译后二进制 < 20MB |
| NFR-03.2 | 运行时内存 < 50MB（不含下载缓冲区） |

### NFR-04: 安全性

| 要求 | 说明 |
|------|------|
| NFR-04.1 | 所有 API 请求强制 HTTPS |
| NFR-04.2 | 下载完成后默认进行 SHA256 校验（若可获取校验文件） |
| NFR-04.3 | 下载文件先写入临时目录，校验通过后再移至目标路径 |
| NFR-04.4 | 不存储或传输 GitHub 凭据 |

### NFR-05: 可用性

| 要求 | 说明 |
|------|------|
| NFR-05.1 | 命令输出使用 Spectre.Console 美化（表格、颜色、状态动画） |
| NFR-05.2 | 下载进度实时显示（进度条或百分比） |
| NFR-05.3 | 所有错误信息使用中文（当前目标用户群体） |
| NFR-05.4 | `--help` 提供每个命令的参数说明和示例 |

### NFR-06: 可维护性

| 要求 | 说明 |
|------|------|
| NFR-06.1 | 源代码使用 C# 编写，目标框架 net10.0 |
| NFR-06.2 | 核心逻辑与 CLI 表示分离，核心库可独立测试 |
| NFR-06.3 | 关键路径需单元测试覆盖 |
| NFR-06.4 | 依赖库: System.CommandLine, Spectre.Console, System.Text.Json |

---

## 6. 功能优先级矩阵

| 优先级 | 需求编号 | 说明 |
|--------|----------|------|
| **P0** | FR-01, FR-01a, FR-02 | MVP: 安装 + 更新 |
| **P1** | FR-03, FR-04, FR-05, FR-07, FR-09, FR-10 | 完整工具管理闭环 |
| **P2** | FR-06, FR-08 | 增强体验 |

**MVP 迭代建议**: 先实现 P0 (install + update)，交付后可用的最小功能集，再逐步迭代 P1、P2。

---

## 7. 附录

### A. 命令速查

```
gpkg install <owner/repo>[@<version>] [--dir <path>] [--add-path] [--verify-gpg <key>]
gpkg update [<name>]
gpkg outdated
gpkg uninstall <name>
gpkg list
gpkg info <owner/repo>|<name>
gpkg manifest export
gpkg --version
gpkg --help
```

### B. 资产匹配示例

```
系统: macOS arm64
资产列表:
  - tool-1.0.0-x86_64-apple-darwin.tar.gz    → 匹配 macos
  - tool-1.0.0-aarch64-apple-darwin.tar.gz   → 匹配 macos + arm64 ✓ (精确)
  - tool-1.0.0-x86_64-unknown-linux-gnu.tar.gz → 不匹配 macos
  - tool-1.0.0-x86_64-pc-windows-msvc.zip    → 不匹配 macos
```
