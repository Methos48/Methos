# L2Sudamerica — auditoría de interfaz

Fecha de actualización: 2026-11-14

## Alcance y separación

Este registro corresponde exclusivamente al cliente Interlude de L2Sudamerica. No mezclar con Berserker, AnimalGame, otros servidores, Methos-Interface ni interfaces de terceros. Los paquetes originales deben conservarse inmutables.

## Descargas oficiales localizadas

Página oficial: https://www.l2sudamerica.com/?page=download

La página publica dos variantes:

| Variante | Descripción publicada | Descarga oficial |
|---|---|---|
| Interface 2.0 | Más liviana, menor carga gráfica | https://drive.google.com/file/d/1EcLm1ln28486fSbX624VxPH_8iDksHa8/view?usp=drive_link |
| Interface 3.0 | Más completa, interfaz más moderna y detallada | https://drive.google.com/file/d/1Qn5_BPk1AD8NPBpGWlJAys57eWtmOUSj/view?usp=drive_link |

Espejos oficiales publicados para 2.0:

- MediaFire: https://www.mediafire.com/file/z6ja61scxvy4xmu/l2Sudamerica_fafureon_20.rar/file
- MediaFire mirror 1: https://www.mediafire.com/file/mag7cknt9aoddgo/l2Sudamerica_fafureon_20_-_miror_1.rar/file

Los dos mirrors de MediaFire muestran un archivo RAR de 3.54 GB. El primero figura cargado el 2023-06-09 y el segundo el 2023-06-10. Los enlaces Google Drive y MediaFire son enlaces publicados por la página oficial. El tamaño y las fechas describen esos mirrors; no prueban que su contenido sea idéntico a la versión actual del servidor.

Updater obligatorio publicado por el servidor:

- ZIP directo: https://www.l2sudamerica.com/L2SudamericaUpdater.zip
- Mega: https://mega.nz/file/DNkyAIAC

La página indica ejecutar el updater después de descomprimir el cliente y antes del primer inicio.

## Estado de adquisición

- Los enlaces fueron localizados en la página oficial y abiertos para identificar sus destinos.
- Los mirrors MediaFire muestran landing pages activas y el tamaño de 3.54 GB.
- **No se ha descargado ni inspeccionado todavía ninguno de los RAR de 2.0/3.0 ni el updater.**
- Por tanto, no se registra ningún SHA-256 ni se afirma que los paquetes contengan archivos concretos hasta descargarlos y examinarlos.
- La descarga del updater ZIP fue localizada, pero la lectura web no pudo recuperar el binario por el tipo de contenido ZIP. Mega tampoco pudo abrirse desde la herramienta de consulta.

## Trabajo técnico previo sobre la copia original de L2Sudamerica

Según el registro de trabajo anterior:

- `Interface.u`: extracción de 150/150 clases sin errores.
- `interface.xdat`: se probó una modificación real y se verificó la reconstrucción.
- Recompilación y reprotección de `Interface.u`: preparadas, pero el resultado final todavía está pendiente.
- Próxima prueba técnica: modificar una clase `.uc` de forma controlada, recompilar, aplicar nuevamente la protección compatible y comprobar que el cliente conserva el cambio.
- No declarar la prueba cerrada hasta inspeccionar el archivo resultante y verificar su funcionamiento.

## Próximos pasos

1. Descargar las variantes oficiales 2.0 y 3.0, preferiblemente en una carpeta de trabajo separada.
2. Calcular SHA-256 de cada archivo descargado antes de extraerlo.
3. Inventariar el contenido sin modificar los originales: nombres, tamaños, fechas y presencia de `Interface.u`, `interface.xdat`, DLL y archivos de sistema.
4. Comparar las dos variantes y determinar qué protección usa cada archivo.
5. Completar la prueba controlada de recompilación/reprotección de `Interface.u`.
6. Registrar hashes de originales y derivados por separado.


## Reconciliación con el repositorio (2026-11-14)

