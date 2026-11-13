from __future__ import annotations
import csv
import json
import sys
from collections import Counter, defaultdict
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from xdat_core import parse_xdat

SOURCE = Path("reference-data/packages/animal/interface.xdat")
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("animal-xdat-texture-dependencies")
OUT.mkdir(parents=True, exist_ok=True)

root = parse_xdat(SOURCE.read_bytes())
refs = []
counts = Counter()
package_windows = defaultdict(set)

def walk(node, window_name, parent_path=""):
    kind = node.get("__type__", "unknown")
    name = node.get("name") or node.get("unk0") or ""
    current = f"{parent_path}/{name or kind}"
    file_ref = node.get("file", "")
    if isinstance(file_ref, str) and file_ref.strip() and "." in file_ref:
        package, object_name = file_ref.split(".", 1)
        item = {
            "package": package,
            "texture": file_ref,
            "object": object_name,
            "window": window_name,
            "control_path": current,
            "control_type": kind,
            "control_name": name,
        }
        refs.append(item)
        counts[package] += 1
        package_windows[package].add(window_name)
    for field, value in node.items():
        if isinstance(value, list):
            for index, child in enumerate(value):
                if isinstance(child, dict) and "__type__" in child:
                    walk(child, window_name, f"{current}/{field}[{index}]")

windows = root.get("windows", [])
for index, window in enumerate(windows):
    walk(window, window.get("name") or f"Window_{index}")

unique = sorted({(x["package"], x["texture"]) for x in refs})
packages = sorted(counts)
with (OUT / "texture-references.csv").open("w", newline="", encoding="utf-8-sig") as f:
    writer = csv.DictWriter(f, fieldnames=["package", "texture", "object", "window", "control_path", "control_type", "control_name"])
    writer.writeheader()
    writer.writerows(sorted(refs, key=lambda x: (x["package"].lower(), x["texture"].lower(), x["window"])))

with (OUT / "texture-packages.csv").open("w", newline="", encoding="utf-8-sig") as f:
    writer = csv.DictWriter(f, fieldnames=["package", "reference_count", "unique_texture_count", "window_count", "windows"])
    writer.writeheader()
    for package in packages:
        names = sorted({x["texture"] for x in refs if x["package"] == package})
        writer.writerow({
            "package": package,
            "reference_count": counts[package],
            "unique_texture_count": len(names),
            "window_count": len(package_windows[package]),
            "windows": "; ".join(sorted(package_windows[package])),
        })

lines = [
    "# Animal XDAT - dependencias de texturas",
    "",
    f"- Ventanas inspeccionadas: **{len(windows)}**",
    f"- Referencias de textura encontradas: **{len(refs)}**",
    f"- Texturas únicas (paquete + nombre): **{len(unique)}**",
    f"- Paquetes gráficos referenciados: **{len(packages)}**",
    "",
    "## Paquetes que deben localizarse",
    "",
    "| Paquete | Referencias | Texturas únicas | Ventanas |",
    "|---|---:|---:|---:|",
]
for package in packages:
    names = {x["texture"] for x in refs if x["package"] == package}
    lines.append(f"| {package} | {counts[package]} | {len(names)} | {len(package_windows[package])} |")
lines += [
    "",
    "## Interpretación",
    "",
    "Este inventario extrae los nombres de recursos gráficos solicitados por los controles del XDAT. No contiene los píxeles de las texturas ni demuestra que los paquetes estén instalados. Para recuperar o editar visualmente las ventanas hay que localizar los paquetes Unreal de texturas correspondientes (normalmente archivos .utx) del cliente compatible.",
    "",
    "El archivo texture-references.csv conserva cada referencia con la ventana y el control que la utiliza. texture-packages.csv resume los paquetes necesarios.",
]
(OUT / "README.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
print(f"ANIMAL_XDAT_TEXTURE_DEPENDENCIES_OK windows={len(windows)} references={len(refs)} unique_textures={len(unique)} packages={len(packages)} output={OUT}")
for package in packages:
    print(f"TEXTURE_PACKAGE={package} references={counts[package]} unique={len({x['texture'] for x in refs if x['package'] == package})}")
