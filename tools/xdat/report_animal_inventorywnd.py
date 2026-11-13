from __future__ import annotations
import csv
import json
import sys
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from xdat_core import parse_xdat

SOURCE = Path("reference-data/packages/animal/interface.xdat")
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("animal-inventorywnd-analysis")
OUT.mkdir(parents=True, exist_ok=True)

root = parse_xdat(SOURCE.read_bytes())
matches = [w for w in root.get("windows", []) if w.get("name") == "InventoryWnd"]
if len(matches) != 1:
    raise SystemExit(f"Se esperaba exactamente una InventoryWnd; encontradas: {len(matches)}")
window = matches[0]
rows = []
texture_refs = Counter()

def walk(node, parent="InventoryWnd"):
    name = node.get("name") or node.get("__type__", "control")
    path = f"{parent}/{name}"
    props = {k: v for k, v in node.items()
             if not k.startswith("__") and not isinstance(v, (list, dict, bytes))}
    refs = []
    for key, value in props.items():
        if isinstance(value, str) and ("texture" in key.lower() or key.lower() in {"file", "backtex", "normaltex", "pushedtex", "highlighttex", "droptex"}):
            if value:
                refs.append({"field": key, "value": value})
                texture_refs[value] += 1
    rows.append({
        "path": path,
        "type": node.get("__type__", ""),
        "name": name,
        "properties": props,
        "texture_references": refs,
        "raw_fields": sorted(node.get("__raw__", {}).keys()),
    })
    for field, value in node.items():
        if isinstance(value, list):
            for index, child in enumerate(value):
                if isinstance(child, dict) and "__type__" in child:
                    walk(child, f"{path}/{field}[{index}]")

walk(window)
payload = {
    "source": str(SOURCE),
    "window": "InventoryWnd",
    "window_properties": {k: v for k, v in window.items()
                          if not k.startswith("__") and not isinstance(v, (list, dict, bytes))},
    "control_count_including_window": len(rows),
    "controls": rows,
    "texture_references": [{"name": name, "count": count,
                            "package": name.split(".", 1)[0] if "." in name else ""}
                           for name, count in sorted(texture_refs.items())],
    "notes": [
        "Este informe describe exclusivamente datos presentes en Animal interface.xdat.",
        "Los nombres de textura son referencias; el XDAT no contiene los píxeles de esas texturas.",
        "No se modificó el archivo original ni el comportamiento de controles."
    ]
}
(OUT / "InventoryWnd.json").write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
with (OUT / "InventoryWnd-controls.csv").open("w", newline="", encoding="utf-8-sig") as f:
    w = csv.DictWriter(f, fieldnames=["path", "type", "name", "properties", "texture_references", "raw_fields"])
    w.writeheader()
    for row in rows:
        w.writerow({**row, "properties": json.dumps(row["properties"], ensure_ascii=False),
                    "texture_references": json.dumps(row["texture_references"], ensure_ascii=False),
                    "raw_fields": ";".join(row["raw_fields"])})
with (OUT / "InventoryWnd-textures.csv").open("w", newline="", encoding="utf-8-sig") as f:
    w = csv.DictWriter(f, fieldnames=["name", "package", "count"])
    w.writeheader()
    for item in payload["texture_references"]:
        w.writerow(item)

lines = [
    "# Animal - analisis detallado de InventoryWnd", "",
    f"- Controles contabilizados (incluye ventana): **{len(rows)}**",
    f"- Referencias de textura unicas: **{len(texture_refs)}**", "",
    "## Referencias de textura", "",
    "| Paquete | Referencia | Usos |", "|---|---|---:|"
]
for item in payload["texture_references"]:
    lines.append(f"| {item['package']} | {item['name']} | {item['count']} |")
lines += ["", "## Archivos", "",
          "- InventoryWnd.json: jerarquia, propiedades y referencias completas.",
          "- InventoryWnd-controls.csv: controles y propiedades para filtrar.",
          "- InventoryWnd-textures.csv: referencias de textura unicas.",
          "", "## Limite importante", "",
          "Las referencias como NphrnTextures.*, Abaddon.* y L2UI_CH3.* son nombres solicitados por los controles; no son imagenes incluidas dentro de XDAT. Para confirmar y modificar su aspecto hay que localizar los paquetes de texturas que contienen esos recursos."]
(OUT / "README.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
print(f"ANIMAL_INVENTORYWND_ANALYSIS_OK controls={len(rows)} unique_textures={len(texture_refs)}")