Se inspeccionó el árbol completo de la rama `main` y se localizaron finalmente los dos binarios originales dentro de `reference-data/packages/l2sudamerica/`:

| Archivo | Tamaño | Git blob SHA |
|---|---:|---|
| `Interface.u` | 987,258 bytes | `5f3943d7faf7b85386873524fe2f664e28867549` |
| `interface.xdat` | 940,381 bytes | `93bc3ceb3fd5999da3aaa98e5b18d3607422686a` |

Corrección del estado anterior: los binarios sí están versionados en el repositorio; lo que no estaba disponible en el workspace de esta conversación era una copia local montada para ejecutar las herramientas. Los SHA indicados son identificadores Git blob, no hashes SHA-256.

### Qué queda pendiente

- Descargar/materializar ambos blobs en un entorno de trabajo que permita procesar binarios.
- Calcular SHA-256 independientes y registrar metadatos.
- Verificar que el `Interface.u` versionado corresponde al mismo paquete del que provino la extracción histórica de 150/150 clases.
- Repetir/validar la extracción contra ese binario concreto.
- Ejecutar la prueba controlada de recompilación y reprotección; no se considera terminada por el mero hecho de que existan los binarios en GitHub.

Los originales versionados se mantienen inmutables. No se modificó ningún binario en esta pasada.


## Auditoría del pipeline disponible en Methos (2026-11-14)

Se revisaron los componentes existentes antes de intentar transformar el paquete:

- `src/Methos.Engine/UnrealPackageDecompiler.cs`: usa UELib para abrir paquetes UE y exportar objetos de clase a fuentes; conserva un mapa de nombres saneados y aplica reparaciones controladas al código decompilado.
- `src/Methos.Engine/UnrealScriptCompilerPipeline.cs`: permite compilar cuerpos de función limitados mediante el generador de bytecode actual, reconstruye el paquete y comprueba que el bytecode insertado coincide con el generado. **No equivale todavía a recompilar sin restricciones una interfaz completa de Interlude.**
- `reference-data/ucc-runtime/System/Compilar.ps1`: ejecuta UCC en una carpeta de prueba `Interlude\\System`, valida el paquete producido y copia el resultado a `Testar\\system`. Es un arnés de prueba genérico; no está configurado por sí solo para el paquete L2Sudamerica.
- `reference-data/ucc-runtime/System/ValidatePackage.ps1`: comprueba cabecera, tablas de nombres y exports, offsets y nombres de exports públicos.
- `tools/xdat/README.md`: el módulo XDAT documenta soporte canónico para reconstrucción ct0 y pruebas round-trip; los protocolos cifrados se mantienen separados y un protocolo desconocido debe detener el proceso sin alterar el original.

### Intento de acceso a los binarios

Los dos blobs están presentes en GitHub y sus tamaños/SHA Git quedaron registrados. La lectura binaria directa del conector GitHub falla al intentar decodificar el contenido como UTF-8; por ello no fue posible materializarlos en este entorno para calcular SHA-256 ni ejecutar el decompilador/UCC. No se ha alterado ninguno de los blobs.

### Conclusión técnica de esta pasada

El repositorio ya contiene piezas útiles para descompilar, compilar bytecode de funciones concretas, reconstruir paquetes y validar estructuras. No hay evidencia suficiente para afirmar que el pipeline completo de recompilación y reprotección de L2Sudamerica esté cerrado. Antes de una prueba real hay que materializar los dos binarios y determinar la protección efectiva de cada uno; luego se debe probar el UCC/runtime contra el paquete exacto, en una copia de trabajo aislada.


## Recuperación binaria desde GitHub — 2026-11-14 (prueba adicional)

Se confirmó una vía de lectura que evita el error UTF-8 de `fetch_blob`: el endpoint de contenidos del repositorio permite solicitar `encoding=base64` mediante `fetch_file`. La lectura respondió correctamente para ambos originales y devolvió los mismos Git blob SHA ya registrados:

