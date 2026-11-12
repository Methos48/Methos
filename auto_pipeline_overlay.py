from __future__ import annotations
import shutil, sys
from pathlib import Path
def main():
    if len(sys.argv)!=2: raise SystemExit("Uso: auto_pipeline_overlay.py <carpeta-proyecto>")
    repo=Path(__file__).resolve().parent; project=Path(sys.argv[1]).resolve()
    shutil.copy2(repo/"auto_pipeline.py",project/"auto_pipeline.py")
    shutil.copy2(repo/"package_origin_metadata.py",project/"package_origin_metadata.py")
    tests=project/"tests"; tests.mkdir(exist_ok=True)
    shutil.copy2(repo/"tests"/"test_auto_pipeline.py",tests/"test_auto_pipeline.py")
    shutil.copy2(repo/"tests"/"test_package_origin_metadata.py",tests/"test_package_origin_metadata.py")
    app=project/"app.py"; source=app.read_text(encoding="utf-8")
    line="from xdat_core import decompile_to_folder, compile_from_folder, parse_xdat\n"
    old_paths = """APP_DIR = Path(__file__).resolve().parent
# Datos editables en Documentos: no requieren permisos de administrador.
DATA_DIR = Path.home() / 'Documents' / 'Methos XDAT'
COMPLETE_DIR = DATA_DIR / 'xdat completo'
PARTS_DIR = DATA_DIR / 'xdat por partes'
"""
    new_paths = """APP_DIR = Path(sys.executable).resolve().parent if getattr(sys, 'frozen', False) else Path(__file__).resolve().parent
WORK_ROOT = APP_DIR.parent if getattr(sys, 'frozen', False) else APP_DIR
DATA_DIR = WORK_ROOT / 'Compilados' / 'XDAT'
COMPLETE_DIR = DATA_DIR / 'Archivo'
PARTS_DIR = DATA_DIR / 'Decopilado'
"""
    if old_paths in source:
        source = source.replace(old_paths, new_paths, 1)
    elif "DATA_DIR = APP_DIR / 'Archivos XDAT'" not in source:
        raise SystemExit("No se localizaron las rutas de trabajo XDAT esperadas")
    if "from auto_pipeline import decode_xdat, encode_xdat" not in source:
        source=source.replace(line,line+"from auto_pipeline import decode_xdat, encode_xdat\nfrom package_origin_metadata import save_origin_record, load_and_validate_origin, require_recorded_protocol\n",1)
    if "import xml.etree.ElementTree as ET" not in source:
        source = source.replace("import tkinter", "import xml.etree.ElementTree as ET\nimport tkinter", 1)
    if "import shutil" not in source:
        source = source.replace("import tkinter", "import shutil\nimport tkinter", 1)
    source = source.replace(
        "            self.set_status(f'Compilando {len(xmls)} archivos XML…')",
        """            xml_errors=[]
            for xml_file in xmls:
                try:
                    ET.parse(xml_file)
                except Exception as xml_error:
                    xml_errors.append(f"{Path(xml_file).name}: {type(xml_error).__name__}: {xml_error}")
            if xml_errors:
                report=("COMPILACIÓN FALLIDA\\n\\nArchivos con errores:\\n- "
                        + "\\n- ".join(
                            item.split(": ", 1)[0] + "\\n  Motivo: " + item.split(": ", 1)[1]
                            if ": " in item else item
                            for item in xml_errors
                        ))
                self.set_status(f'{len(xml_errors)} archivo(s) XML con errores; compilación cancelada.',False)
                messagebox.showerror('Informe de archivos XML',report)
                return
            self.set_status(f'Compilando {len(xmls)} archivos XML…')""", 1)
    source = source.replace("1. XDAT COMPLETO", "1. ARCHIVO XDAT")
    source = source.replace("Aquí se guardan los archivos .xdat.", "Aquí se colocan los archivos .xdat de entrada y se guardan los compilados.")
    source = source.replace("2. XDAT POR PARTES", "2. DECOPILADO XDAT")
    source = source.replace("Aquí se guardan las ventanas y accesos directos XML.", "Aquí se guardan las ventanas y accesos directos XML extraídos.")
    source = source.replace("text='Abrir carpeta'", "text='Abrir Archivo'", 1)
    source = source.replace("text='Abrir carpeta'", "text='Abrir Decopilado'", 1)
    source = source.replace("ARCHIVOS POR PARTES", "ARCHIVOS XML DECOPILADOS")
    old="""            data=source.read_bytes()
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
    new="""            data=source.read_bytes()
            plain_data,detection=decode_xdat(source,data,parse_xdat)
            if PARTS_DIR.exists():
                shutil.rmtree(PARTS_DIR)
            PARTS_DIR.mkdir(parents=True,exist_ok=True)
            windows,shortcuts=decompile_to_folder(plain_data,PARTS_DIR)\n            manifest=save_origin_record(PARTS_DIR,source,data,detection,file_kind="XDAT")\n            print(f"Perfil de origen guardado: {manifest}")
"""
    if old in source: source=source.replace(old,new,1)
    elif "plain_data,detection=decode_xdat(source,data,parse_xdat)" not in source: raise SystemExit("Bloque de descompilación no localizado")
    old="""        if source and source.exists():
            try: original_data=source.read_bytes()
            except Exception as e:
                messagebox.showerror('No se pudo leer el XDAT original',str(e)); return
"""
    new="""        original_detection=None
        if source and source.exists():
            try:
                raw_original=source.read_bytes()
                original_data,original_detection=decode_xdat(source,raw_original,parse_xdat)
            except Exception as e:
                messagebox.showerror('No se pudo preparar el XDAT original',str(e)); return
