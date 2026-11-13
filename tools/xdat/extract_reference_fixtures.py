#!/usr/bin/env python3
"""Safely extract only XDAT reference fixtures from the historical ZIP.

The legacy GUI app.py, tests, and binaries are deliberately not extracted or run.
"""
from __future__ import annotations

import argparse
import zipfile
from pathlib import Path, PurePosixPath

FIXTURE_DIRS = {"xdat completo", "xdat por partes"}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--archive", default="reference-data/kits/Methos-XDAT-Windows-Build-Kit.zip")
    parser.add_argument("--destination", required=True)
    args = parser.parse_args()

    archive = Path(args.archive).resolve()
    destination = Path(args.destination).resolve()
    if not archive.is_file():
        raise SystemExit(f"No se encontró el archivo de referencia: {archive}")
    destination.mkdir(parents=True, exist_ok=True)

    extracted = 0
    total_bytes = 0
    with zipfile.ZipFile(archive) as zf:
        bad = zf.testzip()
        if bad is not None:
            raise SystemExit(f"El ZIP tiene un error CRC en: {bad}")
        for info in zf.infolist():
            parts = PurePosixPath(info.filename).parts
            fixture_index = next(
                (i for i, part in enumerate(parts) if part.casefold() in FIXTURE_DIRS),
                None,
            )
            if fixture_index is None:
                continue
            relative = PurePosixPath(*parts[fixture_index:])
            target = (destination / Path(*relative.parts)).resolve()
            if not target.is_relative_to(destination):
                raise SystemExit(f"Ruta insegura en el ZIP: {info.filename}")
            if info.is_dir():
                target.mkdir(parents=True, exist_ok=True)
                continue
            target.parent.mkdir(parents=True, exist_ok=True)
            data = zf.read(info)
            target.write_bytes(data)
            extracted += 1
            total_bytes += len(data)

    original = destination / "xdat completo" / "interface.xdat"
    parts = destination / "xdat por partes"
    if not original.is_file() or not parts.is_dir():
        raise SystemExit("El kit no contiene los fixtures XDAT esperados (interface.xdat / xdat por partes).")

    print(f"Fixtures XDAT extraídos: {extracted} archivos, {total_bytes} bytes.")
    print(f"XDAT_REFERENCE_ROOT={destination}")
    print("No se extrajo ni ejecutó app.py, l2encdec.exe ni código Python del kit.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
