#!/usr/bin/env python3
"""Explicit operator backup, no scheduler or cloud provisioning. Secrets via environment/files only."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import stat
import subprocess
import tempfile
import xml.etree.ElementTree as ET


def run(args):
    # Do not relay provider/SQL output: it may contain credentials or personal data.
    r = subprocess.run([str(x) for x in args], capture_output=True)
    if r.returncode:
        raise RuntimeError(f'{Path(args[0]).name} failed (exit {r.returncode}); inspect locally with redacted diagnostics')
    return r.stdout


def ring_files(directory):
    if directory.is_symlink() or not directory.is_dir():
        raise ValueError('keyring must be a real directory')
    result = {}; total = 0
    for p in directory.iterdir():
        if len(result) >= 512:
            raise ValueError('oversized keyring')
        if p.is_symlink() or not p.is_file() or p.suffix != '.xml':
            raise ValueError('keyring must contain only regular XML files')
        remaining = 16 * 1024 * 1024 - total
        if p.stat().st_size > remaining:
            raise ValueError('keyring exceeds byte bound')
        with p.open('rb') as stream:
            data = stream.read(remaining + 1)
        if len(data) > remaining:
            raise ValueError('keyring grew beyond byte bound')
        total += len(data); ET.fromstring(data)
        result[p.name] = data
    if not result:
        raise ValueError('empty keyring')
    return result


def backup(keyring, parent):
    if parent.is_symlink() or not parent.is_dir() or stat.S_IMODE(parent.stat().st_mode) != 0o700:
        raise ValueError('staging parent must be private 0700 directory')
    before = ring_files(keyring)
    with tempfile.TemporaryDirectory(prefix='coachos-capture-', dir=parent) as tmp:
        stage = Path(tmp)
        run(['pg_dump', '--format=custom', '--no-owner', '--no-privileges', '--file', stage/'database.dump'])
        # Data Protection persists a new key before it can encrypt database values.
        # An unchanged full ring around the consistent pg_dump spans all keys used by that dump.
        # Rotation/revocation/change => fail closed and retry a later scheduled run, without API stop.
        after = ring_files(keyring)
        if before != after:
            raise ValueError('keyring changed during database capture; retry without stopping API')
        ring = stage/'keyring'; ring.mkdir(mode=0o700)
        for name, data in after.items():
            (ring/name).write_bytes(data); (ring/name).chmod(0o600)
        (stage/'database.dump').chmod(0o600)
        (stage/'manifest.json').write_text(json.dumps({'format': 1, 'quarantined_restore': True,
            'keyring_sha256': {k: hashlib.sha256(v).hexdigest() for k, v in after.items()}}))
        # Stable root/host/tag allows Restic retention to group snapshots across temporary staging paths.
        run(['restic', 'backup', '--host', 'coachos-backup', '--tag', 'coachos-db-keyring', '--stdin-from-command',
             '--stdin-filename', 'capture.tar', '--', 'tar', '-C', stage, '-cf', '-', '.'])
    print('Backup completed: encrypted Restic snapshot; restore remains quarantined pending current privacy ledger reconciliation.')


def guard(ledger):
    from datetime import datetime, timezone, timedelta
    from uuid import UUID
    if ledger.is_symlink() or ledger.stat().st_size > 8 * 1024 * 1024:
        raise ValueError('invalid ledger file')
    data = json.loads(ledger.read_text())
    stamp = datetime.fromisoformat(data['complete_through'])
    age = datetime.now(timezone.utc) - stamp
    if age < timedelta(minutes=-5) or age > timedelta(hours=24):
        raise ValueError('current independently maintained ledger required; stale/future ledger rejected')
    checks = []
    tables = {'enrollment': 'Enrollments', 'camp_enrollment': 'CampEnrollments', 'organization': 'Organizations'}
    erasures = data['erasures']; leads = data['unsubscribed_entry_ids']
    if not isinstance(erasures, list) or not isinstance(leads, list) or len(erasures) + len(leads) > 10000:
        raise ValueError('invalid ledger bounds')
    for entry in erasures:
        table = tables[entry['kind']]
        org = str(UUID(entry['organization_id'])); target = str(UUID(entry['target_id']))
        where = f'"Id"=\'{target}\''
        if table != 'Organizations': where += f' AND "OrganizationId"=\'{org}\''
        elif org != target: raise ValueError('organization receipt mismatch')
        checks.append(f'SELECT 1 FROM "{table}" WHERE {where}')
    for lead in leads:
        target = str(UUID(lead))
        checks.append(f'SELECT 1 FROM "WebsiteWaitlistEntries" WHERE "Id"=\'{target}\'')
    if checks:
        sql = 'BEGIN READ ONLY; SET LOCAL statement_timeout=\'30s\'; SELECT count(*) FROM (' + ' UNION ALL '.join(checks) + ') resurrected; COMMIT;'
        count = int(run(['psql', '-XqAt', '-v', 'ON_ERROR_STOP=1', '-c', sql]).decode().strip())
        if count:
            print(f'QUARANTINE: {count} deleted/unsubscribed records reappeared; no API/SMTP activation, reconcile and rerun.')
            return 3
    print('Ledger targets absent. This is NOT release authorization or proof of ledger completeness; independent operator reconciliation still required.')
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    b = sub.add_parser('backup'); b.add_argument('--keyring', type=Path, required=True); b.add_argument('--staging-parent', type=Path, required=True)
    sub.add_parser('prune')
    g = sub.add_parser('guard'); g.add_argument('--ledger', type=Path, required=True)
    a = parser.parse_args()
    os.umask(0o077)
    try:
        if a.command == 'backup': backup(a.keyring, a.staging_parent)
        elif a.command == 'guard': return guard(a.ledger)
        else:
            run(['restic', 'forget', '--host', 'coachos-backup', '--tag', 'coachos-db-keyring', '--group-by', 'host,tags', '--keep-within', '14d', '--prune'])
            print('Restic snapshot retention applied; no bucket object expiry.')
    except (ValueError, RuntimeError, OSError, ET.ParseError) as e:
        print(str(e)); return 1
    return 0

if __name__ == '__main__': raise SystemExit(main())
