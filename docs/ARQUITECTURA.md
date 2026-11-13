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

## Mapa del código

| Ruta | Responsabilidad |
|---|---|
| `src/MethosUEditor/` | Aplicación gráfica principal, navegación y conexión con los módulos. El nombre de carpeta/namespace conserva un identificador técnico histórico; el producto visible es Methos. |
| `src/MethosUEngine/` | Motor de paquetes Unreal, UCC y herramientas de UnrealScript utilizadas por el módulo U. |
| `tools/xdat/` | Motor XDAT Python y pruebas/utilidades relacionadas. |
| `src/MethosUEditor/XdatBridge.cs` y `XdatBridge.py` | Puente interno entre la aplicación principal y el motor XDAT. No es una segunda aplicación. |
| `tools/xdat/xdat_core.py` | Código XDAT mantenido en el repositorio. Debe ser la fuente canónica una vez completada la migración del kit histórico. |
| `Methos_XDAT_Windows_Build_Kit_v1.0 (1).zip` (raíz) | Insumo heredado que el workflow Windows todavía extrae para copiar el motor XDAT y ejecutar sus pruebas. No es una segunda app, pero sí una duplicación pendiente de retirar después de migrar y validar todo su contenido en `tools/xdat/`. |
| `methos_crypto/` | Copias históricas/experimentales de componentes del motor. No es el proyecto que compila la aplicación; el motor compilado por la app está en `src/MethosUEngine/`. No borrar ni sincronizar automáticamente sin comparar versiones y revisar workflows. |
| `tests/` | Pruebas automatizadas de los componentes. |
| `tools/` | Herramientas de diagnóstico, inspección y validación; no todas forman parte del producto distribuido. |
| `installer/Methos.iss` | Definición del único instalador de Methos. |
| `docs/` | Documentación técnica, auditorías y resultados de investigación. |
| `reference-data/` | Archivos de referencia para pruebas e investigación; no son archivos de trabajo del usuario ni necesariamente se instalan. |
| `.github/workflows/` | Automatización de compilación, validación, inspección y archivo de fuentes. |

## Archivos de entrada de compilación y referencias

Algunos archivos ZIP y archivos de cliente permanecen en la raíz porque workflows actuales todavía los consumen como insumos de compilación o pruebas. No moverlos ni eliminarlos hasta migrar sus consumidores a rutas canónicas y comprobar la compilación resultante. En particular, el ZIP histórico del kit XDAT todavía alimenta la compilación de lanzamiento; editar únicamente `tools/xdat/xdat_core.py` no garantiza que el instalador use esa versión hasta completar la migración.

Un archivo de referencia no se convierte en parte del producto por estar almacenado en el repositorio. Solo los archivos explícitamente copiados por el workflow de lanzamiento y el instalador forman parte de la distribución.

## Workflows y repositorios históricos

Los workflows de inspección de Dunk3l y validación Unreal/UCC deben tomar sus fuentes desde este repositorio. Se eliminaron las descargas del antiguo repositorio `Methos-U-Editor`; los paquetes UCC y el paquete de referencia se leen ahora desde `Compilador  U.zip` y `reference-data/Interface-UEditor.u`. No reintroducir checkouts del repositorio histórico.

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

La primera pasada elimina la automatización obsoleta que intentaba construir una distribución XDAT independiente y normaliza el nombre del script del instalador. Los insumos ZIP históricos permanecen temporalmente en la raíz porque el workflow oficial todavía depende de ellos. La duplicación entre `tools/xdat/` y el kit ZIP debe resolverse como una tarea de migración con pruebas, no mediante borrado.
