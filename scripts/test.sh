#!/usr/bin/env bash
set -euo pipefail

PROJECT_PATH="$(cd "$(dirname "$0")/.." && pwd)"
UNITY="/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity"
PLATFORM="${1:-EditMode}"
RESULTS="/tmp/test-results.xml"
LOG="/tmp/test.log"

rm -f "$PROJECT_PATH/Temp/UnityLockfile"

echo "Running $PLATFORM tests..."
"$UNITY" \
  -runTests \
  -projectPath "$PROJECT_PATH" \
  -testPlatform "$PLATFORM" \
  -testResults "$RESULTS" \
  -batchmode \
  -logFile "$LOG"

# Parse results
passed=$(grep -o 'passed="[0-9]*"' "$RESULTS" | head -1 | grep -o '[0-9]*')
failed=$(grep -o 'failed="[0-9]*"' "$RESULTS" | head -1 | grep -o '[0-9]*')
total=$(grep -o 'total="[0-9]*"' "$RESULTS" | head -1 | grep -o '[0-9]*')

echo ""
echo "Results: $passed/$total passed, $failed failed"

if [ "$failed" != "0" ]; then
  echo ""
  echo "Failures:"
  grep -B1 'result="Failed"' "$RESULTS" | grep 'name=' | sed 's/.*name="\([^"]*\)".*/  - \1/'
  exit 1
fi
