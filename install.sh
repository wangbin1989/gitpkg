#!/usr/bin/env bash
# GitPkg Install Script
# 自动检测平台，下载最新版本 gitpkg 并安装到 ~/.gitpkg/bin
#
# 用法:
#   curl -fsSL https://raw.githubusercontent.com/wangbin1989/gitpkg/main/install.sh | bash
#
#   或指定版本:
#   curl -fsSL https://raw.githubusercontent.com/wangbin1989/gitpkg/main/install.sh | bash -s v1.1.4

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
    trap "rm -rf '${tmp_dir}'" EXIT

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

# ---- PATH 检查 ----
check_path() {
    if [[ ":$PATH:" == *":${INSTALL_DIR}:"* ]]; then
        return 0
    fi

    echo ""
    warn "${INSTALL_DIR} 不在 PATH 中"

    local shell_name
    shell_name=$(basename "${SHELL:-/bin/bash}")

    local rc_file=""
    case "${shell_name}" in
        zsh)  rc_file="${HOME}/.zshrc"  ;;
        bash) rc_file="${HOME}/.bashrc"  ;;
        fish) rc_file="${HOME}/.config/fish/config.fish" ;;
    esac

    if [[ -n "${rc_file}" ]]; then
        echo ""
        echo "  将以下行添加到 ${rc_file} 后重新打开终端:"
        echo ""
        printf "  ${CYAN}export PATH=\"%s:\$PATH\"${NC}\n" "${INSTALL_DIR}"
        echo ""
        read -r -p "  是否自动添加? [Y/n] " answer
        if [[ -z "${answer}" || "${answer}" =~ ^[Yy] ]]; then
            echo "" >> "${rc_file}"
            echo "# GitPkg" >> "${rc_file}"
            echo "export PATH=\"${INSTALL_DIR}:\$PATH\"" >> "${rc_file}"
            info "已添加到 ${rc_file}，运行 'source ${rc_file}' 或重新打开终端使其生效"
        fi
    else
        echo "  请手动将 ${INSTALL_DIR} 加入 PATH"
    fi
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

    for cmd in curl tar; do
        if ! command -v "${cmd}" &> /dev/null; then
            error "缺少依赖: ${cmd}"
            exit 1
        fi
    done

    download_and_install "${platform}" "${version}"
    check_path

    echo ""
    info "安装成功！运行 'gitpkg --help' 查看用法"
    echo ""
}

main "$@"
