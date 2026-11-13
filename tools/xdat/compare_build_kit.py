#!/usr/bin/env python3
"""Compare the historical XDAT Windows kit with Methos-maintained Python sources.

This is a read-only audit: it does not execute files from the kit or modify the archive.
"""
from __future__ import annotations

import argparse
import difflib
import hashlib
import json
import zipfile
from collections import defaultdict
from pathlib import Path, PurePosixPath


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--archive", default="reference-data/kits/Methos-XDAT-Windows-Build-Kit.zip")
    parser.add_argument("--repo", default=".")
    parser.add_argument("--json-out", default="xdat-kit-comparison.json")
    parser.add_argument("--markdown-out", default="xdat-kit-comparison.md")
    args = parser.parse_args()

    root = Path(args.repo).resolve()
    archive = Path(args.archive).resolve()
    if not archive.is_file():
        raise SystemExit(f"No se encontró el kit XDAT: {archive}")

    source_roots = [root / "tools" / "xdat", root / "src" / "Methos.App"]
    candidates: dict[str, list[Path]] = defaultdict(list)
    for source_root in source_roots:
        if source_root.is_dir():
            for path in source_root.rglob("*.py"):
                candidates[path.name.casefold()].append(path)

    report = {
        "archive": str(archive.relative_to(root)),
        "archive_size": archive.stat().st_size,
        "archive_sha256": sha256(archive.read_bytes()),
        "zip_integrity": None,
        "kit_python_files": [],
        "repository_python_files": [],
        "findings": [],
    }
    markdown = [
        "# Comparación del kit XDAT histórico con las fuentes Methos",
        "",
        f"- Archivo: `{report['archive']}`",
        f"- Tamaño: {report['archive_size']} bytes",
        f"- SHA-256: `{report['archive_sha256']}`",
        "- Inspección de solo lectura; no se ejecutó ningún archivo del kit.",
        "",
        "| Archivo del kit | Estado | Fuente(s) del repositorio |",
        "|---|---|---|",
    ]
    matched_repo: set[Path] = set()
    identical = different = kit_only = 0

    with zipfile.ZipFile(archive) as zf:
        bad = zf.testzip()
        report["zip_integrity"] = "OK" if bad is None else f"CRC error: {bad}"
        if bad is not None:
            raise SystemExit(report["zip_integrity"])

        for info in zf.infolist():
            if info.is_dir() or not info.filename.casefold().endswith(".py"):
                continue
            data = zf.read(info)
            basename = PurePosixPath(info.filename).name
            matches = sorted(candidates.get(basename.casefold(), []))
            item = {
                "kit_path": info.filename,
                "size": len(data),
                "sha256": sha256(data),
                "matches": [],
            }
            if not matches:
                kit_only += 1
                item["status"] = "KIT_ONLY"
                markdown.append(f"| `{info.filename}` | KIT_ONLY | — |")
            else:
                states = []
                for candidate in matches:
                    repo_data = candidate.read_bytes()
                    same = repo_data == data
                    state = "IDENTICAL" if same else "DIFFERENT"
                    states.append(state)
                    matched_repo.add(candidate)
                    match = {
                        "repository_path": candidate.relative_to(root).as_posix(),
                        "size": len(repo_data),
                        "sha256": sha256(repo_data),
                        "status": state,
                    }
                    if not same:
                        different += 1
                        try:
                            left = data.decode("utf-8").splitlines()
                            right = repo_data.decode("utf-8").splitlines()
                            diff = list(difflib.unified_diff(
                                left, right,
                                fromfile=f"kit/{info.filename}",
                                tofile=match["repository_path"],
                                lineterm="",
                            ))
                            match["diff_preview"] = diff[:80]
                        except UnicodeDecodeError:
                            match["diff_preview"] = ["(contenido binario o no UTF-8)"]
                    else:
                        identical += 1
                    item["matches"].append(match)
                item["status"] = "IDENTICAL" if all(s == "IDENTICAL" for s in states) else "DIFFERENT"
                locations = ", ".join(m["repository_path"] + " (" + m["status"] + ")" for m in item["matches"])
                markdown.append(f"| `{info.filename}` | {item['status']} | {locations} |")
            report["kit_python_files"].append(item)

    repo_files = sorted(
        p for source_root in source_roots if source_root.is_dir()
        for p in source_root.rglob("*.py")
    )
    for path in repo_files:
        if path not in matched_repo:
            report["repository_python_files"].append(path.relative_to(root).as_posix())

    report["summary"] = {
        "kit_python_files": len(report["kit_python_files"]),
        "identical_matches": identical,
        "different_matches": different,
        "kit_only_files": kit_only,
        "repository_only_files": len(report["repository_python_files"]),
    }
    report["findings"].append(
        "Las coincidencias se comparan por nombre de archivo y bytes; los nombres repetidos se reportan por separado."
    )

    markdown.extend([
        "",
        "## Resumen",
        "",
        f"- Integridad ZIP: **{report['zip_integrity']}**",
        f"- Archivos Python en el kit: **{len(report['kit_python_files'])}**",
        f"- Coincidencias idénticas: **{identical}**",
        f"- Coincidencias diferentes: **{different}**",
        f"- Archivos solo en el kit: **{kit_only}**",
        f"- Archivos solo en el repositorio: **{len(report['repository_python_files'])}**",
        "",
        "## Archivos solo en el repositorio",
        "",
    ])
    markdown.extend(f"- `{p}`" for p in report["repository_python_files"] or ["(ninguno)"])

    for item in report["kit_python_files"]:
        for match in item["matches"]:
            if match["status"] == "DIFFERENT" and PurePosixPath(item["kit_path"]).name.casefold() in {
                "xdat_core.py", "encryption_detection.py", "auto_pipeline.py",
                "package_origin_metadata.py", "xdat_bridge.py"
            }:
                markdown.extend([
                    "",
                    f"## Diferencias: `{item['kit_path']}` vs `{match['repository_path']}`",
                    "",
                    "```diff",
                    *match.get("diff_preview", []),
                    "```",
                ])

    Path(args.json_out).write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    Path(args.markdown_out).write_text("\n".join(markdown) + "\n", encoding="utf-8")
    print(json.dumps(report["summary"], ensure_ascii=False))
    print(f"Integridad ZIP: {report['zip_integrity']}")
    for item in report["kit_python_files"]:
        paths = ", ".join(
            f"{m['repository_path']}={m['status']}" for m in item["matches"]
        ) or "(sin equivalente)"
        print(f"{item['status']}: {item['kit_path']} -> {paths}")
    print(f"Informe: {args.markdown_out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
