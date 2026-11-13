# Methos

**Methos es una sola aplicación de Windows x64** para trabajar con los dos formatos de interfaz de Lineage II Interlude:

- **ARCHIVOS U:** importar, respaldar, descompilar, editar, compilar y validar paquetes Unreal `.u`.
- **ARCHIVOS XDAT:** importar, respaldar, extraer, editar, reconstruir y validar paquetes `.xdat`.

Los dos módulos están dentro de la misma ventana y del mismo producto. El único ejecutable principal es `Methos.exe` y el único instalador es `Methos-Setup.exe`.

## Única fuente oficial

**Repositorio:** https://github.com/Methos48/Methos  
**Rama activa:** `main`

Este es el único repositorio desde el que se desarrolla, valida y distribuye Methos. No se deben crear ni utilizar repositorios separados para los módulos U o XDAT. Las ramas distintas de `main` son referencias históricas y no deben recibir desarrollo nuevo.

## Instalación

El lanzamiento oficial genera un único instalador Windows x64: `Methos-Setup.exe`. Los runtimes necesarios se incorporan a la instalación; el usuario no debe instalar aplicaciones auxiliares para editar U o XDAT.

## Estructura del proyecto

- `src/Methos.App/` — aplicación gráfica principal y conexión de los módulos.
- `src/Methos.Engine/` — biblioteca interna del motor Unreal/UCC, codecs y protecciones.
- `tools/xdat/` — código fuente mantenido del motor XDAT, detección de cifrado, pipeline, metadatos y preparación de fixtures.
- `tests/` y `tools/xdat/tests/` — pruebas automatizadas.
- `installer/Methos.iss` — definición del único instalador.
- `docs/` — arquitectura, manuales e investigación técnica.
- `reference-data/` — archivos originales de referencia y datos de prueba; no son aplicaciones ni se instalan automáticamente.
- `third_party/` — dependencias externas y sus avisos de licencia.

## Seguridad de archivos

- El archivo original seleccionado por el usuario no se modifica.
- Methos trabaja sobre copias dentro de `Compilados`.
- Las compilaciones se validan antes de reemplazar el resultado de trabajo.
- Se conservan respaldos independientes para permitir recuperación.
- Si no se reconoce una protección o no se puede verificar el resultado, Methos debe detenerse sin generar un archivo aparentemente válido.

## Construcción y validación

El proyecto utiliza .NET 8, Windows Forms, el runtime UCC de Interlude y un runtime Python embebido para las funciones XDAT integradas.

El workflow oficial de distribución es [windows-release.yml](.github/workflows/windows-release.yml). Los workflows de validación comprueban componentes y operaciones específicas. Una ejecución correcta de CI demuestra únicamente las pruebas que ese workflow ejecuta; no garantiza compatibilidad con todos los clientes, cifrados o paquetes personalizados.

El parser/compilador XDAT ct0 está conectado mediante el puente interno de Methos. La integración de todos los protocolos cifrados detectados sigue siendo una tarea funcional pendiente y no debe presentarse como compatibilidad universal.

Antes de usar un paquete modificado en el juego, conserva el original y valida el resultado en una copia de prueba del cliente.

## Documentación de mantenimiento

- [Arquitectura](docs/ARQUITECTURA.md)
- [Auditoría de unificación](docs/AUDITORIA-UNIFICACION.md)
- [Workflows](docs/WORKFLOWS.md)
- [Inventario de referencias y fixtures](docs/ROOT-REFERENCES.md)
- [Reglas para contribuir](CONTRIBUTING.md)
- [Registro de progreso](PROGRESO-METHOS.md)
