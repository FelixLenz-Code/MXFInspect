#!/usr/bin/env bash
#
# Builds a Linux AppImage from a self-contained publish of MXFInspect.
#
# Rather than executing appimagetool (which needs FUSE and is easily blocked by
# AppImageLauncher on developer machines), this squashes the AppDir with
# mksquashfs and prepends the static AppImage type-2 runtime. That works
# unchanged on CI runners and locally.
#
# Usage: build/linux/build-appimage.sh <publish-dir> <output-dir> [arch]
#   <publish-dir>  directory containing the published MXFInspect executable
#   <output-dir>   where the resulting .AppImage is written
#   [arch]         runtime arch (default: x86_64)
#
set -euo pipefail

PUBLISH_DIR="${1:?publish dir required}"
OUTPUT_DIR="${2:?output dir required}"
ARCH="${3:-x86_64}"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
WORK="$(mktemp -d)"
APPDIR="$WORK/MXFInspect.AppDir"

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

# Fetch the static AppImage runtime (no FUSE, no execution of an AppImage).
RUNTIME="$WORK/runtime-$ARCH"
echo ">> Downloading AppImage runtime ($ARCH)"
curl -fsSL -o "$RUNTIME" \
    "https://github.com/AppImage/type2-runtime/releases/download/continuous/runtime-$ARCH"

echo ">> Squashing AppDir"
SQFS="$WORK/app.squashfs"
mksquashfs "$APPDIR" "$SQFS" -root-owned -noappend -quiet -comp zstd

mkdir -p "$OUTPUT_DIR"
OUT="$OUTPUT_DIR/MXFInspect-${ARCH}.AppImage"
echo ">> Writing $OUT"
cat "$RUNTIME" "$SQFS" > "$OUT"
chmod +x "$OUT"

echo ">> Done: $OUT ($(du -h "$OUT" | cut -f1))"