| Archivo | Git blob SHA | Longitud Base64 recibida |
|---|---|---:|
| `reference-data/packages/l2sudamerica/Interface.u` | `5f3943d7faf7b85386873524fe2f664e28867549` | 1,338,284 caracteres |
| `reference-data/packages/l2sudamerica/interface.xdat` | `93bc3ceb3fd5999da3aaa98e5b18d3607422686a` | 1,274,742 caracteres |

La cabecera Base64 de ambos empieza por `/+36zgE`, consistente con que se recibieron datos binarios y no texto fuente. Esto confirma que GitHub entrega los contenidos íntegros en Base64 a través del conector. Sin embargo, la respuesta de esa herramienta no crea un archivo local ni entrega un identificador de archivo materializable; por ello todavía no se han decodificado los bytes en el entorno de trabajo, calculado SHA-256, ni ejecutado el decompilador/UCC/XDAT sobre estos dos binarios. No se modificó ningún original del repositorio.

**Siguiente bloqueo técnico:** transferir esa respuesta Base64 a un archivo binario local de forma verificable. No se debe declarar iniciada la descompilación hasta disponer de los archivos locales y comprobar sus hashes.


## Resultado de descifrado y descompilación real — 2026-11-14

****Prueba automatizada aprobada.**** Workflow: https://github.com/Methos48/Methos/actions/runs/43579091099

Se procesaron los dos binarios exactos del directorio `reference-data/packages/l2sudamerica/`, sin tocar sus originales.

### Perfil de protección detectado

- Marcador SmartCrypt: `FF ED FA CE`.
- Selector del seed: `0x01`; segundo byte de estado: `0x00`.
- La bandera de clave externa está desactivada: el seed de 24 bytes del propio archivo permite derivar el perfil.
- Segunda capa: `Lineage2Ver111`.
- Las capas SmartCrypt 1.5.2.2 y Lineage2Ver111 se volvieron a codificar y reprodujeron exactamente los bytes protegidos originales en ambos archivos.

### Interface.u

| Dato | Resultado |
|---|---:|
| Original protegido | 987,258 bytes |
| SHA-256 original | `c1399078d889b42b7224efeae50df083a2456eeea142afd6dda68afe7f8b606b` |
| Intermedio tras SmartCrypt | 987,258 bytes |
| Paquete Unreal descifrado | 987,230 bytes |
| SHA-256 descifrado | `877a6873fecfcb141b603010524f4601634ab5f31de5ae3371a1705b76f5dbc9` |
| Versión Unreal / Licensee | 123 / 37 |
| Nombres / imports / exports | 4,987 / 793 / 9,433 |
| Objetos / clases | 10,226 / 150 |
| Clases exportadas / errores | 150 / 0 |

La variante correcta conserva los últimos 20 bytes dentro del payload Unreal. La hipótesis de separarlos como tráiler falla al analizar el paquete. UELib exportó las 150 clases sin errores.

### interface.xdat

| Dato | Resultado |
|---|---:|
| Original protegido | 940,381 bytes |
| SHA-256 original | `97c1c35a8206ebc7d2ce97d891a689a1fcc1887897390f6d93dedb7ba9417269` |
| XDAT descifrado | 940,353 bytes |
| SHA-256 descifrado | `ce63c0c1972812621d4553b7e2dba8041bd917e3c1d220a25cdd11599777bafb` |
| Ventanas / shortcuts | 169 / 8 |
| Reconstrucción XML → XDAT | Coincidencia binaria exacta |

También aquí los últimos 20 bytes pertenecen al payload. La variante que los separa no supera el parser. El XML de las 169 ventanas se exportó y la reconstrucción produjo el mismo XDAT descifrado byte por byte.

### Conclusión y límites

