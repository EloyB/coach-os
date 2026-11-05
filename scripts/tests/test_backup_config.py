import re
from pathlib import Path
import unittest

class BackupConfigTests(unittest.TestCase):
    def test_existing_database_has_explicit_daily_14_day_backups(self):
        source=(Path(__file__).parents[2]/'infrastructure/terraform/main.tf').read_text()
        block=re.search(r'resource "scaleway_rdb_instance" "coach_os" \{(.*?)\n\}',source,re.S).group(1)
        self.assertRegex(block,r'disable_backup\s*=\s*false')
        self.assertRegex(block,r'backup_schedule_frequency\s*=\s*24\b')
        self.assertRegex(block,r'backup_schedule_retention\s*=\s*14\b')

    def test_legal_backup_proposal_is_not_presented_as_live_guarantee(self):
        text=(Path(__file__).parents[2]/'docs/legal/verwerkersovereenkomst.md').read_text()
        self.assertIn('Voorstel #265: dagelijks (elke 24 uur), 14 dagen bewaren',text)
        self.assertIn('nog niet operationeel geverifieerd',text)
        self.assertIn('[INVULLEN:',text)

if __name__=='__main__': unittest.main()
