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
