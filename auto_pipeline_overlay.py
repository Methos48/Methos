from __future__ import annotations
import shutil, sys
from pathlib import Path
def main():
    if len(sys.argv)!=2: raise SystemExit("Uso: auto_pipeline_overlay.py <carpeta-proyecto>")
    repo=Path(__file__).resolve().parent; project=Path(sys.argv[1]).resolve()
    shutil.copy2(repo/"auto_pipeline.py",project/"auto_pipeline.py")
    tests=project/"tests"; tests.mkdir(exist_ok=True)
    shutil.copy2(repo/"tests"/"test_auto_pipeline.py",tests/"test_auto_pipeline.py")
    app=project/"app.py"; source=app.read_text(encoding="utf-8")
    line="from xdat_core import decompile_to_folder, compile_from_folder, parse_xdat\n"
    old_paths = """APP_DIR = Path(__file__).resolve().parent
# Datos editables en Documentos: no requieren permisos de administrador.
DATA_DIR = Path.home() / 'Documents' / 'Methos XDAT'
COMPLETE_DIR = DATA_DIR / 'xdat completo'
PARTS_DIR = DATA_DIR / 'xdat por partes'
"""
    new_paths = """APP_DIR = Path(sys.executable).resolve().parent if getattr(sys, 'frozen', False) else Path(__file__).resolve().parent
DATA_DIR = APP_DIR / 'Archivos XDAT'
COMPLETE_DIR = DATA_DIR / 'Comprimido'
PARTS_DIR = DATA_DIR / 'Descomprimido'
"""
    if old_paths in source:
        source = source.replace(old_paths, new_paths, 1)
    elif "DATA_DIR = APP_DIR / 'Archivos XDAT'" not in source:
        raise SystemExit("No se localizaron las rutas de trabajo XDAT esperadas")
    if "from auto_pipeline import decode_xdat, encode_xdat" not in source:
        source=source.replace(line,line+"from auto_pipeline import decode_xdat, encode_xdat\n",1)
    source = source.replace("1. XDAT COMPLETO", "1. XDAT COMPRIMIDO")
    source = source.replace("Aquí se guardan los archivos .xdat.", "Aquí se colocan los archivos .xdat de entrada y se guardan los compilados.")
    source = source.replace("2. XDAT POR PARTES", "2. XDAT DESCOMPRIMIDO")
    source = source.replace("Aquí se guardan las ventanas y accesos directos XML.", "Aquí se guardan las ventanas y accesos directos XML extraídos.")
    source = source.replace("text='Abrir carpeta'", "text='Abrir Comprimido'", 1)
    source = source.replace("text='Abrir carpeta'", "text='Abrir Descomprimido'", 1)
    source = source.replace("ARCHIVOS POR PARTES", "ARCHIVOS XML DESCOMPRIMIDOS")
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
            windows,shortcuts=decompile_to_folder(plain_data,PARTS_DIR)
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
    old="""            if original_data is not None:
                detection=detect_encryption(original_data, source.name if source else '')
                if detection.encrypted is True:
                    raise ValueError(f"El original usa cifrado {detection.version} ({detection.family}). La capa de detección ya lo reconoce, pero todavía falta conectar su codificador. Se canceló la compilación para no generar un archivo incompatible.")
            self.set_status(f'Compilando {len(xmls)} archivos XML…')
            data,windows,shortcuts=compile_from_folder(PARTS_DIR,original_data)
"""
    new="""            self.set_status(f'Compilando {len(xmls)} archivos XML…')
            data,windows,shortcuts=compile_from_folder(PARTS_DIR,original_data)
            plain_compiled=data
            parse_xdat(plain_compiled)
            if original_detection is not None and original_detection.encrypted is True:
                data=encode_xdat(plain_compiled,source.name,original_detection.version,parse_xdat)
"""
    if old in source: source=source.replace(old,new,1)
    elif "data=encode_xdat(plain_compiled,source.name,original_detection.version,parse_xdat)" not in source: raise SystemExit("Bloque de compilación no localizado")
    source=source.replace("            parsed=parse_xdat(data)\n","            parsed=parse_xdat(plain_compiled)\n",1)
    app.write_text(source,encoding="utf-8",newline="\n")
    installer = project / "installer" / "MethosXDAT.iss"
    if installer.is_file():
        iss = installer.read_text(encoding="utf-8")
        iss = iss.replace('DestDir: "{userdocs}\\Methos XDAT\\xdat completo"', 'DestDir: "{app}\\Archivos XDAT\\Comprimido"')
        iss = iss.replace('DestDir: "{userdocs}\\Methos XDAT\\xdat por partes"', 'DestDir: "{app}\\Archivos XDAT\\Descomprimido"')
        iss = iss.replace('Name: "{userdocs}\\Methos XDAT\\xdat completo"', 'Name: "{app}\\Archivos XDAT\\Comprimido"')
        iss = iss.replace('Name: "{userdocs}\\Methos XDAT\\xdat por partes\\windows"', 'Name: "{app}\\Archivos XDAT\\Descomprimido\\windows"')
        iss = iss.replace('Name: "{userdocs}\\Methos XDAT\\xdat por partes\\shortcuts"', 'Name: "{app}\\Archivos XDAT\\Descomprimido\\shortcuts"')
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
    print("Carpetas XDAT: Archivos XDAT/Comprimido y Archivos XDAT/Descomprimido.")
if __name__=="__main__": main()
