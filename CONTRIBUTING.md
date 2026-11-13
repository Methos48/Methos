# Reglas para contribuir a Methos

## Identidad del producto

Methos es una sola aplicación de Windows. El usuario inicia un único `Methos.exe`; los módulos U y XDAT se abren dentro de esa misma aplicación. No crear un segundo producto, ejecutable, instalador ni repositorio para XDAT.

## Fuente oficial

- Repositorio único: `Methos48/Methos`
- Rama de integración: `main` (única rama activa; ver [estado de ramas](docs/BRANCHES.md))
- Aplicación: `src/MethosUEditor/`
- Motor Unreal/UCC y protección: `src/MethosUEngine/`
- Motor y soporte XDAT: `tools/xdat/`
- Pruebas: `tests/` y `tools/xdat/tests/`
- Instalador único: `installer/Methos.iss`
- Distribución: `.github/workflows/windows-release.yml`

## Antes de cambiar rutas o carpetas

1. Buscar todas las referencias en código, proyectos, scripts, documentación y workflows.
2. Revisar los filtros `paths` de GitHub Actions: una ruta mal actualizada puede dejar de ejecutar una prueba necesaria.
3. Para archivos binarios, registrar SHA-256 antes y después de moverlos.
4. No borrar ZIP, DLL, paquetes originales, fixtures o capturas solo porque parezcan duplicados.
5. No modificar ni sobrescribir archivos originales de Lineage II durante pruebas.
6. Ejecutar las pruebas correspondientes y revisar el resultado del workflow de Windows Release.

## Motor único

El proyecto de motor referenciado por la aplicación y las pruebas debe ser `src/MethosUEngine/MethosUEngine.csproj`. No crear otra copia del motor C# bajo una carpeta alternativa. Si se necesita una función nueva, incorporarla al motor canónico y actualizar sus pruebas.

## Integración XDAT

`tools/xdat/` es la ubicación del código XDAT mantenido. La preparación de la distribución todavía depende de un kit ZIP histórico; esa dependencia solo puede retirarse después de migrar la aplicación base y el motor, comparar resultados y pasar pruebas de reconstrucción/instalación.

## Documentación y estado

Cada cambio estructural debe actualizar `docs/ARQUITECTURA.md`, `docs/WORKFLOWS.md`, el README si afecta a la instalación y `PROGRESO-METHOS.md`. No declarar una función terminada por una compilación exitosa si las pruebas funcionales correspondientes siguen pendientes.
