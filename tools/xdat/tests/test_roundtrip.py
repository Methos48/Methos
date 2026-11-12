from pathlib import Path
import os
import sys
import tempfile
import unittest

MODULE_ROOT = Path(__file__).resolve().parents[1]
REFERENCE_ROOT = Path(os.environ.get("XDAT_REFERENCE_ROOT", MODULE_ROOT))
sys.path.insert(0, str(MODULE_ROOT))

from xdat_core import compile_from_folder, decompile_to_folder, parse_xdat


class XdatReferenceRoundTripTest(unittest.TestCase):
    def setUp(self):
        self.original_path = REFERENCE_ROOT / "xdat completo" / "interface.xdat"
        self.parts_path = REFERENCE_ROOT / "xdat por partes"
        self.original = self.original_path.read_bytes()

    def test_reference_package_structure(self):
        parsed = parse_xdat(self.original)
        self.assertEqual(len(parsed["windows"]), 168)
        self.assertEqual(len(parsed["shortcuts"]), 8)

    def test_existing_parts_recompile_identically(self):
        compiled, windows, shortcuts = compile_from_folder(self.parts_path, self.original)
        self.assertEqual((windows, shortcuts), (168, 8))
        self.assertEqual(compiled, self.original)

    def test_decompile_then_compile_is_identical(self):
        with tempfile.TemporaryDirectory() as temp:
            parts = Path(temp)
            windows, shortcuts = decompile_to_folder(self.original, parts)
            compiled, out_windows, out_shortcuts = compile_from_folder(parts, self.original)
            self.assertEqual((windows, shortcuts), (168, 8))
            self.assertEqual((out_windows, out_shortcuts), (168, 8))
            self.assertEqual(compiled, self.original)


if __name__ == "__main__":
    unittest.main()
