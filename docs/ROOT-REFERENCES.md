# Inventario de referencias de Methos

Los archivos de esta sección son **material de prueba e investigación**, no componentes que se instalen automáticamente con Methos.exe. Se conservan con sus bytes originales y se organizan por función.

## Kits de compilación

| Ruta | Uso |
|---|---|
| `reference-data/kits/Compilador-U.zip` | Kit UCC/Interlude usado por compilación y validación Unreal. |
| `reference-data/kits/Methos-XDAT-Windows-Build-Kit.zip` | Kit histórico del que Windows Release aún extrae la aplicación base y el motor XDAT. Dependencia pendiente de migración. |
| `reference-data/kits/Carpeta-interface-u.zip` | Fuentes Unreal de referencia usadas por pruebas de compilación. |

## Cliente Lineage II

`reference-data/client/lineage2/` conserva archivos del cliente y recursos relacionados: `interface.u`, `interface.xdat`, `l2.exe`, `l2.ini`, DLL, `system.zip`, `engine.zip` y `sysstring-e.dat`. Los workflows de inspección los leen desde esta carpeta. No se deben incorporar automáticamente a Methos.exe.

## Runtime UCC

`reference-data/ucc-runtime/System/` contiene el runtime/archivos de soporte UCC que usa la prueba Silent Dark. No es un segundo producto ni un runtime que el usuario tenga que instalar por separado.

## Paquetes y fixtures

- `reference-data/packages/animal/`, `berserker/` y `l2sudamerica/`: pares de paquetes de distintas referencias.
- `reference-data/packages/silent-dark/`: paquetes y fuentes de referencia para pruebas de ida y vuelta U/XDAT.
- `reference-data/packages/dunk3l/Dunk3lFX_ByBALENCIAGA.u`: paquete Unreal de referencia.
- `reference-data/Interface-UEditor.u`: paquete Unreal de referencia usado en inspecciones.
- `reference-data/visuals/`: capturas de referencia visual.

## Dependencias externas

`third_party/` contiene dependencias externas utilizadas por herramientas; conservar sus licencias y avisos.

## Reglas

1. Mantener los archivos originales sin modificaciones.
2. Al mover un archivo binario, conservar su blob Git y verificar que el SHA-256 no cambie.
3. Actualizar las rutas y filtros `paths` de todos los workflows que lo consumen.
4. No copiar referencias al instalador salvo que exista una necesidad explícita y documentada.
5. No borrar un kit histórico hasta que su reemplazo integrado supere las mismas pruebas.
