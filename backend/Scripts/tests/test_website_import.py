"""Synthetic real-PostgreSQL import integration. Requires loopback fixture and migrated DB."""
import hashlib, json, os, pathlib, subprocess, tempfile, unittest

TOOL = pathlib.Path(__file__).parents[1] / 'import-website-submissions.py'


class ImportTests(unittest.TestCase):
    def test_existing_target_capability_conflict_fails_closed(self):
        with tempfile.TemporaryDirectory() as tmp:
            source = pathlib.Path(tmp) / 'source.json'
            suppress = pathlib.Path(tmp) / 'suppress.json'
            source.write_text(json.dumps([{'email':'conflict@example.test','submittedAt':'2026-11-01T12:00:00Z','unsubscribeTokenHash':'a'*64}]))
            suppress.write_text('[]')
            sql('INSERT INTO "WebsiteWaitlistEntries" ("Id","CreatedAt","UpdatedAt","Email","UnsubscribeTokenHash") VALUES (gen_random_uuid(),now(),now(),\'conflict@example.test\',\''+'b'*64+'\')')
            result = subprocess.run(['python3',str(TOOL),'--kind','waitlist','--input',str(source),'--suppressions',str(suppress)],capture_output=True,text=True)
            self.assertNotEqual(result.returncode,0,'A conflicting live capability must require explicit reconciliation, never silently accept an obsolete source')
            self.assertEqual(sql('SELECT "UnsubscribeTokenHash" FROM "WebsiteWaitlistEntries" WHERE "Email"=\'conflict@example.test\''),'b'*64)
            sql('DELETE FROM "WebsiteWaitlistEntries" WHERE "Email"=\'conflict@example.test\'')

    def test_contact_import_preserves_receipt_and_drops_expired_data_atomically(self):
        with tempfile.TemporaryDirectory() as tmp:
            source = pathlib.Path(tmp) / 'contact.json'
            suppress = pathlib.Path(tmp) / 'suppress.json'
            suppress.write_text('[]')
            record = {'name': "O'Brien \\ synthetic", 'email': 'import-contact@example.test', 'message': "Synthetic '; DROP TABLE anything; --", 'submittedAt': '2026-11-01T12:00:00Z'}
            source.write_text(json.dumps([record, {**record, 'email': 'expired@example.test', 'submittedAt': '2020-01-01T12:00:00Z'}]))
            args = ['python3', str(TOOL), '--kind', 'contact', '--input', str(source), '--suppressions', str(suppress)]
            dry = json.loads(subprocess.run(args, capture_output=True, text=True, check=True).stdout)
            self.assertEqual(dry['eligible'], 1)
            self.assertEqual(dry['skipped'], 1)
            wrong = subprocess.run(args + ['--apply', '--confirm-sha256', 'wrong'], capture_output=True, text=True)
            self.assertNotEqual(wrong.returncode, 0)
            result = json.loads(subprocess.run(args + ['--apply', '--confirm-sha256', dry['sha256']], capture_output=True, text=True, check=True).stdout)
            self.assertEqual(result['inserted'], 1)
            self.assertEqual(sql('SELECT "Name" FROM "WebsiteContactEntries" WHERE "Email"=\'import-contact@example.test\''), record['name'])
            self.assertEqual(sql('SELECT "CreatedAt"::date FROM "WebsiteContactEntries" WHERE "Email"=\'import-contact@example.test\''), '2026-11-01')
            self.assertEqual(sql('SELECT count(*) FROM "WebsiteContactEntries" WHERE "Email"=\'expired@example.test\''), '0')
            source.write_text(json.dumps([record, {**record, 'email': 'bad'}]))
            failed = subprocess.run(args, capture_output=True, text=True)
            self.assertNotEqual(failed.returncode, 0)
            self.assertNotIn(record['message'], failed.stderr)

    def test_dry_run_apply_reconcile_and_deleted_rows_never_resurrect(self):
        with tempfile.TemporaryDirectory() as tmp:
            source = pathlib.Path(tmp) / 'source.json'
            suppress = pathlib.Path(tmp) / 'suppress.json'
            source.write_text(json.dumps([
                {'email': 'legacy@example.test', 'submittedAt': '2026-11-01T12:00:00Z', 'role': 'club', 'unsubscribeTokenHash': 'a' * 64},
                {'email': 'removed@example.test', 'submittedAt': '2026-11-01T12:00:00Z', 'role': 'coach', 'unsubscribeTokenHash': 'b' * 64},
            ]))
            suppress.write_text(json.dumps([hashlib.sha256(b'removed@example.test').hexdigest()]))
            args = ['python3', str(TOOL), '--kind', 'waitlist', '--input', str(source), '--suppressions', str(suppress)]
            def call(extra=None):
                p = subprocess.run(args + (extra or []), capture_output=True, text=True, check=True)
                return json.loads(p.stdout)
            dry = call()
            self.assertEqual(dry['eligible'], 1)
            self.assertEqual(dry['inserted'], 0)
            self.assertEqual(sql('SELECT count(*) FROM "WebsiteWaitlistEntries"'), '0')
            apply = ['--apply', '--confirm-sha256', dry['sha256']]
            from concurrent.futures import ThreadPoolExecutor
            with ThreadPoolExecutor(max_workers=4) as pool:
                results = list(pool.map(lambda _: call(apply), range(4)))
            self.assertEqual(sum(result['inserted'] for result in results), 1)
            self.assertEqual(sql('SELECT "UnsubscribeTokenHash" FROM "WebsiteWaitlistEntries"'), 'a' * 64)
            self.assertEqual(sql('SELECT count(*) FROM "WebsiteWaitlistMails"'), '0')
            self.assertEqual(call(apply)['inserted'], 0)
            sql('DELETE FROM "WebsiteWaitlistEntries"')
            self.assertEqual(call(apply)['inserted'], 0)
            self.assertEqual(sql('SELECT count(*) FROM "WebsiteWaitlistEntries"'), '0')


def sql(query):
    return subprocess.run(['psql', '-X', '-At', '-v', 'ON_ERROR_STOP=1'], input=query, capture_output=True, text=True, check=True).stdout.strip()


if __name__ == '__main__':
    unittest.main()
