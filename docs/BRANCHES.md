# Organización de ramas de Methos

## Rama oficial

- `main`: única rama activa para desarrollo, integración, validación y distribución de Methos.

## Ramas históricas

Las ramas `migration/*`, `research/*` y `work/*` que puedan seguir visibles en GitHub son ramas de trabajos anteriores. No son líneas de producto, no contienen versiones oficiales y no deben utilizarse para continuar el desarrollo.

La existencia de esas referencias Git no cambia la regla: Methos tiene una sola fuente activa, el repositorio `Methos48/Methos`, rama `main`.

## Regla de mantenimiento

1. Todo cambio nuevo debe partir de `main`.
2. No crear ramas permanentes para separar U y XDAT: son módulos de la misma aplicación.
3. Antes de eliminar una rama histórica, comprobar que sus cambios estén integrados o que no contenga trabajo único que deba conservarse.
4. La distribución oficial se genera exclusivamente desde `main` mediante `windows-release.yml`.

## Auditoría de ramas históricas (13-11-2026)

Se compararon los árboles de las cinco ramas históricas con `main`:

- `migration/methos-unified`: su commit de retiro apunta al árbol Git vacío.
- `research/xdat-format-probe`: su commit de retiro apunta al árbol Git vacío; la fuente XDAT vigente está en `main`.
- `work/unification-audit`: conserva una instantánea anterior con documentación duplicada, un kit XDAT antiguo y overlays de mantenimiento retirados. La aplicación y las fuentes vigentes están en `main`.
- `work/xdat-runtime-source`: conserva versiones previas y herramientas temporales de extracción/comparación del kit antiguo; el extractor vigente y las fuentes canónicas están en `main`.
- `work/xdat-source-audit`: conserva una herramienta de comparación del kit antiguo y versiones previas de archivos que ya están consolidados en `main`.

**Resultado:** no se identificó una segunda aplicación ni código activo necesario que deba fusionarse desde esas ramas. La auditoría de contenido está completada; pueden eliminarse las cinco ramas históricas cuando el usuario lo decida, después de que la validación final de `main` termine correctamente. No eliminar `main`.
