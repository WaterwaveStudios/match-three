#!/usr/bin/env bash
set -euo pipefail

PROJECT_PATH="$(cd "$(dirname "$0")/.." && pwd)"
UNITY="/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity"
LOG="/tmp/compile.log"

rm -f "$PROJECT_PATH/Temp/UnityLockfile"

echo "Compiling..."
"$UNITY" \
  -projectPath "$PROJECT_PATH" \
  -batchmode \
  -quit \
  -logFile "$LOG"

errors=$(grep "error CS" "$LOG" 2>/dev/null || true)
if [ -n "$errors" ]; then
  echo ""
  echo "Compile errors:"
  echo "$errors"
  exit 1
else
  echo "Compile OK"
fi
