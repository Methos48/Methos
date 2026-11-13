# Arquitectura y organización de Methos

## Regla principal del producto

**Methos es una sola aplicación de Windows.** El único ejecutable principal que utiliza el usuario es `Methos.exe`, instalado mediante `Methos-Setup.exe`.

La ventana principal contiene dos módulos:

- **ARCHIVOS U**: paquetes Unreal `.u`.
- **ARCHIVOS XDAT**: paquetes `.xdat`.

Son dos capacidades de la misma aplicación, no dos programas, productos, instaladores ni repositorios. Los runtimes y procesos técnicos auxiliares, cuando existan, son internos y no constituyen productos independientes.

## Fuente oficial

- Repositorio: https://github.com/Methos48/Methos
- Rama de trabajo: `main`
- Este repositorio es la única fuente activa del producto.

No continuar desarrollo en repositorios históricos ni reconstruir una aplicación XDAT separada.

## Documentación complementaria

- `docs/WORKFLOWS.md`: inventario y responsabilidad de cada workflow.
- `docs/ROOT-REFERENCES.md`: inventario de kits, archivos del cliente y paquetes de referencia organizados bajo `reference-data/`.
- `tools/xdat/README.md`: estructura y estado de integración del motor XDAT.

## Mapa del código

| Ruta | Responsabilidad |
|---|---|
| `src/MethosUEditor/` | Aplicación gráfica principal, navegación y conexión con los módulos. El nombre de carpeta/namespace conserva un identificador técnico histórico; el producto visible es Methos. |
| `tools/xdat/` | Paquete de código XDAT mantenido: `xdat_core.py`, detección de cifrado, pipeline, metadatos de origen y validadores. Las adaptaciones del kit de Windows están en `tools/xdat/maintenance/`. El instalador todavía obtiene `xdat_core.py` del kit histórico; la migración completa del motor sigue pendiente. |
| `src/MethosUEditor/XdatBridge.cs` y `XdatBridge.py` | Puente interno entre la aplicación principal y el motor XDAT. No es una segunda aplicación. |
| `tools/xdat/xdat_core.py` | Código XDAT mantenido en el repositorio. Debe ser la fuente canónica una vez completada la migración del kit histórico. |

| `src/MethosUEngine/` | Proyecto C# canónico del motor Unreal/UCC y de los codecs/protecciones, referenciado por la aplicación y las pruebas. |
| `tests/` | Pruebas automatizadas U y XDAT. Las pruebas Python añaden `tools/xdat/` al camino de importación para que la fuente canónica no dependa de archivos en la raíz. |
| `tools/` | Herramientas de diagnóstico, inspección y validación; no todas forman parte del producto distribuido. |
| `installer/Methos.iss` | Definición del único instalador de Methos. |
| `docs/` | Documentación técnica, auditorías y resultados de investigación. |
| `reference-data/` | Archivos de referencia para pruebas e investigación; no son archivos de trabajo del usuario ni necesariamente se instalan. |
| `.github/workflows/` | Automatización de compilación, validación, inspección y archivo de fuentes. |

## Archivos de entrada de compilación y referencias

Los scripts de soporte XDAT ya no están en la raíz: su ubicación canónica es `tools/xdat/` y `tools/xdat/maintenance/`. Las pruebas Python permanecen bajo `tests/`. Los kits, paquetes y binarios de referencia están clasificados bajo `reference-data/kits/`, `reference-data/client/lineage2/`, `reference-data/ucc-runtime/`, `reference-data/packages/` y `reference-data/visuals/`. Los workflows deben consumirlos desde esas rutas. El kit XDAT histórico todavía alimenta la compilación de lanzamiento y aporta la aplicación fuente sobre la que se aplican las capas de mantenimiento; el código `tools/xdat/xdat_core.py` aún no sustituye por completo ese kit. No retirar el kit hasta migrar la aplicación base y validar el runtime resultante.

Un archivo de referencia no se convierte en parte del producto por estar almacenado en el repositorio. Solo los archivos explícitamente copiados por el workflow de lanzamiento y el instalador forman parte de la distribución.

## Workflows y repositorios históricos

Los workflows de inspección de Dunk3l y validación Unreal/UCC deben tomar sus fuentes desde este repositorio. Se eliminaron las descargas del antiguo repositorio `Methos-U-Editor`; los paquetes UCC y el paquete de referencia se leen ahora desde `reference-data/kits/Compilador-U.zip` y `reference-data/Interface-UEditor.u`. No reintroducir checkouts del repositorio histórico.

## Flujo oficial de compilación

El workflow de distribución oficial es `.github/workflows/windows-release.yml`. Debe producir:

1. Una aplicación Windows x64 llamada `Methos.exe`.
2. Un instalador único llamado `Methos-Setup.exe`.
3. Los runtimes internos UCC y XDAT necesarios para las funciones incluidas.
4. Las cuatro carpetas de trabajo bajo `Compilados/U` y `Compilados/XDAT`.

No debe publicar un segundo ejecutable de producto para XDAT ni un instalador XDAT independiente.

## Compatibilidad de datos locales

Algunas carpetas antiguas bajo `%LOCALAPPDATA%` conservan nombres históricos como `Methos U Editor`. Se mantienen temporalmente para no perder cachés ni respaldos de instalaciones anteriores. Son ubicaciones de datos, no aplicaciones independientes. No renombrarlas sin implementar una migración que copie o detecte los datos existentes.

## Organización de archivos de usuario

La aplicación mantiene separados los espacios de trabajo:

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

## Convenciones para cambios futuros

1. Usar siempre el nombre **Methos** para el producto, instalador, accesos directos, documentación y artefactos.
2. Hablar de **módulo U** y **módulo XDAT**, nunca de dos programas.
3. Mantener un único repositorio activo: `Methos48/Methos`.
4. No borrar ni mover archivos de referencia, DLL, ZIP, fuentes duplicadas aparentes o herramientas hasta revisar todos sus usos en código, scripts y workflows.
5. No cambiar rutas de datos de usuario sin migración compatible; puede haber copias de seguridad y cachés de versiones anteriores.
6. No renombrar carpetas o namespaces técnicos históricos solo por estética. Hacerlo únicamente si se actualizan todas las referencias y se prueba la compilación.
7. Cada cambio estructural debe actualizar este documento y `PROGRESO-METHOS.md`.
8. Un workflow exitoso solo acredita las pruebas que realmente ejecutó. No declarar compatibilidad universal sin pruebas funcionales con archivos reales.

## Estado de limpieza

La raíz ya no contiene los kits ni los archivos del cliente usados por los workflows. Se trasladaron a `reference-data/` conservando sus blobs binarios originales y se actualizaron las rutas de las automatizaciones. La duplicación entre `tools/xdat/` y el kit XDAT histórico aún debe resolverse mediante migración y pruebas, no mediante borrado.
