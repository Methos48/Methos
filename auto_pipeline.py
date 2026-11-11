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
def decode_xdat(source:Path,data:bytes,validate_xdat:Callable):
    source=Path(source); detection=detect_encryption(data,source.name)
    if detection.encrypted is not True:
        try: validate_xdat(data)
        except Exception as exc:
            raise AutoPipelineError(f"No se reconoció una cabecera compatible y el contenido no pasó la validación XDAT. Firma: {data[:16].hex(' ').upper()}") from exc
        return data,detection
    version=detection.version
    if version not in ENGINE_PROTOCOLS:
        raise AutoPipelineError(f"Se detectó {detection.description}, pero el motor integrado todavía no implementa esa variante. El original permanece intacto.")
    engine=engine_path()
    if engine is None: raise AutoPipelineError("No se encontró el motor integrado l2encdec.exe.")
    with tempfile.TemporaryDirectory(prefix="methos_xdat_decode_") as tmp:
        output=Path(tmp)/"decoded.xdat"; result=_decode(engine,source,output)
        if result.returncode!=0 or not output.is_file():
            if version in {411,412,413,414}:
                output.unlink(missing_ok=True); result=_decode(engine,source,output,True)
        if result.returncode!=0 or not output.is_file():
            raise AutoPipelineError(f"Falló el descifrado automático ({version}): {(result.stderr or result.stdout or 'sin salida').strip()}")
        plain=output.read_bytes()
    try: validate_xdat(plain)
    except Exception as exc: raise AutoPipelineError("El resultado descifrado no pasó la validación XDAT; se conservó el original.") from exc
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
