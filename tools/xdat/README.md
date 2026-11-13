# Módulo XDAT de Methos

Esta carpeta es la ubicación canónica del código Python XDAT mantenido en el repositorio.

## Código

- `xdat_core.py`: lectura, análisis, extracción XML y reconstrucción XDAT.
- `encryption_detection.py`: identificación explícita de cabeceras y protocolos; no inventa claves ni afirma que un cifrado desconocido sea compatible.
- `auto_pipeline.py`: despacho de decodificación/codificación y publicación transaccional.
- `package_origin_metadata.py`: manifiesto de origen, hash y protocolo registrado.
- `validate_dunk3l_smartcrypt.py`: validación específica del paquete de referencia.

## Preparación del kit Windows

Los scripts de integración están en `maintenance/`. El workflow oficial los ejecuta sobre una copia temporal del kit XDAT histórico. No se debe ejecutar la modificación directamente sobre los archivos originales del ZIP.

## Fuente del motor en la compilación

`tools/xdat/xdat_core.py` es ahora la fuente canónica del parser/compilador ct0. El workflow Windows la copia sobre la versión incluida en el kit histórico antes de aplicar las capas de integración y comprueba que el SHA-256 de la copia de trabajo coincide con el archivo versionado en Methos. Los cambios de `xdat_core.py` también disparan la validación Windows.

El kit histórico todavía aporta la aplicación base (`app.py`), sus pruebas/fixtures y el binario de compatibilidad `l2encdec.exe`. Por tanto, el motor de formato ya tiene una fuente canónica, pero la migración completa del paquete XDAT no está cerrada hasta trasladar y validar esos componentes restantes. No borrar el ZIP ni afirmar que todo el paquete es autónomo antes de superar esa validación.

## Pruebas

Las pruebas Python compartidas están en `/tests`; las pruebas específicas del motor XDAT están en `tools/xdat/tests`. Ejecutar las pruebas sobre copias temporales y mantener intactos los paquetes de referencia.