"""
    if old in source: source=source.replace(old,new,1)
    elif "raw_original=source.read_bytes()" not in source: raise SystemExit("Lectura del original no localizada")
    compile_start = source.find("            if original_data is not None:", source.find("def compile"))
    compile_call = "            data,windows,shortcuts=compile_from_folder(PARTS_DIR,original_data)"
    compile_call_start = source.find(compile_call, compile_start)
    if compile_start < 0 or compile_call_start < 0:
        raise SystemExit("No se pudo localizar de forma segura la llamada de compilación XDAT")
    compile_end = compile_call_start + len(compile_call)
    compile_replacement = """            self.set_status(f'Compilando {len(xmls)} archivos XML…')
            data,windows,shortcuts=compile_from_folder(PARTS_DIR,original_data)
            plain_compiled=data
            parse_xdat(plain_compiled)
            if source is not None and source.exists():
                manifest_path=PARTS_DIR / ".methos" / (source.name + ".json")
                if manifest_path.is_file():
                    immutable_original=manifest_path.parent / "originals" / source.name
                    if not immutable_original.is_file():
                        raise ValueError("Falta la copia original inmutable de Methos; no se puede validar el perfil.")
                    origin=load_and_validate_origin(manifest_path,immutable_original)
                    if original_detection is not None and original_detection.encrypted is True:
                        require_recorded_protocol(origin,original_detection.version)
                if original_detection is not None and original_detection.encrypted is True:
                    if not manifest_path.is_file():
                        raise ValueError("Falta el manifiesto de origen. No se codificará el XDAT con un protocolo supuesto.")
                    data=encode_xdat(plain_compiled,source.name,original_detection.version,parse_xdat)"""
    source = source[:compile_start] + compile_replacement + source[compile_end:]
    parse_marker = "            parsed=parse_xdat(data)\n"
    parse_replacement = """            parsed=parse_xdat(plain_compiled)\n"""

    if parse_marker in source:
        source=source.replace(parse_marker,parse_replacement,1)
    elif "backup_root=Path.home() / \"AppData\"" not in source:
        raise SystemExit("No se localizó el punto seguro para sustituir el contenido Archivo XDAT")
    commit_marker = "            if temp.read_bytes()!=data: raise ValueError('El archivo temporal no coincide con el resultado compilado.')\\n"
    commit_replacement = commit_marker + """            # Publicar solo después de validar el temporal; no borrar el temporal al limpiar Archivo.
            backup_root=Path.home() / "AppData" / "Local" / "Methos" / "Backups" / "XDAT"
            backup_root.mkdir(parents=True,exist_ok=True)
            if COMPLETE_DIR.exists():
                for old_file in COMPLETE_DIR.rglob("*"):
                    if old_file.is_file() and old_file != temp:
                        backup_name=f"{old_file.stem}_{__import__('datetime').datetime.now().strftime('%Y%m%d_%H%M%S_%f')}_{old_file.name}"
                        shutil.copy2(old_file,backup_root/backup_name)
                for old_item in COMPLETE_DIR.iterdir():
                    if old_item.is_dir(): shutil.rmtree(old_item)
                    elif old_item != temp: old_item.unlink()
            COMPLETE_DIR.mkdir(parents=True,exist_ok=True)
"""
    if commit_marker in source:
        source=source.replace(commit_marker,commit_replacement,1)
    elif "Publicar solo después de validar el temporal" not in source:
        raise SystemExit("No se localizó el punto seguro para publicar XDAT")
    source = source.replace(
        "messagebox.showerror('Error al descompilar',f'{prefix}{e}\\n\\n{traceback.format_exc()}')",
        "messagebox.showerror('Error al descompilar',f'{e}\\n\\n{traceback.format_exc()}')"
    )
    app.write_text(source,encoding="utf-8",newline="\n")
    installer = project / "installer" / "MethosXDAT.iss"
    if installer.is_file():
        iss = installer.read_text(encoding="utf-8")
        iss = iss.replace('DestDir: "{userdocs}\\Methos XDAT\\xdat completo"', 'DestDir: "{app}\\Compilados\\XDAT\\Archivo"')
        iss = iss.replace('DestDir: "{userdocs}\\Methos XDAT\\xdat por partes"', 'DestDir: "{app}\\Compilados\\XDAT\\Decopilado"')
        iss = iss.replace('Name: "{userdocs}\\Methos XDAT\\xdat completo"', 'Name: "{app}\\Compilados\\XDAT\\Archivo"')
        iss = iss.replace('Name: "{userdocs}\\Methos XDAT\\xdat por partes\\windows"', 'Name: "{app}\\Compilados\\XDAT\\Decopilado\\windows"')
        iss = iss.replace('Name: "{userdocs}\\Methos XDAT\\xdat por partes\\shortcuts"', 'Name: "{app}\\Compilados\\XDAT\\Decopilado\\shortcuts"')
        installer.write_text(iss, encoding="utf-8", newline="\n")
    else:
        raise SystemExit("No se encontró installer/MethosXDAT.iss")
    bat_path=project/"build_windows.bat"; bat=bat_path.read_text(encoding="utf-8")
    old='py -3.12 -m PyInstaller --noconfirm --clean --windowed --onedir --name "Methos XDAT" app.py'
    new='py -3.12 -m PyInstaller --noconfirm --clean --windowed --onedir --name "Methos XDAT" --add-binary "l2encdec.exe;." app.py'
    if old in bat: bat=bat.replace(old,new,1)
    elif new not in bat: raise SystemExit("No se pudo añadir el motor a PyInstaller")
    bat_path.write_text(bat,encoding="utf-8",newline="\n")
    print("Despacho automático integrado y motor incluido en el paquete Windows.")
    print("Carpetas XDAT: Compilados/XDAT/Archivo y Compilados/XDAT/Decopilado.")
if __name__=="__main__": main()
