#!/usr/bin/env bash
# GitPkg Install Script
# 自动检测平台，下载最新版本 gitpkg 并安装到 ~/.gitpkg/bin
#
# 用法:
#   curl -fsSL https://raw.githubusercontent.com/wangbin1989/gitpkg/main/install.sh | bash
#
#   或指定版本:
#   curl -fsSL https://raw.githubusercontent.com/wangbin1989/gitpkg/main/install.sh | bash -s v2.1.0

set -euo pipefail

# ---- 配置 ----
REPO="wangbin1989/gitpkg"
GITHUB_API="https://api.github.com/repos/${REPO}"
INSTALL_DIR="${HOME}/.gitpkg/bin"
BINARY_NAME="gitpkg"

# 颜色
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

# ---- 辅助函数 ----
info()    { printf "${GREEN}[+]${NC} %s\n" "$*"; }
warn()    { printf "${YELLOW}[!]${NC} %s\n" "$*"; }
error()   { printf "${RED}[x]${NC} %s\n" "$*" >&2; }
header()  { printf "\n${CYAN}==>${NC} %s\n" "$*"; }

# ---- 平台检测 ----
detect_platform() {
    local os arch

    case "$(uname -s)" in
        Darwin) os="darwin" ;;
        Linux)  os="linux"  ;;
        *)
            error "不支持的操作系统: $(uname -s)"
            exit 1
            ;;
    esac

    case "$(uname -m)" in
        x86_64|amd64)   arch="x86_64" ;;
        arm64|aarch64)  arch="arm64"  ;;
        *)
            error "不支持的架构: $(uname -m)"
            exit 1
            ;;
    esac

    echo "${os}-${arch}"
}

# ---- CPU ISA 检测 ----
# 检查 x86_64 CPU 是否支持 AVX2（.NET 8+ 默认要求的指令集）
# ARM64 架构无需检测，始终使用标准版本
supports_avx2() {
    local arch="$1"

    # ARM64 不需要检测
    if [[ "${arch}" == "arm64" ]]; then
        return 0
    fi

    # x86_64 架构检测 AVX2 支持
    case "$(uname -s)" in
        Linux)
            grep -q 'avx2' /proc/cpuinfo 2>/dev/null
            ;;
        Darwin)
            # macOS Intel: 检查 CPU 特性
            sysctl -n machdep.cpu.features 2>/dev/null | grep -q 'AVX2'
            ;;
        *)
            # 未知系统，假设支持
            return 0
            ;;
    esac
}

# ---- 下载与安装 ----
download_and_install() {
    local platform="$1"
    local version="$2"
    local arch="$3"
    local archive_suffix archive_ext

    case "${platform}" in
        darwin-arm64)   archive_suffix="darwin-arm64"; archive_ext="tar.gz" ;;
        linux-x86_64)   archive_suffix="linux-x86_64"; archive_ext="tar.gz" ;;
        linux-arm64)    archive_suffix="linux-arm64";  archive_ext="tar.gz" ;;
        *)
            error "未知平台: ${platform}"
            exit 1
            ;;
    esac

    # 检查 x86_64 平台是否需要 novector 版本
    if [[ "${arch}" == "x86_64" ]]; then
        if ! supports_avx2 "${arch}"; then
            info "CPU 不支持 AVX2，使用 novector 版本"
            archive_suffix="${archive_suffix}-novector"
        fi
    fi

    # 获取下载 URL
    local api_url
    if [[ "$version" == "latest" ]]; then
        info "获取最新版本信息..."
        api_url="${GITHUB_API}/releases/latest"
    else
        info "获取版本 ${version} 信息..."
        api_url="${GITHUB_API}/releases/tags/${version}"
    fi

    local download_url
    download_url=$(curl -fsSL "${api_url}" \
        | grep "browser_download_url" \
        | grep "gitpkg-.*-${archive_suffix}\.${archive_ext}" \
        | head -n 1 \
        | cut -d '"' -f 4)

    if [[ -z "${download_url}" ]]; then
        error "未找到 ${platform} 平台的发布包"
        exit 1
    fi

    local archive_name
    archive_name=$(basename "${download_url}")

    info "下载: ${archive_name}"
    local tmp_dir
    tmp_dir=$(mktemp -d)
    # 使用函数封装 trap，避免路径中特殊字符（如单引号）导致命令注入或中断
    cleanup() { rm -rf "${tmp_dir:-}"; }
    trap cleanup EXIT

    curl -fSL --progress-bar "${download_url}" -o "${tmp_dir}/${archive_name}"

    header "解压..."
    cd "${tmp_dir}"
    tar -xzf "${archive_name}"

    if [[ ! -f "${BINARY_NAME}" ]]; then
        error "归档中未找到 ${BINARY_NAME} 可执行文件"
        exit 1
    fi

    chmod +x "${BINARY_NAME}"

    header "安装到 ${INSTALL_DIR}"
    mkdir -p "${INSTALL_DIR}"

    if [[ -f "${INSTALL_DIR}/${BINARY_NAME}" ]]; then
        warn "覆盖已有文件: ${INSTALL_DIR}/${BINARY_NAME}"
    fi

    mv "${BINARY_NAME}" "${INSTALL_DIR}/${BINARY_NAME}"

    info "安装完成: ${INSTALL_DIR}/${BINARY_NAME}"
    "${INSTALL_DIR}/${BINARY_NAME}" --version
}

