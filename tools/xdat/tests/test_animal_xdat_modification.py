from __future__ import annotations

import hashlib
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools" / "xdat"))
from xdat_core import compile_from_folder, decompile_to_folder, parse_xdat

SOURCE = ROOT / "reference-data" / "packages" / "animal" / "interface.xdat"
original = SOURCE.read_bytes()
original_hash = hashlib.sha256(original).hexdigest()

def find_textbox(node):
    if node.tag == "ct0.TextBox":
        text = node.find("text")
        if text is not None and (text.text or "").strip():
            return text
    for child in list(node):
        found = find_textbox(child)
        if found is not None:
            return found
    return None

def walk_objects(value):
    if isinstance(value, dict):
        yield value
        for key, child in value.items():
            if key not in ("__raw__", "__tag__"):
                yield from walk_objects(child)
    elif isinstance(value, list):
        for child in value:
            yield from walk_objects(child)

with tempfile.TemporaryDirectory(prefix="animal-xdat-edit-test-") as temp:
    work = Path(temp) / "parts"
    windows, shortcuts = decompile_to_folder(original, work)

    target_file = None
    target_tree = None
    target_text = None
    for path in sorted((work / "windows").glob("*.xml")):
        tree = ET.parse(path)
        found = find_textbox(tree.getroot())
        if found is not None:
            target_file, target_tree, target_text = path, tree, found
            break

    if target_file is None:
        raise SystemExit("No se encontró un ct0.TextBox con texto visible no vacío para la prueba.")

    before = target_text.text or ""
    marker = "_XDAT_TEST"
    if marker in before:
        marker = "_XDAT_TEST2"
    changed = before + marker
    target_text.text = changed
    target_tree.write(target_file, encoding="utf-8", xml_declaration=True)

    rebuilt, rebuilt_windows, rebuilt_shortcuts = compile_from_folder(work, original_data=original)
    if rebuilt == original:
        raise SystemExit("La edición de prueba no produjo cambios en el XDAT compilado.")

    parsed = parse_xdat(rebuilt)
    persisted = any(
        obj.get("__type__") == "ct0.TextBox" and obj.get("text") == changed
        for obj in walk_objects(parsed.get("windows", []))
    )
    if not persisted:
        raise SystemExit("El texto modificado no sobrevivió a la recompilación y relectura del XDAT.")

    if SOURCE.read_bytes() != original or hashlib.sha256(SOURCE.read_bytes()).hexdigest() != original_hash:
        raise SystemExit("ERROR: el XDAT original fue alterado durante la prueba.")

    print(
        "ANIMAL_XDAT_MODIFICATION_OK "
        f"windows={windows} shortcuts={shortcuts} "
        f"rebuilt_windows={rebuilt_windows} rebuilt_shortcuts={rebuilt_shortcuts} "
        f"field=ct0.TextBox.text marker={marker} "
        "modified_binary_differs=True modified_value_persisted=True original_unchanged=True"
    )
