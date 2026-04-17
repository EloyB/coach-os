#!/usr/bin/env bash
# Runs when Claude finishes a task. Checks build + TS + seed scripts.

REPO="$(git -C "$(dirname "$0")/../.." rev-parse --show-toplevel 2>/dev/null)"
if [ -z "$REPO" ]; then exit 0; fi

CHANGED=$(git -C "$REPO" diff --name-only HEAD 2>/dev/null)
if [ -z "$CHANGED" ]; then
  CHANGED=$(git -C "$REPO" diff --name-only 2>/dev/null)
fi

CS=$(echo "$CHANGED" | grep '\.cs$')
TS=$(echo "$CHANGED" | grep -E '\.(ts|tsx)$')
SEED=$(echo "$CHANGED" | grep -iE 'Service\.cs|Endpoint\.cs|Repository\.cs')

if [ -n "$CS" ]; then
  echo "--- [on-stop] CS files changed — verifying dotnet build ---"
  cd "$REPO/backend" && dotnet build CoachOS.slnx --no-restore -v quiet 2>&1 \
    | grep -E "^Build (succeeded|FAILED)|error CS[0-9]+" | head -10
fi

if [ -n "$TS" ]; then
  echo "--- [on-stop] TS files changed — running tsc check ---"
  cd "$REPO/frontend" && node_modules/.bin/tsc --noEmit --pretty false 2>&1 \
    | grep "error TS" | head -10 || echo "tsc: geen fouten"
fi

if [ -n "$SEED" ]; then
  echo ""
  echo "⚠️  Service/Endpoint/Repository gewijzigd — controleer backend/Scripts/ seed-bestanden!"
fi

# Graphify incremental update (no LLM, fast)
if [ -n "$CS" ] || [ -n "$TS" ]; then
  echo "--- [on-stop] Code gewijzigd — graphify graph bijwerken ---"
  C:/Users/loren/AppData/Roaming/Python/Python314/Scripts/graphify.exe update "$REPO" 2>&1 \
    | tail -3 || echo "graphify update mislukt (niet kritiek)"
fi
