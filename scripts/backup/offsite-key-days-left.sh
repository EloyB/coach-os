#!/usr/bin/env bash
# Print het aantal dagen tot de offsite-backup API-key vervalt, gelezen uit
# expires_at in infrastructure/terraform/offsite-backup.tf.
# Gebruikt door .github/workflows/offsite-backup-key-reminder.yml.
#
#   scripts/backup/offsite-key-days-left.sh [pad/naar/offsite-backup.tf] [vandaag als YYYY-MM-DD]
set -euo pipefail

tf_file="${1:-infrastructure/terraform/offsite-backup.tf}"
today="${2:-}"

expires_at=$(awk '/resource "scaleway_iam_api_key" "offsite_backup"/ {inside=1}
                  inside && /expires_at/ {gsub(/[" ]/, "", $3); print $3; exit}
                  inside && /^}/ {inside=0}' "$tf_file" | sed 's/^=//')

if [[ -z "$expires_at" ]]; then
  echo "expires_at niet gevonden in $tf_file" >&2
  exit 1
fi

python3 - "$expires_at" "$today" <<'PY'
import sys
from datetime import date, datetime
expires = datetime.strptime(sys.argv[1], "%Y-%m-%dT%H:%M:%SZ").date()
today = date.fromisoformat(sys.argv[2]) if sys.argv[2] else date.today()
print(f"{(expires - today).days} {expires.strftime('%d/%m/%Y')}")
PY
