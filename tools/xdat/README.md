# Módulo XDAT de Methos

Esta carpeta contiene la fuente Python canónica mantenida por el repositorio.

## Código

- `xdat_core.py`: lectura, análisis, extracción XML y reconstrucción XDAT. Se copia directamente al runtime embebido de Methos.
- `encryption_detection.py`: identificación explícita de cabeceras y protocolos.
- `auto_pipeline.py`: despacho de decodificación/codificación y publicación transaccional. Todavía no está conectado al puente C# de la interfaz.
- `package_origin_metadata.py`: manifiesto de origen, hash y protocolo registrado.
- `compare_build_kit.py`: auditoría de solo lectura del ZIP histórico.
- `extract_reference_fixtures.py`: extrae únicamente los directorios de referencia `xdat completo/` y `xdat por partes/`; no extrae ni ejecuta `app.py`, pruebas antiguas ni binarios del kit.

## Runtime de Methos

El workflow Windows copia directamente `tools/xdat/xdat_core.py` a `publish/XdatRuntime/xdat_core.py` y `src/Methos.App/XdatBridge.py` a `xdat_bridge.py`. El runtime se carga dentro de `Methos.exe`; no se distribuye una aplicación Python separada.

`auto_pipeline.py` todavía referencia el motor externo `l2encdec.exe`, pero ese pipeline no forma parte del runtime actual ni se presenta como una capacidad integrada en la interfaz C#. Su integración requiere un bloque posterior de desarrollo y pruebas.

## Archivo histórico y pruebas

`reference-data/kits/Methos-XDAT-Windows-Build-Kit.zip` se conserva como archivo de referencia. El workflow extrae solo los fixtures y ejecuta los tests desde el repositorio:

- `tests/`: detección, pipeline, metadatos, publicación y edición XML.
- `tools/xdat/tests/`: reconstrucción con el paquete de referencia.

La variable `XDAT_REFERENCE_ROOT` apunta a la carpeta temporal de fixtures. El kit histórico no aporta código fuente al runtime y su antigua interfaz `app.py` no se ejecuta.
