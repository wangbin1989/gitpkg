# GitPkg Install Script for Windows
# 自动检测平台，下载最新版本 gitpkg 并安装到 ~/.gitpkg/bin
#
# 用法 (PowerShell):
#   Invoke-Expression (Invoke-RestMethod https://raw.githubusercontent.com/wangbin1989/gitpkg/main/install.ps1)
#
#   或指定版本:
#   .\install.ps1 -Version v2.1.0
#
#   安装单文件版本 (SCD):
#   .\install.ps1 -Scd

param(
    [string]$Version = "latest",
    [switch]$Scd
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

# ---- 下载与安装 ----
function Install-GitPkg {
    $platform = Get-Platform
    Write-Host ""
    Write-Host "仓库: $Repo" -ForegroundColor Cyan
    Write-Host "安装目录: $InstallDir" -ForegroundColor Cyan
    Write-Host "检测到平台: $platform" -ForegroundColor Cyan

    if ($Scd) {
        Write-Host "安装模式: 单文件 (SCD)" -ForegroundColor Cyan
    }

    $platformSuffix = switch ($platform) {
        "windows-x86_64" { "windows-x86_64" }
        "windows-arm64"  { "windows-arm64" }
        default {
            Write-Host "不支持的平台: $platform" -ForegroundColor Red
            exit 1
        }
    }

    $apiUrl = if ($Version -eq "latest") {
        "$GitHubApi/releases/latest"
    } else {
        "$GitHubApi/releases/tags/$Version"
    }

    Write-Host "获取版本信息..." -ForegroundColor Green
    $releaseJson = Invoke-RestMethod -Uri $apiUrl -Headers @{ "User-Agent" = "gitpkg-installer" }

    # 根据是否使用 SCD 版本构造匹配模式
    if ($Scd) {
        $assetPattern = "gitpkg-.*-scd-$platformSuffix\.zip"
    } else {
        # 使用负向前瞻排除 scd 版本，避免误匹配
        $assetPattern = "gitpkg-.*-(?!scd-)$platformSuffix\.zip"
    }

    $asset = $releaseJson.assets | Where-Object {
        $_.name -match $assetPattern
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
