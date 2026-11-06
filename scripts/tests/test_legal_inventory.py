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
        if hashes == data['source_sha256']:
            records=data['occurrences']
        else:
            self.assertEqual(hashes,data.get('integration_source_sha256'), 'Unexpected source outside independently audited branch/integration snapshots')
            records=data['integration_occurrences']
        recorded=[(row['source'],row['occurrence'],row['line'],row['marker']) for row in records]
        self.assertEqual(recorded,found,'Every occurrence, including explanatory legends, must appear exactly once')
        for row in records:
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

    def test_decisions_and_delivery_snapshot_are_current(self):
        data=json.loads((ROOT/'docs/legal-readiness-inventory.json').read_text())
        self.assertEqual([d['number'] for d in data.get('policy_decisions',[])],list(range(1,19)))
        heads={p['number']:p['sha'] for p in data['pull_request_snapshot']}
        self.assertTrue(heads[289].startswith('3291c856'))
        self.assertTrue(heads[290].startswith('2b54a29'))
        self.assertTrue(heads[291].startswith('9a35aa7'))
        self.assertTrue(heads[292].startswith('d04ebb8'))
        self.assertTrue(heads[293].startswith('2e5a5cc'))
        report=(ROOT/'docs/legal-readiness-inventory.md').read_text()
        self.assertNotIn('uitsluitend opt-in beperkte read-only preview',report)
        self.assertIn('974',report)
        self.assertIn('17 bestaande lintfouten',report)

    def test_concept_mailbox_incident_and_access_policies_match_approved_choices(self):
        privacy=(ROOT/'docs/legal/privacyverklaring.md').read_text()
        self.assertIn('twaalf maanden na afhandeling',privacy)
        self.assertIn('Billit als primair archief',privacy)
        self.assertNotIn('[wordt geschrapt na issue #260]',privacy)
        dpa=(ROOT/'docs/legal/verwerkersovereenkomst.md').read_text()
        self.assertIn('binnen dezelfde club',dpa)
        self.assertIn('werkelijke einde',dpa)
        self.assertIn('info@coach-os.be',dpa)
        incident=ROOT/'docs/privacy-incident-procedure.md'
        self.assertTrue(incident.exists(),'usable incident concept required')
        text=incident.read_text()
        for term in ['CONCEPT','info@coach-os.be','72 uur','zonder onredelijke vertraging','48 uur','33(5)','https://www.edpb.europa.eu/sme/assess-the-risks/data-breaches_en']:
            self.assertIn(term,text)
        self.assertIn('geen externe melding',text)

if __name__=='__main__': unittest.main()
