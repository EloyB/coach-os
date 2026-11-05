"""Behavior checks use only disposable fictional key files; no cloud or Docker."""
import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPT = Path(__file__).parents[1] / 'keyring_snapshot.py'

class SnapshotTests(unittest.TestCase):
    def test_roundtrip_preserves_all_key_bytes_and_keeps_source_unchanged(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory); source=root/'source'; source.mkdir()
            (source/'key-old.xml').write_text('<fictional-old-key/>')
            (source/'key-current.xml').write_text('<fictional-current-key/>')
            archive=root/'backup.zip'; restored=root/'restored'
            result=subprocess.run([sys.executable,str(SCRIPT),'backup',str(source),str(archive),'--quiesced'],capture_output=True,text=True)
            self.assertEqual(result.returncode,0,result.stderr)
            result=subprocess.run([sys.executable,str(SCRIPT),'restore',str(archive),str(restored)],capture_output=True,text=True)
            self.assertEqual(result.returncode,0,result.stderr)
            self.assertEqual({p.name:p.read_bytes() for p in source.iterdir()}, {p.name:p.read_bytes() for p in restored.iterdir()})
            self.assertEqual(archive.stat().st_mode & 0o777,0o600)
            self.assertEqual(restored.stat().st_mode & 0o777,0o700)

    def test_restore_rejects_untrusted_paths_and_corruption_without_partial_target(self):
        import zipfile
        for name in ['../escaped.xml', '/absolute.xml', 'nested/key.xml', 'key.xml']:
            with self.subTest(name=name), tempfile.TemporaryDirectory() as directory:
                root=Path(directory); archive=root/'backup.zip'; target=root/'restored'
                if name == 'key.xml': archive.write_bytes(b'corrupt-not-a-zip')
                else:
                    with zipfile.ZipFile(archive,'w') as snapshot:
                        snapshot.writestr('valid.xml','fictional')
                        snapshot.writestr(name,'fictional')
                result=subprocess.run([sys.executable,str(SCRIPT),'restore',str(archive),str(target)],capture_output=True)
                self.assertNotEqual(result.returncode,0)
                self.assertFalse(target.exists(), 'Failed restore must not leave partial keyring')
                self.assertFalse((root/'escaped.xml').exists())

    def test_backup_rejects_symlink_and_preserves_existing_archive_on_failure(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory); source=root/'source'; source.mkdir()
            outside=root/'outside.xml'; outside.write_text('fictional-outside-key')
            (source/'key.xml').symlink_to(outside)
            archive=root/'backup.zip'
            result=subprocess.run([sys.executable,str(SCRIPT),'backup',str(source),str(archive),'--quiesced'],capture_output=True)
            self.assertNotEqual(result.returncode,0)
            self.assertFalse(archive.exists())
            archive.write_bytes(b'previous-good-backup')
            result=subprocess.run([sys.executable,str(SCRIPT),'backup',str(source),str(archive),'--quiesced'],capture_output=True)
            self.assertNotEqual(result.returncode,0)
            self.assertEqual(archive.read_bytes(),b'previous-good-backup')

    def test_missing_quiescence_and_existing_restore_target_are_refused(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory); source=root/'source'; source.mkdir(); (source/'key.xml').write_text('fictional')
            archive=root/'backup.zip'
            result=subprocess.run([sys.executable,str(SCRIPT),'backup',str(source),str(archive)],capture_output=True)
            self.assertNotEqual(result.returncode,0); self.assertFalse(archive.exists())
            result=subprocess.run([sys.executable,str(SCRIPT),'backup',str(source),str(archive),'--quiesced'],capture_output=True)
            self.assertEqual(result.returncode,0)
            result=subprocess.run([sys.executable,str(SCRIPT),'restore',str(archive),str(source)],capture_output=True)
            self.assertNotEqual(result.returncode,0); self.assertEqual((source/'key.xml').read_text(),'fictional')

if __name__=='__main__': unittest.main()
