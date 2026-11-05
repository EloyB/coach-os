import re
from pathlib import Path
import unittest

class DatabaseAclTests(unittest.TestCase):
    def test_database_acl_allows_only_reserved_vps_ipv4(self):
        directory=Path(__file__).parents[2]/'infrastructure/terraform'
        text='\n'.join(p.read_text() for p in directory.glob('*.tf'))
        matches=re.findall(r'resource "scaleway_rdb_acl" "[^"]+"',text)
        self.assertEqual(len(matches),1,'Exactly one resource owns ALL RDB ACL entries')
        acl=(directory/'database-acl.tf').read_text()
        self.assertIn('instance_id = scaleway_rdb_instance.coach_os.id',acl)
        self.assertIn('"${scaleway_instance_ip.vps.address}/32"',acl)
        self.assertEqual(acl.count('acl_rules {'),1)
        self.assertNotIn('0.0.0.0/0',acl)
        self.assertNotIn('variable ',acl,'No required configuration added')

if __name__=='__main__': unittest.main()
