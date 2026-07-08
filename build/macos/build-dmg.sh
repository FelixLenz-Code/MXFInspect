#!/usr/bin/env bash
#
# Builds a macOS .app bundle and wraps it in a .dmg.
# Must run on macOS (uses hdiutil, sips, iconutil).
#
# Usage: build/macos/build-dmg.sh <publish-dir> <output-dir>
#   <publish-dir>  directory containing the published MXFInspect executable
#   <output-dir>   where the resulting .dmg is written
#
set -euo pipefail

PUBLISH_DIR="${1:?publish dir required}"
OUTPUT_DIR="${2:?output dir required}"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
WORK="$(mktemp -d)"
APP="$WORK/MXFInspect.app"

echo ">> Assembling $APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp "$REPO_ROOT/build/macos/Info.plist" "$APP/Contents/Info.plist"
cp -a "$PUBLISH_DIR"/. "$APP/Contents/MacOS/"
chmod +x "$APP/Contents/MacOS/MXFInspect"

# Build an .icns from the 256px PNG.
ICONSET="$WORK/mxfinspect.iconset"
mkdir -p "$ICONSET"
for size in 16 32 64 128 256 512; do
    sips -z "$size" "$size" "$REPO_ROOT/build/mxfinspect.png" \
        --out "$ICONSET/icon_${size}x${size}.png" >/dev/null
    dbl=$((size * 2))
    sips -z "$dbl" "$dbl" "$REPO_ROOT/build/mxfinspect.png" \
        --out "$ICONSET/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/mxfinspect.icns"

mkdir -p "$OUTPUT_DIR"
DMG="$OUTPUT_DIR/MXFInspect-macos.dmg"

echo ">> Building $DMG"
STAGE="$WORK/dmg"
mkdir -p "$STAGE"
cp -R "$APP" "$STAGE/"
ln -s /Applications "$STAGE/Applications"

hdiutil create -volname "MXFInspect" -srcfolder "$STAGE" -ov -format UDZO "$DMG"

echo ">> Done: $DMG"
