# CPU ISA level is lower than required

## 错误现象

运行 gitpkg 二进制文件时，程序直接崩溃并报错：

```
CPU ISA level is lower than required
```

## 原因分析

该错误发生在 .NET NativeAOT 编译的二进制文件中。NativeAOT 在发布时会针对特定的 CPU 指令集架构（ISA level）进行优化编译。当目标运行机器的 CPU 不支持编译时所要求的指令集（如 AVX2、AVX-512 等），就会出现此错误。

常见触发场景：

- 使用较高版本的 .NET SDK 编译，默认启用了更激进的 CPU 优化（如 .NET 8+ 默认要求 AVX2）
- 在较旧的 CPU 机器上运行针对新架构编译的二进制文件
- 跨机器部署时，编译环境与运行环境的 CPU 架构不一致

## 解决方案

### 方案一：在发布时降低 CPU ISA 要求 ✅ 本项目采用

> **本项目采用此方案**：在 CI/CD 发布流程中，除了发布默认的优化版本外，额外发布一个 `-p:TargetInstructionSet=baseline` 的 baseline 版本。这样既能让大多数用户享受性能优化，又为旧 CPU 用户提供兼容版本。

在 `dotnet publish` 命令中添加 `-p:TargetInstructionSet=` 参数，禁用特定指令集优化：

```bash
dotnet publish src/GitPkg -c Release -r linux-x64 -o publish -p:TargetInstructionSet=baseline
```

> **`baseline` 的含义**：`baseline` 表示使用目标架构（如 x86-64）的最低公共指令集，不启用任何额外的 SIMD 或高级指令集优化。对于 x86-64 架构，`baseline` 等同于仅使用 SSE2 指令集。这样编译出的二进制文件可以在所有该架构的 CPU 上运行，兼容性最好，但会牺牲部分性能优化。

或者明确指定目标指令集为较基础的 SSE2（几乎所有 x86-64 CPU 都支持）：

```bash
dotnet publish src/GitPkg -c Release -r linux-x64 -o publish -p:TargetInstructionSet=sse2
```

### 方案二：通过 MSBuild 属性设置

在项目的 `.csproj` 文件中添加属性（本项目禁止修改 csproj，仅作参考）：

```xml
<PropertyGroup>
  <TargetInstructionSet>baseline</TargetInstructionSet>
</PropertyGroup>
```

### 方案三：升级运行环境 CPU

如果目标机器的 CPU 过于老旧，考虑升级硬件或更换到支持 AVX2 的机器上运行。

## 参考

- [Microsoft Learn: Native AOT deployment - CPU architecture](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [.NET 8 Breaking Change: Native AOT targeting](https://learn.microsoft.com/en-us/dotnet/core/compatibility/deployment/8.0/aot-instructions)
