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

- **Nombre único del producto:** Methos.
- **Repositorio único de trabajo:** https://github.com/Methos48/Methos (rama `main`).
- **Aplicación única:** `Methos.exe`, con dos módulos accesibles desde la misma interfaz: **ARCHIVOS U** y **ARCHIVOS XDAT**.
- U y XDAT no son dos productos ni dos instaladores. Se mantienen sus motores, dependencias y pruebas dentro del mismo proyecto; cualquier proceso auxiliar interno no cambia esta regla de producto.
- No duplicar la aplicación, no crear un ejecutable de producto separado para XDAT y no dividir el desarrollo en repositorios independientes.
- La compilación Windows (run `43387205032`) y la validación UCC (run `43386687102`) finalizaron correctamente. Esto acredita los pasos automatizados ejecutados, no compatibilidad universal ni pruebas completas dentro del cliente real.
- Artefacto Windows más reciente: `Methos-Windows-Installer-x64`, ID `14018119516`, tamaño 77,736,613 bytes.

## Registro de avances

### 13-11-2026 — XDAT desde fuente canónica (validación Windows superada en PR)

- La comparación del kit histórico confirmó que `app.py` no tiene equivalente en Methos y que `tools/xdat/xdat_core.py` es más completo que la copia antigua (incluye compatibilidad Latin-1 y preservación de listas originales).
- La rama `work/xdat-runtime-source` elimina los overlays que modificaban la antigua interfaz Python. El ZIP se usa solo para extraer fixtures de referencia; no se ejecuta ni se copia su `app.py` al runtime.
- El runtime copia directamente `tools/xdat/xdat_core.py` y `src/Methos.App/XdatBridge.py`. Las pruebas se ejecutan desde `tests/` y `tools/xdat/tests/` contra las fuentes del repositorio con `XDAT_REFERENCE_ROOT`.
- **Validación:** Windows Release pasó en el PR: compilación, pruebas Python, creación del instalador, instalación silenciosa y arranque de `Methos.exe`.
- El ZIP histórico se conserva como archivo de fixtures. `auto_pipeline.py` sigue sin estar conectado a la interfaz C#; no se declara integrado el uso de `l2encdec.exe` ni el soporte de todas las variantes cifradas.

### 13-11-2026 — Normalización de nombres internos (rama de revisión)

- La rama `work/unification-audit` reemplaza los nombres técnicos heredados por `Methos.App` y `Methos.Engine`: proyecto de interfaz, biblioteca interna, namespaces, pruebas y rutas de workflows.
- El ejecutable de producto sigue siendo uno solo: `Methos.exe`. `Methos.Engine` es una biblioteca interna que no se distribuye como aplicación separada.
- La carpeta duplicada `methos_crypto/` ya fue eliminada de `main` por la consolidación paralela; esta rama se basa en el `main` actualizado y mantiene el motor canónico en `src/Methos.Engine/`.
- **Estado:** los cambios estructurales están preparados en la rama de revisión, pero todavía requieren compilación y pruebas Windows. No están incorporados a `main` ni se consideran validados.
- **Bloqueo restante para completar la unificación:** el workflow de lanzamiento todavía extrae parte del motor XDAT desde el kit histórico; migrarlo a `tools/xdat/` requiere comparación funcional y pruebas de round-trip antes de retirar el ZIP.

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

1. **Validar la versión actual en Windows CI:** comprobar compilación, generación del icono, instalador, acceso directo, instalación silenciosa y arranque. Revisar logs si falla y corregir antes de dar por finalizada la interfaz.
2. **Revisar el recorrido real de ARCHIVOS U:** selector de archivos, copia de seguridad, descompilación, importación de fuentes, compilación, exportación y preservación de la protección original. Probar con archivos de referencia y verificar integridad/round-trip.
3. **Revisar el recorrido real de ARCHIVOS XDAT:** selección, copia de seguridad, detección, descifrado, edición, recompilación y validación. Separar claramente protocolos confirmados de los desconocidos.
4. **Investigar la segunda interfaz personalizada:** utilizar los dos lados (archivo protegido y archivo descifrado) que el usuario mantiene en sus repositorios Methos. No asumir que es Dunk3l ni confundirla con el ejemplo XDAT del proyecto.
5. **Integrar cada método nuevo solo después de probarlo** con archivos de referencia y comprobar que el resultado vuelve a ser legible por el formato/herramienta correspondiente.
6. **Preparar la entrega final llamada Methos:** instalador Windows, código fuente, instrucciones claras y registro actualizado en GitHub.


