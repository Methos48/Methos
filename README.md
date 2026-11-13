# Methos

**Methos** es una única aplicación de Windows x64 para trabajar con archivos de interfaz de Lineage II Interlude:

- **ARCHIVOS U:** importar, respaldar, descompilar, editar, compilar y validar paquetes Unreal `.u`.
- **ARCHIVOS XDAT:** importar, respaldar, extraer, editar, reconstruir y validar paquetes `.xdat`.

Ambos módulos forman parte de la misma aplicación, la misma ventana principal y el mismo instalador. El único ejecutable principal es `Methos.exe`; no existe un segundo producto XDAT.

## Repositorio oficial único

https://github.com/Methos48/Methos

La rama activa es `main`. La arquitectura y auditoría de unificación están en `docs/ARQUITECTURA.md` y `docs/AUDITORIA-UNIFICACION.md`. La arquitectura y las reglas de mantenimiento están en [docs/ARQUITECTURA.md](docs/ARQUITECTURA.md). Consulta también el [mapa de workflows](docs/WORKFLOWS.md) y el [inventario de kits y referencias de prueba](docs/ROOT-REFERENCES.md).

## Instalación

El lanzamiento oficial genera un único instalador Windows x64: `Methos-Setup.exe`. La aplicación incorpora los runtimes internos necesarios para los módulos U y XDAT. El usuario no debe instalar un programa XDAT separado.

## Estructura del proyecto

- `src/Methos.App/` — aplicación gráfica principal Methos.
- `src/Methos.Engine/` — biblioteca interna del motor Unreal/UCC, codecs y protecciones.
- `tools/xdat/` — código XDAT canónico, detección de cifrado, pipeline y metadatos; `tools/xdat/maintenance/` contiene las capas que preparan el kit de Windows. La integración total del motor desde esta carpeta sigue pendiente.
- `tests/` — pruebas automatizadas.
- `installer/Methos.iss` — único instalador del producto.
- `docs/` — arquitectura, manuales e investigación técnica.
- `reference-data/` — archivos de referencia para pruebas e investigación.

Los kits, binarios y paquetes de referencia están organizados bajo `reference-data/` y no se distribuyen automáticamente. Consulta [docs/ROOT-REFERENCES.md](docs/ROOT-REFERENCES.md) para conocer su función y [CONTRIBUTING.md](CONTRIBUTING.md) antes de modificar rutas.

## Seguridad de archivos

- El archivo original seleccionado por el usuario no se modifica.
- Methos trabaja sobre copias dentro de `Compilados`.
- Las compilaciones se validan antes de reemplazar el resultado de trabajo.
- Se conservan respaldos independientes para permitir recuperación.
- Si no se reconoce una protección o no se puede verificar el resultado, Methos debe detenerse sin generar un archivo aparentemente válido.

## Construcción y validación

El proyecto utiliza .NET 8, Windows Forms, el runtime UCC de Interlude y un runtime Python embebido para el motor XDAT.

El workflow oficial de distribución es [windows-release.yml](.github/workflows/windows-release.yml). Los workflows de validación comprueban componentes y operaciones específicas. Una ejecución correcta de CI demuestra únicamente las pruebas que ese workflow ejecuta; no garantiza compatibilidad con todos los clientes, cifrados o paquetes personalizados.

Antes de usar un paquete modificado en el juego, conserva el original y valida el resultado en una copia de prueba del cliente.
