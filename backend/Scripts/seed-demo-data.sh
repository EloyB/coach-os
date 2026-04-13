#!/bin/bash
#
# Seeds a demo environment with realistic data via the CoachOS API.
# Thin wrapper — all logic lives in seed-demo-data.py, all data in seed-data.json.
#
# Usage:
#   ./seed-demo-data.sh
#   ./seed-demo-data.sh http://localhost:5142/api
#
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Find a Python 3 interpreter.
for cmd in python3 python py; do
    if command -v "$cmd" >/dev/null 2>&1; then
        # `py` is Windows Python launcher; force Python 3.
        if [ "$cmd" = "py" ]; then
            PYTHON=("py" "-3")
        else
            PYTHON=("$cmd")
        fi
        break
    fi
done

if [ -z "${PYTHON+x}" ]; then
    echo "ERROR: python3 is required but not found in PATH." >&2
    exit 1
fi

exec "${PYTHON[@]}" "$SCRIPT_DIR/seed-demo-data.py" "$@"
