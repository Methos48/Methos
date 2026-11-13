# Archivos de referencia conservados en la raíz

La raíz todavía contiene insumos de clientes Lineage II, kits de compilación y paquetes de prueba. No todos forman parte de Methos.exe. Se conservan temporalmente en su ubicación actual porque varios workflows los consumen mediante rutas explícitas.

## Kits y paquetes de fuente

| Archivo | Uso conocido |
|---|---|
| `Compilador  U.zip` | Kit UCC/Interlude usado por workflows de compilación y validación Unreal. |
| `Methos_XDAT_Windows_Build_Kit_v1.0 (1).zip` | Kit histórico del que el workflow Windows extrae la aplicación fuente y el motor XDAT para preparar el runtime. Es una dependencia pendiente de migrar. |
| `Carpeta interface.u.zip` | Paquete de fuentes Unreal de referencia; revisar su consumidor exacto antes de moverlo. |
| `engine.zip`, `system.zip` | Archivos del cliente usados por workflows de inspección de runtime, DLL y protección. |

## Archivos del cliente y paquetes de prueba

En la raíz hay referencias como `interface.u`, `interface.xdat`, `l2.exe`, `l2.ini`, `Core.dll`, `Core.u`, `Engine.u`, `Engine.int`, `Window.dll`, `ifc23.dll`, `npkcrypt.dll`, `npkscrypt.dll`, `ogg.dll`, `vorbis.dll`, `vorbisfile.dll`, `dsetup.dll` y `sysstring-e.dat`. Se usan en distintos análisis del cliente y pruebas de protección. No se deben incluir automáticamente en Methos.exe.

También hay paquetes de referencia como `Dunk3lFX_ByBALENCIAGA.u` y una captura PNG. Mantenerlos como evidencia/fixtures, no como código de producto.

## Carpetas de referencia

- `System/`: runtime y archivos de soporte UCC/Interlude utilizados por pruebas.
- `animal/`, `berserker/`, `l2sudamerica/`: pares de paquetes U/XDAT de distintas referencias.
- `silent dark/`: paquetes y archivos fuente de referencia para pruebas de ida y vuelta U/XDAT.
- `reference-data/`: paquete Unreal de referencia ya clasificado.
- `third_party/`: dependencias externas; conservar sus licencias y avisos.

## Regla para la siguiente reorganización

Antes de mover un archivo, actualizar todos los workflows que lo leen, comprobar filtros `paths`, ejecutar las pruebas afectadas y verificar el artefacto resultante. Los paquetes binarios deben moverse conservando sus bytes y hashes SHA-256. No renombrar ni volver a comprimir paquetes originales como sustituto de una migración.