- [x] Descifrado SmartCrypt y Lineage2Ver111 de ambos originales.
- [x] Round-trip exacto de las dos capas de protección.
- [x] 150 clases Unreal exportadas sin errores.
- [x] 169 ventanas y 8 shortcuts XDAT exportados; round-trip exacto.
- [ ] La recompilación UCC del paquete L2Sudamerica aún no se ha ejecutado. La extracción de fuentes no equivale a una compilación completa.
- [ ] La reprotección final de un paquete U/XDAT editado debe verificarse por separado con el perfil original.

El artefacto del workflow conserva los binarios descifrados, las 150 fuentes `.uc`, el XML XDAT y los informes de verificación durante 30 días. No se reemplazó ningún archivo original.


## Inventario estructurado completo — 2026-11-14

El workflow actualizado ejecutó `tools/l2sa_audit/extract_interface_inventory.py` y generó los inventarios tabulares/JSON a partir de los 150 fuentes UnrealScript y los 169 XML de ventanas:

| Inventario | Registros |
|---|---:|
| Clases exportadas | 150 |
| Declaraciones de variables de clase | 1,028 |
| Constantes de clase | 372 |
| Firmas de funciones | 2,245 |
| Ventanas XML | 169 |
| Shortcuts XML | 8 |
| Controles XDAT con nombre | 4,092 |

Los CSV incluyen clase, línea de origen, tipo, identificador seguro y FName original cuando está disponible. El JSON reúne variables, constantes y funciones por clase. Los controles XDAT incluyen ventana, tipo, nombre y propiedades de tamaño/anclaje cuando existen. Las variables locales de funciones no se mezclan con los campos de clase; las declaraciones de arreglos se cuentan como una declaración y conservan su longitud.

- Extractor reproducible: `tools/l2sa_audit/extract_interface_inventory.py`.
- Auditoría específica de la clase: [L2SUDAMERICA-INVENTORYWND-AUDIT.md](L2SUDAMERICA-INVENTORYWND-AUDIT.md).
- Workflow y artefactos de esta ejecución: https://github.com/Methos48/Methos/actions/runs/43579091099.


## Verificación del paquete de trabajo recuperado — 2026-11-14

Se materializó desde la biblioteca de archivos el archivo L2Sudamerica-Interface-Decodificada.zip (1,300,504 bytes) para inspeccionar sus contenidos. El ZIP contiene 341 archivos:

- Decoded-Binaries/Interface.u: 987,230 bytes; SHA-256 877a6873fecfcb141b603010524f4601634ab5f31de5ae3371a1705b76f5dbc9.
- Decoded-Binaries/interface.xdat: 940,353 bytes; SHA-256 ce63c0c1972812621d4553b7e2dba8041bd917e3c1d220a25cdd11599777bafb.
- 150 fuentes UnrealScript .uc.
- 169 XML de ventanas y 8 XML de shortcuts.
- Inventarios CSV/JSON e informes de verificación.

Los SHA-256 de los dos binarios descifrados coinciden con los valores registrados en Verification-Reports/. Esto verifica la integridad de los derivados contenidos en el ZIP respecto de sus informes; no sustituye la repetición independiente del descifrado desde los originales protegidos.

### Estado después de inspeccionar el ZIP

- [x] Archivo ZIP recuperado y abierto correctamente.
- [x] Conteo de archivos y fuentes .uc comprobado.
- [x] Hashes SHA-256 de los dos binarios descifrados recalculados y coincidentes con los informes.
- [x] Conteos XDAT de ventanas y shortcuts comprobados.
- [ ] Repetir el proceso completo desde los originales protegidos en el entorno actual.
- [ ] Recompilar Interface.u con UCC y validar el paquete resultante.
- [ ] Editar y reconstruir XDAT desde XML con una prueba controlada en el entorno actual.
- [ ] Reproteger ambos derivados y comprobar round-trip protegido exacto.

Nota de alcance: la inspección actual confirma los artefactos históricos empaquetados, pero no prueba que una interfaz editada haya sido recompilada ni que el cliente L2Sudamerica la haya cargado correctamente. Los originales versionados permanecen intactos.
