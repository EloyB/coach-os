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
                self.assertEqual(path.read_bytes(), original.stdout, relative + ' must not alter current access or migration routes')

if __name__ == '__main__': unittest.main()
