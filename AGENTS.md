# AGENTS.md

GitPkg 是一个 GitHub Release 二进制工具的自动安装与更新管理器，通过 CLI 一键完成下载、解压、符号链接和 PATH 配置。

## Tech Stack

- .NET 10 NativeAOT
- System.CommandLine 2.0.9 (CLI 解析)
- Spectre.Console 0.57.1 (进度条、交互提示)
- xunit 2.9.3 + Shouldly 4.3.0 (测试)

## Commands

```bash
dotnet clean                                        # 清理构建产物
dotnet build                                        # 构建
dotnet test                                         # 运行全部测试
dotnet publish src/GitPkg -c Release -r osx-arm64 -o publish  # AOT 发布 (RID: osx-arm64/linux-x64/linux-arm64/win-x64/win-arm64)
```

## Project Structure

```
src/GitPkg/                  # 主项目
  Program.cs                 # 入口
  Commands/                  # CLI 命令
  Services/                  # 核心服务
  Models/                    # 数据模型和 JSON 序列化上下文
test/GitPkg.Tests/           # 测试项目
  Commands/                  # 命令测试
  Models/                    # 模型/工具测试
  Services/                  # 服务测试
  Integration/               # 端到端 CLI 集成测试
```

## NativeAOT

- 绝不使用动态加载（例如 `Assembly.LoadFile`）
- 绝不生成运行时代码（例如 `System.Reflection.Emit`）
- 绝不使用反射
- JSON 序列化必须使用 AppJsonContext 源生成上下文
- InvariantGlobalization 已启用，不支持区域性特定格式

## Testing

- 测试框架: xunit
- 断言库: Shouldly

## Never (绝不)

## Do Not (不准)

- 不准修改 sln 文件。
- 不准修改 csproj 文件。
- 不准添加或删除项目引用的 NuGet 包。
- 不准修改项目引用的 NuGet 包版本。

## CRITICAL (关键)

- 每次执行 `git commit` 操作都必须手动确认，确保不会误提交不必要的更改。
- 每次执行 `git push` 操作都必须手动确认，确保不会误推送不必要的更改。
- 每次执行 `pull request / merge` 操作都必须手动确认，确保不会误创建不必要的 pull request。
