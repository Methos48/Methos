"""Lineage II file-encryption header detection.

Detection only: identifies documented L2Crypt protocol headers. It does not
decrypt payloads or infer custom keys from arbitrary bytes.
"""
from __future__ import annotations

from dataclasses import asdict, dataclass
from typing import Optional

HEADER_PREFIX = "Lineage2Ver"
HEADER_SIZE = 28

# Dispatch table follows acmi/L2crypt L2Crypt.decrypt()/encrypt().
# Values: (family, human-readable description, original filename required)
PROTOCOLS = {
    111: ("XOR", "XOR fijo (111)", False),
    120: ("XOR", "XOR variante 120", False),
    121: ("XOR", "XOR dependiente del nombre de archivo", True),
    211: ("BLOWFISH", "Blowfish 211", False),
    212: ("BLOWFISH", "Blowfish 212", False),
    411: ("RSA", "RSA 411", False),
    412: ("RSA", "RSA 412", False),
    413: ("RSA", "RSA 413", False),
    414: ("RSA", "RSA 414", False),
    611: ("LAMECRYPT_RSA", "LameCrypt + RSA 411", False),
    612: ("LAMECRYPT_RSA", "LameCrypt + RSA 412", False),
    613: ("LAMECRYPT_RSA", "LameCrypt + RSA 413", False),
    614: ("LAMECRYPT_RSA", "LameCrypt + RSA 414", False),
    811: ("LAMECRYPT_XOR", "LameCrypt + XOR 111", False),
    820: ("LAMECRYPT_XOR", "LameCrypt + XOR 120", False),
    821: ("LAMECRYPT_XOR", "LameCrypt + XOR 121", True),
    911: ("LAMECRYPT_BLOWFISH", "LameCrypt + Blowfish 211", False),
    912: ("LAMECRYPT_BLOWFISH", "LameCrypt + Blowfish 212", False),
}


@dataclass(frozen=True)
class DetectionResult:
    status: str
    encrypted: Optional[bool]
    version: Optional[int]
    family: Optional[str]
    description: str
    header: Optional[str]
    header_size: int
    filename_required: bool = False
    decoder_available: bool = False
    detail: str = ""

    def to_dict(self) -> dict:
        return asdict(self)


def _plain(status: str, detail: str = "") -> DetectionResult:
    return DetectionResult(
        status=status,
        encrypted=False if status == "plain_candidate" else None,
        version=None,
        family=None,
        description=(
            "Sin cabecera Lineage2Ver reconocida; candidato a archivo sin cifrar"
            if status == "plain_candidate" else "Formato o cabecera no reconocidos"
        ),
        header=None,
        header_size=0,
        detail=detail,
    )



def _unrecognized(raw: bytes, detail: str) -> DetectionResult:
    """Use the XDAT editor's count-field sanity check without guessing a cipher.

    A plaintext ct0 XDAT begins with a little-endian window count. The known
    reader rejects values outside 0..65535 as likely encrypted. We preserve
    uncertainty because this can also mean corruption or another XDAT version.
    """
    if len(raw) >= 4:
        first = int.from_bytes(raw[:4], "little", signed=True)
        if first < 0 or first > 0xFFFF:
            unsigned = int.from_bytes(raw[:4], "little", signed=False)
            return DetectionResult(
                status="suspected_encryption",
                encrypted=None,
                version=None,
                family="UNKNOWN",
                description="Posible cifrado u ofuscación no identificada",
                header=None,
                header_size=0,
                detail=(
                    f"El primer campo XDAT vale {first} (sin signo: {unsigned}, "
                    f"0x{unsigned:08X}); un XDAT ct0 sin cifrar debe comenzar "
                    "con un contador entre 0 y 65535. Puede ser cifrado, "
                    "ofuscación personalizada, corrupción o una variante distinta."
                ),
            )
    return _plain("plain_candidate", detail)

def detect_encryption(data: bytes, filename: str = "") -> DetectionResult:
    """Detect a documented Lineage2Ver header at offset zero.

    A plain_candidate must still be validated by the relevant file parser.
    Unknown signatures are never treated as valid plaintext with certainty.
    """
    if not isinstance(data, (bytes, bytearray, memoryview)):
        raise TypeError("data debe ser bytes")
    raw = bytes(data)
    if len(raw) < HEADER_SIZE:
        return _plain(
            "unknown_or_truncated",
            f"Archivo demasiado corto para una cabecera L2 de {HEADER_SIZE} bytes",
        )

    try:
        header = raw[:HEADER_SIZE].decode("utf-16le", errors="strict")
    except UnicodeDecodeError:
        return _unrecognized(raw, "Los primeros 28 bytes no son una cabecera UTF-16LE Lineage2Ver")

    if not header.startswith(HEADER_PREFIX):
        return _unrecognized(raw, "No aparece Lineage2Ver al inicio del archivo")

    suffix = header[len(HEADER_PREFIX):]
    if len(header) != 14 or len(suffix) != 3 or not suffix.isascii() or not suffix.isdigit():
        return DetectionResult(
            status="malformed_header",
            encrypted=None,
            version=None,
            family=None,
            description="Cabecera Lineage2Ver dañada o con versión mal formada",
            header=header,
            header_size=HEADER_SIZE,
            detail=f"Se esperaba {HEADER_PREFIX} seguido de tres dígitos ASCII",
        )

    version = int(suffix)
    if version not in PROTOCOLS:
        return DetectionResult(
            status="unsupported_version",
            encrypted=True,
            version=version,
            family="UNKNOWN",
            description=f"Cifrado Lineage II detectado, pero la versión {version} no está catalogada",
            header=header,
            header_size=HEADER_SIZE,
            detail="No se intentó descifrar ni modificar el contenido",
        )

    family, description, needs_filename = PROTOCOLS[version]
    return DetectionResult(
        status="recognized_encryption",
        encrypted=True,
        version=version,
        family=family,
        description=description,
        header=header,
        header_size=HEADER_SIZE,
        filename_required=needs_filename,
        decoder_available=version in {111, 120, 121, 211, 212, 411, 412, 413, 414},
        detail=(
            ("Motor integrado disponible" if version in {111, 120, 121, 211, 212, 411, 412, 413, 414} else "Variante detectada; motor integrado pendiente")
            + (". Se necesita el nombre original del archivo." if needs_filename else ".")
        ),
    )


def supported_versions() -> tuple[int, ...]:
    """Return all documented versions recognized by this detector."""
    return tuple(sorted(PROTOCOLS))
