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

**一键安装（推荐）：**

```bash
# macOS / Linux
curl -fsSL https://raw.githubusercontent.com/wangbin1989/gitpkg/main/install.sh | bash

# 指定版本
curl -fsSL https://raw.githubusercontent.com/wangbin1989/gitpkg/main/install.sh | bash -s v1.1.4

# Windows (PowerShell)
Invoke-Expression (Invoke-RestMethod https://raw.githubusercontent.com/wangbin1989/gitpkg/main/install.ps1)
```

脚本会自动检测平台，下载最新版本并安装到 `~/.gitpkg/bin`。

安装完成后，将 gitpkg 加入 PATH：

```bash
# zsh
eval "$(~/.gitpkg/bin/gitpkg init zsh)" >> ~/.zshrc
source ~/.zshrc

# bash
eval "$(~/.gitpkg/bin/gitpkg init bash)" >> ~/.bashrc
source ~/.bashrc

# fish
~/.gitpkg/bin/gitpkg init fish >> ~/.config/fish/config.fish
source ~/.config/fish/config.fish
```

**手动安装：**

从 [GitHub Releases](https://github.com/wangbin1989/gitpkg/releases) 下载对应平台的归档文件，解压后将 `gitpkg` 放入 PATH 目录即可。

```bash
# macOS / Linux
tar -xzf gitpkg-v1.1.4-darwin-arm64.tar.gz
chmod +x gitpkg
mv gitpkg ~/.gitpkg/bin/   # 或其他 PATH 目录

# Windows
# 解压 gitpkg-v1.1.4-windows-x86_64.zip，将 gitpkg.exe 放入 PATH 目录
```

### 基本用法

```bash
# 安装工具（自动匹配当前平台）
gitpkg install BurntSushi/ripgrep

# 安装指定版本
gitpkg install sharkdp/fd@v10.0.0

# 查看已安装工具
gitpkg list

# 检查更新
gitpkg outdated

# 更新全部工具
gitpkg update

# 更新指定工具
gitpkg update ripgrep

# 查看工具详情
gitpkg info ripgrep
gitpkg info BurntSushi/ripgrep   # 未安装时使用 owner/repo 格式

# 卸载工具
gitpkg uninstall ripgrep
```

### 高级选项

```bash
# 指定安装目录
gitpkg install junegunn/fzf --dir /opt/tools

# 自动加入 PATH
gitpkg install BurntSushi/ripgrep --add-path

# 开启 GPG 签名校验
gitpkg install sharkdp/fd --verify-gpg <key-id>

# 从清单文件批量安装
gitpkg manifest export > tools.json
gitpkg install --from tools.json --dry-run   # 预览
gitpkg install --from tools.json             # 安装
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
| `gitpkg init <shell>` | 输出 shell 初始化脚本 |
| `gitpkg install <owner/repo>[@version]` | 安装工具 |
| `gitpkg update [name]` | 更新工具 |
| `gitpkg outdated` | 检查更新 |
| `gitpkg uninstall <name>` | 卸载工具 |
| `gitpkg list` | 列出已安装工具 |
| `gitpkg info <name>` | 查看工具详情 |
| `gitpkg manifest export` | 导出清单 |
| `gitpkg --version` | 版本信息 |
