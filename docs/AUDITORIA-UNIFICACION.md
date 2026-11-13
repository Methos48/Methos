# Auditoría de unificación de Methos

**Fecha:** 13-11-2026  
**Repositorio:** https://github.com/Methos48/Methos  
**Rama principal:** `main`  
**Estado:** unificación estructural integrada y validada por Windows Release

## Regla de producto

Methos es una sola aplicación Windows. El único ejecutable principal de usuario es `Methos.exe`; la misma ventana contiene los módulos ARCHIVOS U y ARCHIVOS XDAT. Puede tener bibliotecas y runtimes internos separados, pero no debe distribuir aplicaciones, accesos directos ni instaladores independientes para U y XDAT.

## Estructura interna consolidada

| Responsabilidad | Ruta |
|---|---|
| Aplicación principal | `src/Methos.App/Methos.App.csproj` |
| Namespace de la aplicación | `Methos.App` |
| Motor Unreal/UCC | `src/Methos.Engine/Methos.Engine.csproj` |
| Namespace del motor | `Methos.Engine` |
| Pruebas del motor | `tests/Methos.Engine.Tests/` |
| Puente XDAT interno | `src/Methos.App/XdatBridge.cs` y `XdatBridge.py` |
| Motor XDAT mantenido | `tools/xdat/` |
| Instalador único | `installer/Methos.iss` |

La aplicación referencia el motor como biblioteca interna. Esto sigue siendo un solo programa y no constituye un segundo ejecutable de producto.

## Consolidación del motor C#

La carpeta paralela `methos_crypto/` fue retirada de la rama principal por la consolidación del motor. Se conserva una sola fuente C# del motor U. La auditoría previa encontró 20 archivos idénticos y cuatro variantes; se eligió el motor usado por la aplicación y las pruebas, que incluye las reparaciones y diagnósticos más completos.

## Consolidación XDAT completada

`tools/xdat/xdat_core.py` es la fuente canónica del runtime. El workflow copia esa fuente directamente y carga el puente interno `XdatBridge.py`. El antiguo kit fue sustituido por `Methos-XDAT-Reference-Fixtures.zip`, que conserva únicamente el paquete binario y los XML de referencia necesarios para pruebas. El extractor rechaza rutas inseguras y no extrae ni ejecuta `app.py`, DLL o ejecutables. Los antiguos overlays de mantenimiento se retiraron. Las pruebas Python se ejecutan desde `tests/` y `tools/xdat/tests/` contra las fuentes del repositorio.

`auto_pipeline.py` y `package_origin_metadata.py` permanecen en el repositorio; el pipeline de protocolos cifrados aún no está conectado al puente C# y no forma parte del runtime distribuido actual.

## Validación de la unificación

Windows Release [43401463785](https://github.com/Methos48/Methos/actions/runs/43401463785) terminó SUCCESS en 27 pasos: compilación Release Windows x64, pruebas U/XDAT, runtime Python embebido, creación del instalador, instalación silenciosa y arranque de `Methos.exe`.

- El instalador `Methos-Windows-Installer-x64` se publicó como artefacto ID `14024748596` (77,768,945 bytes).
- El ZIP de referencia se redujo a fixtures; el workflow no distribuye `app.py` ni `l2encdec.exe`.
- La estructura activa no contiene carpetas/proyectos `methos_crypto`, `MethosUEditor` ni `MethosUEngine`.

## Incidencia funcional separada

El workflow `Silent Dark U and XDAT round-trip` continúa fallando durante la compilación U con `InventoryWnd.uc(1529): Error, Missing function name`. El mismo error se observó en el run `43400534612`, anterior a la unificación, por lo que no se atribuye al cambio estructural. El flujo aborta antes de ejecutar la fase XDAT. Resolverlo en la siguiente etapa de validación funcional.

## Criterio de cierre

La unificación estructural de Methos está completada y validada por Windows Release. Mantener el producto como una sola aplicación y un solo instalador. No añadir capacidades nuevas ni afirmar soporte de cifrados no probados. Conservar los archivos originales y copias de seguridad de los usuarios.
