from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET

from xdat_core import compile_from_folder, decompile_to_folder, parse_xdat


class EditedXmlRoundTripTest(unittest.TestCase):
    def test_modified_window_xml_is_used_by_compiler(self):
        root = Path(__file__).resolve().parents[1]
        original_path = root / "xdat completo" / "interface.xdat"
        original = original_path.read_bytes()
        with tempfile.TemporaryDirectory() as temp:
            parts = Path(temp) / "Decopilado"
            decompile_to_folder(original, parts)
            xml_path = parts / "windows" / "InterfaceOptionsWnd.xml"
            tree = ET.parse(xml_path)
            width = tree.getroot().find("size__absolute__width")
            self.assertIsNotNone(width)
            old_value = width.text
            width.text = str(int(old_value) + 1)
            tree.write(xml_path, encoding="utf-8", xml_declaration=True)

            compiled, windows, shortcuts = compile_from_folder(parts, original)
            parsed = parse_xdat(compiled)
            edited = next(item for item in parsed["windows"] if item.get("name") == "InterfaceOptionsWnd")
            self.assertEqual(edited["size__absolute__width"], int(old_value) + 1)
            self.assertEqual((windows, shortcuts), (168, 8))


if __name__ == "__main__":
    unittest.main()
