import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('restic_backup', Path(__file__).resolve().parents[1]/'restic_backup.py')
assert spec is not None and spec.loader is not None
backup = importlib.util.module_from_spec(spec); spec.loader.exec_module(backup)

class CaptureBoundsTests(unittest.TestCase):
    def test_file_count_limit_is_enforced_before_loading_extra_key(self):
        with tempfile.TemporaryDirectory() as tmp:
            ring = Path(tmp)
            for i in range(513): (ring/f'{i}.xml').write_text('<key/>')
            real = Path.open; calls = []
            def read(path, *args, **kwargs):
                calls.append(path)
                return real(path, *args, **kwargs)
            with patch.object(Path, 'open', read):
                with self.assertRaises(ValueError): backup.ring_files(ring)
            self.assertLessEqual(len(calls), 512, 'oversized ring must not be read unbounded before rejection')

if __name__ == '__main__': unittest.main()
