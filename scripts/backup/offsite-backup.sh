#!/usr/bin/env bash
#
# Nachtelijke versleutelde off-site backup van de CoachOS DataProtection-keyring
# en de database naar een restic-repository (Scaleway Object Storage, EU).
#
# Gebruik:
#   offsite-backup.sh                 backup + retentie (+ wekelijkse check)
#   offsite-backup.sh --check-config  valideert env, tools en connectiviteit;
#                                     schrijft niets (alias: --dry-run)
#
# Alle configuratie komt uit de omgeving (systemd EnvironmentFile), zie
# infrastructure/terraform/offsite-backup.tf (offsite-backup-env). Geen secrets op argv of in
# de logs: restic leest het wachtwoord uit RESTIC_PASSWORD_FILE, pg_dump leest
# PGPASSWORD/PGPASSFILE uit de omgeving. Alle stderr gaat naar de journal.
#
# Exitcodes: 0 = ok, 1 = backup/retentie/check mislukt, 2 = ongeldige config.
#
# Vereist restic >= 0.17 (--stdin-from-command) en pg_dump met dezelfde of een
# hogere major-versie dan de server.

set -euo pipefail

readonly EXIT_FAILURE=1
readonly EXIT_CONFIG=2

BACKUP_HOST="${BACKUP_HOST:-coachos-prod}"
RETENTION_DAILY="${RETENTION_DAILY:-14}"
RETENTION_WEEKLY="${RETENTION_WEEKLY:-8}"
# Dag van de week (1 = maandag ... 7 = zondag) waarop `restic check` draait.
CHECK_WEEKDAY="${CHECK_WEEKDAY:-7}"
# Deel van de data dat bij de wekelijkse check echt wordt gedownload en
# geverifieerd. Leeg = alleen structuur/index controleren.
CHECK_READ_DATA_SUBSET="${CHECK_READ_DATA_SUBSET:-5%}"
PG_DUMP_BIN="${PG_DUMP_BIN:-pg_dump}"

log() { printf '[offsite-backup] %s\n' "$*" >&2; }
die_config() { log "CONFIG-FOUT: $*"; exit "$EXIT_CONFIG"; }

# Optionele healthcheck (bv. healthchecks.io): succes pingt de URL zelf,
# falen pingt <URL>/fail. De URL gaat via stdin naar curl, niet via argv.
ping_healthcheck() {
  local suffix="$1"
  [[ -n "${HEALTHCHECK_URL:-}" ]] || return 0
  command -v curl >/dev/null 2>&1 || { log "curl ontbreekt; healthcheck overgeslagen"; return 0; }
  printf 'url = "%s%s"\n' "${HEALTHCHECK_URL%/}" "$suffix" \
    | curl --silent --show-error --fail --max-time 10 --retry 3 --output /dev/null --config - \
    || log "healthcheck-ping mislukt (backupresultaat blijft ongewijzigd)"
}

on_exit() {
  local status=$?
  if [[ "$MODE" == "backup" ]]; then
    if [[ $status -eq 0 ]]; then
      ping_healthcheck ""
    else
      log "MISLUKT met exitcode $status"
      ping_healthcheck "/fail"
    fi
  fi
}

require_var() {
  local name="$1"
  [[ -n "${!name:-}" ]] || die_config "$name is niet gezet"
}

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || die_config "commando '$1' niet gevonden"
}

