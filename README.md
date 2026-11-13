# Methos

**Methos** es una única aplicación de Windows x64 para trabajar con archivos de interfaz de Lineage II Interlude:

- **ARCHIVOS U:** importar, respaldar, descompilar, editar, compilar y validar paquetes Unreal `.u`.
- **ARCHIVOS XDAT:** importar, respaldar, extraer, editar, reconstruir y validar paquetes `.xdat`.

Ambos módulos forman parte de la misma aplicación y del mismo instalador: `Methos.exe`. No se distribuye un segundo ejecutable de aplicación para XDAT.

## Repositorio único

Todo el desarrollo activo se mantiene en:

https://github.com/Methos48/Methos-XDAT

El código de la aplicación de Windows está en `src/MethosUEditor`, el motor Unreal y UCC en `src/MethosUEngine`, y el motor XDAT y sus herramientas en la raíz y `tools/xdat`.

## Seguridad de archivos

- El archivo original seleccionado por el usuario no se modifica.
- Methos trabaja sobre una copia dentro de `Compilados`.
- Las compilaciones se validan antes de reemplazar el resultado de trabajo.
- Se conservan respaldos independientes para permitir recuperación.
- Si no se reconoce una protección o no se puede verificar el resultado, Methos se detiene sin generar un archivo aparentemente válido.

## Construcción y validación

El proyecto utiliza .NET 8, Windows Forms, el runtime UCC de Interlude y un runtime Python embebido para el motor XDAT.

Los workflows de GitHub Actions validan, entre otras cosas:

- compilación de la aplicación e instalador Windows x64;
- pruebas del motor Unreal/UCC y estructura de paquetes;
- extracción, edición y reconstrucción XDAT;
- instalación silenciosa y arranque de `Methos.exe`;
- presencia del runtime UCC y del motor XDAT integrado.

Una ejecución correcta de CI demuestra únicamente las pruebas que ese workflow ejecuta. No equivale a garantizar compatibilidad con todos los clientes, cifrados o paquetes personalizados. Los archivos modificados deben probarse primero en una copia del cliente.

## Estructura principal

- `src/MethosUEditor/` — aplicación gráfica Methos.
- `src/MethosUEngine/` — motor Unreal, UCC y reparaciones de UnrealScript.
- `methos_crypto/` — componentes de análisis y protección.
- `tools/xdat/` y `tests/` — motor, utilidades y pruebas XDAT.
- `installer/` — configuración del instalador Windows.
- `docs/` — documentación técnica y registros.
- `reference-data/` — paquetes de referencia que no deben confundirse con archivos de trabajo.

## Nombre del producto

El único producto en desarrollo y entrega es **Methos**.
