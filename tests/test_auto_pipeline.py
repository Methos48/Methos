import unittest
from pathlib import Path
from unittest.mock import patch
from auto_pipeline import ENGINE_PROTOCOLS, AutoPipelineError, decode_xdat
class AutoPipelineTests(unittest.TestCase):
    def test_supported_engine_protocols_are_explicit(self):
        self.assertEqual(ENGINE_PROTOCOLS,{111,120,121,211,212,411,412,413,414})
    def test_plain_candidate_is_validated_and_returned_unchanged(self):
        payload=b"plain-xdat-fixture"
        result,detection=decode_xdat(Path("interface.xdat"),payload,lambda data:object())
        self.assertEqual(result,payload); self.assertIsNone(detection.encrypted)
    def test_invalid_plain_candidate_is_rejected(self):
        with self.assertRaises(AutoPipelineError):
            decode_xdat(Path("unknown.xdat"),b"not-valid",lambda data:(_ for _ in ()).throw(ValueError()))
    def test_lamecrypt_is_detected_but_not_claimed_supported(self):
        header="Lineage2Ver811".encode("utf-16le")
        with patch("auto_pipeline.engine_path",return_value=Path("missing.exe")):
            with self.assertRaisesRegex(AutoPipelineError,"todavía no implementa"):
                decode_xdat(Path("interface.xdat"),header+b"0"*100,lambda data:object())
if __name__=="__main__": unittest.main()
