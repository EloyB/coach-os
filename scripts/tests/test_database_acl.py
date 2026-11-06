"""Regression for Lorenz's explicit decision: preserve direct dynamic-network DB access."""
from pathlib import Path
import re
import subprocess
import unittest

ROOT = Path(__file__).resolve().parents[2]

class DatabaseAccessDecisionTests(unittest.TestCase):
    def test_rejected_vps_only_acl_is_not_in_proposal(self):
        directory = ROOT/'infrastructure/terraform'
        source = '\n'.join(p.read_text() for p in directory.glob('*.tf'))
        self.assertNotRegex(source, r'resource\s+"scaleway_rdb_acl"', 'Rejected authoritative ACL must not be deployed')

    def test_database_network_and_runner_workflows_remain_identical_to_main(self):
        for directory, glob in [('infrastructure/terraform', '*.tf'), ('.github/workflows', '*.yml')]:
            for path in (ROOT/directory).glob(glob):
                relative = str(path.relative_to(ROOT))
                original = subprocess.run(['git', 'show', 'origin/main:' + relative], cwd=ROOT, capture_output=True)
                self.assertEqual(original.returncode, 0, relative + ' must not introduce unapproved network resources')
                if directory == 'infrastructure/terraform' and path.name == 'main.tf':
                    # #265 independently owns backup scheduling, not database access.
                    # Compare all other statements, allowing formatter alignment only.
                    def access_source(text):
                        return [re.sub(r'\s*=\s*', '=', line.strip()) for line in text.splitlines()
                                if not line.strip().startswith(('backup_schedule_frequency', 'backup_schedule_retention'))]
                    self.assertEqual(access_source(path.read_text()), access_source(original.stdout.decode()), relative + ' changes access beyond independent backup policy')
                else:
                    self.assertEqual(path.read_bytes(), original.stdout, relative + ' must not alter current access or migration routes')

if __name__ == '__main__': unittest.main()
