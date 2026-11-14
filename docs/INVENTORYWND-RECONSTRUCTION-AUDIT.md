# InventoryWnd reconstruction audit

Date: 2026-11-14  
Source package: AnimalGame `Interface.u`  
Extracted source: `Animal-Rebuilt/UnrealScript/InventoryWnd.uc`  
Validation artifact: `Animal-U-Reconstruction-Validation`

## Verified state

- `InventoryWnd.uc` contains 72 functions and 3,523 lines in the validation artifact.
- The rebuilt `Interface.u` passed the latest package validation: 160/160 classes reopened.
- The clean and edited package outputs were produced by the same successful workflow run.
- No source rename is made in this audit unless the original identifier is supported by direct evidence.

## Anonymous / recovered identifiers

| Current identifier | Verified behavior | Confidence / next action |
|---|---|---|
| `function30(string)` | Event 2600 handler. Parses an item string, routes equipped items to the paperdoll, quest items to the quest pane, and other items to inventory plus category panes. | High-confidence descriptive role: add-item event handler. Compare against the exact AnimalGame source before restoring the original identifier. |
| `function31(ItemInfo)` | Returns true for ItemType 0, 1, or 2 (weapon, armor, accessory category). | Role verified; original name not yet proven. |
| `function32(ItemInfo)` | Returns true for ItemSubType 5 or 6. | Role verified; exact subtype meaning/name needs enum evidence. |
| `function37(string)` | Calls `UpdateHennaInfo()`; invoked for event 2630. | Role verified: henna update event handler. |
| `function44(int)` | Compares the argument with the first two outputs of `GetAccessoryServerID`, returning -1/1/0. No call site found in this class. | Leave unchanged; do not conflate with the used ear/finger helpers. |
| `function26(ItemInfo)` | Returns `bArrow`. | Duplicate behavior exists in named `IsArrow`; inspect call sites before removing or renaming. |
| `function2()` | Reads `TrashList` values from INI section `TrashGrp` and updates `int_2`. | Likely trash-list initialization/restore; exact name unconfirmed. |
| `function13()` | Toggles the automatic-trash button state; enabling it invokes `function46()`. | Role verified; original name unconfirmed. |
| `function47()` | Writes -1 to the current `TrashList` INI entry. | Role verified; likely removal/reset of one trash-list entry, but exact name unconfirmed. |
| `function46()` | Iterates the configured trash list, finds matching inventory items, and calls `RequestDestroyItem`. | Role verified; preserve carefully because it initiates item destruction. |
| `function3()` | Configures expanded/collapsed inventory dimensions, columns, textures, and anchors. | Role verified: inventory layout/size update. |
| `function48(...)` | Maps an integer 1–8 to the corresponding anchor pair and calls `SetAnchor`. | Role verified: generic window-anchor helper. |
| `function14(ItemInfo)` | Empty body; no behavior. | Preserve until references and bytecode are checked. |

## Important cross-check

A public Interlude source was found in `L2jBrasil/InterludeClassicInterface`. Its `InventoryWnd.uc` confirms the conventional names and structure for `HandleAddItem(string)`, `EquipItemUpdate`, `IsLOrREar`, and `IsLOrRFinger`. It is a useful comparison source, but it is not AnimalGame's exact package; therefore it is not sufficient evidence to rename AnimalGame methods automatically.

## Next safe steps

1. Compare the AnimalGame function bodies and call sites against the public Interlude reference, keeping version-specific differences.
2. Resolve `ItemSubType` values 5 and 6 from the matching AnimalGame item-data enum/table.
3. Trace all anonymous functions and duplicate helpers before any renaming.
4. Rebuild and reopen the package after each source change; keep the current clean package as the rollback point.

No gameplay behavior was changed by this audit.


## Investigación adicional: valores de ItemSubType (avance 2026-11-14)

Se revisó el volcado de las 160 clases reabiertas incluido en el artefacto local de validación, buscando usos de `ItemSubType` en todas las clases, no solo en `InventoryWnd`.

Hallazgo confirmado:

