#!/usr/bin/env bash
# Test connectivity to Make.com webhook from this Mac.
# Run from your Terminal:
#   bash scripts/test_make_webhook.sh
#
# Requirements:
#   - .env must contain MAKE_WEBHOOK_URL=...
#   - Make.com scenario must be in "Run once" mode (listening for one webhook)
set -euo pipefail

# Resolve workspace root regardless of where the script is called from
WORKSPACE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="$WORKSPACE_DIR/.env"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "ERROR: .env not found at $ENV_FILE" >&2
  exit 1
fi

# Load .env
set -a
# shellcheck source=/dev/null
source "$ENV_FILE"
set +a

if [[ -z "${MAKE_WEBHOOK_URL:-}" ]]; then
  echo "ERROR: MAKE_WEBHOOK_URL is empty in $ENV_FILE" >&2
  exit 1
fi

PAYLOAD=$(cat <<'JSON'
{
  "platform": "linkedin",
  "scheduled_time": "2026-05-11T08:30:00+02:00",
  "text": "TEST PAYLOAD — niet publiceren.\n\nDrie weekenden Excel om één lessenseizoen rond te krijgen.\n\n80 leerlingen. 12 banen. 4 trainers. 6 niveaugroepen.\n\nCoachOS doet het in één middag.\n\n1 middag · 0 Excel · 1 tool",
  "image_url": "https://coach-os.be/social/week-01-mon.png",
  "first_comment": "→ coach-os.be",
  "_test": true,
  "_source": "local-test-script"
}
JSON
)

echo "Sending test POST to Make.com webhook..."
echo "(Make.com scenario must be listening — click 'Run once' in the scenario editor)"
echo

# Build curl args. Add API key header if MAKE_API_KEY is set.
CURL_ARGS=(-sS -o /tmp/make_response.txt -w "HTTP %{http_code} | %{time_total}s\n" \
  -X POST "$MAKE_WEBHOOK_URL" \
  -H "Content-Type: application/json" \
  --data "$PAYLOAD")

if [[ -n "${MAKE_API_KEY:-}" ]]; then
  CURL_ARGS+=(-H "x-make-apikey: $MAKE_API_KEY")
  echo "(Including x-make-apikey header)"
fi

curl "${CURL_ARGS[@]}"

echo
echo "--- Make.com response ---"
cat /tmp/make_response.txt 2>/dev/null || echo "(no response body)"
echo
echo
echo "Now check Make.com — the scenario should have caught the payload"
echo "and shown all the JSON fields it can use in downstream modules."
