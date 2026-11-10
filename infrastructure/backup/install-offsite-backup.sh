#!/usr/bin/env bash
#
# Installeert of actualiseert de off-site backup op de VPS. Idempotent: veilig
# om bij elke deploy opnieuw te draaien.
#
# Draait als root via .github/workflows/offsite-backup.yml:
#   install-offsite-backup.sh <pad-naar-repo-checkout>
#
# Vereist in de omgeving: SCW_SECRET_KEY en SCW_DEFAULT_PROJECT_ID (de key van
# de VPS, die Secret Manager mag lezen). Optioneel: SCW_REGION (standaard fr-par).
#
# De configuratie komt uit Scaleway Secret Manager (pad /offsite-backup),
# aangemaakt door infrastructure/terraform/offsite-backup.tf. Bestaan die
# secrets nog niet (Terraform nog niet toegepast), dan stopt het script zonder
# fout en met een waarschuwing.

set -euo pipefail

readonly RESTIC_VERSION="0.19.1"
readonly RESTIC_SHA256_AMD64="f415415624dcc452f2a02b8c33641791a8c6d6d3b65bbb3543fcf9a25151585c"
readonly RESTIC_SHA256_ARM64="a5f64aaab53d51e311fa3829124c5b703f2d14cf187d8640b6be3b2b49376465"

readonly BACKUP_USER="coachos-backup"
readonly CONF_DIR="/etc/coachos-backup"
readonly ENV_FILE="$CONF_DIR/offsite-backup.env"
readonly SECRET_PATH="/offsite-backup"
readonly SERVICE="coachos-offsite-backup"

REPO_DIR="${1:?gebruik: install-offsite-backup.sh <pad-naar-repo-checkout>}"
REGION="${SCW_REGION:-fr-par}"
API_URL="${SCW_API_URL:-https://api.scaleway.com}"  # override enkel voor tests

log() { printf '[install-offsite-backup] %s\n' "$*" >&2; }

[[ $EUID -eq 0 ]] || { log "moet als root draaien"; exit 1; }
: "${SCW_SECRET_KEY:?SCW_SECRET_KEY ontbreekt}"
: "${SCW_DEFAULT_PROJECT_ID:?SCW_DEFAULT_PROJECT_ID ontbreekt}"

# ── Secret Manager ──────────────────────────────────────────────────────────
# De key gaat via stdin naar curl (--config -), nooit via argv of de logs.
scw_api() {
  printf 'header = "X-Auth-Token: %s"\n' "$SCW_SECRET_KEY" \
    | curl --silent --show-error --fail --max-time 20 --retry 3 --config - \
      "$API_URL/secret-manager/v1beta1/regions/$REGION/$1"
}

# Zoekt de secret-ID op naam; leeg als de secret niet bestaat.
secret_id() {
  scw_api "secrets?project_id=$SCW_DEFAULT_PROJECT_ID&name=$1&path=$SECRET_PATH" \
    | python3 -c 'import json, sys
s = json.load(sys.stdin).get("secrets", [])
print(s[0]["id"] if len(s) == 1 else "")'
}

# Schrijft de laatste versie van secret $1 naar bestand $2 (eigenaar $3, mode $4),
# atomair via een tijdelijk bestand in dezelfde map.
write_secret() {
  local name="$1" dest="$2" owner="$3" mode="$4" id tmp
  id="$(secret_id "$name")"
  [[ -n "$id" ]] || { log "secret $SECRET_PATH/$name ontbreekt"; return 1; }
  tmp="$(mktemp "$CONF_DIR/.tmp.XXXXXX")"
  scw_api "secrets/$id/versions/latest/access" \
    | python3 -c 'import base64, json, sys
sys.stdout.buffer.write(base64.b64decode(json.load(sys.stdin)["data"]))' > "$tmp"
  [[ -s "$tmp" ]] || { rm -f "$tmp"; log "secret $name is leeg"; return 1; }
  chown "$owner" "$tmp"
  chmod "$mode" "$tmp"
  mv -f "$tmp" "$dest"
}

if [[ -z "$(secret_id offsite-backup-env)" ]]; then
  echo "::warning::Off-site backup niet geïnstalleerd: de secrets in Secret Manager ontbreken nog. Laat eerst de Terraform-workflow lopen."
  exit 0
fi

# ── Gebruiker en config ────────────────────────────────────────────────────
id -u "$BACKUP_USER" >/dev/null 2>&1 \
  || useradd --system --no-create-home --shell /usr/sbin/nologin "$BACKUP_USER"
install -d -m 0750 -o root -g "$BACKUP_USER" "$CONF_DIR"

