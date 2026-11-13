# Inventario de referencias y datos de prueba de Methos

Esta carpeta conserva archivos originales de Lineage II, paquetes de muestra y datos necesarios para reproducir pruebas. **No contiene aplicaciones alternativas ni ejecutables de producto.** Los archivos de esta sección no se instalan automáticamente con `Methos.exe`.

## Kits y fuentes de referencia

| Ruta | Uso actual |
|---|---|
| `reference-data/kits/Compilador-U.zip` | Archivos UCC/Interlude usados para compilar y validar paquetes Unreal `.u`. El workflow extrae solo los componentes requeridos. |
| `reference-data/kits/Carpeta-interface-u.zip` | Fuentes Unreal de referencia para pruebas y análisis. |
| `reference-data/kits/Methos-XDAT-Reference-Fixtures.zip` | Archivo de datos de prueba XDAT. Contiene el paquete `interface.xdat` y sus XML; no contiene código Python, ejecutables, DLL ni instaladores. |

## Cliente Lineage II

`reference-data/client/lineage2/` conserva muestras del cliente y recursos relacionados, como `interface.u`, `interface.xdat`, ejecutables, DLL, archivos de configuración y archivos ZIP. Se utilizan para inspección y pruebas controladas. No se incorporan automáticamente al instalador.

## Runtime UCC

`reference-data/ucc-runtime/System/` contiene archivos de soporte UCC conservados para las pruebas de compilación y análisis. El usuario no debe instalar un segundo editor ni configurar un producto separado.

## Paquetes de prueba

- `reference-data/packages/animal/`, `berserker/` y `l2sudamerica/`: paquetes de distintas referencias.
- `reference-data/packages/silent-dark/`: paquetes y fuentes usados en pruebas de ida y vuelta U/XDAT.
- `reference-data/packages/dunk3l/`: paquete Unreal de referencia.
- `reference-data/Interface-UEditor.u`: paquete Unreal usado en inspecciones.
- `reference-data/visuals/`: capturas visuales de referencia.

## Dependencias externas

`third_party/` contiene dependencias externas utilizadas por Methos o por sus herramientas de validación. Conservar sus licencias y avisos en `THIRD-PARTY-NOTICES.txt`.

## Reglas de conservación

1. Mantener los archivos originales sin modificaciones.
2. Al mover un archivo binario, registrar y verificar su SHA-256.
3. Actualizar rutas y filtros `paths` de todos los workflows que consumen un archivo movido.
4. No copiar referencias al instalador salvo que exista una necesidad explícita y documentada.
5. No confundir archivos de referencia con módulos, aplicaciones o ejecutables del producto.
6. No borrar datos necesarios para reproducir pruebas sin reemplazarlos y validar el flujo completo.
