from __future__ import annotations
import csv
import json
import sys
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from xdat_core import parse_xdat

SOURCE = Path("reference-data/packages/animal/interface.xdat")
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("animal-xdat-inventory")
OUT.mkdir(parents=True, exist_ok=True)

root = parse_xdat(SOURCE.read_bytes())
rows = []
windows = []
type_counts = Counter()

def scalar_properties(node):
    return {
        key: value for key, value in node.items()
        if not key.startswith("__") and not isinstance(value, (list, dict, bytes))
    }

def walk(node, window_name, parent_path=""):
    kind = node.get("__type__", "unknown")
    name = node.get("name") or node.get("unk0") or ""
    current = f"{parent_path}/{name or kind}"
    props = scalar_properties(node)
    type_counts[kind] += 1
    rows.append({
        "window": window_name,
        "path": current,
        "type": kind,
        "name": name,
        "text": node.get("text", ""),
        "texture_file": node.get("file", ""),
        "properties": props,
    })
    for field, value in node.items():
        if isinstance(value, list):
            for index, child in enumerate(value):
                if isinstance(child, dict) and "__type__" in child:
                    walk(child, window_name, f"{current}/{field}[{index}]")

for index, window in enumerate(root.get("windows", [])):
    window_name = window.get("name") or f"Window_{index}"
    before = len(rows)
    walk(window, window_name)
    windows.append({
        "index": index,
        "name": window_name,
        "type": window.get("__type__", ""),
        "control_count_including_window": len(rows) - before,
        "properties": scalar_properties(window),
    })

payload = {
    "source": str(SOURCE),
    "window_count": len(windows),
    "shortcut_count": len(root.get("shortcuts", [])),
    "node_count_including_windows": len(rows),
    "type_counts": dict(sorted(type_counts.items())),
    "windows": windows,
    "nodes": rows,
}
(OUT / "inventory.json").write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")

with (OUT / "windows.csv").open("w", newline="", encoding="utf-8-sig") as f:
    writer = csv.DictWriter(f, fieldnames=["index", "name", "type", "control_count_including_window", "properties"])
    writer.writeheader()
    for item in windows:
        writer.writerow({**item, "properties": json.dumps(item["properties"], ensure_ascii=False)})

with (OUT / "controls.csv").open("w", newline="", encoding="utf-8-sig") as f:
    writer = csv.DictWriter(f, fieldnames=["window", "path", "type", "name", "text", "texture_file", "properties"])
    writer.writeheader()
    for item in rows:
        writer.writerow({**item, "properties": json.dumps(item["properties"], ensure_ascii=False)})

lines = [
    "# Animal interface.xdat - inventario estructural",
    "",
    f"- Ventanas: **{len(windows)}**",
    f"- Accesos directos: **{len(root.get('shortcuts', []))}**",
    f"- Nodos contabilizados (incluye ventanas): **{len(rows)}**",
    "",
    "## Tipos encontrados",
    "",
    "| Tipo | Cantidad |",
    "|---|---:|",
]
for kind, count in sorted(type_counts.items()):
    lines.append(f"| {kind} | {count} |")
lines += ["", "## Ventanas", "", "| # | Nombre | Nodos bajo la ventana (incluyéndola) |", "|---:|---|---:|"]
for item in windows:
    lines.append(f"| {item['index']} | {item['name']} | {item['control_count_including_window']} |")
lines += [
    "",
    "## Alcance y cautelas",
    "",
    "Este reporte refleja nombres, tipos y valores almacenados en el XDAT. Los campos cuyo significado no está definido por el esquema se conservan literalmente dentro de properties; no se interpretan como coordenadas, tamaños ni comportamiento.",
    "",
    "Los archivos controls.csv e inventory.json contienen el detalle recursivo de los controles y sus propiedades.",
]
(OUT / "README.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
print(f"ANIMAL_XDAT_INVENTORY_OK windows={len(windows)} nodes={len(rows)} shortcuts={len(root.get('shortcuts', []))} types={len(type_counts)} output={OUT}")
