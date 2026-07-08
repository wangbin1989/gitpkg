# CPU ISA level is lower than required

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

## 解决方案

### 方案一：在发布时降低 CPU ISA 要求 ✅ 本项目采用

> **本项目采用此方案**：在 CI/CD 发布流程中，除了发布默认的优化版本外，额外发布一个 `-p:VectorSupport=false` 的 novector 版本。这样既能让大多数用户享受 SIMD 性能优化，又为旧 CPU 用户提供兼容版本。

在 `dotnet publish` 命令中添加 `-p:VectorSupport=false` 参数，禁用 SIMD 向量指令：

```bash
dotnet publish src/GitPkg -c Release -r linux-x64 -o publish -p:VectorSupport=false
```

> **`VectorSupport=false` 的含义**：禁用所有 SIMD 向量指令支持（SSE、AVX 等），生成的二进制文件仅使用标量指令，兼容所有 x86-64 CPU。相比 `TargetInstructionSet`，此选项更彻底地消除了对向量指令集的依赖。

### 方案二：通过 MSBuild 属性设置

在项目的 `.csproj` 文件中添加属性（本项目禁止修改 csproj，仅作参考）：

```xml
<PropertyGroup>
  <VectorSupport>false</VectorSupport>
</PropertyGroup>
```

### 方案三：升级运行环境 CPU

如果目标机器的 CPU 过于老旧，考虑升级硬件或更换到支持 AVX2 的机器上运行。

## 参考

- [Microsoft Learn: Native AOT deployment - CPU architecture](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [.NET 8 Breaking Change: Native AOT targeting](https://learn.microsoft.com/en-us/dotnet/core/compatibility/deployment/8.0/aot-instructions)
