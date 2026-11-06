"""Opt-in actual PostgreSQL/Restic integration; only disposable loopback databases."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest
import uuid

ROOT = Path(__file__).resolve().parents[2]

@unittest.skipUnless(os.getenv('COACHOS_BACKUP_LOCAL_TEST') == '1', 'explicit local integration opt-in')
class ResticBackupTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(prefix='coachos-restic-test-')
        self.addCleanup(self.tmp.cleanup)
        self.p = Path(self.tmp.name)
        self.env = {k: v for k, v in os.environ.items() if k in {'PATH', 'LD_LIBRARY_PATH', 'HOME'}}
        self.env.update(PGHOST='127.0.0.1', PGPORT='55432', PGUSER='coachos_test', PGDATABASE='backup_test_' + uuid.uuid4().hex,
                        RESTIC_REPOSITORY=str(self.p/'repo'), RESTIC_PASSWORD='disposable-local-test-only')
        self.run_cmd(['createdb', self.env['PGDATABASE']])
        self.addCleanup(lambda: self.run_cmd(['dropdb', '--if-exists', self.env['PGDATABASE']]))
        self.run_cmd(['restic', 'init'])
        self.ring = self.p/'ring'; self.ring.mkdir(mode=0o700)
        (self.ring/'key.xml').write_text('<key id="fictional"/>')
        self.sql('CREATE TABLE proof (id int, cipher text); INSERT INTO proof VALUES (1,\'fictional\');')

    def run_cmd(self, args, expected=0):
        r = subprocess.run([str(x) for x in args], env=self.env, text=True, capture_output=True)
        self.assertEqual(r.returncode, expected, r.stdout + r.stderr)
        return r.stdout

    def sql(self, sql):
        return self.run_cmd(['psql', '-XAt', '-v', 'ON_ERROR_STOP=1', '-c', sql])

    def tool(self, *args, expected=0):
        return self.run_cmd([sys.executable, ROOT/'scripts/restic_backup.py', *args], expected)

    def test_real_dump_restic_restore_and_original_loss(self):
        probe = ROOT/'scripts/tests/KeyringProbe/bin/Debug/net10.0/KeyringProbe.dll'
        if not probe.exists(): self.fail('Build KeyringProbe first for real database+crypto proof')
        (self.ring/'key.xml').unlink()
        cipher = self.p/'cipher'
        self.run_cmd(['dotnet', probe, 'protect', self.ring, cipher])
        self.sql("UPDATE proof SET cipher='" + cipher.read_text() + "';")
        self.tool('backup', '--keyring', self.ring, '--staging-parent', self.p)
        snapshots = json.loads(self.run_cmd(['restic', 'snapshots', '--json']))
        self.assertEqual(len(snapshots), 1)
        self.sql('DROP TABLE proof;')
        shutil.rmtree(self.ring)
        restored = self.p/'restored'
        self.run_cmd(['restic', 'restore', snapshots[0]['id'], '--target', restored])
        import tarfile
        with tarfile.open(next(restored.rglob('capture.tar'))) as archive:
            archive.extractall(restored/'capture', filter='data')
        dump = next(restored.rglob('database.dump'))
        self.run_cmd(['pg_restore', '--no-owner', '--no-privileges', '--exit-on-error', '-d', self.env['PGDATABASE'], dump])
        cipher.write_text(self.sql('SELECT cipher FROM proof;').strip())
        restored_ring = next(restored.rglob('keyring'))
        self.run_cmd(['dotnet', probe, 'unprotect', self.p/'lost-ring', cipher], expected=3)
        self.run_cmd(['dotnet', probe, 'unprotect', restored_ring, cipher])
        self.run_cmd(['dotnet', probe, 'unprotect', restored_ring, cipher, 'WrongApplication'], expected=3)
        self.run_cmd(['restic', 'check', '--read-data'])
        self.env['RESTIC_PASSWORD'] = 'wrong-local-test-password'
        self.run_cmd(['restic', 'snapshots'], expected=12)

    def test_keyring_change_during_dump_does_not_publish_snapshot(self):
        wrapper = self.p/'bin'; wrapper.mkdir()
        real = shutil.which('pg_dump', path=self.env['PATH'])
        script = wrapper/'pg_dump'
        script.write_text('#!' + sys.executable + '\nimport subprocess,sys\n'
            + 'subprocess.run(' + repr([real]) + '+sys.argv[1:],check=True)\n'
            + 'from pathlib import Path\nPath(' + repr(str(self.ring/'key.xml')) + ').write_text(\'<key changed="yes"/>\')\n')
        script.chmod(0o700)
        self.env['PATH'] = str(wrapper) + ':' + self.env['PATH']
        self.tool('backup', '--keyring', self.ring, '--staging-parent', self.p, expected=1)
        self.assertEqual(json.loads(self.run_cmd(['restic', 'snapshots', '--json'])), [])
        self.assertEqual(list(self.p.glob('coachos-capture-*')), [])

    def test_real_retention_prunes_older_snapshot_and_preserves_recent(self):
        from datetime import datetime, timedelta
        source = self.p/'retention-proof'; source.write_text('fictional')
        old = (datetime.now() - timedelta(days=20)).strftime('%Y-%m-%d %H:%M:%S')
        self.run_cmd(['restic', 'backup', '--host', 'coachos-backup', '--tag', 'coachos-db-keyring', '--time', old, source])
        source.write_text('new fictional')
        self.run_cmd(['restic', 'backup', '--host', 'coachos-backup', '--tag', 'coachos-db-keyring', source])
        self.assertEqual(len(json.loads(self.run_cmd(['restic', 'snapshots', '--json']))), 2)
        self.tool('prune')
        snapshots = json.loads(self.run_cmd(['restic', 'snapshots', '--json']))
        self.assertEqual(len(snapshots), 1)
        self.run_cmd(['restic', 'check', '--read-data'])

    def test_guard_blocks_restored_deleted_player_and_unsubscribed_lead(self):
        from datetime import datetime, timezone
        org, target, lead = [str(uuid.uuid4()) for _ in range(3)]
        self.sql('CREATE TABLE "Enrollments" ("Id" uuid, "OrganizationId" uuid);'
                 'CREATE TABLE "WebsiteWaitlistEntries" ("Id" uuid);'
                 f'INSERT INTO "Enrollments" VALUES (\'{target}\',\'{org}\');'
                 f'INSERT INTO "WebsiteWaitlistEntries" VALUES (\'{lead}\');')
        self.tool('backup', '--keyring', self.ring, '--staging-parent', self.p)
        self.sql('DELETE FROM "Enrollments"; DELETE FROM "WebsiteWaitlistEntries";')
        restored = self.p/'old-snapshot'
        self.run_cmd(['restic', 'restore', 'latest', '--target', restored])
        import tarfile
        with tarfile.open(next(restored.rglob('capture.tar'))) as archive:
            archive.extractall(restored/'capture', filter='data')
        self.run_cmd(['pg_restore', '--clean', '--no-owner', '--no-privileges', '--exit-on-error', '-d', self.env['PGDATABASE'], next(restored.rglob('database.dump'))])
        ledger = self.p/'current-ledger.json'
        ledger.write_text(json.dumps({'complete_through': datetime.now(timezone.utc).isoformat(),
            'erasures': [{'organization_id': org, 'kind': 'enrollment', 'target_id': target}],
            'unsubscribed_entry_ids': [lead]}))
        self.tool('guard', '--ledger', ledger, expected=3)
        self.assertEqual(self.sql('SELECT count(*) FROM "Enrollments";').strip(), '1')
        self.assertEqual(self.sql('SELECT count(*) FROM "WebsiteWaitlistEntries";').strip(), '1')
        self.sql('DELETE FROM "Enrollments"; DELETE FROM "WebsiteWaitlistEntries";')
        self.tool('guard', '--ledger', ledger)

if __name__ == '__main__': unittest.main()
