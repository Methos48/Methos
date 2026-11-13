# Reglas para contribuir a Methos

## Identidad del producto

Methos es una sola aplicación de Windows. El usuario inicia un único `Methos.exe`; los módulos U y XDAT se utilizan desde esa misma aplicación. No crear un segundo producto, ejecutable, instalador ni repositorio para ninguno de los formatos.

## Fuente oficial

- Repositorio único: `Methos48/Methos`
- Rama activa: `main`
- Aplicación: `src/Methos.App/`
- Motor Unreal/UCC y protección: `src/Methos.Engine/`
- Código XDAT: `tools/xdat/`
- Pruebas: `tests/` y `tools/xdat/tests/`
- Instalador único: `installer/Methos.iss`
- Distribución: `.github/workflows/windows-release.yml`

## Antes de cambiar rutas o carpetas

1. Buscar todas las referencias en código, proyectos, scripts, documentación y workflows.
2. Revisar los filtros `paths` de GitHub Actions para no dejar de ejecutar pruebas necesarias.
3. Para archivos binarios, registrar SHA-256 antes y después de moverlos.
4. No borrar ZIP, DLL, paquetes originales, fixtures o capturas sin revisar sus usos.
5. No modificar ni sobrescribir archivos originales de Lineage II durante las pruebas.
6. Ejecutar las pruebas correspondientes y revisar Windows Release.

## Motores internos

- `src/Methos.Engine/Methos.Engine.csproj` es el motor C# utilizado por la aplicación.
- `tools/xdat/` contiene el código fuente XDAT mantenido por el repositorio.
- `src/Methos.App/XdatBridge.cs` y `XdatBridge.py` conectan las funciones XDAT con la aplicación principal.

Las bibliotecas, scripts internos y runtimes no son productos independientes. No crear interfaces, instaladores o accesos directos separados para ellos.

## Datos de referencia

`reference-data/` conserva paquetes y fixtures necesarios para pruebas e investigación. `Methos-XDAT-Reference-Fixtures.zip` es exclusivamente un archivo de datos de prueba; no contiene ni distribuye una aplicación. No eliminar referencias hasta que las pruebas que dependen de ellas tengan una alternativa validada.

## Documentación y estado

Cada cambio estructural debe actualizar `docs/ARQUITECTURA.md`, `docs/WORKFLOWS.md`, el README cuando afecte a instalación y `PROGRESO-METHOS.md`. No declarar una función terminada por una compilación exitosa si sus pruebas funcionales siguen pendientes.
