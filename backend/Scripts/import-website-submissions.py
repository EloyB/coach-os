#!/usr/bin/env python3
"""Eenmalige import van de oude JSON-websiteformulieren naar PostgreSQL.

Standaard dry-run (transactie wordt teruggedraaid); pas met --apply wordt gecommit.
Alles gebeurt in een transactie. Herhalen is veilig: wachtlijst slaat bestaande e-mailadressen
over, contact slaat berichten met hetzelfde e-mailadres en dezelfde ontvangstdatum over.
De databaseverbinding komt uit de PG*-omgevingsvariabelen van psql (nooit uit appsettings).
Er wordt nooit mail verstuurd of klaargezet.
"""
import argparse
import datetime as dt
import hashlib
import json
import re
import secrets
import shlex
import subprocess
import sys
from pathlib import Path

EMAIL = re.compile(r'[^\s@]+@[^\s@]+\.[^\s@]+')
HASH = re.compile(r'[a-f0-9]{64}')
ROLES = (None, 'club', 'coach', 'anders')
WAITLIST_DEADLINE = dt.datetime(2027, 5, 15, tzinfo=dt.timezone.utc)
MAX_RECORDS = 100_000

WAITLIST_SQL = '''
INSERT INTO "WebsiteWaitlistEntries" ("Id", "CreatedAt", "UpdatedAt", "Email", "Role", "UnsubscribeTokenHash")
SELECT gen_random_uuid(), (x->>'received')::timestamptz, (x->>'received')::timestamptz,
       x->>'email', x->>'role', x->>'token_hash'
FROM jsonb_array_elements(:payload) x
ON CONFLICT DO NOTHING
'''

CONTACT_SQL = '''
INSERT INTO "WebsiteContactEntries" ("Id", "CreatedAt", "UpdatedAt", "Name", "Email", "Message", "Organization")
SELECT gen_random_uuid(), (x->>'received')::timestamptz, (x->>'received')::timestamptz,
       x->>'name', x->>'email', x->>'message', x->>'organization'
FROM jsonb_array_elements(:payload) x
WHERE NOT EXISTS (SELECT 1 FROM "WebsiteContactEntries" c
                  WHERE c."Email" = x->>'email' AND c."CreatedAt" = (x->>'received')::timestamptz)
'''


class SourceError(ValueError):
    """Ongeldige bron; de melding bevat nooit persoonsgegevens."""


def parse_received(record, now):
    raw = record.get('submittedAt')
    if not isinstance(raw, str):
        raise SourceError('submittedAt ontbreekt')
    try:
        received = dt.datetime.fromisoformat(raw.replace('Z', '+00:00'))
    except ValueError:
        raise SourceError('ongeldige submittedAt') from None
    if received.tzinfo is None or received > now:
        raise SourceError('ongeldige of toekomstige submittedAt')
    return received.astimezone(dt.timezone.utc)


def parse_email(record):
    email = record.get('email')
    email = email.strip().lower() if isinstance(email, str) else ''
    if len(email) > 254 or not EMAIL.fullmatch(email):
        raise SourceError('ongeldig e-mailadres')
    return email


def text_field(record, field, minimum, maximum, optional=False):
    value = record.get(field)
    if optional and value is None:
        return None
    if not isinstance(value, str) or not minimum <= len(value.strip()) <= maximum:
        raise SourceError(f'ongeldig veld {field}')
    return value.strip()


def contact_expired(received, now):
    # Twaalf kalendermaanden; een bericht van 29 februari verloopt op 28 februari.
    try:
        expiry = received.replace(year=received.year + 1)
    except ValueError:
        expiry = received.replace(year=received.year + 1, day=28)
    return now >= expiry


def token_hash(record):
    existing = record.get('unsubscribeTokenHash')
    if existing:
        if not isinstance(existing, str) or not HASH.fullmatch(existing):
            raise SourceError('ongeldige unsubscribeTokenHash')
        return existing  # oude uitschrijflinks blijven werken
    # Geen bestaande link: unieke, onbruikbare hash (het ruwe token wordt nergens bewaard).
    # Een nieuwe link kan later via de superadmin-route verstuurd worden.
    return hashlib.sha256(secrets.token_hex(32).encode()).hexdigest()


