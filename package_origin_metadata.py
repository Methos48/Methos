"""Persistent source-profile metadata for Methos package round-trips.

The manifest records what was detected; it never guesses unknown ciphers or
stores keys. Re-encoding must explicitly match the recorded protocol.
"""
from __future__ import annotations

import hashlib
import json
import os
import tempfile
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

SCHEMA = "methos.package-origin"
SCHEMA_VERSION = 1


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def build_origin_record(source: Path, raw: bytes, detection: Any,
                        *, file_kind: str = "XDAT",
                        unreal_version: int | None = None,
                        licensee_version: int | None = None) -> dict:
    version = getattr(detection, "version", None)
    return {
        "schema": SCHEMA,
        "schema_version": SCHEMA_VERSION,
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "source": {
            "file_name": Path(source).name,
            "extension": Path(source).suffix.lower(),
            "size_bytes": len(raw),
            "sha256": hashlib.sha256(raw).hexdigest().upper(),
            "kind": file_kind,
        },
        "encoding": {
            "detection_status": getattr(detection, "status", "unknown"),
            "family": getattr(detection, "family", None),
            "protocol_version": version,
            "header": getattr(detection, "header", None),
            "original_filename_required": bool(
                getattr(detection, "filename_required", False)),
            "unreal_package_version": unreal_version,
            "unreal_licensee_version": licensee_version,
            "diagnostic": getattr(detection, "detail", ""),
            "reproducible": False,
            "reproducibility_note": (
                "Perfil registrado; se debe verificar motor, protocolo, "
                "nombre original y parámetros antes de volver a codificar."
            ),
        },
        "security": {"secrets_saved": False, "original_file_preserved": True},
    }


def save_origin_record(output_directory: Path, source: Path, raw: bytes,
                       detection: Any, **kwargs) -> Path:
    target_dir = Path(output_directory) / ".methos"
    target_dir.mkdir(parents=True, exist_ok=True)
    target = target_dir / "origin.json"
    payload = json.dumps(build_origin_record(source, raw, detection, **kwargs),
                         ensure_ascii=False, indent=2) + "\\n"
    fd, temporary = tempfile.mkstemp(prefix="origin.", suffix=".tmp", dir=target_dir)
    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\\n") as stream:
            stream.write(payload)
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary, target)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)
    return target


def load_and_validate_origin(metadata_path: Path, source_path: Path) -> dict:
    data = json.loads(Path(metadata_path).read_text(encoding="utf-8"))
    if data.get("schema") != SCHEMA or data.get("schema_version") != SCHEMA_VERSION:
        raise ValueError("El manifiesto Methos no tiene un formato compatible.")
    expected = data.get("source", {}).get("sha256", "")
    if not isinstance(expected, str) or len(expected) != 64:
        raise ValueError("El manifiesto no contiene un SHA-256 válido.")
    actual = sha256_file(Path(source_path))
    if actual.lower() != expected.lower():
        raise ValueError("El archivo no coincide con el SHA-256 del manifiesto.")
    return data


def require_recorded_protocol(metadata: dict, requested_protocol: int) -> None:
    recorded = metadata.get("encoding", {}).get("protocol_version")
    if recorded is None:
        raise ValueError("El origen no tiene un protocolo de cifrado confirmado.")
    if int(recorded) != int(requested_protocol):
        raise ValueError(
            f"El archivo original usaba el protocolo {recorded}; "
            f"no se permite codificarlo silenciosamente como {requested_protocol}."
        )
