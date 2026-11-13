# Methos

**Methos** es una única aplicación de Windows x64 para trabajar con archivos de interfaz de Lineage II Interlude:

- **ARCHIVOS U:** importar, respaldar, descompilar, editar, compilar y validar paquetes Unreal `.u`.
- **ARCHIVOS XDAT:** importar, respaldar, extraer, editar, reconstruir y validar paquetes `.xdat`.

Ambos módulos forman parte de la misma aplicación, la misma ventana principal y el mismo instalador. El único ejecutable principal es `Methos.exe`; no existe un segundo producto XDAT.

## Repositorio oficial único

https://github.com/Methos48/Methos

La rama activa es `main`. La arquitectura, las rutas de código y las reglas para evitar separar el producto están documentadas en [docs/ARQUITECTURA.md](docs/ARQUITECTURA.md).

## Instalación

El lanzamiento oficial genera un único instalador Windows x64: `Methos-Setup.exe`. La aplicación incorpora los runtimes internos necesarios para los módulos U y XDAT. El usuario no debe instalar un programa XDAT separado.

## Estructura del proyecto

- `src/MethosUEditor/` — aplicación gráfica principal Methos (nombre técnico histórico de la carpeta).
- `src/MethosUEngine/` — motor Unreal/UCC y procesamiento de paquetes U.
- `tools/xdat/` — código XDAT canónico, detección de cifrado, pipeline y metadatos; `tools/xdat/maintenance/` contiene las capas que preparan el kit de Windows. La integración total del motor desde esta carpeta sigue pendiente.
- `methos_crypto/` — componentes históricos/experimentales de análisis y protección; el motor que compila la aplicación está en `src/MethosUEngine/`.
- `tests/` — pruebas automatizadas.
- `installer/Methos.iss` — único instalador del producto.
- `docs/` — arquitectura, manuales e investigación técnica.
- `reference-data/` — archivos de referencia para pruebas e investigación.

Los ZIP y archivos del cliente que todavía aparecen en la raíz son insumos históricos que algunos workflows utilizan. No deben moverse ni borrarse hasta actualizar y validar esos workflows. Consulta [docs/ARQUITECTURA.md](docs/ARQUITECTURA.md) antes de reorganizar rutas.

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
