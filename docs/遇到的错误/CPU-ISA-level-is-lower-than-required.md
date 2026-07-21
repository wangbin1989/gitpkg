# CPU ISA level is lower than required

## 问题背景

- 开发环境：MacBook Air M2 + macOS 26
- 发布环境：GitHub Actions
  - Linux x64: `ubuntu-26.04`
  - Linux arm64: `ubuntu-26.04-arm`
  - macOS arm64: `macos-26`
  - Windows x64: `windows-2025`
  - Windows arm64: `windows-11-arm`
- 运行环境：
  - MacBook Air M2 + macOS 26 ✅
  - AMD 5800X + Windows 11 Pro 26H1 ✅
  - AMD 3400G + Ubuntu 24.04 ✅
  - 联想小新 Mini（Intel i5-13500H）+ Windows 11 Home 26H1 ✅
  - Synology DS918+（Intel Celeron J4125）+ DSM 7.3.2 ❌
  - Synology DS723+（AMD Ryzen R1600V）+ DSM 7.3.2 ✅

## 错误现象

在 Synology DS918+（CPU: Intel Celeron J4125）上运行 gitpkg 二进制文件时，程序直接崩溃并报错：

```
CPU ISA level is lower than required
```

## 原因分析

该错误发生在 .NET NativeAOT 编译的二进制文件中。NativeAOT 在发布时会针对特定的 CPU 指令集架构（ISA level）进行优化编译。当目标运行机器的 CPU 不支持编译时所要求的指令集（如 AVX2、AVX-512 等），就会出现此错误。

常见触发场景：

- 使用较高版本的 .NET SDK 编译，默认启用了更激进的 CPU 优化（如 .NET 8+ 默认要求 AVX2）
- 在较旧的 CPU 机器上运行针对新架构编译的二进制文件
- 跨机器部署时，编译环境与运行环境的 CPU 架构不一致

## 已验证无效的方案

以下方案经测试无法解决该问题：

- `IlcInstructionSet=base`：运行时仍会检查默认 ISA 级别
- `IlcInstructionSet=sse4.2`：运行时仍会检查默认 ISA 级别
- `IlcInstructionSet=x64-v1`：运行时仍会检查默认 ISA 级别
- `TargetInstructionSet=baseline`：运行时仍会检查默认 ISA 级别
- `TargetInstructionSet=x64-v1`：运行时仍会检查默认 ISA 级别
- `VectorSupport=false`：运行时仍会检查默认 ISA 级别

## 解决方案

### 方案一：使用单文件非 AOT 版本 ✅ 推荐

从 v2.4.5 开始，Release 页面提供 `-singlefile` 后缀的单文件非 AOT 版本。该版本使用 `PublishSingleFile` 打包但不进行 AOT 编译，由 .NET 运行时处理 CPU 兼容性，避免 ISA 级别检查问题。

```bash
# 下载 singlefile 版本（以 Linux x64 为例）
curl -LO https://github.com/wangbin1989/gitpkg/releases/download/v2.4.5/gitpkg-v2.4.5-linux-x86_64-singlefile.tar.gz
tar -xzf gitpkg-v2.4.5-linux-x86_64-singlefile.tar.gz
chmod +x gitpkg
mv gitpkg ~/.gitpkg/bin/
```

> **原理**：非 AOT 版本由 .NET 运行时（CLR）执行，运行时会在启动时检测 CPU 支持的指令集并动态选择代码路径，因此不会出现 ISA 级别不匹配的问题。

### 方案二：在目标机器上编译

在目标机器上直接编译 NativeAOT 二进制文件，编译器会自动使用该 CPU 支持的指令集：

```bash
# 在目标机器上执行
dotnet publish src/GitPkg -c Release -r linux-x64 -o publish
```

> **原理**：NativeAOT 编译器会检测当前 CPU 支持的指令集，生成与之匹配的二进制文件。这样生成的二进制文件在该机器上运行不会出现 ISA 级别不匹配的问题。

### 方案三：升级运行环境 CPU

如果目标机器的 CPU 过于老旧（如 Intel Celeron J4125），考虑升级硬件或更换到支持 AVX2 的机器上运行。

## 参考

- [Microsoft Learn: Native AOT deployment - CPU architecture](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [NativeAOT optimizing 文档](https://github.com/dotnet/runtime/blob/main/src/coreclr/nativeaot/docs/optimizing.md) - IlcInstructionSet 等编译选项说明
- [System.Runtime.Intrinsics 命名空间](https://learn.microsoft.com/zh-cn/dotnet/api/system.runtime.intrinsics) - CPU 指令集 API 文档
- [dotnet/runtime ISA 测试代码](https://github.com/dotnet/runtime/blob/main/src/tests/JIT/Regression/JitBlue/Runtime_34587/Runtime_34587.cs) - 用于检测 CPU 支持的指令集
- [ISA_TESTS.cs](./ISA_TESTS.cs) - 本地 ISA 测试脚本（使用 `dotnet run ISA_TESTS.cs` 运行）
