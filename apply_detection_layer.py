from __future__ import annotations
import shutil
import sys
from pathlib import Path


def main() -> None:
    if len(sys.argv) != 2:
        raise SystemExit("Uso: apply_detection_layer.py <carpeta-proyecto>")
    repo_root = Path(__file__).resolve().parent
    project = Path(sys.argv[1]).resolve()
    app_path = project / "app.py"
    if not app_path.is_file():
        raise SystemExit(f"No se encontró app.py en {project}")

    detector_source = repo_root / "encryption_detection.py"
    detector_target = project / "encryption_detection.py"
    if detector_source.resolve() != detector_target.resolve():
        shutil.copy2(detector_source, detector_target)
    tests_dir = project / "tests"
    tests_dir.mkdir(exist_ok=True)
    test_source = repo_root / "tests" / "test_encryption_detection.py"
    test_target = tests_dir / "test_encryption_detection.py"
    if test_source.resolve() != test_target.resolve():
        shutil.copy2(test_source, test_target)

    source = app_path.read_text(encoding="utf-8")
    import_line = "from xdat_core import decompile_to_folder, compile_from_folder, parse_xdat\n"
    detector_import = "from encryption_detection import detect_encryption\n"
    if detector_import not in source:
        if source.count(import_line) != 1:
            raise SystemExit("No se pudo localizar de forma segura el bloque de imports de app.py")
        source = source.replace(import_line, import_line + detector_import, 1)

    old_decompile = """            data=source.read_bytes()
            windows,shortcuts=decompile_to_folder(data,PARTS_DIR)
"""
    new_decompile = """            data=source.read_bytes()
            detection=detect_encryption(data, source.name)
            if detection.encrypted is True:
                message=(f"Methos detectó cifrado Lineage II: {detection.description}.\\n\\n"
                         f"Versión: {detection.version}\\nFamilia: {detection.family}\\n"
                         f"\\n{detection.detail}\\n\\nNo se modificó el archivo original.")
                self.set_status(f'Cifrado detectado ({detection.version}); falta conectar el decodificador.',False)
                messagebox.showwarning('Cifrado detectado',message)
                return
            if detection.status == 'malformed_header':
                raise ValueError(f"{detection.description}. {detection.detail}")
            windows,shortcuts=decompile_to_folder(data,PARTS_DIR)
"""
    if old_decompile in source:
        source = source.replace(old_decompile, new_decompile, 1)
    elif "detection=detect_encryption(data, source.name)" not in source:
        raise SystemExit("No se pudo localizar el bloque de descompilación de forma segura")

    old_error = """            self.set_status('Error al descompilar.',False)
            messagebox.showerror('Error al descompilar',f'{e}\\n\\n{traceback.format_exc()}')
"""
    new_error = """            self.set_status('Error al descompilar.',False)
            prefix=''
            try:
                probe=detect_encryption(source.read_bytes(), source.name)
                prefix=(f"Detección: {probe.status}. {probe.detail}\\n"
                        f"Primeros 16 bytes: {source.read_bytes()[:16].hex(' ').upper()}\\n\\n")
            except Exception:
                pass
            messagebox.showerror('Error al descompilar',f'{prefix}{e}\\n\\n{traceback.format_exc()}')
"""
    if old_error in source:
        source = source.replace(old_error, new_error, 1)
    elif "probe=detect_encryption(source.read_bytes(), source.name)" not in source:
        raise SystemExit("No se pudo localizar el bloque de errores de descompilación")

    compile_marker = """        try:
            self.set_status(f'Compilando {len(xmls)} archivos XML…')
            data,windows,shortcuts=compile_from_folder(PARTS_DIR,original_data)
"""
    compile_replacement = """        try:
            if original_data is not None:
                detection=detect_encryption(original_data, source.name if source else '')
                if detection.encrypted is True:
                    raise ValueError(f"El original usa cifrado {detection.version} ({detection.family}). La capa de detección ya lo reconoce, pero todavía falta conectar su codificador. Se canceló la compilación para no generar un archivo incompatible.")
            self.set_status(f'Compilando {len(xmls)} archivos XML…')
            data,windows,shortcuts=compile_from_folder(PARTS_DIR,original_data)
"""
    if compile_marker in source:
        source = source.replace(compile_marker, compile_replacement, 1)
    elif "detection=detect_encryption(original_data" not in source:
        raise SystemExit("No se pudo localizar el bloque de compilación de forma segura")

    app_path.write_text(source, encoding="utf-8", newline="\n")
    print("Capa de detección aplicada a app.py")


if __name__ == "__main__":
    main()
