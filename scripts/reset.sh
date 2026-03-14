#!/usr/bin/env bash
set -euo pipefail

echo "Resetting Deadlocked save data..."

# Unity PlayerPrefs on macOS are stored in ~/Library/Preferences/unity.*
# The plist file name uses the company and product name from ProjectSettings
PLIST="$HOME/Library/Preferences/unity.DefaultCompany.match-three.plist"

if [ -f "$PLIST" ]; then
  rm "$PLIST"
  echo "Deleted $PLIST"
else
  echo "No save file found at $PLIST"
fi

# Flush macOS preferences cache so Unity picks up the deletion immediately
killall cfprefsd 2>/dev/null || true

echo "Done. Wallet, upgrades, and all progress reset."
