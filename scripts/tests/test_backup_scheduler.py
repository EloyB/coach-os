from pathlib import Path
import unittest
ROOT = Path(__file__).resolve().parents[2]

class SchedulerProposalTests(unittest.TestCase):
    def test_optin_daily_proposal_does_not_stop_api_and_requires_private_operator_config(self):
        service = ROOT/'infrastructure/backup/coachos-backup.service.example'
        timer = ROOT/'infrastructure/backup/coachos-backup.timer.example'
        self.assertTrue(service.exists(), 'reviewable scheduler proposal missing')
        text = service.read_text()
        self.assertIn('EnvironmentFile=/etc/coachos-backup/environment', text)
        self.assertIn('restic_backup.py backup', text)
        self.assertIn('restic_backup.py prune', text)
        self.assertIn('UMask=0077', text)
        self.assertNotIn('docker', text)
        self.assertNotIn(' stop ', text)
        self.assertIn('OnCalendar=daily', timer.read_text())

if __name__ == '__main__': unittest.main()
