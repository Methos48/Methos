#!/usr/bin/env python3
"""Extract only XDAT round-trip fixtures from the historical reference archive.

No application source, installer, executable, DLL, or build script is extracted.
"""
from __future__ import annotations

import argparse
from pathlib import Path, PurePosixPath
import zipfile


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--archive", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    archive = Path(args.archive)
    output = Path(args.output).resolve()
    if not archive.is_file():
        raise SystemExit(f"No se encontró el archivo de fixtures: {archive}")

    output.mkdir(parents=True, exist_ok=True)
    complete_count = 0
    parts_count = 0

    with zipfile.ZipFile(archive) as zf:
        bad = zf.testzip()
        if bad is not None:
            raise SystemExit(f"El ZIP tiene un error CRC: {bad}")

        for info in zf.infolist():
            if info.is_dir():
                continue
            raw = info.filename.replace("\\", "/")
            path = PurePosixPath(raw)
            segments = [segment.casefold() for segment in path.parts]
            if path.is_absolute() or ".." in path.parts:
                continue

            if "xdat completo" in segments and path.name.casefold() == "interface.xdat":
                target = output / "xdat completo" / "interface.xdat"
                data = zf.read(info)
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(data)
                complete_count += 1
                continue

            if "xdat por partes" in segments and path.suffix.casefold() == ".xml":
                index = segments.index("xdat por partes")
                relative = Path(*path.parts[index + 1:])
                if not relative.parts:
                    continue
                target = (output / "xdat por partes" / relative).resolve()
                if output not in target.parents:
                    raise SystemExit(f"Ruta de fixture fuera del destino: {info.filename}")
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(zf.read(info))
                parts_count += 1

    if complete_count != 1:
        raise SystemExit(f"Se esperaba exactamente un interface.xdat de referencia; encontrados: {complete_count}")
    if parts_count == 0:
        raise SystemExit("No se encontraron fixtures XML dentro de 'xdat por partes'.")

    print(f"Fixtures XDAT extraídos: interface.xdat=1, XML={parts_count}")
    print("No se extrajeron app.py, ejecutables, DLL, instaladores ni scripts del kit.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
