#!/usr/bin/env python3
"""Build searchable variable/function/constant and XDAT control inventories."""
from __future__ import annotations

import argparse
import csv
import json
import re
import collections
from pathlib import Path
import xml.etree.ElementTree as ET


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--u-source", required=True, type=Path, help="Folder containing decompiled .uc files and methos_name_map.json")
    parser.add_argument("--xdat-xml", required=True, type=Path, help="Root folder containing windows/ and shortcuts/")
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()

    name_map_path = args.u_source / "methos_name_map.json"
    name_map = json.loads(name_map_path.read_text(encoding="utf-8")) if name_map_path.is_file() else []
    reverse_names = {item["SafeName"]: item["OriginalName"] for item in name_map}

    classes = []
    variable_rows = []
    function_rows = []
    constant_rows = []
    for source_path in sorted(args.u_source.glob("*.uc")):
        source = source_path.read_text(encoding="utf-8", errors="replace")
        class_match = re.search(r"^class\\s+(\\w+)", source, re.MULTILINE)
        class_name = class_match.group(1) if class_match else source_path.stem
        variables, constants, functions = [], [], []

        for line_number, line in enumerate(source.splitlines(), 1):
            match = re.match(r"^var\\s+(.+?);\\s*$", line)
            if match:
                declaration = match.group(1).strip()
                parsed = re.match(r"(.+?)\\s+([A-Za-z_]\\w*)(?:\\[(\\d+)\\])?$", declaration)
                variable_type = parsed.group(1).strip() if parsed else ""
                identifier = parsed.group(2) if parsed else declaration
                array_length = int(parsed.group(3)) if parsed and parsed.group(3) else 1
                record = {
                    "class": class_name,
                    "line": line_number,
                    "type": variable_type,
                    "identifier": identifier,
                    "original_fname": reverse_names.get(identifier, identifier),
                    "array_length": array_length,
                    "declaration": declaration,
                }
                variables.append(record)
                variable_rows.append(record)

            match = re.match(r"^const\\s+(.+?);\\s*$", line)
            if match:
                record = {"class": class_name, "line": line_number, "declaration": match.group(1).strip()}
                constants.append(record)
                constant_rows.append(record)

            match = re.match(r"^function\\s+(.+?)\\s*$", line)
            if match:
                record = {"class": class_name, "line": line_number, "signature": match.group(1).strip()}
                functions.append(record)
                function_rows.append(record)

        classes.append({
            "class": class_name,
            "source_file": source_path.name,
            "class_variable_count": len(variables),
            "constant_count": len(constants),
            "function_count": len(functions),
            "variables": variables,
            "constants": constants,
            "functions": functions,
        })

    controls = []
    control_type_counts = collections.Counter()
    windows_dir = args.xdat_xml / "windows"
    window_files = sorted(windows_dir.glob("*.xml"))
    for xml_path in window_files:
        root = ET.parse(xml_path).getroot()
        window_name = root.findtext("name") or xml_path.stem
        for node in root.iter():
            if not node.tag.startswith("ct0."):
                continue
            name = node.findtext("name")
            if not name:
                continue
            element_type = node.tag.removeprefix("ct0.")
            control_type_counts[element_type] += 1
            record = {
                "window": window_name,
                "xml_file": xml_path.name,
                "element_type": element_type,
                "name": name,
            }
            for key in (
                "size__absolute__width", "size__absolute__height",
                "anchor__parent", "anchor__this", "anchor__ctrl", "anchor__x", "anchor__y",
                "col", "row", "maxItemNum", "iconWidth", "iconHeight", "gapX", "gapY",
                "file", "normalTex", "pushedTex", "highlightTex",
            ):
                record[key] = node.findtext(key) or ""
            controls.append(record)

    shortcut_dir = args.xdat_xml / "shortcuts"
    summary = {
        "class_count": len(classes),
        "class_variable_declaration_count": len(variable_rows),
        "constant_count": len(constant_rows),
        "function_count": len(function_rows),
        "xdat_window_xml_count": len(window_files),
        "xdat_shortcut_xml_count": len(list(shortcut_dir.glob("*.xml"))),
        "named_control_count": len(controls),
        "control_type_counts": dict(control_type_counts),
        "notes": [
            "Original FName values are retained alongside safe decompiler identifiers.",
            "Only class-level var declarations are counted; function-local variables are excluded.",
            "XDAT control records are extracted from every decoded window XML.",
            "Array declarations are counted as one declaration; array_length records their declared size.",
        ],
    }

    args.output.mkdir(parents=True, exist_ok=True)
    (args.output / "class-structure-inventory.json").write_text(
        json.dumps({"summary": summary, "classes": classes}, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    csv_specs = [
        ("class-variable-inventory.csv", variable_rows,
         ["class", "line", "type", "identifier", "original_fname", "array_length", "declaration"]),
        ("class-function-inventory.csv", function_rows, ["class", "line", "signature"]),
        ("class-constant-inventory.csv", constant_rows, ["class", "line", "declaration"]),
        ("xdat-control-inventory.csv", controls,
         ["window", "xml_file", "element_type", "name", "size__absolute__width",
          "size__absolute__height", "anchor__parent", "anchor__this", "anchor__ctrl",
          "anchor__x", "anchor__y", "col", "row", "maxItemNum", "iconWidth",
          "iconHeight", "gapX", "gapY", "file", "normalTex", "pushedTex", "highlightTex"]),
    ]
    for filename, rows, columns in csv_specs:
        with (args.output / filename).open("w", encoding="utf-8-sig", newline="") as stream:
            writer = csv.DictWriter(stream, fieldnames=columns)
            writer.writeheader()
            writer.writerows(rows)

    (args.output / "SUMMARY.txt").write_text(
        "\\n".join(f"{key}: {value}" for key, value in summary.items()) + "\\n",
        encoding="utf-8",
    )
    print(json.dumps(summary, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
