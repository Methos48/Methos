import json
import sys
import tempfile
import unittest
from pathlib import Path
from types import SimpleNamespace

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools" / "xdat"))

from package_origin_metadata import (
    build_origin_record,
    save_origin_record,
    load_and_validate_origin,
    require_recorded_protocol,
)


class OriginMetadataTests(unittest.TestCase):
    def test_records_protocol_and_hash_without_secrets(self):
        raw = b"sample encrypted package"
        detection = SimpleNamespace(
            status="recognized_encryption",
            version=121,
            family="XOR",
            header="Lineage2Ver121",
            filename_required=True,
            detail="filename required",
        )
        record = build_origin_record(Path("interface.xdat"), raw, detection)
        self.assertEqual(record["encoding"]["protocol_version"], 121)
        self.assertTrue(record["encoding"]["original_filename_required"])
        self.assertEqual(record["source"]["size_bytes"], len(raw))
        self.assertFalse(record["security"]["secrets_saved"])
        self.assertNotIn("key", json.dumps(record).lower())

    def test_saved_manifest_hash_validation(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            source = root / "interface.xdat"
            source.write_bytes(b"original")
            detection = SimpleNamespace(
                status="plain_candidate",
                version=None,
                family=None,
                header=None,
                filename_required=False,
                detail="plain",
            )
            manifest = save_origin_record(root / "project", source, source.read_bytes(), detection)
            loaded = load_and_validate_origin(manifest, source)
            self.assertEqual(loaded["source"]["file_name"], source.name)
            source.write_bytes(b"changed")
            with self.assertRaisesRegex(ValueError, "SHA-256"):
                load_and_validate_origin(manifest, source)

    def test_protocol_mismatch_is_rejected(self):
        record = {"encoding": {"protocol_version": 121}}
        with self.assertRaisesRegex(ValueError, "no se permite"):
            require_recorded_protocol(record, 111)


if __name__ == "__main__":
    unittest.main()
