#!/usr/bin/env bash
# PostToolUse hook: runs tsc --noEmit when a .ts/.tsx file was edited.
# Claude Code passes the tool call as JSON on stdin.

INPUT=$(cat)
FILE=$(echo "$INPUT" | node -e "const d=JSON.parse(require('fs').readFileSync('/dev/stdin','utf8')); process.stdout.write(d.tool_input?.file_path || d.tool_input?.path || '')" 2>/dev/null)

# Only act on .ts/.tsx files inside the frontend directory
if [[ "$FILE" != *frontend* ]] || [[ "$FILE" != *.ts && "$FILE" != *.tsx ]]; then
  exit 0
fi

REPO="$(git -C "$(dirname "$0")/../.." rev-parse --show-toplevel 2>/dev/null)"
if [ -z "$REPO" ]; then exit 0; fi

echo "--- [ts-edit] tsc check na bewerking van $(basename "$FILE") ---"
cd "$REPO/frontend" && node_modules/.bin/tsc --noEmit --pretty false 2>&1 \
  | grep "error TS" | head -5

exit 0  # nooit blokkeren
