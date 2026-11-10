"""Tests voor de eenmalige websiteformulierimport.

De validatietests draaien altijd. De PostgreSQL-tests zijn opt-in: zet COACHOS_TEST_PSQL op een
psql-commando naar een lege, gemigreerde testdatabase, bv.
    COACHOS_TEST_PSQL="docker exec -i coachos-review-pg psql -U postgres -d fix289_import"
"""
import datetime as dt
import importlib.util
import json
import os
import pathlib
import shlex
import subprocess
import sys
import tempfile
import unittest

TOOL = pathlib.Path(__file__).parents[1] / 'import-website-submissions.py'
spec = importlib.util.spec_from_file_location('website_import', TOOL)
tool = importlib.util.module_from_spec(spec)
spec.loader.exec_module(tool)

NOW = dt.datetime(2026, 11, 10, 12, 0, tzinfo=dt.timezone.utc)
PSQL = os.environ.get('COACHOS_TEST_PSQL')


def contact(**overrides):
    return {'name': 'Synthetic Person', 'email': 'Contact@Example.test', 'message': 'Synthetic message text',
            'submittedAt': '2026-11-01T12:00:00Z', **overrides}


class SelectRecordsTests(unittest.TestCase):
    def test_waitlist_without_hash_gets_unique_random_hash(self):
        records = [{'email': f'a{i}@example.test', 'submittedAt': '2026-11-01T12:00:00Z'} for i in range(3)]
        hashes = {r['token_hash'] for r in tool.select_records('waitlist', records, NOW)}
        self.assertEqual(len(hashes), 3)
        self.assertTrue(all(tool.HASH.fullmatch(h) for h in hashes))

    def test_waitlist_keeps_existing_hash_normalizes_email_and_dedupes(self):
        records = [
            {'email': ' Owner@Example.TEST ', 'submittedAt': '2026-11-01T12:00:00Z', 'role': 'club', 'unsubscribeTokenHash': 'a' * 64},
            {'email': 'owner@example.test', 'submittedAt': '2026-11-02T12:00:00Z'},
        ]
        selected = tool.select_records('waitlist', records, NOW)
        self.assertEqual(len(selected), 1)
        self.assertEqual(selected[0]['email'], 'owner@example.test')
        self.assertEqual(selected[0]['token_hash'], 'a' * 64)

    def test_skips_unsubscribed_and_expired_records(self):
        records = [contact(), contact(email='old@example.test', submittedAt='2020-01-01T00:00:00Z'),
                   contact(email='gone@example.test', deletedAt='2026-11-02T00:00:00Z')]
        self.assertEqual([r['email'] for r in tool.select_records('contact', records, NOW)], ['contact@example.test'])

    def test_waitlist_after_deadline_imports_nothing(self):
        late = dt.datetime(2027, 5, 15, tzinfo=dt.timezone.utc)
        records = [{'email': 'a@example.test', 'submittedAt': '2026-11-01T12:00:00Z'}]
        self.assertEqual(tool.select_records('waitlist', records, late), [])

    def test_invalid_records_reject_whole_import_without_pii(self):
        for bad in [contact(email='bad'), contact(message='kort'), contact(submittedAt='2099-01-01T00:00:00Z'),
                    contact(submittedAt='2026-11-01T12:00:00'), contact(submittedAt='geen datum'), 'x']:
            with self.assertRaises(tool.SourceError) as caught:
                tool.select_records('contact', [contact(), bad], NOW)
            self.assertNotIn('Synthetic', str(caught.exception))
        with self.assertRaises(tool.SourceError):
            tool.select_records('waitlist', [{'email': 'a@example.test', 'submittedAt': '2026-11-01T12:00:00Z', 'role': 'https://x'}], NOW)

    def test_sql_never_contains_source_text_and_dry_run_rolls_back(self):
        selected = tool.select_records('contact', [contact(message="Robert'); DROP TABLE x; --")], NOW)
        sql = tool.build_sql('contact', selected, apply=False)
        self.assertNotIn('DROP TABLE', sql)
        self.assertTrue(sql.rstrip().endswith('ROLLBACK;'))
        self.assertTrue(tool.build_sql('contact', selected, apply=True).rstrip().endswith('COMMIT;'))


@unittest.skipUnless(PSQL, 'COACHOS_TEST_PSQL niet gezet: PostgreSQL-importtests overgeslagen')
class PostgresImportTests(unittest.TestCase):
    def setUp(self):
        sql('DELETE FROM "WebsiteWaitlistMails"; DELETE FROM "WebsiteWaitlistEntries"; DELETE FROM "WebsiteContactEntries";')

    def run_tool(self, kind, records, apply=False):
        with tempfile.TemporaryDirectory() as tmp:
            source = pathlib.Path(tmp) / 'source.json'
            source.write_text(json.dumps(records), encoding='utf-8')
            args = [sys.executable, str(TOOL), '--kind', kind, '--input', str(source), '--psql', PSQL]
            proc = subprocess.run(args + (['--apply'] if apply else []), capture_output=True, text=True)
            self.assertEqual(proc.returncode, 0, proc.stderr)
            return json.loads(proc.stdout)

    def test_waitlist_dry_run_then_apply_is_idempotent_and_queues_no_mail(self):
        records = [{'email': f'legacy{i}@example.test', 'submittedAt': '2026-11-01T12:00:00Z'} for i in range(3)]
        records.append({'email': 'kept@example.test', 'submittedAt': '2026-11-01T12:00:00Z', 'unsubscribeTokenHash': 'b' * 64})
        dry = self.run_tool('waitlist', records)
        self.assertEqual((dry['eligible'], dry['inserted']), (4, 4))
        self.assertEqual(sql('SELECT count(*) FROM "WebsiteWaitlistEntries"'), '0')
        self.assertEqual(self.run_tool('waitlist', records, apply=True)['inserted'], 4)
        self.assertEqual(self.run_tool('waitlist', records, apply=True)['inserted'], 0)
        self.assertEqual(sql('SELECT count(DISTINCT "UnsubscribeTokenHash") FROM "WebsiteWaitlistEntries"'), '4')
        self.assertEqual(sql('SELECT "UnsubscribeTokenHash" FROM "WebsiteWaitlistEntries" WHERE "Email"=\'kept@example.test\''), 'b' * 64)
        self.assertEqual(sql('SELECT count(*) FROM "WebsiteWaitlistMails"'), '0')

    def test_contact_import_keeps_text_and_receipt_date_and_skips_existing(self):
        record = contact(name="O'Brien \\ synthetic", message="Synthetic '; DROP TABLE anything; --")
        self.assertEqual(self.run_tool('contact', [record], apply=True)['inserted'], 1)
        self.assertEqual(self.run_tool('contact', [record], apply=True)['inserted'], 0)
        self.assertEqual(sql('SELECT "Name" FROM "WebsiteContactEntries"'), record['name'])
        self.assertEqual(sql('SELECT "CreatedAt" AT TIME ZONE \'UTC\' FROM "WebsiteContactEntries"'), '2026-11-01 12:00:00')


def sql(query):
    proc = subprocess.run(shlex.split(PSQL) + ['-X', '-At', '-v', 'ON_ERROR_STOP=1'],
                          input=query, capture_output=True, text=True, check=True)
    return proc.stdout.strip()


if __name__ == '__main__':
    unittest.main()
