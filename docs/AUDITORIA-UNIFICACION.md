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

El código mantenido está en `tools/xdat/`, pero el workflow de lanzamiento todavía extrae `reference-data/kits/Methos-XDAT-Windows-Build-Kit.zip`, aplica las capas de mantenimiento y copia el `xdat_core.py` preparado desde el kit. No se debe retirar ese ZIP ni reemplazar el runtime distribuido hasta comparar sus funciones con `tools/xdat/`, migrar lo que falte y superar las pruebas de reconstrucción/round-trip.

## Validación pendiente

La rama `work/unification-audit` actualiza los nombres y rutas de proyectos, pruebas, workflows y documentación. Antes de integrar en `main` debe comprobarse:

1. Compilación Release de `src/Methos.App/Methos.App.csproj` para Windows x64.
2. Compilación y pruebas de `src/Methos.Engine/Methos.Engine.csproj` y `tests/Methos.Engine.Tests/`.
3. Ejecución de los workflows de protección y round-trip.
4. Generación del instalador único, instalación silenciosa y arranque de `Methos.exe`.
5. Ausencia de referencias activas a los nombres de proyectos anteriores en workflows y referencias de proyecto.

## Criterio de cierre

No declarar finalizada la unificación hasta que los workflows y las pruebas anteriores pasen en la rama que contiene los cambios. No añadir nuevas funciones durante esta fase. Mantener intactos los archivos originales y las copias de respaldo de los usuarios.
