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
