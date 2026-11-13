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
        self.assertEqual(result,payload)
        self.assertIsNone(detection.encrypted)

    def test_invalid_plain_candidate_shows_technical_diagnostics(self):
        payload=bytes.fromhex("FF ED FA CE 01 00 FE 0F C3 9D 01 09 E1 BB 1A DF")
        with self.assertRaises(AutoPipelineError) as caught:
            decode_xdat(Path("interface.xdat"),payload,lambda data:(_ for _ in ()).throw(ValueError("bad xdat")))
        message=str(caught.exception)
        self.assertTrue(message.startswith("No se reconoce este cifrado."))
        self.assertIn("INFORMACIÓN TÉCNICA",message)
        self.assertIn("FF ED FA CE",message)
        self.assertIn("interface.xdat",message)

    def test_suspicious_user_signature_is_not_sent_to_plain_parser(self):
        payload = bytes.fromhex("FF ED FA CE 01 00 FE 0F C3 9D 01 09 E1 BB 1A DF") + bytes(100)
        def should_not_parse(data):
            raise AssertionError("A suspicious encrypted candidate must not be parsed as plaintext")
        with self.assertRaisesRegex(AutoPipelineError, "Posible cifrado u ofuscación personalizada"):
            decode_xdat(Path("interface.xdat"), payload, should_not_parse)

    def test_lamecrypt_is_detected_but_not_claimed_supported(self):
        header="Lineage2Ver811".encode("utf-16le")
        with self.assertRaisesRegex(AutoPipelineError,"Cifrado reconocido, pero esta variante"):

            decode_xdat(Path("interface.xdat"),header+b"0"*100,lambda data:object())

if __name__=="__main__":
    unittest.main()