def clean_record(kind, record, now):
    """Geeft een opgeschoond record terug, of None als het overgeslagen moet worden."""
    if not isinstance(record, dict):
        raise SourceError('record is geen object')
    if record.get('unsubscribedAt') or record.get('deletedAt'):
        return None
    email = parse_email(record)
    received = parse_received(record, now)
    clean = {'email': email, 'received': received.isoformat()}
    if kind == 'waitlist':
        if now >= WAITLIST_DEADLINE:
            return None
        role = record.get('role')
        if role not in ROLES:
            raise SourceError('ongeldige rol')
        return {**clean, 'role': role, 'token_hash': token_hash(record)}
    if contact_expired(received, now):
        return None
    return {
        **clean,
        'name': text_field(record, 'name', 2, 100),
        'message': text_field(record, 'message', 10, 2000),
        'organization': text_field(record, 'organization', 0, 100, optional=True) or None,
    }


def select_records(kind, records, now):
    if not isinstance(records, list) or len(records) > MAX_RECORDS:
        raise SourceError('bron moet een JSON-array zijn')
    selected = {}
    for index, record in enumerate(records):
        try:
            clean = clean_record(kind, record, now)
        except (SourceError, ValueError) as error:
            raise SourceError(f'record {index}: {error}') from None
        if clean is None:
            continue
        key = clean['email'] if kind == 'waitlist' else (clean['email'], clean['received'])
        selected.setdefault(key, clean)  # eerste inschrijving wint bij dubbels
    return list(selected.values())


def build_sql(kind, payload, apply):
    # Payload als hex in de SQL-tekst die via stdin naar psql gaat: brontekst kan nooit SQL-syntax
    # worden en verschijnt niet in de proceslijst.
    literal = "convert_from(decode('" + json.dumps(payload).encode().hex() + "','hex'),'UTF8')::jsonb"
    insert = (WAITLIST_SQL if kind == 'waitlist' else CONTACT_SQL).replace(':payload', literal)
    return '\n'.join([
        'BEGIN;',
        "SET LOCAL lock_timeout = '10s';",
        'WITH inserted AS (' + insert + ' RETURNING 1) SELECT count(*) FROM inserted;',
        'COMMIT;' if apply else 'ROLLBACK;',
        '',
    ])


def run_psql(command, sql):
    proc = subprocess.run(shlex.split(command) + ['-X', '-qAt', '-v', 'ON_ERROR_STOP=1'],
                          input=sql, text=True, capture_output=True)
    if proc.returncode:
        raise RuntimeError('psql faalde; de transactie is teruggedraaid')
    return int(proc.stdout.strip().splitlines()[-1])


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--kind', choices=['contact', 'waitlist'], required=True)
    parser.add_argument('--input', required=True, help='pad naar <kind>-submissions.json')
    parser.add_argument('--apply', action='store_true', help='echt importeren (anders dry-run)')
    parser.add_argument('--psql', default='psql', help='psql-commando, bv. "docker exec -i db psql -U postgres -d coachos"')
    args = parser.parse_args(argv)

    now = dt.datetime.now(dt.timezone.utc)
    records = json.loads(Path(args.input).read_text(encoding='utf-8'))
    selected = select_records(args.kind, records, now)
    inserted = run_psql(args.psql, build_sql(args.kind, selected, args.apply))
    print(json.dumps({
        'kind': args.kind, 'dry_run': not args.apply, 'source': len(records),
        'eligible': len(selected), 'skipped': len(records) - len(selected), 'inserted': inserted,
    }))


if __name__ == '__main__':
    try:
        main()
    except (SourceError, ValueError, OSError, RuntimeError) as error:
        # Nooit broninhoud tonen; de melding noemt enkel recordindex en veldnaam.
        print(f'Import geweigerd, niets gecommit: {error}', file=sys.stderr)
        sys.exit(1)
