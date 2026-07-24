#!/usr/bin/env bash
# GitPkg Install Script
# 自动检测平台，下载最新版本 gitpkg 并安装到 ~/.gitpkg/bin
#
# 用法:
#   curl -fsSL https://raw.githubusercontent.com/wangbin1989/gitpkg/main/install.sh | bash
#
#   或指定版本:
#   curl -fsSL https://raw.githubusercontent.com/wangbin1989/gitpkg/main/install.sh | bash -s v2.1.0
#
#   安装单文件版本 (SCD):
#   curl -fsSL https://raw.githubusercontent.com/wangbin1989/gitpkg/main/install.sh | bash -s -- -scd

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

# ---- 下载与安装 ----
download_and_install() {
    local platform="$1"
    local version="$2"
    local use_scd="${3:-false}"
    local archive_suffix archive_ext asset_pattern

    case "${platform}" in
        darwin-arm64)   archive_suffix="darwin-arm64"; archive_ext="tar.gz" ;;
        linux-x86_64)   archive_suffix="linux-x86_64"; archive_ext="tar.gz" ;;
        linux-arm64)    archive_suffix="linux-arm64";  archive_ext="tar.gz" ;;
        *)
            error "未知平台: ${platform}"
            exit 1
            ;;
    esac

    # 根据是否使用 SCD 版本构造匹配模式
    if [[ "${use_scd}" == "true" ]]; then
        asset_pattern="gitpkg-.*-scd-${archive_suffix}\.${archive_ext}"
    else
        asset_pattern="gitpkg-.*-${archive_suffix}\.${archive_ext}"
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
    if [[ "${use_scd}" == "true" ]]; then
        download_url=$(curl -fsSL "${api_url}" \
            | grep "browser_download_url" \
            | grep "${asset_pattern}" \
            | head -n 1 \
            | cut -d '"' -f 4)
    else
        # 非 scd 模式时，排除 scd 版本避免误匹配
        download_url=$(curl -fsSL "${api_url}" \
            | grep "browser_download_url" \
            | grep "${asset_pattern}" \
            | grep -v "scd" \
            | head -n 1 \
            | cut -d '"' -f 4)
    fi

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

# ---- 参数解析 ----
parse_args() {
    local version="latest"
    local use_scd="false"

    while [[ $# -gt 0 ]]; do
        case "$1" in
            -scd|--scd)
                use_scd="true"
                shift
                ;;
            -*)
                error "未知选项: $1"
                exit 1
                ;;
            *)
                version="$1"
                shift
                ;;
        esac
    done

    echo "${version} ${use_scd}"
}

# ---- 主流程 ----
main() {
    local args
    args=$(parse_args "$@")
    local version use_scd
    read -r version use_scd <<< "${args}"

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

    for cmd in curl tar; do
        if ! command -v "${cmd}" &> /dev/null; then
            error "缺少依赖: ${cmd}"
            exit 1
        fi
    done

    if [[ "${use_scd}" == "true" ]]; then
        info "安装模式: 单文件 (SCD)"
    fi

    download_and_install "${platform}" "${version}" "${use_scd}"
    check_path

    echo ""
    info "安装成功！运行 'gitpkg --help' 查看用法"
    echo ""
}

main "$@"
