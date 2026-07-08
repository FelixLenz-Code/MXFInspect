#!/usr/bin/env bash
#
# Builds a Linux AppImage from a self-contained publish of MXFInspect using the
# official appimagetool (the reference implementation), which pairs a compatible
# runtime with a gzip squashfs that every squashfuse/libappimage build can read.
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

# Fetch appimagetool if it is not already on PATH.
APPIMAGETOOL="$(command -v appimagetool || true)"
if [ -z "$APPIMAGETOOL" ]; then
    echo ">> Downloading appimagetool"
    APPIMAGETOOL="$WORK/appimagetool"
    curl -fsSL -o "$APPIMAGETOOL" \
        "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-${ARCH}.AppImage"
    chmod +x "$APPIMAGETOOL"
fi

mkdir -p "$OUTPUT_DIR"
OUT="$OUTPUT_DIR/MXFInspect-${ARCH}.AppImage"

# Avoid AppImageLauncher hijacking; force gzip for maximum reader compatibility.
export APPIMAGELAUNCHER_DISABLE=1
export ARCH

echo ">> Building $OUT with appimagetool"
# --appimage-extract-and-run runs appimagetool without needing FUSE (CI runners).
# Use appimagetool's default compressor (zstd): its bundled mksquashfs supports
# only zstd, and the matching type-2 runtime reads it. Note: on systems whose
# libappimage/squashfuse is too old for zstd, use the FUSE-free tar.gz instead.
"$APPIMAGETOOL" --appimage-extract-and-run --no-appstream "$APPDIR" "$OUT"

echo ">> Done: $OUT ($(du -h "$OUT" | cut -f1))"
