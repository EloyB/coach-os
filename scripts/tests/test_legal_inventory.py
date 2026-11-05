import hashlib
import json
from pathlib import Path
import re
import unittest

ROOT=Path(__file__).parents[2]
PATTERN=re.compile(r'\[(TE BOUWEN|INVULLEN|TE DOEN)[^\]]*\]')

class LegalInventoryTests(unittest.TestCase):
    def test_each_legal_marker_occurrence_is_classified_without_compliance_claim(self):
        path=ROOT/'docs/legal-readiness-inventory.json'
        self.assertTrue(path.exists(),'Missing complete occurrence inventory')
        data=json.loads(path.read_text())
        found=[]
        hashes={}
        for source in sorted((ROOT/'docs/legal').glob('*.md')):
            text=source.read_text();relative=str(source.relative_to(ROOT))
            hashes[relative]=hashlib.sha256(source.read_bytes()).hexdigest()
            for index,match in enumerate(PATTERN.finditer(text),1):
                found.append((relative,index,text.count('\n',0,match.start())+1,match.group(0)))
        recorded=[(row['source'],row['occurrence'],row['line'],row['marker']) for row in data['occurrences']]
        self.assertEqual(recorded,found,'Every occurrence, including explanatory legends, must appear exactly once')
        self.assertEqual(data['source_sha256'],hashes)
        for row in data['occurrences']:
            self.assertIn(row['classification'],['gebouwd','tekstcorrectie','onbeslist'])
            self.assertTrue(row['assessment'])
            self.assertFalse(row['merged'])
            self.assertEqual(row['production_verification'],'niet uitgevoerd')
        self.assertFalse(data['publication_authorized'])
        self.assertTrue(data['concept_warnings_preserved'])
        report=(ROOT/'docs/legal-readiness-inventory.md').read_text()
        for number in [287,288,289,290,291,292,293]:
            self.assertIn(f'/pull/{number}',report)
        self.assertIn('geen complianceverklaring',report)

if __name__=='__main__': unittest.main()
