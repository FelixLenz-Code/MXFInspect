#!/usr/bin/env bash
#
# Builds a Linux AppImage from a self-contained publish of MXFInspect.
#
# We deliberately do NOT use appimagetool here. Two independent problems on real
# user machines (verified on Ubuntu 24.04, kernel 6.8, glibc 2.39) forced this:
#
#   1) Desktop integration. AppImageLauncher/libappimage (still shipped by many
#      distros, e.g. libappimage 1.0.4) rejects the runtime that current
#      appimagetool embeds: libappimage's appimage_get_type() returns -1, so
#      "register in system" fails with the dialog
#      "Fehler beim Registrieren des AppImages im System via libappimage".
#      uruntime (below) is detected as type 2 and registers cleanly.
#
#   2) Mounting. The squashfuse embedded in the old type-2 runtime (0.5.2) fails
#      to mount on modern glibc/kernels with "fuse: memory allocation failed /
#      Can't open squashfs image: Bad address", so the app never launches even
#      after integration succeeds.
#
# uruntime (https://github.com/VHSgunzo/uruntime) fixes both: libappimage
# accepts it as a type-2 AppImage, its bundled musl-static squashfuse mounts
# fine on modern systems, and if FUSE is unavailable it transparently falls back
# to extract-and-run. We pair it with a squashfs written by the *system*
# mksquashfs (appimagetool's bundled mksquashfs only speaks zstd; we use xz,
# which every libappimage build and uruntime's squashfuse can read).
#
# Usage: build/linux/build-appimage.sh <publish-dir> <output-dir> [arch]
#   <publish-dir>  directory containing the published MXFInspect executable
#   <output-dir>   where the resulting .AppImage is written
#   [arch]         AppImage arch tag (default: x86_64)
#
set -euo pipefail

PUBLISH_DIR="${1:?publish dir required}"
OUTPUT_DIR="${2:?output dir required}"
ARCH="${3:-x86_64}"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
APPDIR="$WORK/MXFInspect.AppDir"

# Pinned uruntime (type-2 AppImage runtime with a working squashfuse + FUSE-free
# fallback). Override with URUNTIME=/path/to/uruntime to use a local copy.
URUNTIME_VERSION="v0.5.8"
URUNTIME_SHA256="6cb51eb66d24a03db49dd832f881289c076b922d0f358c9b7ebfe4a83859ef5a"

command -v mksquashfs >/dev/null 2>&1 || {
    echo "!! mksquashfs not found — install squashfs-tools" >&2
    exit 1
}

echo ">> Assembling AppDir at $APPDIR"
mkdir -p "$APPDIR/usr/bin"
cp -a "$PUBLISH_DIR"/. "$APPDIR/usr/bin/"

# Desktop entry and icon (AppImage expects both at the AppDir root).
cp "$REPO_ROOT/build/mxfinspect.desktop" "$APPDIR/mxfinspect.desktop"
cp "$REPO_ROOT/build/mxfinspect.png" "$APPDIR/mxfinspect.png"
cp "$REPO_ROOT/build/mxfinspect.png" "$APPDIR/.DirIcon"
mkdir -p "$APPDIR/usr/share/icons/hicolor/256x256/apps"
cp "$REPO_ROOT/build/mxfinspect.png" "$APPDIR/usr/share/icons/hicolor/256x256/apps/mxfinspect.png"

cat > "$APPDIR/AppRun" <<'EOF'
#!/bin/sh
HERE="$(dirname "$(readlink -f "$0")")"
export LD_LIBRARY_PATH="$HERE/usr/bin:${LD_LIBRARY_PATH:-}"
exec "$HERE/usr/bin/MXFInspect" "$@"
EOF
chmod +x "$APPDIR/AppRun"
chmod +x "$APPDIR/usr/bin/MXFInspect" || true

# Obtain uruntime.
RUNTIME="${URUNTIME:-}"
if [ -z "$RUNTIME" ]; then
    RUNTIME="$WORK/uruntime"
    echo ">> Downloading uruntime $URUNTIME_VERSION"
    curl -fsSL -o "$RUNTIME" \
        "https://github.com/VHSgunzo/uruntime/releases/download/${URUNTIME_VERSION}/uruntime-appimage-squashfs-${ARCH}"
    echo "$URUNTIME_SHA256  $RUNTIME" | sha256sum -c - >/dev/null && \
        echo ">> uruntime checksum OK"
fi
chmod +x "$RUNTIME"

# Write the squashfs payload with the system mksquashfs (xz: readable by every
# libappimage build for desktop integration and by uruntime's squashfuse).
SQFS="$WORK/payload.squashfs"
echo ">> Building squashfs payload (xz)"
mksquashfs "$APPDIR" "$SQFS" \
    -root-owned -noappend -no-progress \
    -comp xz -Xbcj x86 -b 256K -mem 512M >/dev/null

mkdir -p "$OUTPUT_DIR"
OUT="$OUTPUT_DIR/MXFInspect-${ARCH}.AppImage"

echo ">> Assembling AppImage: uruntime + squashfs"
cat "$RUNTIME" "$SQFS" > "$OUT"
chmod +x "$OUT"

echo ">> Done: $OUT ($(du -h "$OUT" | cut -f1))"