- `RecipeBookWnd.AddRecipeBookItem` asigna explícitamente `infItem.ItemSubType = 5` antes de insertar el objeto en `RecipeBookWnd.RecipeItem`.
- En `InventoryWnd`, el clasificador interno de objetos de `ItemType == 5` usa un `switch` de `ItemSubType`; el caso `5` se añade a `RecipeList`.
- Por tanto, **ItemSubType 5 corresponde a recetas** en este build AnimalGame. Esto está respaldado por dos rutas independientes del propio paquete.
- El filtro anónimo `function32` agrupa los valores 5 y 6. El significado del valor 6 todavía no está demostrado; no se le asigna nombre ni categoría por semejanza.
- La búsqueda del volcado de clases no encontró una declaración de enum que defina todos los valores de `ItemSubType`. Los scripts usan conversiones y comparaciones numéricas directas.

Implicación para la reconstrucción:

- `function32` parece ser un filtro de dos subtipos de objetos de `ItemType == 5`, pero su nombre original sigue sin poder recuperarse solo con la evidencia actual.
- No se cambió ningún identificador ni comportamiento en `InventoryWnd.uc`.

## Estado de los insumos

El árbol versionado de `Methos48/Methos` contiene el paquete binario AnimalGame `reference-data/packages/animal/Interface.u` y su `interface.xdat`, además del artefacto de validación con el volcado de clases. No contiene, entre los archivos de fuente actualmente versionados, una tabla completa de ítems del cliente ni una declaración de enum que resuelva el valor 6. La siguiente búsqueda debe centrarse en los datos de ítems del cliente AnimalGame o en una fuente exacta de la misma versión, no en asumir que una fuente Interlude genérica es idéntica.


## Tercera pasada: cruce de ItemSubType con ToolTip (2026-11-14)

Se revisaron todos los usos de ItemSubType en las 160 clases extraídas y, en particular, las conversiones del tooltip:

- ToolTip convierte ItemSubType a UIEventManager.EEtcItemType en dos rutas de tooltip. En la fuente extraída, los valores comparados explícitamente para opciones especiales son 7, 13, 14 y 15. No aparece una comparación que identifique el valor 6.
- La clase UIEventManager y la declaración del enum EEtcItemType no están entre los 160 fuentes reabiertos disponibles en el artefacto; por eso el nombre simbólico del valor 6 no puede recuperarse desde este volcado.
- Dentro del conjunto de scripts extraídos, el valor 6 solo aparece como parte del filtro function32(ItemInfo), junto al valor 5. El valor 5 está confirmado como receta; el 6 permanece sin resolver.
- El control CraftingItem se limpia en HandleClear y se consulta para decidir si se muestra/obtiene el item window. Esto confirma que function32 alimenta una ventana específica del inventario, pero no revela por sí solo la clase exacta del objeto con subtipo 6.

### Estado de nombres

- function37(string): candidato de alta confianza HandleAddHennaInfo(string), por coincidencia exacta de evento 2630, firma y cuerpo con la fuente Interlude comparativa. Se conserva el identificador original hasta validar contra el código fuente/bytecode específico de AnimalGame.
- function30(string): handler de evento 2600; la fuente comparativa lo llama HandleAddItem(string), pero AnimalGame ya contiene HandleAddItem(ItemInfo). No se renombra por el riesgo de colisión y por falta de evidencia del nombre exacto.
- function31(ItemInfo): clasificador de ItemType 0/1/2; comportamiento confirmado, nombre original todavía no recuperado.
- function32(ItemInfo): clasificador de ItemSubType 5/6 destinado al control CraftingItem; nombre original y significado del subtipo 6 pendientes.

No se modificó código ni comportamiento del cliente. Esta pasada amplía la evidencia y evita asignar un significado inventado al subtipo 6.


## Cuarta pasada: referencias y código inalcanzable (2026-11-14)

Se inspeccionaron las referencias textuales y los cuerpos completos de varios identificadores todavía anónimos en el `InventoryWnd.uc` reabierto:

