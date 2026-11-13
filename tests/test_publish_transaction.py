import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools" / "xdat"))

from auto_pipeline import publish_xdat_transactionally


class TransactionalXdatPublishTests(unittest.TestCase):
    def test_success_replaces_archive_and_keeps_backup(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            archive = root / "Compilados" / "XDAT" / "Archivo"
            archive.mkdir(parents=True)
            (archive / "old.xdat").write_bytes(b"old-package")
            nested = archive / "nested"
            nested.mkdir()
            (nested / "old.xml").write_bytes(b"old-source")
            output = archive / "interface.xdat"
            result = publish_xdat_transactionally(b"new-package", output, archive, root / "Backups")
            self.assertEqual(result.read_bytes(), b"new-package")
            self.assertEqual(sorted(p.name for p in archive.iterdir()), ["interface.xdat"])
            backups = list((root / "Backups").rglob("*"))
            self.assertTrue(any(p.is_file() and p.read_bytes() == b"old-package" for p in backups))
            self.assertTrue(any(p.is_file() and p.read_bytes() == b"old-source" for p in backups))

    def test_failed_publish_restores_previous_archive(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            archive = root / "Compilados" / "XDAT" / "Archivo"
            archive.mkdir(parents=True)
            original = archive / "old.xdat"
            original.write_bytes(b"previous-package")
            output = archive / "new.xdat"
            original_replace = Path.replace

            def fail_staged_replace(path, target):
                if path.name.endswith(".staged"):
                    raise OSError("simulated atomic publish failure")
                return original_replace(path, target)

            with patch.object(Path, "replace", fail_staged_replace):
                with self.assertRaisesRegex(OSError, "simulated atomic publish failure"):
                    publish_xdat_transactionally(b"new-package", output, archive, root / "Backups")
            self.assertTrue(original.is_file())
            self.assertEqual(original.read_bytes(), b"previous-package")
            self.assertFalse(output.exists())


if __name__ == "__main__":
    unittest.main()
