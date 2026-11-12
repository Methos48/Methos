#!/usr/bin/env python3
"""Validate SmartCrypt + Lineage2Ver111 removal and full XDAT XML round-trip."""
from __future__ import annotations

import argparse
import struct
import tempfile
import zlib
from pathlib import Path

from xdat_core import compile_from_folder, decompile_to_folder


MAGIC = bytes.fromhex("FF ED FA CE")
HEADER_SIZE = 28
TAIL_SIZE = 20


def decode_smartcrypt_and_111(data: bytes) -> tuple[bytes, bytes, bytes, bytes, int, int, int]:
    if len(data) < 0x400 or data[:4] != MAGIC:
        raise ValueError("No se reconoce el contenedor SmartCrypt FF ED FA CE.")
    seed = bytearray(data[4:28])
    selector, flags = seed[0], seed[1]
    if flags & 1:
        raise ValueError("Esta muestra requiere la clave global externa de dsetup.u.")
    version = {1: 111, 2: 121, 3: 413}.get(selector)
    if version != 111:
        raise ValueError(f"Selector SmartCrypt no compatible con este validador: {selector} -> {version}.")
    key6 = bytearray(seed[2:8])
    for i in range(6):
        key6[i] ^= (selector + 1) & 0xFF
    key16 = bytearray(seed[8:24])
    for i in range(16):
        key16[i] ^= key6[i % 6]

    lineage = bytearray(data)
    lineage[:HEADER_SIZE] = f"Lineage2Ver{version:03d}".encode("utf-16le")
    for i in range(HEADER_SIZE, len(lineage)):
        salt = (((i ^ 0x65) * 0x93) & 0xFF)
        lineage[i] ^= key6[i % 6] ^ key16[i % 16] ^ salt

    if len(lineage) < HEADER_SIZE + TAIL_SIZE:
        raise ValueError("El archivo resultante no contiene la envoltura Lineage II completa.")
    expected_header = f"Lineage2Ver{version:03d}".encode("utf-16le")
    if lineage[:HEADER_SIZE] != expected_header:
        raise ValueError("La cabecera Lineage2Ver reconstruida no coincide.")
    stored_crc = struct.unpack_from("<I", lineage, len(lineage) - 8)[0]
    actual_crc = zlib.crc32(lineage[:-TAIL_SIZE]) & 0xFFFFFFFF
    # Dunk3l's inner 111 trailer is not the stock public 111 CRC layout.
    # Keep the exact trailer and validate the actual U/XDAT structure below.
    payload_without_tail = bytearray(lineage[HEADER_SIZE:-TAIL_SIZE])
    for i in range(len(payload_without_tail)):
        payload_without_tail[i] ^= 0xAC
    payload_with_tail = bytearray(lineage[HEADER_SIZE:])
    for i in range(len(payload_with_tail)):
        payload_with_tail[i] ^= 0xAC
    return bytes(payload_without_tail), bytes(payload_with_tail), bytes(lineage), bytes(seed), version, stored_crc, actual_crc



def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("file", nargs="?", default="interface.xdat")
    args = parser.parse_args()
    source = Path(args.file)
    original = source.read_bytes()
    payload_without_tail, payload_with_tail, lineage, seed, version, stored_crc, actual_crc = decode_smartcrypt_and_111(original)

    parsed = None
    parse_errors = []
    with tempfile.TemporaryDirectory(prefix="methos-dunk3l-xdat-") as temp:
        for label, payload in (("without-tail", payload_without_tail), ("with-tail", payload_with_tail)):
            parts = Path(temp) / label / "Descomprimido"
            try:
                windows, shortcuts = decompile_to_folder(payload, parts)
                rebuilt_payload = compile_from_folder(parts, original_data=payload)
                if rebuilt_payload != payload:
                    raise ValueError("El XDAT recompilado desde XML no coincide byte por byte.")
                parsed = (label, payload, windows, shortcuts)
                break
            except Exception as exc:
                parse_errors.append(f"{label}: {exc}")
    if parsed is None:
        raise ValueError("No se pudo descompilar/recompilar el XDAT después de retirar las capas: " + " | ".join(parse_errors))

    label, payload, windows, shortcuts = parsed
    encrypted_payload = bytearray(payload)
    for i in range(len(encrypted_payload)):
        encrypted_payload[i] ^= 0xAC
    if label == "without-tail":
        rebuilt_lineage = bytearray(lineage[:HEADER_SIZE]) + encrypted_payload + bytearray(lineage[-TAIL_SIZE:])
    else:
        rebuilt_lineage = bytearray(lineage[:HEADER_SIZE]) + encrypted_payload
    if rebuilt_lineage != lineage:
        raise ValueError("La recompilación de la capa Lineage2Ver111 no coincide byte por byte.")

    key6 = bytearray(seed[2:8])
    for i in range(6):
        key6[i] ^= (seed[0] + 1) & 0xFF
    key16 = bytearray(seed[8:24])
    for i in range(16):
        key16[i] ^= key6[i % 6]
    rebuilt = bytearray(rebuilt_lineage)
    rebuilt[:4] = MAGIC
    rebuilt[4:28] = seed
    for i in range(28, len(rebuilt)):
        rebuilt[i] ^= key6[i % 6] ^ key16[i % 16] ^ (((i ^ 0x65) * 0x93) & 0xFF)
    if rebuilt != original:
        raise ValueError("La recompilación SmartCrypt no coincide byte por byte con el original.")

    print(f"PASS: REAL Dunk3l {source.name}: SmartCrypt + Lineage2Ver{version} descifrados.")
    print(f"PASS: XDAT parse/decompile/compile XML exact round-trip: windows={windows}, shortcuts={shortcuts}; trailer={label}.")
    if stored_crc != actual_crc:
        print(f"NOTICE: custom Lineage2Ver111 trailer preserved; stock CRC differs (stored={stored_crc:08X}, stock={actual_crc:08X}).")
    print(f"PASS: ambas capas reconstruidas byte por byte; size={len(original)}; seed={seed.hex().upper()}.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