- `function14(ItemInfo)`: solo aparece su declaración; el cuerpo está vacío y no se encontró ninguna llamada dentro de la clase. No hay base para asignarle un nombre funcional. Mantenerla sin cambios hasta comprobar referencias externas/bytecode.
- `function26(ItemInfo)`: solo aparece su declaración y devuelve `bArrow`. Existe `IsArrow(ItemInfo)` con el mismo resultado. La duplicación está confirmada dentro de la clase, pero no hay llamadas textuales a `function26`; no eliminarla sin revisar referencias compiladas externas.
- `function44(int)`: solo aparece su declaración y no tiene llamadas textuales dentro de `InventoryWnd`. Compara el ID recibido con los dos primeros IDs de `GetAccessoryServerID` y devuelve -1, 1 o 0. Los helpers nombrados `IsLOrREar` y `IsLOrRFinger` consultan otros pares de IDs; no hay evidencia suficiente para afirmar que `function44` sea uno de ellos.
- `MethosRecoveredName_444()`: tiene una llamada desde `HandleGamingStateEnter`. El cuerpo ejecuta `vvCanUseSetEquipment = true` y retorna inmediatamente. Después del `return` existe código que obtiene `UserInfo` y compara el nombre del jugador con una lista de siete nombres, pero ese bloque es inalcanzable en el flujo de control extraído. No debe describirse como una whitelist activa: el comportamiento ejecutable observado es habilitar `vvCanUseSetEquipment` sin condición.
- `MethosRecoveredName_1105(ItemInfo)`: solo se encontró su declaración en esta clase; no hay llamada textual interna. Su cuerpo, si `vvCanUseSetEquipment` está activo, recorre las piezas de set de `GetSetItemIDList` y solicita usar las piezas faltantes que encuentra en el inventario. Esto confirma su función descriptiva, pero no su nombre original ni que el método se invoque en este build por una ruta externa.

### Consecuencia para la reconstrucción

La falta de llamadas internas no prueba que una función sea inútil: puede ser invocada desde otra clase, por reflexión/delegados o por bytecode no representado como llamada textual. Por eso no se borraron funciones duplicadas o sin referencias. También se registra el `return` temprano de `MethosRecoveredName_444` como un hallazgo del fuente extraído, no como una corrección propuesta.

No se modificó `InventoryWnd.uc` ni el comportamiento del cliente.


## Quinta pasada: flujo de borrado rápido y lista Trash (2026-11-14)

Se siguieron las referencias y los puntos de entrada de `function2`, `function13`, `function46` y `function47` en `InventoryWnd.uc`:

- `function2()` se invoca durante `OnLoad`, después de `InitHandle`. Recorre las posiciones 1 a 99 de la sección INI `TrashGrp`, clave `TrashList`, y cada vez que encuentra un valor cero actualiza `int_2` con el índice anterior. Es una inicialización del límite/última posición vacía de la lista; no es el ejecutor del borrado.
- `function13()` se invoca al pulsar `TrashButton`. Alterna `bool_1`, cambia las texturas del botón entre estado normal y automático y, al activar el modo automático, llama inmediatamente a `function46()`.
- `function46()` recorre la lista configurada desde la posición 1 hasta `int_2`. Lee cada ClassID guardado, busca ese objeto en `m_invenItem` y, si lo encuentra, ejecuta `RequestDestroyItem(ServerID, ItemNum)`. Si encuentra un valor cero, termina el recorrido. Esto confirma que el modo automático puede solicitar la destrucción de objetos configurados; no es solo un cambio visual.
- `function47()` escribe -1 en la entrada `TrashList[int_2]` de `TrashGrp`. No se encontró ninguna llamada textual a esta función dentro de `InventoryWnd`; por tanto, su propósito parece relacionado con limpiar/deshabilitar una entrada, pero el punto de invocación original no está recuperado.
- En el manejo de arrastre hacia `TrashButton`, los objetos apilables con más de una unidad abren un diálogo para elegir cantidad o confirmar el lote; los demás abren una confirmación de borrado. Es una ruta distinta del modo automático.

### Nombres descriptivos candidatos (no restaurados)

- `function2`: `LoadTrashList` / inicialización del índice de la lista; nombre original no probado.
- `function13`: alternar el modo de borrado automático; nombre original no probado.
- `function46`: ejecutar la lista de borrado automático; nombre original no probado.
- `function47`: marcar una posición de la lista como vacía/deshabilitada; nombre original y uso no probados.

