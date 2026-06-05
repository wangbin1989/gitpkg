# GitPkg

一款基于 .NET 10 AOT 的 GitHub Release 自动更新工具。通过简单的命令从 GitHub Release 安装、更新和管理二进制工具，无需手动下载归档、解压和配置环境变量。

## 技术栈

- 核心框架: .NET 10 (AOT)
- 组件: System.CommandLine, Spectre.Console

## 运行环境

- 操作系统: macOS (Apple Silicon) / Windows (x64, ARM64) / Linux (x64, ARM64)
- 架构: x64 / arm64

## 快速开始

### 安装 GitPkg

从 [GitHub Releases](https://github.com/ggml-org/gitpkg/releases) 下载对应平台的归档文件，解压后将 `gpkg` 放入 PATH 目录即可。

```bash
# macOS / Linux
tar -xzf gpkg-darwin-arm64.tar.gz
sudo mv gpkg /usr/local/bin/

# 或放入用户目录
mv gpkg ~/.local/bin/
```

### 基本用法

```bash
# 安装工具（自动匹配当前平台）
gpkg install BurntSushi/ripgrep

# 安装指定版本
gpkg install sharkdp/fd@v10.0.0

# 查看已安装工具
gpkg list

# 检查更新
gpkg outdated

# 更新全部工具
gpkg update

# 更新指定工具
gpkg update ripgrep

# 查看工具详情
gpkg info ripgrep
gpkg info BurntSushi/ripgrep   # 未安装时使用 owner/repo 格式

# 卸载工具
gpkg uninstall ripgrep
```

### 高级选项

```bash
# 指定安装目录
gpkg install junegunn/fzf --dir /opt/tools

# 自动加入 PATH
gpkg install BurntSushi/ripgrep --add-path

# 开启 GPG 签名校验
gpkg install sharkdp/fd --verify-gpg <key-id>

# 从清单文件批量安装
gpkg manifest export > tools.json
gpkg install --from tools.json --dry-run   # 预览
gpkg install --from tools.json             # 安装
```

### 文件结构

```
~/.gitpkg/
├── manifest.json          # 已安装工具清单
├── tools/                 # 工具安装目录
│   ├── ripgrep/
│   └── fd/
└── tmp/                   # 下载临时目录
```

## 命令速查

| 命令 | 说明 |
|------|------|
| `gpkg install <owner/repo>[@version]` | 安装工具 |
| `gpkg update [name]` | 更新工具 |
| `gpkg outdated` | 检查更新 |
| `gpkg uninstall <name>` | 卸载工具 |
| `gpkg list` | 列出已安装工具 |
| `gpkg info <name>` | 查看工具详情 |
| `gpkg manifest export` | 导出清单 |
| `gpkg --version` | 版本信息 |
