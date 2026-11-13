# Estado de las ramas de Methos

## Única rama activa

- `main`: fuente oficial y única rama para cambios del producto, validaciones y lanzamientos.

## Ramas históricas o experimentales

Estas ramas no son fuentes activas de Methos y no deben usarse para compilar ni publicar el producto:

- `migration/methos-unified`: rama temporal de migración; su último commit indica que la migración quedó retirada.
- `research/xdat-format-probe`: investigación XDAT integrada; su último commit indica que las fuentes unificadas ya están en `main`.
- `work/unification-audit`: propuesta experimental para renombrar proyectos/carpetas a `Methos.App` y `Methos.Engine`. No está integrada ni validada en el flujo oficial; no continuar allí sin una decisión explícita y una migración completa de proyectos, workflows, instalador y pruebas.

## Regla

Todo cambio futuro debe partir de `main`. Antes de reactivar una rama histórica, comparar sus cambios con `main`, revisar si reintroduce rutas antiguas y ejecutar la validación Windows completa. La presencia de una rama histórica no significa que haya otro producto o repositorio.
