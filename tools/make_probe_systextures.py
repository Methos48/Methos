#!/usr/bin/env python3
"""Create minimal valid splash BMP placeholders for the isolated L2 capture probe.

These files are test scaffolding only. They are never copied to the user's game
installation or used as replacements for real client assets.
"""
from __future__ import annotations

import struct
import sys
from pathlib import Path


def bmp32(path: Path) -> None:
    width = height = 2
    stride = width * 4
    pixels = bytes((0, 0, 0, 255)) * (width * height)
    image_size = stride * height
    offset = 14 + 40
    file_size = offset + image_size
    header = struct.pack("<2sIHHI", b"BM", file_size, 0, 0, offset)
    info = struct.pack(
        "<IiiHHIIiiII",
        40, width, height, 1, 32, 0, image_size, 2835, 2835, 0, 0
    )
    path.write_bytes(header + info + pixels)


def bmp8(path: Path) -> None:
    width = height = 2
    stride = 4
    palette = b"".join(bytes((i, i, i, 0)) for i in range(256))
    pixels = bytes((0, 0, 0, 0)) * height
    offset = 14 + 40 + len(palette)
    image_size = len(pixels)
    file_size = offset + image_size
    header = struct.pack("<2sIHHI", b"BM", file_size, 0, 0, offset)
    info = struct.pack(
        "<IiiHHIIiiII",
        40, width, height, 1, 8, 0, image_size, 2835, 2835, 256, 256
    )
    path.write_bytes(header + info + palette + pixels)


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: make_probe_systextures.py <SysTextures-directory>", file=sys.stderr)
        return 2
    root = Path(sys.argv[1])
    root.mkdir(parents=True, exist_ok=True)
    for index in range(256):
        bmp32(root / f"sp_32b_{index:02d}.bmp")
        bmp8(root / f"sp_256_{index:02d}.bmp")
    print(f"CREATED_PROBE_SPLASH_PLACEHOLDERS={512} DIRECTORY={root}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
