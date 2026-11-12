"""Automatic Lineage II XDAT crypto dispatch for Methos XDAT."""
from __future__ import annotations
import subprocess, sys, tempfile
from pathlib import Path
from typing import Callable
from encryption_detection import detect_encryption
ENGINE_NAME = "l2encdec.exe"
ENGINE_PROTOCOLS = frozenset({111,120,121,211,212,411,412,413,414})
class AutoPipelineError(RuntimeError): pass
def engine_path():
    candidates=[]
    if getattr(sys,"frozen",False):
        candidates += [Path(getattr(sys,"_MEIPASS",Path(sys.executable).parent))/ENGINE_NAME, Path(sys.executable).parent/ENGINE_NAME]
    candidates.append(Path(__file__).resolve().parent/ENGINE_NAME)
    return next((p for p in candidates if p.is_file()),None)
def _decode(engine,source,output,legacy=False):
    cmd=[str(engine),"-c","decode","-o",str(output)]
    if legacy: cmd.append("-l")
    cmd.append(str(source))
    return subprocess.run(cmd,capture_output=True,text=True,timeout=120,check=False)
def _technical_details(source:Path,data:bytes,detection,reason:str):
    header=detection.header or "(no identificada)"
    signature=data[:32].hex(" ").upper() or "(archivo vacío)"
    return (
        "INFORMACIÓN TÉCNICA\n"
        f"Archivo: {source.name}\n"
        f"Tamaño: {len(data):,} bytes\n"
        f"Estado de detección: {detection.status}\n"
        f"Versión detectada: {detection.version if detection.version is not None else '(ninguna)'}\n"
        f"Cabecera: {header}\n"
        f"Primeros 32 bytes (hex): {signature}\n"
        f"Diagnóstico: {detection.detail or reason}\n"
        f"Motivo: {reason}\n\n"
        "El archivo original no fue modificado."
    )
def _unknown_cipher_message(source:Path,data:bytes,detection,reason:str):
    if detection.status == "suspected_encryption":
        heading = "Posible cifrado u ofuscación personalizada; protocolo no identificado.\n\n"
    else:
        heading = "No se reconoce este cifrado.\n\n"
    return heading + _technical_details(source,data,detection,reason)
def decode_xdat(source:Path,data:bytes,validate_xdat:Callable):
    source=Path(source); detection=detect_encryption(data,source.name)
    if detection.status == "suspected_encryption":
        raise AutoPipelineError(_unknown_cipher_message(
            source,data,detection,
            "El primer campo no coincide con el contador esperado de un XDAT ct0 sin cifrar."
        ))
    if detection.encrypted is not True:
        try: validate_xdat(data)
        except Exception as exc:
            raise AutoPipelineError(_unknown_cipher_message(
                source,data,detection,
                f"El analizador XDAT rechazó el contenido: {type(exc).__name__}: {exc}"
            )) from exc
        return data,detection
    version=detection.version
    if version not in ENGINE_PROTOCOLS:
        raise AutoPipelineError(
            "Cifrado reconocido, pero esta variante todavía no está soportada.\n\n"
            + _technical_details(source,data,detection,"No existe un motor compatible integrado para esta versión.")
        )
    engine=engine_path()
    if engine is None: raise AutoPipelineError("No se encontró el motor integrado l2encdec.exe. El archivo original no fue modificado.")
    with tempfile.TemporaryDirectory(prefix="methos_xdat_decode_") as tmp:
        output=Path(tmp)/"decoded.xdat"; result=_decode(engine,source,output)
        if result.returncode!=0 or not output.is_file():
            if version in {411,412,413,414}:
                output.unlink(missing_ok=True); result=_decode(engine,source,output,True)
        if result.returncode!=0 or not output.is_file():
            detail=(result.stderr or result.stdout or "el motor no produjo salida").strip()
            raise AutoPipelineError(
                "No se reconoce este cifrado o no se pudo descifrar.\n\n"
                + _unknown_cipher_message(source,data,detection,f"El motor devolvió error: {detail}")
            )
        plain=output.read_bytes()
    try: validate_xdat(plain)
    except Exception as exc:
        raise AutoPipelineError(
            "No se reconoce este cifrado o el resultado no es un XDAT válido.\n\n"
            + _unknown_cipher_message(source,data,detection,f"El resultado descifrado no pasó el analizador: {exc}")
        ) from exc
    return plain,detection
