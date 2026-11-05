#!/usr/bin/env python3
"""Offline JSON cutover using operator PG* environment, never website DB credentials.
Default dry-run rolls its complete transaction back, including ledger creation.
Quiesce legacy writers and supply a reviewed SHA256-email suppression manifest.
No mail is queued. Repeat imports cannot recreate records deleted after cutover.
"""
import argparse
import datetime as dt
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path


def digest(value):
    return hashlib.sha256(value.encode()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--kind', choices=['contact', 'waitlist'], required=True)
    parser.add_argument('--input', required=True)
    parser.add_argument('--suppressions', required=True, help='JSON array of SHA256 normalized email hashes; explicitly reviewed, possibly empty')
    parser.add_argument('--apply', action='store_true')
    parser.add_argument('--confirm-sha256')
    args = parser.parse_args()
    source = Path(args.input).read_bytes()
    suppression_bytes = Path(args.suppressions).read_bytes()
    fingerprint = hashlib.sha256(args.kind.encode() + b'\0' + source + b'\0' + suppression_bytes).hexdigest()
    if args.apply and args.confirm_sha256 != fingerprint:
        parser.error('Apply requires the exact dry-run sha256 of source AND suppression manifest')
    records = json.loads(source)
    suppressions = json.loads(suppression_bytes)
    if not isinstance(records, list) or len(records) > 100000:
        raise ValueError('Expected a bounded JSON array')
    if not isinstance(suppressions, list) or any(not isinstance(x, str) or not re.fullmatch('[a-f0-9]{64}', x) for x in suppressions):
        raise ValueError('Invalid suppression manifest')
    now = dt.datetime.now(dt.timezone.utc)
    deadline = dt.datetime(2027, 5, 15, tzinfo=dt.timezone.utc)
    selected = {}
    skipped = 0
    for record in records:
        if not isinstance(record, dict):
            raise ValueError('Invalid record')
        email = record.get('email', '').strip().lower()
        if len(email) > 254 or not re.fullmatch(r'[^\s@]+@[^\s@]+\.[^\s@]+', email):
            raise ValueError('Invalid record email')
        received = dt.datetime.fromisoformat(record['submittedAt'].replace('Z', '+00:00'))
        if received.tzinfo is None or received > now:
            raise ValueError('Invalid receipt timestamp')
        if digest(email) in suppressions or record.get('unsubscribedAt') or record.get('deletedAt'):
            skipped += 1
            continue
        clean = {'email': email, 'received': received.astimezone(dt.timezone.utc).isoformat()}
        if args.kind == 'waitlist':
            if now >= deadline:
                skipped += 1
                continue
            token = record.get('unsubscribeTokenHash', '')
            if token and not re.fullmatch('[a-f0-9]{64}', token):
                raise ValueError('Invalid capability hash')
            role = record.get('role')
            if role not in [None, 'club', 'coach', 'anders']:
                raise ValueError('Invalid role')
            clean.update(role=role, token=token)
            key = digest('waitlist:' + email)
        else:
            # Calendar anniversary, clamped for leap-day receipts.
            try:
                expiry = received.replace(year=received.year + 1)
            except ValueError:
                expiry = received.replace(year=received.year + 1, day=28)
            if now >= expiry:
                skipped += 1
                continue
            for field, minimum, maximum in [('name', 2, 100), ('message', 10, 2000)]:
                value = record.get(field)
                if not isinstance(value, str) or not minimum <= len(value) <= maximum:
                    raise ValueError('Invalid contact fields')
                clean[field] = value
            organization = record.get('organization')
            if organization is not None and (not isinstance(organization, str) or len(organization) > 100):
                raise ValueError('Invalid organization')
            clean['organization'] = organization
            key = digest('contact:' + json.dumps(clean, sort_keys=True))
        clean['key'] = key
        # Duplicate waitlist capabilities must not be arbitrarily overwritten.
        if key in selected and selected[key] != clean:
            raise ValueError('Conflicting duplicate; reconcile source before cutover')
        selected[key] = clean
    payload = json.dumps(list(selected.values()))
    if args.kind == 'waitlist':
        insertion = '''INSERT INTO "WebsiteWaitlistEntries" ("Id", "CreatedAt", "UpdatedAt", "Email", "Role", "UnsubscribeTokenHash")
            SELECT id, received, received, email, role, token FROM candidates
            ON CONFLICT ("Email") DO NOTHING RETURNING "Id"'''
    else:
        insertion = '''INSERT INTO "WebsiteContactEntries" ("Id", "CreatedAt", "UpdatedAt", "Name", "Email", "Message", "Organization")
            SELECT id, received, received, name, email, message, organization FROM candidates
            ON CONFLICT ("Id") DO NOTHING RETURNING "Id"'''
    # Payload travels only over stdin as hex, never argv; source text cannot enter SQL syntax.
    query = '''BEGIN;
SET LOCAL lock_timeout = '10s';
SELECT pg_advisory_xact_lock(264289) AS locked \\gset
CREATE TABLE IF NOT EXISTS "WebsiteSubmissionImportReceipts" ("Key" text PRIMARY KEY, "ImportedAt" timestamptz NOT NULL DEFAULT now());
CREATE TEMP TABLE candidates ON COMMIT DROP AS
SELECT (substr(x->>'key',1,32))::uuid AS id, x->>'key' AS key,
       (x->>'received')::timestamptz AS received, x->>'email' AS email,
       x->>'role' AS role, x->>'token' AS token, x->>'name' AS name,
       x->>'message' AS message, x->>'organization' AS organization
FROM jsonb_array_elements(:'payload'::jsonb) x
WHERE NOT EXISTS (SELECT 1 FROM "WebsiteSubmissionImportReceipts" r WHERE r."Key"=x->>'key');
'''
    if args.kind == 'waitlist':
        query += '''LOCK TABLE "WebsiteWaitlistEntries" IN SHARE ROW EXCLUSIVE MODE;
DO $$ BEGIN
IF EXISTS (SELECT 1 FROM candidates c JOIN "WebsiteWaitlistEntries" e ON e."Email"=c.email
           WHERE e."UnsubscribeTokenHash" IS DISTINCT FROM c.token) THEN
    RAISE EXCEPTION 'Target capability conflict; explicit reconciliation required';
END IF;
END $$;
'''
    query += 'WITH inserted AS (' + insertion + ') SELECT count(*) AS inserted FROM inserted \\gset\n'
    query += '''INSERT INTO "WebsiteSubmissionImportReceipts" ("Key") SELECT key FROM candidates ON CONFLICT DO NOTHING;
SELECT json_build_object('eligible', (SELECT count(*) FROM candidates), 'inserted', ''' + (":'inserted'::int" if args.apply else '0') + ");\n"
    query += 'COMMIT;\n' if args.apply else 'ROLLBACK;\n'
    query = query.replace(":'payload'::jsonb", "convert_from(decode('" + payload.encode().hex() + "','hex'),'UTF8')::jsonb")
    proc = subprocess.run(['psql', '-X', '-qAt', '-v', 'ON_ERROR_STOP=1'], input=query, text=True, capture_output=True)
    if proc.returncode:
        raise RuntimeError('Database import failed and was rolled back; inspect permissions/schema locally. No source payload is logged.')
    result = json.loads(proc.stdout.strip())
    result.update(sha256=fingerprint, skipped=skipped, source=len(records), dry_run=not args.apply)
    print(json.dumps(result))


if __name__ == '__main__':
    try:
        main()
    except (ValueError, KeyError, TypeError, OSError, RuntimeError):
        print('Import rejected: invalid source, conflicting records, unavailable tool or database transaction failure. No partial import committed.', file=sys.stderr)
        sys.exit(1)