write_secret offsite-backup-env "$ENV_FILE" root:root 0600
write_secret offsite-backup-restic-password "$CONF_DIR/restic-password" "$BACKUP_USER:$BACKUP_USER" 0400
# libpq negeert een .pgpass die groep- of wereldleesbaar is.
write_secret offsite-backup-pgpass "$CONF_DIR/pgpass" "$BACKUP_USER:$BACKUP_USER" 0400
log "config geschreven naar $CONF_DIR"

PG_MAJOR="$(sed -n 's/^PG_MAJOR=\([0-9]\+\)$/\1/p' "$ENV_FILE")"
[[ -n "$PG_MAJOR" ]] || { log "PG_MAJOR ontbreekt in $ENV_FILE"; exit 1; }

# ── pg_dump (PGDG; Ubuntu 22.04 heeft zelf enkel 14) ───────────────────────
if [[ ! -x "/usr/lib/postgresql/$PG_MAJOR/bin/pg_dump" ]]; then
  log "postgresql-client-$PG_MAJOR installeren via apt.postgresql.org"
  export DEBIAN_FRONTEND=noninteractive
  install -d /usr/share/postgresql-common/pgdg
  curl --silent --show-error --fail -o /usr/share/postgresql-common/pgdg/apt.postgresql.org.asc \
    https://www.postgresql.org/media/keys/ACCC4CF8.asc
  # shellcheck disable=SC1091
  codename="$(. /etc/os-release && echo "$VERSION_CODENAME")"
  echo "deb [signed-by=/usr/share/postgresql-common/pgdg/apt.postgresql.org.asc] https://apt.postgresql.org/pub/repos/apt ${codename}-pgdg main" \
    > /etc/apt/sources.list.d/pgdg.list
  apt-get update -qq
  apt-get install -y -qq "postgresql-client-$PG_MAJOR"
fi

# ── restic (officiële binary, vastgepinde versie en checksum) ──────────────
if [[ "$(/usr/local/bin/restic version 2>/dev/null | awk '{print $2}')" != "$RESTIC_VERSION" ]]; then
  case "$(dpkg --print-architecture)" in
    amd64) arch=amd64; sha="$RESTIC_SHA256_AMD64" ;;
    arm64) arch=arm64; sha="$RESTIC_SHA256_ARM64" ;;
    *) log "onbekende architectuur"; exit 1 ;;
  esac
  log "restic $RESTIC_VERSION installeren"
  command -v bunzip2 >/dev/null || apt-get install -y -qq bzip2
  tmpdir="$(mktemp -d)"
  trap 'rm -rf "$tmpdir"' EXIT
  curl --silent --show-error --fail --location -o "$tmpdir/restic.bz2" \
    "https://github.com/restic/restic/releases/download/v${RESTIC_VERSION}/restic_${RESTIC_VERSION}_linux_${arch}.bz2"
  echo "$sha  $tmpdir/restic.bz2" | sha256sum --check --quiet
  bunzip2 "$tmpdir/restic.bz2"
  install -m 0755 "$tmpdir/restic" /usr/local/bin/restic
fi

# ── Script en systemd-units ────────────────────────────────────────────────
install -m 0755 "$REPO_DIR/scripts/backup/offsite-backup.sh" "/usr/local/bin/$SERVICE"
install -m 0644 "$REPO_DIR/infrastructure/backup/$SERVICE.service" /etc/systemd/system/
install -m 0644 "$REPO_DIR/infrastructure/backup/$SERVICE.timer" /etc/systemd/system/
systemctl daemon-reload

# ── Repository aanmaken als ze nog niet bestaat ────────────────────────────
# `restic init` weigert op een bestaande repository, dus een netwerkfout bij
# `cat config` kan nooit een tweede repository aanmaken.
(
  set -a
  # shellcheck disable=SC1090
  . "$ENV_FILE"
  set +a
  if ! restic --no-cache cat config >/dev/null 2>&1; then
    log "restic-repository aanmaken"
    restic --no-cache init
  fi
)

# ── Config testen met dezelfde rechten als de service ──────────────────────
systemd-run --quiet --wait --pipe --collect \
  -p User="$BACKUP_USER" -p Group="$BACKUP_USER" \
  -p AmbientCapabilities=CAP_DAC_READ_SEARCH \
  -p EnvironmentFile="$ENV_FILE" \
  -p Environment=RESTIC_CACHE_DIR=/var/cache/coachos-backup \
  -p CacheDirectory=coachos-backup \
  "/usr/local/bin/$SERVICE" --check-config

systemctl enable --now "$SERVICE.timer"
systemctl list-timers "$SERVICE.timer" --no-pager
log "klaar"
