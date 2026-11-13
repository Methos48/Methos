# Auditoría de unificación de Methos

**Fecha:** 13-11-2026  
**Repositorio:** https://github.com/Methos48/Methos  
**Rama de cambios:** `work/unification-audit`  
**Revisión:** [PR #3](https://github.com/Methos48/Methos/pull/3)

## Regla de producto

Methos es una sola aplicación Windows. El único ejecutable principal de usuario es `Methos.exe`; la misma ventana contiene los módulos ARCHIVOS U y ARCHIVOS XDAT. Puede tener bibliotecas y runtimes internos separados, pero no debe distribuir aplicaciones, accesos directos ni instaladores independientes para U y XDAT.

## Estructura interna en revisión

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

## Migración XDAT pendiente

`tools/xdat/xdat_core.py` es la fuente canónica. La comparación automatizada encontró que el kit contiene tres archivos Python: `app.py` (sin equivalente en Methos), `xdat_core.py` y `tests/test_roundtrip.py`. La versión del repositorio añade compatibilidad Latin-1 y preservación de datos originales. La rama `work/xdat-runtime-source` cambia el workflow para copiar directamente la fuente canónica al runtime y extraer del ZIP solo los directorios de fixtures; deja de ejecutar o modificar `app.py`.

## Validación pendiente

La rama `work/unification-audit` normalizó nombres y rutas de proyectos. La rama `work/xdat-runtime-source` elimina el uso del código fuente del kit y ejecuta las pruebas desde el repositorio. Antes de integrar esta segunda etapa en `main` debe comprobarse:

1. Compilación Release de `src/Methos.App/Methos.App.csproj` para Windows x64.
2. Compilación y pruebas de `src/Methos.Engine/Methos.Engine.csproj` y `tests/Methos.Engine.Tests/`.
3. Ejecución de los workflows de protección y round-trip.
4. Generación del instalador único, instalación silenciosa y arranque de `Methos.exe`.
5. Ausencia de referencias activas a los nombres de proyectos anteriores en workflows y referencias de proyecto.

## Criterio de cierre

El ZIP histórico se conserva únicamente como archivo de fixtures de prueba; no se distribuye como aplicación ni como código fuente del runtime. No declarar finalizada la unificación hasta que el nuevo workflow y las pruebas anteriores pasen. No añadir nuevas funciones durante esta fase. Mantener intactos los archivos originales y las copias de respaldo de los usuarios.
