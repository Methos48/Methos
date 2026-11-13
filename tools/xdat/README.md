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

La fuente del repositorio está aquí, pero el workflow Windows todavía extrae `xdat_core.py` y la aplicación base del kit ZIP histórico antes de preparar el runtime embebido. Por eso este repositorio aún no tiene una única fuente efectiva de todos los archivos XDAT distribuidos.

La migración completa debe comparar los archivos del kit con los mantenidos aquí, copiar las funciones faltantes, ejecutar las pruebas y comprobar el instalador final. No borrar el ZIP ni afirmar que el motor está totalmente unificado antes de superar esa validación.

## Pruebas

Las pruebas Python compartidas están en `/tests`; las pruebas específicas del motor XDAT están en `tools/xdat/tests`. Ejecutar las pruebas sobre copias temporales y mantener intactos los paquetes de referencia.