validate_config() {
  require_var RESTIC_REPOSITORY
  require_var RESTIC_PASSWORD_FILE
  require_var KEYRING_DIR
  require_var PGHOST
  require_var PGUSER
  require_var PGDATABASE

  [[ -z "${RESTIC_PASSWORD:-}" ]] || die_config "gebruik RESTIC_PASSWORD_FILE, niet RESTIC_PASSWORD"
  [[ -r "$RESTIC_PASSWORD_FILE" && -s "$RESTIC_PASSWORD_FILE" ]] \
    || die_config "RESTIC_PASSWORD_FILE is niet leesbaar of leeg"

  if [[ "$RESTIC_REPOSITORY" == s3:* ]]; then
    require_var AWS_ACCESS_KEY_ID
    require_var AWS_SECRET_ACCESS_KEY
  fi

  if [[ -z "${PGPASSWORD:-}" ]]; then
    [[ -n "${PGPASSFILE:-}" && -r "${PGPASSFILE}" ]] \
      || die_config "zet PGPASSWORD of een leesbare PGPASSFILE"
  fi

  [[ -d "$KEYRING_DIR" && -r "$KEYRING_DIR" && -x "$KEYRING_DIR" ]] \
    || die_config "KEYRING_DIR is geen leesbare map"
  compgen -G "$KEYRING_DIR/key-*.xml" >/dev/null \
    || die_config "KEYRING_DIR bevat geen key-*.xml; verkeerde map?"

  [[ "$RETENTION_DAILY" =~ ^[0-9]+$ && "$RETENTION_WEEKLY" =~ ^[0-9]+$ ]] \
    || die_config "RETENTION_DAILY/RETENTION_WEEKLY moeten gehele getallen zijn"
  [[ "$CHECK_WEEKDAY" =~ ^[1-7]$ ]] || die_config "CHECK_WEEKDAY moet 1..7 zijn"

  require_cmd restic
  require_cmd tar
  require_cmd "$PG_DUMP_BIN"
}

check_connectivity() {
  log "restic-repository bereikbaar en wachtwoord correct?"
  if ! restic cat config >/dev/null; then
    die_config "restic-repository niet bereikbaar, verkeerd wachtwoord, of nog niet geïnitialiseerd (restic init)"
  fi

  log "database bereikbaar en pg_dump-versie compatibel? (schema-only, naar /dev/null)"
  if ! "$PG_DUMP_BIN" --schema-only --format=custom >/dev/null; then
    die_config "pg_dump kan de database niet lezen (connectie, rechten of versie)"
  fi
}

backup_keyring() {
  log "keyring -> restic (keyring.tar)"
  restic backup --host "$BACKUP_HOST" --tag coachos --tag keyring \
    --stdin-filename keyring.tar --stdin-from-command -- \
    tar -C "$KEYRING_DIR" -cf - .
}

backup_database() {
  # --stdin-from-command: restic bewaart geen snapshot als pg_dump faalt, en de
  # dump raakt nooit onversleuteld de schijf.
  log "database -> restic (coachos.dump)"
  restic backup --host "$BACKUP_HOST" --tag coachos --tag db \
    --stdin-filename coachos.dump --stdin-from-command -- \
    "$PG_DUMP_BIN" --format=custom --lock-wait-timeout=2min
}

apply_retention() {
  log "retentie: ${RETENTION_DAILY} dagelijks, ${RETENTION_WEEKLY} wekelijks, prune"
  restic forget --host "$BACKUP_HOST" --tag coachos --group-by host,paths \
    --keep-daily "$RETENTION_DAILY" --keep-weekly "$RETENTION_WEEKLY" --prune
}

maybe_check() {
  if [[ "$(date +%u)" != "$CHECK_WEEKDAY" && "${FORCE_CHECK:-0}" != "1" ]]; then
    return 0
  fi
  log "wekelijkse restic check"
  if [[ -n "$CHECK_READ_DATA_SUBSET" ]]; then
    restic check --read-data-subset="$CHECK_READ_DATA_SUBSET"
  else
    restic check
  fi
}

main() {
  MODE="backup"
  case "${1:-}" in
    "") ;;
    --check-config|--dry-run) MODE="check" ;;
    -h|--help) sed -n '2,19p' "$0"; exit 0 ;;
    *) log "onbekend argument: $1"; exit "$EXIT_CONFIG" ;;
  esac

  trap on_exit EXIT
  validate_config

  if [[ "$MODE" == "check" ]]; then
    check_connectivity
    log "config OK (niets geschreven)"
    return 0
  fi

  backup_keyring || exit "$EXIT_FAILURE"
  backup_database || exit "$EXIT_FAILURE"
  apply_retention || exit "$EXIT_FAILURE"
  maybe_check || exit "$EXIT_FAILURE"
  log "klaar"
}

MODE="init"
main "$@"
