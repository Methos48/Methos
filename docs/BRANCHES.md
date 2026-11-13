# Organización de ramas de Methos

## Rama oficial

- `main`: única rama activa para desarrollo, integración, validación y distribución de Methos.

## Ramas históricas

Las ramas `migration/*`, `research/*` y `work/*` enumeradas en la auditoría fueron ramas históricas de trabajos anteriores. El usuario ya las eliminó de GitHub; actualmente no deben existir como ramas activas.

La existencia de esas referencias Git no cambia la regla: Methos tiene una sola fuente activa, el repositorio `Methos48/Methos`, rama `main`.

## Regla de mantenimiento

1. Todo cambio nuevo debe partir de `main`.
2. No crear ramas permanentes para separar U y XDAT: son módulos de la misma aplicación.
3. Si en el futuro aparece una rama histórica, auditar sus commits únicos y conservar cualquier material técnico útil antes de eliminarla.
4. La distribución oficial se genera exclusivamente desde `main` mediante `windows-release.yml`.

## Auditoría de ramas históricas (13-11-2026)

Se compararon los árboles de las cinco ramas históricas con `main`:

- `migration/methos-unified`: su commit de retiro apunta al árbol Git vacío.
- `research/xdat-format-probe`: su commit de retiro apunta al árbol Git vacío; la fuente XDAT vigente está en `main`.
- `work/unification-audit`: conserva una instantánea anterior con documentación duplicada, un kit XDAT antiguo y overlays de mantenimiento retirados. La aplicación y las fuentes vigentes están en `main`.
- `work/xdat-runtime-source`: conserva versiones previas y herramientas temporales de extracción/comparación del kit antiguo; el extractor vigente y las fuentes canónicas están en `main`.
- `work/xdat-source-audit`: conserva una herramienta de comparación del kit antiguo y versiones previas de archivos que ya están consolidados en `main`.

**Resultado:** no se identificó una segunda aplicación ni código activo necesario que deba fusionarse desde esas ramas. Tras la validación final, el usuario eliminó las cinco ramas históricas. La consulta posterior a GitHub confirmó que `main` es la única rama restante. No eliminar `main`.
