# GitPkg Install Script for Windows
# 自动检测平台，下载最新版本 gitpkg 并安装到 ~/.gitpkg/bin
#
# 用法 (PowerShell):
#   Invoke-Expression (Invoke-RestMethod https://raw.githubusercontent.com/wangbin1989/gitpkg/main/install.ps1)
#
#   或指定版本:
#   .\install.ps1 -Version v2.1.0

param(
    [string]$Version = "latest"
)

$ErrorActionPreference = "Stop"

# ---- 配置 ----
$Repo = "wangbin1989/gitpkg"
$GitHubApi = "https://api.github.com/repos/$Repo"
$InstallDir = "$env:USERPROFILE\.gitpkg\bin"
$BinaryName = "gitpkg.exe"

# ---- 平台检测 ----
function Get-Platform {
    $arch = switch ($env:PROCESSOR_ARCHITECTURE) {
        "AMD64" { "x86_64" }
        "ARM64" { "arm64" }
        default { $env:PROCESSOR_ARCHITECTURE.ToLower() }
    }
    return "windows-$arch"
}

# ---- CPU ISA 检测 ----
# 检查 x86_64 CPU 是否支持 AVX2（.NET 8+ 默认要求的指令集）
# ARM64 架构无需检测，始终使用标准版本
function Test-Avx2Support {
    param([string]$Arch)

    # ARM64 不需要检测
    if ($Arch -eq "arm64") {
        return $true
    }

    # x86_64 架构检测 AVX2 支持
    try {
        # 通过 .NET 运行时检测 AVX2 支持
        return [System.Runtime.Intrinsics.X86.Avx2]::IsSupported
    } catch {
        # 如果 .NET 不可用，回退到检查 CPU 特性
        try {
            $cpu = Get-CimInstance -ClassName Win32_Processor | Select-Object -First 1
            # 较新的 Intel/AMD CPU 通常支持 AVX2
            # 这里使用保守策略：假设支持
            return $true
        } catch {
            # 无法检测，假设支持
            return $true
        }
    }
}

# ---- 下载与安装 ----
function Install-GitPkg {
    $platform = Get-Platform
    Write-Host ""
    Write-Host "仓库: $Repo" -ForegroundColor Cyan
    Write-Host "安装目录: $InstallDir" -ForegroundColor Cyan
    Write-Host "检测到平台: $platform" -ForegroundColor Cyan

    # 从平台字符串中提取架构
    $arch = $platform -replace '^windows-', ''

    $platformSuffix = switch ($platform) {
        "windows-x86_64" { "windows-x86_64" }
        "windows-arm64"  { "windows-arm64" }
        default {
            Write-Host "不支持的平台: $platform" -ForegroundColor Red
            exit 1
        }
    }

    # 检查 x86_64 平台是否需要 novector 版本
    if ($arch -eq "x86_64") {
        if (-not (Test-Avx2Support -Arch $arch)) {
            Write-Host "CPU 不支持 AVX2，使用 novector 版本" -ForegroundColor Yellow
            $platformSuffix = "$platformSuffix-novector"
        }
    }

    $apiUrl = if ($Version -eq "latest") {
        "$GitHubApi/releases/latest"
    } else {
        "$GitHubApi/releases/tags/$Version"
    }

    Write-Host "获取版本信息..." -ForegroundColor Green
    $releaseJson = Invoke-RestMethod -Uri $apiUrl -Headers @{ "User-Agent" = "gitpkg-installer" }
    $asset = $releaseJson.assets | Where-Object {
        $_.name -match "gitpkg-.*-$platformSuffix\.zip"
    } | Select-Object -First 1

    if (-not $asset) {
        Write-Host "未找到 $platform 平台的发布包" -ForegroundColor Red
        exit 1
    }

    Write-Host "下载: $($asset.name)" -ForegroundColor Green
    $tmpDir = Join-Path $env:TEMP "gitpkg-install-$(Get-Random)"
    New-Item -ItemType Directory -Force -Path $tmpDir | Out-Null

    $archivePath = Join-Path $tmpDir $asset.name
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $archivePath

    Write-Host "解压..." -ForegroundColor Cyan
    Expand-Archive -Path $archivePath -DestinationPath $tmpDir -Force

    $exePath = Get-ChildItem -Path $tmpDir -Name "gitpkg.exe" -Recurse | Select-Object -First 1
    if (-not $exePath) {
        Write-Host "归档中未找到 gitpkg.exe" -ForegroundColor Red
        exit 1
    }

    Write-Host "安装到 $InstallDir" -ForegroundColor Cyan
    New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null

    $destPath = Join-Path $InstallDir $BinaryName
    Copy-Item -Path (Join-Path $tmpDir $exePath) -Destination $destPath -Force

    Write-Host "安装完成: $destPath" -ForegroundColor Green
    & $destPath --version

    Remove-Item -Recurse -Force $tmpDir

    # PATH 提示
    $displayPath = '"$env:USERPROFILE\.gitpkg\bin\gitpkg.exe"'
    $currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
    if ($currentPath -like "*$InstallDir*") {
        Write-Host ""
        Write-Host "$InstallDir 已在 PATH 中" -ForegroundColor Green
        Write-Host ""
        Write-Host "  运行以下命令初始化 Shell 环境:" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  & $displayPath init powershell | Out-String | Invoke-Expression" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  可将上述命令追加到 `$PROFILE 中使其永久生效" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  如需启用自动补全，运行:" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  & $displayPath completion powershell | Out-String | Invoke-Expression" -ForegroundColor Cyan
    } else {
        Write-Host ""
        Write-Host "$InstallDir 不在 PATH 中" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "  运行以下命令初始化 Shell 环境:" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  & $displayPath init powershell | Out-String | Invoke-Expression" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  可将上述命令追加到 `$PROFILE 中使其永久生效" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  如需启用自动补全，运行:" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  & $displayPath completion powershell | Out-String | Invoke-Expression" -ForegroundColor Cyan
    }
}

Install-GitPkg
