# METHOS — Registro de progreso

**Última actualización:** 13-11-2026  
**Proyecto:** Methos (editor de archivos de Lineage II Interlude)  
**Repositorio de trabajo:** https://github.com/Methos48/Methos  
**Repositorio único del proyecto:** https://github.com/Methos48/Methos

Este documento es la bitácora permanente del proyecto. Cada sesión de trabajo debe actualizarlo con cambios reales, pruebas ejecutadas, resultados, problemas pendientes y el siguiente paso. No marcar una tarea como terminada sin una prueba que lo demuestre.

## Objetivo final

Entregar una aplicación de Windows llamada **Methos**, instalable y con acceso directo en el escritorio, que permita elegir entre **ARCHIVOS U** y **ARCHIVOS XDAT**. Debe conservar copias originales y trabajar sobre copias dentro de su espacio de trabajo. La aplicación debe detectar y aplicar los métodos de protección que realmente pueda reconocer y validar; si no conoce un método, debe detenerse sin dañar ni sobrescribir el archivo original.

Flujo esperado: cargar archivo → copia de seguridad automática → descompilar/descifrar → editar → compilar/cifrar de nuevo → verificar el resultado.

## Estado actual y regla de arquitectura (13-11-2026)

- **Producto:** una sola aplicación Windows llamada Methos, con los módulos ARCHIVOS U y ARCHIVOS XDAT dentro de la misma interfaz.
- **Distribución:** un solo ejecutable principal `Methos.exe` y un solo instalador `Methos-Setup.exe`; no se distribuyen aplicaciones independientes U/XDAT.
- **Proyectos canónicos:** `src/Methos.App/Methos.App.csproj` y `src/Methos.Engine/Methos.Engine.csproj`. El motor es una biblioteca interna, no un segundo producto.
- **Código XDAT:** `tools/xdat/xdat_core.py` es la fuente canónica del parser/compilador. El runtime copia ese archivo y `src/Methos.App/XdatBridge.py`; las pruebas se ejecutan desde el repositorio.
- **Fixtures XDAT:** `reference-data/kits/Methos-XDAT-Reference-Fixtures.zip` contiene únicamente paquetes binarios/XML de referencia. El workflow extrae solo `interface.xdat` y los XML necesarios; no extrae ni ejecuta la antigua interfaz `app.py`, ni distribuye `l2encdec.exe`.
- **Limpieza estructural:** se retiraron `methos_crypto/`, los proyectos/rutas heredados `MethosUEditor` y `MethosUEngine`, y los antiguos overlays `tools/xdat/maintenance/`. La estructura activa mantiene un solo motor C# y una sola fuente XDAT.
- **Validación Windows:** [Windows Release 43401463785](https://github.com/Methos48/Methos/actions/runs/43401463785) terminó en **SUCCESS (27 pasos)**. Generó el instalador, probó instalación silenciosa y arranque de `Methos.exe`. Artefacto `Methos-Windows-Installer-x64`, ID `14024748596`, tamaño 77,768,945 bytes.
- **Archivo limpio de fixtures:** el workflow generó `Methos-XDAT-Reference-Fixtures-Clean`, ID `14025406957`, tamaño 472,410 bytes, y lo normalizó en `main`.
- **Límite funcional pendiente:** `auto_pipeline.py` se conserva como módulo fuente y tiene pruebas, pero todavía no está conectado al puente C# de la interfaz. No afirmar que el runtime actual procesa todas las variantes cifradas ni que usa `l2encdec.exe`.
- **Incidencia UCC separada:** el workflow `Silent Dark U and XDAT round-trip` falla en la compilación U por `InventoryWnd.uc(1529): Error, Missing function name`. El mismo error aparece en una ejecución anterior a la unificación (run `43400534612`), por lo que no lo introduce esta limpieza. Debe tratarse como un problema funcional del compilador/fuente U; ese workflow aborta antes de llegar a la fase XDAT.
- **Conclusión:** la unificación estructural y la distribución de un solo Methos están integradas y validadas por Windows Release. La siguiente etapa es resolver la incidencia UCC y validar recorridos funcionales reales U y XDAT, sin añadir capacidades no probadas.

## Registro de avances

### 13-11-2026 — Cierre de unificación estructural y validación Windows

- Se confirmó en `main` la aplicación única `Methos.exe`, con U y XDAT como módulos internos.
- Se normalizaron los proyectos a `Methos.App` y `Methos.Engine`; no quedan las carpetas activas duplicadas `methos_crypto/`, `MethosUEditor/` ni `MethosUEngine/`.
- El runtime XDAT usa directamente `tools/xdat/xdat_core.py`. El ZIP histórico fue sustituido por un archivo de fixtures que contiene solo datos de prueba; el workflow no extrae ni ejecuta `app.py` ni instala `l2encdec.exe`.
- Windows Release `43401463785` terminó SUCCESS en 27 pasos, con instalador, instalación silenciosa y arranque verificados.
- El round-trip UCC de Silent Dark mantiene una incidencia conocida en `InventoryWnd.uc(1529)`, reproducida también antes de la unificación. Se deja como tarea funcional separada.
- No añadir funciones nuevas hasta resolver y probar los recorridos reales de U y XDAT.

### 13-11-2026 — Normalización de nombres internos (registro histórico, superado)

- Los cambios se prepararon en `work/unification-audit` y se integraron en `main` mediante PR #3.
- Este registro describe el estado intermedio de la revisión; las tareas que aquí figuraban como pendientes ya se completaron y validaron según el bloque **Estado actual** al inicio de esta bitácora.

### 13-11-2026 — Unificación de repositorios en Methos

- Se estableció `Methos` como único repositorio activo del producto **Methos**.
- Se migraron a la rama principal las fuentes de la aplicación gráfica, el motor Unreal/UCC, las pruebas .NET, el instalador, el kit UCC de Interlude, documentación y bitácora que estaban en `Methos-U-Editor`.
- Se integraron también los componentes XDAT de la rama de investigación: detección de cifrado, pipeline, metadatos de origen y pruebas.
- La aplicación conserva un único producto y ejecutable principal: `Methos.exe`. Los módulos ARCHIVOS U y ARCHIVOS XDAT forman parte de Methos; XDAT no se distribuye como una segunda aplicación independiente.
- Se separó el paquete Unreal de referencia en `reference-data/Interface-UEditor.u` para evitar la colisión de nombres entre `Interface.u` y `interface.u` en Windows.
- GitHub Actions confirmó la migración y las validaciones posteriores. La ejecución UCC `43386687102` terminó en SUCCESS; la compilación Windows más reciente, ejecución `43387205032`, también terminó en SUCCESS en sus 25 pasos principales. El archivo de fuentes se generó correctamente en la ejecución Source Archive `43387204850`.
- **Limpieza completada:** `Methos-U-Editor` quedó sin archivos en sus tres ramas (`main`, `backup/methos-foundation-2026-11-12` y `validate-native-engine`). El historial Git anterior se conserva como respaldo histórico; el repositorio activo del proyecto es únicamente `Methos`.


### 12-11-2026 — Interfaz principal y presentación

- Se eligió como diseño de referencia el primer concepto visual: panel **ARCHIVOS U** rojo a la izquierda y **ARCHIVOS XDAT** azul a la derecha, sobre fondo oscuro.
- Se modificó el lanzador principal para mostrar ambos módulos y permitir entrar a cada uno.
- Se agregó navegación para volver a INICIO.
- Se preparó el icono de aplicación con una **M roja** y se integró su generación al flujo de compilación de Windows.
- Se configuró el acceso directo de escritorio durante la instalación.
- Se guardaron cambios en la rama principal en estos commits:
  - `590a39c7bcb720c60f6be5862dc3d090a936b4c5` — rediseño del lanzador.
  - `d65d7046041bf3bb476eb8fd2fe0127ee7771b0d` — inicio en el lanzador.
  - `72146b133be93cb581c5e5c8d1b6caaa186675e4` — acceso directo de escritorio.
  - `05d379e99ad500432017f0d3cf973a57cc74d8ff` — normalización de la ruta del icono.
- **Estado de verificación:** estos cambios visuales están guardados en GitHub, pero todavía no se debe afirmar que la última versión compila o instala correctamente hasta revisar el resultado de GitHub Actions correspondiente a esos cambios.

### Base técnica ya incorporada (estado conocido antes de esta actualización)

- La aplicación Windows integra los módulos ARCHIVOS U y ARCHIVOS XDAT bajo el producto Methos. Aunque el motor XDAT pueda ejecutarse como proceso auxiliar interno, para el usuario es una sola aplicación y un único flujo de trabajo.
- El flujo U inspecciona las capas de protección antes de permitir el procesamiento y bloquea los casos en los que no puede garantizar una decodificación/codificación completa.
- El módulo XDAT contiene detección de formato y un pipeline automático que contempla protocolos conocidos de `l2encdec`; los métodos desconocidos deben quedar identificados como no compatibles, no tratarse como descifrados.
- Existen pruebas previas del motor de paquetes UE2 y del módulo XDAT. Una ejecución anterior de Windows CI terminó correctamente y produjo un instalador, pero fue **anterior** a los últimos cambios de interfaz e icono. No cuenta como validación de la versión actual.
- La prueba anterior de instalación/arranque verificó que el instalador incluía Methos, UCC, el módulo XDAT y sus carpetas de trabajo. No demostró que cualquier archivo protegido pueda descifrarse y recompilarse.

## Pendientes prioritarios

1. **Resolver la incidencia UCC conocida:** investigar `InventoryWnd.uc(1529): Missing function name` en el workflow Silent Dark; comparar el archivo de entrada, la salida reparada y el diagnóstico del compilador.
2. **Validar ARCHIVOS U con paquetes reales:** copia de seguridad, descompilación, importación, compilación y round-trip sin modificar originales.
3. **Validar ARCHIVOS XDAT con archivos reales:** paquetes ct0 limpios y editados, preservación binaria y verificación del resultado.
4. **Mantener explícito el alcance de cifrado:** `auto_pipeline.py` no está conectado a la interfaz C#; integrar variantes protegidas solo después de validar el motor y su dependencia, sin afirmar soporte no probado.
5. **Publicar una versión estable en GitHub Releases** cuando los recorridos funcionales U y XDAT estén comprobados.

## Reglas para continuar el trabajo

- Continuar desde el estado real de GitHub; consultar la rama principal y los últimos commits antes de modificar archivos.
- No volver a empezar ni reemplazar componentes que ya funcionan sin motivo.
- No declarar compilación, descifrado, cifrado o round-trip exitosos sin evidencia de pruebas.
- Nunca sobrescribir el archivo original seleccionado por el usuario. Trabajar con copias y conservar respaldo.
- No inventar métodos de cifrado ni forzar un protocolo desconocido.
- Mantener esta bitácora actualizada al terminar cada bloque de trabajo, incluyendo fecha, archivos/commits modificados, pruebas y resultado.
- El usuario no programa: entregar archivos y pasos concretos; explicaciones breves y de a un punto por vez.

## Próxima acción

Con la migración y la compilación automatizada confirmadas, continuar la validación funcional de ambos módulos dentro de Methos: primero recorrido U y después XDAT, conservando pruebas, fixtures y archivos originales.

### 12-11-2026 — Instalador Methos Windows x64 generado y probado

- **Resultado: compilación e instalación superadas.** GitHub Actions ejecutó el workflow `Methos U Editor - Windows Release` para el commit `05d379e99ad500432017f0d3cf973a57cc74d8ff`.
- Ejecución: [Windows Release #438](https://github.com/Methos/actions/runs/43234204448) — **SUCCESS**.
- Los 19 pasos principales finalizaron correctamente, incluidos:
  - generación del icono M roja;
  - publicación autónoma de Methos para Windows x64;
  - integración del runtime UCC y 169 fuentes de referencia;
  - compilación y pruebas del módulo XDAT;
  - creación del instalador Inno Setup;
  - instalación silenciosa, comprobación de componentes y arranque de Methos;
  - arranque de prueba del ejecutable XDAT;
  - publicación del instalador como artefacto.
- Artefacto generado: `Methos-Windows-Installer-x64`, ID `13950374516`, tamaño 77,461,895 bytes. SHA-256: `045b010a645c89f881e8e565c1cb99039a03e7083aa59aa8d859ce0c60658d9a`.
- **Qué demuestra:** el instalador Windows x64 se generó; pudo instalarse silenciosamente; Methos y XDAT arrancaron; se encontraron los componentes UCC y las carpetas de trabajo. El instalador incluye el acceso directo de escritorio configurado en Inno Setup.
- **Qué NO demuestra:** que todas las operaciones de descompilación/compilación U funcionen con cualquier archivo, ni que todos los formatos XDAT personalizados puedan descifrarse. Eso sigue siendo trabajo funcional separado.
- **Entrega actual del instalador:** se encuentra guardada como artefacto de GitHub Actions en la ejecución enlazada arriba. Aún no es una publicación permanente de GitHub Releases.

## Próximo bloque de trabajo

1. Probar y corregir el recorrido funcional de ARCHIVOS U con paquetes reales: carga/copia de seguridad, descompilación, importación de fuentes, compilación y verificación.
2. Probar el recorrido XDAT con archivos de referencia protegidos y no protegidos.
3. Publicar una versión estable en GitHub Releases cuando el comportamiento funcional esté validado; conservar el nombre de producto **Methos**.


### 13-11-2026 — Normalización de organización y nombre del producto

- Se reafirma como regla permanente que Methos es una sola aplicación, con módulos internos ARCHIVOS U y ARCHIVOS XDAT, un solo ejecutable principal `Methos.exe` y un solo instalador `Methos-Setup.exe`.
- Se corrigió el enlace del repositorio oficial en la cabecera de esta bitácora: https://github.com/Methos48/Methos.
- Se creó `docs/ARQUITECTURA.md` como mapa oficial del repositorio, responsabilidades de carpetas, reglas de cambios y dependencias estructurales.
- Se normalizó el nombre del script de Inno Setup a `installer/Methos.iss`; el identificador interno del instalador se conserva para que las actualizaciones no creen una instalación paralela.
- Se retiró el workflow heredado que construía un instalador independiente de XDAT. La distribución oficial queda centralizada en `.github/workflows/windows-release.yml`.
- No se eliminaron los ZIP, DLL, paquetes ni archivos del cliente de la raíz: el workflow oficial aún utiliza algunos como insumos. Su traslado requiere primero migrar referencias y validar el build.
- Las carpetas y namespaces técnicos históricos `Methos.App` se mantienen temporalmente para evitar romper rutas, proyecto y pruebas. La marca que ve el usuario sigue siendo Methos.
- Pruebas pendientes de este bloque: ejecutar el workflow Windows Release después de estos cambios y verificar que produzca un único instalador Methos.


- **Auditoría inicial de dependencias (registro histórico, superado):** en ese momento existían copias del motor y el workflow dependía del kit XDAT. La consolidación posterior eliminó la copia `methos_crypto/`, dejó `src/Methos.Engine/` como motor C# canónico y convirtió el ZIP XDAT en un archivo de fixtures; ver el estado actual al inicio de esta bitácora.
- **Compatibilidad de datos:** se mantienen rutas antiguas de caché y respaldos bajo `%LOCALAPPDATA%` para no romper datos existentes; sus nombres históricos no representan programas separados.

- Se retiró también el workflow temporal `.github/workflows/migrate-unified.yml`: la migración del repositorio ya está completada y no debe repetirse como tarea activa.

- Se amplió `README-ES.txt` con el enlace al repositorio oficial y la explicación de que las rutas locales heredadas de caché/respaldo no son una segunda aplicación.
- Se añadió `.gitignore` para excluir salidas de compilación, cachés temporales y el espacio de trabajo local `Compilados/`, sin ignorar de forma global ejecutables o DLL que puedan ser referencias legítimas.

- **Corrección de workflows heredados:** se eliminaron las referencias de checkout a `Methos48/Methos-U-Editor` en los workflows de inspección Dunk3l y validación Unreal. Ahora usan el kit UCC del repositorio único y el paquete de referencia ubicado en `reference-data/Interface-UEditor.u`. Se actualizaron también sus filtros de rutas.

- **Organización inicial del código XDAT (registro histórico, superado):** los módulos se trasladaron a `tools/xdat/` y se crearon overlays temporales para el kit anterior. Esos overlays fueron retirados al migrar las pruebas al repositorio y convertir el ZIP en fixtures de referencia.
- **Estado histórico:** la fuente XDAT todavía dependía del kit ZIP. Esa dependencia de código ya fue retirada; el ZIP actual contiene únicamente fixtures de prueba.

- **Unificación del motor C#:** se incorporaron a `src/Methos.Engine/` las cuatro clases que solo existían en `methos_crypto/`: `UnrealPackageDecompiler`, `UnrealScriptKnownNativeOperatorRepair`, `UnrealScriptNativeEnumArgumentRepair` y `UnrealScriptTypeReferenceRepair`. `tests/ProtectionPipelineSmoke` ahora referencia el proyecto real `src/Methos.Engine/Methos.Engine.csproj`, no la copia antigua. Los workflows afectados vigilan la ruta canónica.
- La carpeta `methos_crypto/` se conserva temporalmente hasta que la nueva referencia compile y pasen las pruebas; después podrá retirarse como duplicado.


### 13-11-2026 — Organización de archivos de referencia

- Se definió la clasificación de kits, cliente, runtime UCC, paquetes de prueba y capturas bajo `reference-data/`.
- Los workflows se actualizaron para consumir las rutas nuevas; el movimiento se realizará como una única actualización del árbol Git, reutilizando los blobs originales para conservar íntegros los binarios.
- La fuente XDAT integrada sigue en migración: el kit Windows histórico permanece como insumo hasta demostrar que `tools/xdat/` puede sustituirlo sin pérdida funcional.

- **Eliminación de motor duplicado:** se retiró `methos_crypto/` (25 archivos) después de trasladar las cuatro clases exclusivas a `src/Methos.Engine/`, integrar las diferencias necesarias de los índices/reparadores y cambiar las pruebas para referenciar el motor canónico. El historial Git conserva la copia anterior.
- **Organización de pruebas:** el smoke test del planificador se movió de `tools/ProtectionPipelineSmoke/` a `tests/ProtectionPlannerSmoke/`; el smoke test de integración Unreal permanece en `tests/ProtectionPipelineSmoke/`. Se actualizaron el workflow y la guía de pruebas.
- Los archivos del cliente, kits UCC/XDAT, runtime y paquetes de referencia se agruparon bajo `reference-data/` y sus rutas de workflows fueron actualizadas. Los blobs Git de los 51 archivos movidos se conservaron sin cambios.


### 13-11-2026 — Auditoría final de unificación (sesión actual)

- Se verificó que `Methos48/Methos` es el repositorio activo y que `Methos.exe` es la única aplicación de producto, con módulos U y XDAT dentro de la misma interfaz.
- Se comprobó que el workflow Windows reciente `43387205032` terminó correctamente: publicación de Methos x64, integración de los runtimes UCC/XDAT, instalación silenciosa, prueba de arranque y verificación de que no se instala un segundo ejecutable de aplicación XDAT.
- **La unificación del producto y del repositorio está consolidada. La unificación completa de la fuente XDAT todavía NO está terminada.** El workflow de distribución aún extrae la aplicación base y `xdat_core.py` del kit histórico `reference-data/kits/Methos-XDAT-Reference-Fixtures.zip`, aplica las capas de mantenimiento y prepara el runtime interno. En paralelo, el repositorio mantiene código XDAT en `tools/xdat/`. No se debe borrar el kit ni declarar que existe una única fuente efectiva hasta comparar ambas versiones, migrar las piezas faltantes y pasar pruebas de reconstrucción e instalación.
- También se confirmó que `src/Methos.App/` conserva un nombre técnico histórico en carpetas y namespace; el ensamblado, producto, ventana e instalador visibles se llaman Methos. Renombrar esa estructura sin una migración completa de referencias no es una limpieza segura y no es necesario para que el usuario tenga una sola aplicación.
- **Resultado de esta auditoría:** un solo producto y un solo repositorio activo, sí; desaparición de todos los restos históricos internos, no; dependencia XDAT histórica pendiente de eliminar mediante migración verificada.
- **Siguiente y único bloqueo de la unificación:** comparar y migrar el kit XDAT histórico hacia `tools/xdat/` y el puente interno de Methos, conservando exactamente el comportamiento ya validado. Hasta superar esa comparación, no marcar el trabajo de unificación como 100 % finalizado.


### 13-11-2026 — Estado de cierre de la organización estructural

- La rama oficial sigue siendo `main`. La política de ramas históricas y experimentales está documentada en `docs/BRANCHES.md`; no se deben usar para lanzamientos.
- La raíz quedó organizada: código, documentación, configuración, instalador, pruebas, herramientas y `reference-data/`. Los kits/archivos de cliente ya no están sueltos en la raíz.
- Se eliminaron `methos_crypto/` y el smoke test duplicado bajo `tools/`. El motor C# canónico es `src/Methos.Engine/`; las pruebas están en `tests/`.
- Se movieron 51 archivos de referencia conservando sus blobs Git originales y se actualizaron los workflows y herramientas que los consumen.
- La automatización oficial Windows Release terminó correctamente en la rama `main`: run [43396508357](https://github.com/Methos48/Methos/actions/runs/43396508357). Publicó el instalador único `Methos-Setup.exe` y ejecutó instalación/arranque de Methos.
- La validación del compilador UCC terminó correctamente: run [43396477953](https://github.com/Methos48/Methos/actions/runs/43396477953). El smoke del planificador/protecciones también terminó correctamente: run [43396477929](https://github.com/Methos48/Methos/actions/runs/43396477929).
- **Validaciones pendientes, no ocultar:** la reconstrucción completa de la interfaz de referencia aún falla por incompatibilidad de tipo en `PartyWnd.uc` (`SetName`, parámetro 2). El round-trip Silent Dark aún falla por identificadores de función corruptos/control characters en fuentes decompiladas y una clase `ChatFilterWnd` mal formada. No se inventaron nombres ni se cambiaron comportamientos para forzar esas compilaciones.
- **Pendiente de integración XDAT:** Windows Release todavía necesita el kit histórico `reference-data/kits/Methos-XDAT-Reference-Fixtures.zip` para parte del runtime/código base. No retirar el kit hasta migrar esa función a `tools/xdat/` y validar extracción, reconstrucción, protección y round-trip.
- Con esto queda organizada la estructura del repositorio para continuar el desarrollo en `main`; las limitaciones de compilación anteriores deben abordarse como tareas técnicas separadas, no como reorganización de carpetas.


### 13-11-2026 — Motor XDAT canónico conectado al build

- Se confirmó que `tools/xdat/xdat_core.py` contiene el parser/compilador ct0 mantenido en el repositorio.
- Se modificó `.github/workflows/windows-release.yml` para copiar esa fuente canónica sobre la copia histórica del kit antes de aplicar las capas de detección y pipeline.
- El workflow compara SHA-256 entre la fuente versionada y la copia que se utilizará en el runtime; si difieren, la compilación falla. Se añadió `tools/xdat/xdat_core.py` a los filtros de cambios que disparan la validación Windows.
- Commits: `82f55b233449a9010ecae7ecc03b2541b2a944f1` (motor canónico en build) y `17972c54233a42eeef3a468c3504e0300fa088c9` (filtro CI).
- **Límite aún pendiente:** el kit histórico sigue proporcionando la aplicación base `app.py`, las pruebas/fixtures del paquete y `l2encdec.exe`. No se borró porque todavía contiene dependencias efectivas del runtime. El motor ct0 ya tiene fuente canónica; la migración de todo el paquete XDAT aún requiere trasladar esos componentes y validarlos.
- Estado de CI de estos commits: todavía no hay ejecución visible asociada en GitHub Actions al momento de registrar esta nota. No se declara validación de Windows superada por estos cambios hasta que aparezca el resultado.

### 13-11-2026 — Auditoría de unificación y validación final solicitada

- Confirmado en `main`: la solución de producto está organizada como `Methos.App` + `Methos.Engine`; la salida de producto es un único `Methos.exe`. Los módulos U y XDAT son funciones internas, no aplicaciones instalables separadas.
- Confirmado: `tools/xdat/xdat_core.py` es la fuente canónica del parser/compilador XDAT. El workflow Windows copia esa fuente al entorno de compilación y compara su SHA-256 antes de empaquetar.
- Límite que no se debe ocultar: la compilación XDAT todavía obtiene del archivo de referencia `Methos-XDAT-Reference-Fixtures.zip` la aplicación base `app.py`, las pruebas/fixtures originales y `l2encdec.exe`. El ZIP se conserva como material de compatibilidad; no es otro producto ni un segundo instalador. Retirarlo sin portar y probar esos componentes rompería la compilación o perdería compatibilidad.
- La carpeta `tools/xdat/tests` contiene una prueba de round-trip que requiere el conjunto de fixtures XDAT del kit. La comparación automatizada de fuentes queda en el workflow como auditoría de diferencias.
- **Verificación de este bloque:** se ha revisado el workflow y la estructura actual del repositorio. Esta actualización activa una nueva ejecución Windows Release; hasta consultar su resultado no se declara aprobada la compilación de este commit.
- **Criterio de cierre:** Methos está unificado como un solo producto y un solo ejecutable; la eliminación completa de la dependencia del kit XDAT queda abierta y explícitamente documentada, porque todavía faltan migración y validación funcional de sus componentes restantes.


### 13-11-2026 — Retirada de la aplicación XDAT histórica del proceso de compilación

- Se eliminó del workflow la extracción, parcheo y ejecución de `app.py` del antiguo programa independiente. El build ya no usa las capas `apply_detection_layer.py` ni `auto_pipeline_overlay.py`; esos scripts de adaptación y el comparador del kit fueron retirados del código activo.
- El workflow compila y prueba directamente las fuentes versionadas en `tools/xdat/` y ejecuta las pruebas desde el repositorio Methos.
- Se añadió `prepare_reference_fixtures.py`: extrae exclusivamente el `interface.xdat` de referencia y los XML necesarios para el round-trip. Comprueba CRC y bloquea rutas inseguras; no extrae Python, ejecutables, DLL ni instaladores.
- Se corrigieron los imports de las pruebas para cargar los módulos desde `tools/xdat/` y se configuró `XDAT_REFERENCE_ROOT` para las pruebas de reconstrucción.
- El ZIP binario fue renombrado a `Methos-XDAT-Reference-Fixtures.zip` para reflejar su única función restante: conservar datos de prueba. No es parte del instalador ni del runtime.
- **Pendiente de verificación:** GitHub Actions Windows Release debe finalizar con éxito para confirmar que los fixtures del ZIP tienen la estructura esperada y que todas las pruebas de Methos pasan. Hasta entonces, no se declara aprobada la nueva compilación.

### 13-11-2026 — Cierre de dependencias del programa XDAT independiente

- La compilación ya no extrae ni ejecuta el `app.py` de la aplicación histórica, ni parchea ese programa para convertirlo en Methos.
- Se retiraron los scripts de overlay y comparación que solo existían para adaptar el antiguo programa: `apply_detection_layer.py`, `auto_pipeline_overlay.py` y `compare_build_kit.py`.
- Se incorporó `prepare_reference_fixtures.py`, que solo extrae los datos necesarios para las pruebas XDAT (un `interface.xdat` y sus XML), valida la integridad CRC del ZIP y bloquea rutas de extracción inseguras.
- El ZIP fue renombrado a `Methos-XDAT-Reference-Fixtures.zip`; queda como archivo de datos de prueba, no como kit de compilación, programa ni runtime. No se copia al instalador.
- El workflow prueba directamente los módulos y tests mantenidos en el repositorio y copia al runtime de Methos únicamente `xdat_core.py` y el puente `XdatBridge.py`.
- La comprobación del instalador ahora rechaza cualquier ejecutable independiente cuyo nombre contenga XDAT; el producto sigue siendo un solo `Methos.exe`.
- **Alcance técnico que se conserva para la siguiente etapa:** el pipeline de detección/cifrado permanece como código interno y pruebas, pero todavía no está conectado al puente de la interfaz. No se debe afirmar que el runtime ya procesa todas las variantes cifradas; esta limpieza no altera esa limitación.
- **Estado de validación:** la nueva ejecución Windows Release debe confirmar la extracción de fixtures, las pruebas de round-trip, la compilación, la instalación y el arranque. El cierre de esta migración se considera sujeto a ese resultado.

### 13-11-2026 — Archivo de referencia XDAT depurado y migrado

- **Commit automático verificado:** `a9fab0fc63cc4ed49fb41cf022c736648461c29c` — reemplazó el antiguo ZIP completo por un ZIP limpio de fixtures.
- Archivo actual: `reference-data/kits/Methos-XDAT-Reference-Fixtures.zip`, 498.092 bytes. Contiene 177 entradas: un `interface.xdat` y los XML de referencia. El ZIP limpio pasó CRC; la inspección confirmó que no contiene `.py`, `.exe`, `.dll`, instaladores ni scripts.
- Por tanto, ya no queda almacenado en el repositorio el código fuente ni el ejecutable del antiguo programa XDAT dentro de ese paquete. El archivo conserva solo datos de prueba.
- La ejecución Windows Release `43400821601` finalizó en **SUCCESS**: compilación, pruebas canónicas XDAT, preparación del runtime, creación del instalador e instalación/arranque silenciosos. Artefacto generado: `Methos-Windows-Installer-x64`, ID `14024169547`, 77.766.886 bytes.
- **Verificación final pendiente:** ejecutar el mismo workflow desde el commit que ya contiene el ZIP limpio para confirmar que las pruebas y el instalador funcionan sin el paquete histórico original. No declarar el cierre completo hasta que esa ejecución termine en SUCCESS.

### 13-11-2026 — Validación final sobre el repositorio unificado y fixtures limpios

- **GitHub Actions Windows Release:** [ejecución 43401463785](https://github.com/Methos48/Methos/actions/runs/43401463785) — **SUCCESS** sobre la rama `main`, con el ZIP limpio y determinista de fixtures XDAT.
- Pasaron las pruebas del motor UE2/UCC, la extracción de fixtures, la sintaxis y pruebas Python XDAT, el round-trip binario de 168 ventanas y 8 accesos directos, la preparación del runtime Python interno, la creación del instalador, la instalación silenciosa y el arranque de `Methos.exe`.
- El instalador comprobó que no se distribuye un segundo ejecutable XDAT ni `l2encdec.exe`; la instalación contiene un único ejecutable de producto.
- Artefacto final: `Methos-Windows-Installer-x64`, ID `14024748596`, tamaño 77.768.945 bytes. Ejecución de origen: `43401463785`.
- El ZIP de referencia ya está depurado y determinista. Su fuente actual es únicamente el archivo binario XDAT y los XML de pruebas; los cambios de empaquetado no generan commits nuevos si el contenido no cambia.
- **Resultado de unificación:** Methos queda consolidado en un solo repositorio activo, una sola aplicación y un único ejecutable principal `Methos.exe`, con los módulos ARCHIVOS U y ARCHIVOS XDAT internos. Se retiraron del build y del repositorio las capas de adaptación del antiguo editor XDAT; el antiguo paquete de referencia fue sustituido por datos de prueba limpios.
- **Límite de funcionalidad que sigue abierto para la siguiente etapa:** el pipeline de protocolos cifrados está versionado y probado como módulo, pero todavía no está conectado al puente de interfaz de XDAT. No se declara soporte completo para todas las variantes cifradas.
- **Otra prueba independiente pendiente:** la ejecución `43401463922` de Silent Dark U/XDAT pasó el round-trip XDAT, pero el compilador UCC se detuvo en `InventoryWnd.uc(1529)` con `Missing function name`. Es un problema de recuperación/compilación de esa interfaz específica, separado de la unificación y del instalador; conservarlo como siguiente tarea funcional.


### 13-11-2026 — Limpieza documental y consolidación de la fuente oficial

- Se revisaron los archivos raíz y la documentación de arquitectura, referencias, workflows, ramas y contribución.
- Se corrigieron referencias obsoletas que describían el antiguo ZIP de compilación XDAT como dependencia de runtime. El archivo actual `Methos-XDAT-Reference-Fixtures.zip` es exclusivamente un conjunto de fixtures de prueba; no contiene aplicación, código Python, DLL ni ejecutables.
- Se actualizó el inventario de `reference-data/` para distinguir claramente el código activo, los datos de referencia y las dependencias UCC que aún requieren las pruebas.
- Se consolidó en README y documentación que `Methos48/Methos`, rama `main`, es la única fuente oficial y activa. U y XDAT son módulos de la misma aplicación.
- Se eliminaron instrucciones contradictorias que sugerían que la integración XDAT todavía dependía de extraer una aplicación Python externa o de overlays de mantenimiento.
- **Pendiente administrativo de Methos:** permanecen ramas históricas inactivas en este repositorio. No se consideran líneas de desarrollo activas; antes de retirarlas hay que auditar sus commits únicos y conservar cualquier material técnico útil.
- **Criterio de conservación:** se mantienen el historial Git, los archivos de referencia y las ubicaciones locales heredadas que protegen datos de usuario. No se borran datos útiles ni respaldos solo para ocultar nombres antiguos.
- Esta limpieza no modifica la aplicación ni sus funciones; prepara la documentación para que el desarrollo futuro continúe exclusivamente en Methos.

### 13-11-2026 — Eliminación de documentación duplicada

- Se auditó `docs/methos-ueditor/` contra la documentación técnica canónica ubicada directamente en `docs/`.
- Se retiraron las 14 copias del subdirectorio heredado. Los documentos canónicos se conservaron en `docs/`; cuando existían diferencias, se mantuvo la versión principal más completa y actualizada.
- No se eliminaron fuentes del motor, fixtures, paquetes originales, dependencias UCC ni herramientas necesarias para compilar o validar Methos.
- La limpieza es documental y no cambia el comportamiento del programa. La compilación de Windows y el archivo de fuentes deben volver a validarse sobre el nuevo HEAD.

### 13-11-2026 — Auditoría final de branches históricas

- Se compararon los árboles de las cinco branches históricas con `main`.
- `migration/methos-unified` y `research/xdat-format-probe` apuntan a árboles vacíos tras sus commits de retiro.
- Las tres branches `work/*` conservan instantáneas anteriores, documentación duplicada, el kit XDAT antiguo y herramientas temporales/overlays ya sustituidos. No se encontró una segunda aplicación ni código activo necesario que falte en `main`.
- Se registró el resultado en `docs/BRANCHES.md`. Las cinco branches pueden eliminarse manualmente cuando se confirme la validación final de `main`; no eliminar `main`.