Se mantienen los identificadores anónimos porque no se encontró una fuente exacta de AnimalGame que demuestre los nombres originales. No se ejecutaron acciones de borrado ni se modificó código del cliente.


## Sexta pasada: código inalcanzable en el cambio de tamaño (2026-11-14)

Una lectura de control de flujo de todas las funciones del `InventoryWnd.uc` reabierto encontró un caso adicional que cambia la interpretación de la reconstrucción:

- `function3()` se invoca desde `OnLoad` y desde el evento/callback que alterna `ExpandedInventory`.
- Sin embargo, su primera instrucción ejecutable es `return;`. Todo el bloque posterior —ajuste de tamaño 673x400/566x400, columnas 12/9, texturas y anclajes— es inalcanzable en el fuente extraído.
- `function48(...)` solo es llamado desde ese bloque inalcanzable. Por tanto, aunque implementa correctamente el mapeo de anclajes 1–8 a `SetAnchor`, no se ejecuta desde las llamadas encontradas en esta clase.
- Se hizo un barrido de las funciones del archivo buscando otros cuerpos que comiencen con `return;` y contengan instrucciones posteriores. `function3()` fue el único caso detectado con ese patrón.
- El botón de expandir/contraer aún cambia la opción `ExpandedInventory` y vuelve a llamar `function3()`, pero con el `return` inicial no aplica el cambio de dimensiones. Esto es consistente con una rutina deshabilitada en el código extraído, aunque no demuestra por qué fue deshabilitada ni si existe otra ruta externa.

### Corrección de interpretación

La descripción anterior de `function3` como rutina que configura efectivamente el inventario expandido/contraído era incompleta. Su cuerpo contiene esa lógica, pero está anulada por el retorno inicial. La descripción correcta es: rutina de ajuste de layout presente pero inactiva en el fuente extraído. `function48` es un helper de anclaje cuyo único conjunto de llamadas conocido también queda inactivo.

No se quitó el `return`, no se activó el redimensionamiento y no se cambió el comportamiento del cliente. Antes de considerar restaurar esa rutina hay que comprobar si el `return` existe en el bytecode original de AnimalGame o fue introducido por la descompilación/reconstrucción.


## Séptima pasada: verificación de `function3` contra las tres extracciones (2026-11-14)

Se comparó `InventoryWnd.uc` en los tres resultados locales del artefacto de validación:

- `UnrealScript/InventoryWnd.uc` (exportación directa del `Interface.u` AnimalGame original).
- `Reopened-Clean/InventoryWnd.uc` (paquete reconstruido limpio, compilado y reabierto).
- `Reopened-Edited/InventoryWnd.uc` (paquete de prueba editado, compilado y reabierto).

En los tres aparece el mismo `function function3()` con `return;` inmediatamente después de declarar sus variables locales, antes de obtener los handles y antes de todo el bloque de redimensionamiento. El bloque posterior también se conserva en los tres archivos. Por tanto, el retorno temprano no fue añadido por la edición de prueba ni por la reapertura del paquete reconstruido: ya está presente en la extracción directa del paquete AnimalGame.

Se contrastó además con `UnrealScriptSourceIndex.RemoveRedundantTrailingReturns`, la reparación aplicada durante la reconstrucción. Esa rutina solo busca y normaliza un `return;` que sea el último statement de una función; no elimina ni mueve un retorno inicial. Así que no es la responsable del `return` temprano.

### Conclusión y límite

La evidencia disponible permite afirmar que el `return;` temprano está en la representación descompilada del paquete AnimalGame original y se conserva en la ida y vuelta. Todavía no se ha inspeccionado el flujo de bytes de script de la exportación `function3` con un desensamblador de bytecode independiente; por ello no se declara probado si el salto/retorno temprano corresponde exactamente a una instrucción real del bytecode o a un defecto específico de UELib al reconstruir el fuente.

No se quitó el retorno. No se activó el cambio de tamaño y no se alteró el paquete del usuario. La próxima comprobación técnica necesaria es extraer el bytecode crudo de la exportación de `function3` y contrastar sus offsets con el flujo de control que UELib produjo.
