# GitPkg Install Script for Windows
# 自动检测平台，下载最新版本 gitpkg 并安装到 ~/.gitpkg/bin
#
# 用法 (PowerShell):
#   Invoke-Expression (Invoke-RestMethod https://raw.githubusercontent.com/wangbin1989/gitpkg/main/install.ps1)
#
#   或指定版本:
#   .\install.ps1 -Version v1.1.4

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

# ---- 下载与安装 ----
function Install-GitPkg {
    $platform = Get-Platform
    Write-Host ""
    Write-Host "仓库: $Repo" -ForegroundColor Cyan
    Write-Host "安装目录: $InstallDir" -ForegroundColor Cyan
    Write-Host "检测到平台: $platform" -ForegroundColor Cyan

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
        Write-Host "请重新打开终端或运行以下命令使新安装的 gitpkg 生效:" -ForegroundColor Green
        Write-Host ""
        Write-Host "  . `$PROFILE" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  如需启用自动补全，运行:" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  Invoke-Expression (& $displayPath completion powershell)" -ForegroundColor Cyan
    } else {
        Write-Host ""
        Write-Host "$InstallDir 不在 PATH 中" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "  请运行以下命令初始化 Shell 环境（PATH + 自动补全）:" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  Invoke-Expression (& $displayPath init powershell)" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  可将上述命令追加到 `$PROFILE 中使其永久生效" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  如需启用自动补全，运行:" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  Invoke-Expression (& $displayPath completion powershell)" -ForegroundColor Cyan
    }
}

Install-GitPkg