- **Validación final de la unificación (13-11-2026):**
  - GitHub Actions UCC: [ejecución 43386687102](https://github.com/Methos/actions/runs/43386687102) — SUCCESS. Compiló las 169 fuentes, validó la estructura del paquete, pasó las pruebas unitarias, la modificación real de bytecode y la comprobación de descompilación/recompilación.
  - Methos Windows x64: [ejecución 43386686912](https://github.com/Methos/actions/runs/43386686912) — SUCCESS. Generó el ejecutable, integró UCC y XDAT, probó instalación silenciosa y arranque, y confirmó que no se instala un segundo ejecutable XDAT.
  - Instalador: artefacto `Methos-Windows-Installer-x64`, ID `14019051859`, tamaño 77,735,569 bytes. SHA-256: `73937bc92cb5051d9b2270167b88b6d23c08043a95187b4e91a727078f8a8ea5`.
  - La fuente del instalador y los artefactos de validación están publicados en el repositorio único `Methos`.
- **Limpieza del repositorio anterior:** el contenido de `Methos-U-Editor` quedó vaciado en sus ramas después de la migración. Su historial Git permanece como respaldo histórico; no es un repositorio de trabajo.

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


- **Auditoría adicional de dependencias:** `src/Methos.Engine/` es el motor compilado por el proyecto .NET. `methos_crypto/` contiene variantes históricas/experimentales y no debe confundirse con la carpeta que compila la aplicación. Asimismo, `tools/xdat/xdat_core.py` está versionado, pero el workflow de lanzamiento todavía extrae el motor XDAT del ZIP histórico `Methos_XDAT_Windows_Build_Kit_v1.0 (1).zip`. Esta duplicación quedó documentada como pendiente; no se borró el ZIP ni se afirmó que `tools/xdat` ya sea la única fuente efectiva del instalador.
- **Compatibilidad de datos:** se mantienen rutas antiguas de caché y respaldos bajo `%LOCALAPPDATA%` para no romper datos existentes; sus nombres históricos no representan programas separados.

- Se retiró también el workflow temporal `.github/workflows/migrate-unified.yml`: la migración del repositorio ya está completada y no debe repetirse como tarea activa.

- Se amplió `README-ES.txt` con el enlace al repositorio oficial y la explicación de que las rutas locales heredadas de caché/respaldo no son una segunda aplicación.
- Se añadió `.gitignore` para excluir salidas de compilación, cachés temporales y el espacio de trabajo local `Compilados/`, sin ignorar de forma global ejecutables o DLL que puedan ser referencias legítimas.

- **Corrección de workflows heredados:** se eliminaron las referencias de checkout a `Methos48/Methos-U-Editor` en los workflows de inspección Dunk3l y validación Unreal. Ahora usan el kit UCC del repositorio único y el paquete de referencia ubicado en `reference-data/Interface-UEditor.u`. Se actualizaron también sus filtros de rutas.

- **Organización del código XDAT:** se trasladaron los módulos `encryption_detection.py`, `auto_pipeline.py` y `package_origin_metadata.py` desde la raíz a `tools/xdat/`; los scripts de preparación `apply_detection_layer.py` y `auto_pipeline_overlay.py` pasaron a `tools/xdat/maintenance/`. Se actualizaron los imports de las pruebas Python y las rutas/filtros del workflow `windows-release.yml`. Los cinco archivos antiguos de la raíz se eliminaron después de verificar las copias nuevas.
- La fuente XDAT todavía no está completamente unificada: el workflow Windows continúa extrayendo el kit ZIP histórico para preparar el runtime de la app. La siguiente etapa debe sustituir esa dependencia mediante migración comprobada y pruebas, no borrando el ZIP.

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
- Se comprobó que el repositorio auxiliar `Methos48/Methos-Interface` ya fue vaciado en el commit `1b99c0fb3190e81db8f1ec0bbc5c8654bd22bd3b` tras migrar su contenido. Su historial queda como respaldo; no contiene ya una segunda línea de desarrollo.
- Se comprobó que el workflow Windows reciente `43387205032` terminó correctamente: publicación de Methos x64, integración de los runtimes UCC/XDAT, instalación silenciosa, prueba de arranque y verificación de que no se instala un segundo ejecutable de aplicación XDAT.
- **La unificación del producto y del repositorio está consolidada. La unificación completa de la fuente XDAT todavía NO está terminada.** El workflow de distribución aún extrae la aplicación base y `xdat_core.py` del kit histórico `reference-data/kits/Methos-XDAT-Windows-Build-Kit.zip`, aplica las capas de mantenimiento y prepara el runtime interno. En paralelo, el repositorio mantiene código XDAT en `tools/xdat/`. No se debe borrar el kit ni declarar que existe una única fuente efectiva hasta comparar ambas versiones, migrar las piezas faltantes y pasar pruebas de reconstrucción e instalación.
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
- **Pendiente de integración XDAT:** Windows Release todavía necesita el kit histórico `reference-data/kits/Methos-XDAT-Windows-Build-Kit.zip` para parte del runtime/código base. No retirar el kit hasta migrar esa función a `tools/xdat/` y validar extracción, reconstrucción, protección y round-trip.
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
- Confirmado: el repositorio histórico auxiliar `Methos-Interface` quedó sin contenido activo; el repositorio de trabajo es `Methos`.
- Límite que no se debe ocultar: la compilación XDAT todavía obtiene del archivo de referencia `Methos-XDAT-Windows-Build-Kit.zip` la aplicación base `app.py`, las pruebas/fixtures originales y `l2encdec.exe`. El ZIP se conserva como material de compatibilidad; no es otro producto ni un segundo instalador. Retirarlo sin portar y probar esos componentes rompería la compilación o perdería compatibilidad.
- La carpeta `tools/xdat/tests` contiene una prueba de round-trip que requiere el conjunto de fixtures XDAT del kit. La comparación automatizada de fuentes queda en el workflow como auditoría de diferencias.
- **Verificación de este bloque:** se ha revisado el workflow y la estructura actual del repositorio. Esta actualización activa una nueva ejecución Windows Release; hasta consultar su resultado no se declara aprobada la compilación de este commit.
- **Criterio de cierre:** Methos está unificado como un solo producto y un solo ejecutable; la eliminación completa de la dependencia del kit XDAT queda abierta y explícitamente documentada, porque todavía faltan migración y validación funcional de sus componentes restantes.
