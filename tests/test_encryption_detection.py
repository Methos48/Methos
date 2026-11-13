from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
sys.path.insert(0, str(ROOT / "tools" / "xdat"))
from encryption_detection import detect_encryption, supported_versions, PROTOCOLS


def make_header(version: int) -> bytes:
    return f"Lineage2Ver{version:03d}".encode("utf-16le")


class EncryptionDetectionTests(unittest.TestCase):
    def test_every_documented_header_version_is_recognized(self):
        expected = (
            111, 120, 121, 211, 212, 411, 412, 413, 414,
            611, 612, 613, 614, 811, 820, 821, 911, 912,
        )
        self.assertEqual(supported_versions(), expected)
        for version in expected:
            with self.subTest(version=version):
                result = detect_encryption(make_header(version) + b"payload", "interface.xdat")
                self.assertEqual(result.status, "recognized_encryption")
                self.assertTrue(result.encrypted)
                self.assertEqual(result.version, version)
                self.assertEqual(result.family, PROTOCOLS[version][0])
                self.assertEqual(result.decoder_available, version in {111, 120, 121, 211, 212, 411, 412, 413, 414})

    def test_filename_dependent_variants_are_flagged(self):
        for version in (121, 821):
            with self.subTest(version=version):
                result = detect_encryption(make_header(version))
                self.assertTrue(result.filename_required)
                self.assertIn("nombre original", result.detail.lower())

    def test_unencrypted_candidate_is_not_claimed_as_valid_xdat(self):
        result = detect_encryption(bytes.fromhex("01 00 00 00") + bytes(32))
        self.assertEqual(result.status, "plain_candidate")
        self.assertFalse(result.encrypted)

    def test_unknown_numeric_version_is_reported_as_encrypted_unsupported(self):
        result = detect_encryption(make_header(999) + b"payload")
        self.assertEqual(result.status, "unsupported_version")
        self.assertTrue(result.encrypted)
        self.assertEqual(result.version, 999)

    def test_malformed_header_is_not_misclassified(self):
        result = detect_encryption("Lineage2VerABC".encode("utf-16le") + bytes(4))
        self.assertEqual(result.status, "malformed_header")
        self.assertIsNone(result.encrypted)

    def test_short_input_is_safe(self):
        result = detect_encryption(bytes.fromhex("FF ED FA CE"))
        self.assertEqual(result.status, "unknown_or_truncated")

    def test_user_file_signature_is_reported_as_suspected_not_confirmed_cipher(self):
        sample_prefix = bytes.fromhex("FF ED FA CE 01 00 FE 0F C3 9D 01 09 E1 BB 1A DF")
        result = detect_encryption(sample_prefix + bytes(100))
        self.assertEqual(result.status, "suspected_encryption")
        self.assertIsNone(result.encrypted)
        self.assertIsNone(result.version)
        self.assertEqual(result.family, "UNKNOWN")
        self.assertIn("0xCEFAEDFF", result.detail)
        self.assertIn("corrupción", result.detail)


if __name__ == "__main__":
    unittest.main()
