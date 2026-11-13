# Módulo XDAT de Methos

Esta carpeta contiene el código fuente canónico del módulo XDAT integrado en la única aplicación **Methos**.

## Componentes mantenidos

- `xdat_core.py`: lectura, análisis, extracción XML y reconstrucción XDAT ct0.
- `encryption_detection.py`: identificación explícita de cabeceras y protocolos; no inventa claves ni declara compatible un cifrado desconocido.
- `auto_pipeline.py`: lógica de despacho y publicación transaccional, mantenida como módulo interno y cubierta por pruebas.
- `package_origin_metadata.py`: manifiesto de origen, hash y protocolo registrado.
- `../..` `src/Methos.App/XdatBridge.py`: puente que carga el motor canónico desde Methos.exe.
- `tests/` y `../../tests/test_*xdat*.py`: pruebas del motor y del pipeline.

## Regla de integración

El workflow Windows **no extrae ni ejecuta la aplicación Python histórica**. Tampoco ejecuta, empaqueta o instala `l2encdec.exe`. El runtime de Methos se prepara directamente desde `tools/xdat/xdat_core.py` y el puente `XdatBridge.py`.

El archivo ZIP de referencia que permanece en `reference-data/kits/` se usa exclusivamente como contenedor de fixtures binarios/XML para las pruebas de round-trip. `prepare_reference_fixtures.py` extrae únicamente `interface.xdat` y XML bajo las carpetas de referencia; rechaza rutas inseguras y no extrae aplicaciones, scripts, DLL ni ejecutables. No forma parte del runtime distribuido.

## Pruebas

En CI se compilan sintácticamente las fuentes canónicas y se ejecutan las pruebas de `tests/` y `tools/xdat/tests/` directamente desde el repositorio. Los fixtures se pasan mediante `XDAT_REFERENCE_ROOT`. La prueba compara la reconstrucción binaria con el archivo de referencia y verifica que una edición XML llegue al XDAT compilado.

## Alcance funcional actual

La integración en la aplicación utiliza el parser/compilador ct0 mediante el puente interno. La detección y el pipeline de protocolos cifrados se mantienen separados y sus variantes solo deben activarse en el runtime después de integrar y validar un motor compatible dentro de Methos. Un protocolo desconocido debe detenerse sin alterar el original.