def encode_xdat(plain:bytes,original_name:str,version:int,validate_xdat:Callable):
    if version not in ENGINE_PROTOCOLS: raise AutoPipelineError(f"No hay codificador integrado para el protocolo {version}.")
    engine=engine_path()
    if engine is None: raise AutoPipelineError("No se encontró el motor integrado l2encdec.exe.")
    name=Path(original_name).name or "interface.xdat"
    with tempfile.TemporaryDirectory(prefix="methos_xdat_encode_") as tmp:
        root=Path(tmp); a=root/"plain"; b=root/"encrypted"; c=root/"check"
        a.mkdir(); b.mkdir(); c.mkdir()
        source=a/name; encrypted_file=b/name; check=c/"roundtrip.xdat"; source.write_bytes(plain)
        result=subprocess.run([str(engine),"-c","encode","-p",str(version),"-o",str(encrypted_file),str(source)],capture_output=True,text=True,timeout=120,check=False)
        if result.returncode!=0 or not encrypted_file.is_file(): raise AutoPipelineError(f"Falló la codificación ({version}): {(result.stderr or result.stdout or 'sin salida').strip()}")
        encrypted=encrypted_file.read_bytes(); verify=_decode(engine,encrypted_file,check)
        if verify.returncode!=0 or not check.is_file():
            if version in {411,412,413,414}: verify=_decode(engine,encrypted_file,check,True)
        if verify.returncode!=0 or not check.is_file() or check.read_bytes()!=plain:
            raise AutoPipelineError("Falló la verificación de ida y vuelta; no se guardó el resultado.")
    return encrypted


def publish_xdat_transactionally(data: bytes, output: Path, archive_dir: Path, backup_root: Path):
    """Publish a verified XDAT atomically, preserving recoverable copies on failure."""
    import datetime
    import shutil
    import uuid

    output = Path(output)
    archive_dir = Path(archive_dir)
    backup_root = Path(backup_root)
    output.parent.mkdir(parents=True, exist_ok=True)
    archive_dir.mkdir(parents=True, exist_ok=True)
    if output.parent.resolve() != archive_dir.resolve():
        raise AutoPipelineError("El archivo XDAT de salida debe quedar directamente dentro de Archivo.")

    transaction = uuid.uuid4().hex
    staged = output.parent.parent / f".{output.name}.{transaction}.staged"
    rollback = output.parent.parent / f".{output.parent.name}.{transaction}.rollback"
    persistent_backup = backup_root / (datetime.datetime.now().strftime("%Y%m%d_%H%M%S_%f") + "_" + transaction)
    archive_cleared = False
    keep_rollback = False

    try:
        staged.write_bytes(data)
        if staged.read_bytes() != data:
            raise AutoPipelineError("El archivo temporal no coincide con el resultado compilado.")

        old_files = [p for p in archive_dir.rglob("*") if p.is_file()]
        if old_files:
            persistent_backup.mkdir(parents=True, exist_ok=False)
            rollback.mkdir(parents=True, exist_ok=False)
            for old_file in old_files:
                relative = old_file.relative_to(archive_dir)
                backup = persistent_backup / relative
                recovery = rollback / relative
                backup.parent.mkdir(parents=True, exist_ok=True)
                recovery.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(old_file, backup)
                shutil.copy2(old_file, recovery)
                if backup.read_bytes() != old_file.read_bytes() or recovery.read_bytes() != old_file.read_bytes():
                    raise AutoPipelineError(f"La copia de seguridad no coincide: {relative}")

        for item in archive_dir.iterdir():
            if item.is_dir():
                shutil.rmtree(item)
            else:
                item.unlink()
        archive_cleared = True
        staged.replace(output)
        if output.read_bytes() != data:
            raise AutoPipelineError("El XDAT publicado no coincide con el resultado compilado.")
        return output
    except Exception as publish_error:
        if archive_cleared:
            try:
                for item in archive_dir.iterdir():
                    if item.is_dir():
                        shutil.rmtree(item)
                    else:
                        item.unlink()
                if rollback.is_dir():
                    for recovery in rollback.rglob("*"):
                        if recovery.is_file():
                            restored = archive_dir / recovery.relative_to(rollback)
                            restored.parent.mkdir(parents=True, exist_ok=True)
                            shutil.copy2(recovery, restored)
                            if restored.read_bytes() != recovery.read_bytes():
                                raise IOError(f"No se pudo verificar la restauración: {restored.name}")
            except Exception as restore_error:
                keep_rollback = True
                raise AutoPipelineError(
                    f"Falló la publicación y la restauración. La copia de recuperación permanece en: {rollback}"
                ) from ExceptionGroup("Errores de publicación y restauración", [publish_error, restore_error])
        raise
    finally:
        staged.unlink(missing_ok=True)
        if rollback.exists() and not keep_rollback:
            shutil.rmtree(rollback)