# ---- PATH 提示 ----
check_path() {
    local shell_name
    shell_name=$(basename "${SHELL:-/bin/bash}")

    local rc_file=""
    case "${shell_name}" in
        zsh)  rc_file="${HOME}/.zshrc"  ;;
        bash) rc_file="${HOME}/.bashrc"  ;;
        fish) rc_file="${HOME}/.config/fish/config.fish" ;;
    esac

    if [[ ":$PATH:" == *":${INSTALL_DIR}:"* ]]; then
        echo ""
        info "${INSTALL_DIR} 已在 PATH 中"
        echo ""
        echo "  运行以下命令初始化 Shell 环境:"
        echo ""
        case "${shell_name}" in
            fish)
                printf "  ${CYAN}%s${NC}\n" "${INSTALL_DIR}/${BINARY_NAME} init fish | source"
                ;;
            *)
                printf "  ${CYAN}%s${NC}\n" "eval \"\$(${INSTALL_DIR}/${BINARY_NAME} init ${shell_name})\""
                ;;
        esac
        echo ""
        echo "  可将上述命令追加到 ${rc_file} 中使其永久生效"
        return 0
    fi

    echo ""
    warn "${INSTALL_DIR} 不在 PATH 中"
    echo ""
    echo "  运行以下命令初始化 Shell 环境:"
    echo ""

    case "${shell_name}" in
        fish)
            printf "  ${CYAN}%s${NC}\n" "${INSTALL_DIR}/${BINARY_NAME} init fish | source"
            ;;
        *)
            printf "  ${CYAN}%s${NC}\n" "eval \"\$(${INSTALL_DIR}/${BINARY_NAME} init ${shell_name})\""
            ;;
    esac

    echo ""
    echo "  可将上述命令追加到 ${rc_file} 中使其永久生效"

    echo ""
    echo "  如需启用自动补全，运行:"
    echo ""
    case "${shell_name}" in
        fish)
            printf "  ${CYAN}%s${NC}\n" "${INSTALL_DIR}/${BINARY_NAME} completion fish > ${HOME}/.config/fish/completions/${BINARY_NAME}.fish"
            ;;
        *)
            printf "  ${CYAN}%s${NC}\n" "eval \"\$(${INSTALL_DIR}/${BINARY_NAME} completion ${shell_name})\""
            ;;
    esac
}

# ---- 主流程 ----
main() {
    local version="${1:-latest}"

    echo ""
    printf "${CYAN}╔════════════════════════════════════╗${NC}\n"
    printf "${CYAN}║       GitPkg 一键安装脚本         ║${NC}\n"
    printf "${CYAN}╚════════════════════════════════════╝${NC}\n"
    echo ""

    info "仓库: ${REPO}"
    info "安装目录: ${INSTALL_DIR}"

    local platform
    platform=$(detect_platform)
    info "检测到平台: ${platform}"

    # 从平台字符串中提取架构
    local arch
    arch="${platform#*-}"

    for cmd in curl tar; do
        if ! command -v "${cmd}" &> /dev/null; then
            error "缺少依赖: ${cmd}"
            exit 1
        fi
    done

    download_and_install "${platform}" "${version}" "${arch}"
    check_path

    echo ""
    info "安装成功！运行 'gitpkg --help' 查看用法"
    echo ""
}

main "$@"
