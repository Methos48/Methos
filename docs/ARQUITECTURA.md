# Arquitectura y organización de Methos

## Identidad del producto

**Methos es una sola aplicación de Windows x64.** El usuario abre un único ejecutable principal, `Methos.exe`, instalado mediante `Methos-Setup.exe`.

La misma ventana contiene dos módulos funcionales:

- **ARCHIVOS U:** paquetes Unreal `.u`.
- **ARCHIVOS XDAT:** paquetes `.xdat`.

Son dos capacidades de una aplicación, no dos programas, productos ni instalaciones. Las bibliotecas, scripts y runtimes auxiliares son componentes internos de Methos.

## Fuente oficial única

- Repositorio: https://github.com/Methos48/Methos
- Rama activa: `main`

Todo el desarrollo, integración, validación y distribución debe realizarse desde este repositorio y esta rama. No crear repositorios o ramas permanentes para separar U y XDAT.

## Mapa del código

| Ruta | Responsabilidad |
|---|---|
| `src/Methos.App/` | Aplicación gráfica principal, ventana, navegación y conexión de los módulos. |
| `src/Methos.Engine/` | Biblioteca interna C# del motor Unreal/UCC y codecs/protecciones. |
| `tools/xdat/` | Código fuente XDAT, detección de cifrado, pipeline, metadatos, validadores y preparación de fixtures. |
| `src/Methos.App/XdatBridge.cs` y `XdatBridge.py` | Puente interno entre Methos y el motor XDAT. |
| `tests/` y `tools/xdat/tests/` | Pruebas automatizadas de los componentes y flujos U/XDAT. |
| `tools/` | Herramientas de diagnóstico y preparación; no son aplicaciones de usuario. |
| `installer/Methos.iss` | Definición del único instalador. |
| `docs/` | Arquitectura, workflows, inventario de referencias y documentación técnica. |
| `reference-data/` | Archivos originales y datos de prueba; no son aplicaciones ni se instalan automáticamente. |
| `third_party/` | Dependencias externas y avisos de licencia. |
| `.github/workflows/` | Automatización de compilación, validación e investigación. |

## Código XDAT y datos de referencia

El runtime XDAT se prepara desde el código mantenido en `tools/xdat/xdat_core.py` y el puente `src/Methos.App/XdatBridge.py`.

`reference-data/kits/Methos-XDAT-Reference-Fixtures.zip` es únicamente un archivo de datos de prueba. Contiene un paquete binario `interface.xdat` y XML de referencia; no contiene código Python, ejecutables, DLL ni instaladores. `tools/xdat/prepare_reference_fixtures.py` extrae los datos necesarios para las pruebas y valida las rutas.

El archivo `reference-data/kits/Compilador-U.zip` conserva los componentes UCC/Interlude necesarios para la compilación y validación de paquetes U. Los demás archivos bajo `reference-data/` son muestras del cliente, paquetes y fuentes originales para pruebas e investigación.

Un archivo de referencia no es parte de la aplicación por estar almacenado en el repositorio. Solo los archivos copiados explícitamente por el workflow de lanzamiento y el instalador forman parte de la distribución.

## Workflows y distribución

El único workflow oficial de distribución es `.github/workflows/windows-release.yml`. Debe producir:

1. Una aplicación Windows x64 llamada `Methos.exe`.
2. Un instalador único llamado `Methos-Setup.exe`.
3. Los runtimes internos necesarios para las funciones incluidas.
4. Las carpetas de trabajo bajo `Compilados/U` y `Compilados/XDAT`.

Los demás workflows realizan pruebas, inspecciones o investigación. No deben crear un segundo producto ni instaladores independientes.

## Repositorios y ramas históricos

El repositorio oficial es exclusivamente `Methos48/Methos`. El repositorio auxiliar `Methos48/Methos-Interface` tiene su rama principal sin archivos de producto, pero el repositorio todavía existe en GitHub. Las ramas `migration/*`, `research/*` y `work/*` que permanezcan visibles en el repositorio oficial son referencias históricas, no ramas activas.

No continuar trabajo en esas ubicaciones. Su eliminación definitiva es una tarea administrativa de GitHub; no se debe confundir el hecho de que estén vacías o inactivas con que hayan sido borradas.

## Compatibilidad de datos locales

Algunas ubicaciones locales heredadas se conservan temporalmente para proteger cachés y respaldos de instalaciones anteriores. Son datos de usuario, no aplicaciones adicionales. No renombrarlas ni eliminarlas sin implementar una migración compatible que detecte y preserve esos datos.

## Organización de archivos de usuario

La aplicación mantiene separados los espacios de trabajo de cada módulo:

```text
Compilados/
  U/
    Archivo/
    Decopilado/
  XDAT/
    Archivo/
    Decopilado/
```

Los originales seleccionados por el usuario deben conservarse sin cambios. Las operaciones se realizan sobre copias de trabajo y deben mantener respaldos y validaciones antes de publicar resultados.

## Convenciones obligatorias

1. Usar siempre el nombre **Methos** para el producto, ejecutable, instalador, accesos directos, documentación y artefactos.
2. Hablar de **módulo U** y **módulo XDAT**, nunca de dos programas.
3. Mantener un solo repositorio activo: `Methos48/Methos`, rama `main`.
4. No borrar archivos de referencia, DLL, ZIP, fuentes o herramientas hasta revisar todos sus usos y ejecutar las pruebas afectadas.
5. No cambiar rutas de datos de usuario sin migración compatible.
6. Cada cambio estructural debe actualizar esta arquitectura y `PROGRESO-METHOS.md`.
7. Un workflow exitoso solo acredita las pruebas que realmente ejecutó. No declarar compatibilidad universal sin pruebas funcionales con archivos reales.

## Estado actual de limpieza

La estructura activa del producto está unificada: una aplicación, un ejecutable principal y un instalador. El antiguo paquete XDAT de compilación fue reemplazado por fixtures puros; el código del antiguo editor no se extrae ni se ejecuta en Windows Release.

Quedan referencias administrativas fuera del código activo: el repositorio vacío `Methos-Interface` y ramas históricas. También se conserva el historial Git y los datos de referencia necesarios. Esos elementos no son programas que se ejecuten ni forman parte del instalador.
