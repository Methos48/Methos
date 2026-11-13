#!/usr/bin/env python3
"""Prove a real XML edit survives raw CT0 XDAT recompilation and rereading."""
from __future__ import annotations

import argparse
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path

from xdat_core import compile_from_folder, decompile_to_folder, parse_xdat


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("file")
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    source = Path(args.file)
    original = source.read_bytes()
    parsed_original = parse_xdat(original)
    with tempfile.TemporaryDirectory(prefix="methos-xdat-edit-") as temp:
        folder = Path(temp) / "parts"
        windows, shortcuts = decompile_to_folder(original, folder)
        xml_files = sorted((folder / "windows").glob("*.xml"), key=lambda p: p.name.casefold())
        if not xml_files:
            raise ValueError("No se extrajeron ventanas XML modificables.")
        xml_path = xml_files[0]
        tree = ET.parse(xml_path)
        name_node = tree.getroot().find("name")
        if name_node is None or not (name_node.text or "").strip():
            raise ValueError(f"{xml_path.name}: no tiene un nombre de ventana válido.")
        marker = "__MethosEditProbe20261113"
        original_name = name_node.text
        if marker in original_name:
            raise ValueError("El marcador ya existe en el origen; se esperaba un archivo limpio.")
        changed_name = original_name + marker
        name_node.text = changed_name
        tree.write(xml_path, encoding="utf-8", xml_declaration=True)

        rebuilt, rebuilt_windows, rebuilt_shortcuts = compile_from_folder(folder, original_data=original)
        if rebuilt == original:
            raise ValueError("La recompilación no reflejó la modificación XML.")
        if (rebuilt_windows, rebuilt_shortcuts) != (windows, shortcuts):
            raise ValueError("Cambió el número de ventanas o accesos directos.")
        verified = parse_xdat(rebuilt)
        if not any(item.get("name") == changed_name for item in verified["windows"]):
            raise ValueError(f"El marcador no persistió al releer XDAT: {changed_name}")

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_bytes(rebuilt)
    print(f"PASS: RAW XDAT MODIFIED edit/compile/reread; source={source.name}; window={changed_name}; markerPersisted=true; windows={windows}; shortcuts={shortcuts}; originalBytes={len(original)}; rebuiltBytes={len(rebuilt)}.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
