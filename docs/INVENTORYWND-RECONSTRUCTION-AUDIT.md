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

## Octava pasada: ruta de inspección del bytecode nativo (2026-11-14)

Se revisó UELib 1.13.7 y se confirmó una ruta de diagnóstico más precisa que comparar nuevamente los fuentes reabiertos:

- UELib expone un deserializador de bytecode por función (UStruct.UByteCodeDecompiler).
- Cada token conserva su posición lógica (Position) y su posición/tamaño almacenados (StoragePosition, StorageSize).
- El deserializador lee el flujo delimitado por ScriptOffset y ByteScriptSize; permite localizar el primer token real de InventoryWnd.function3 y seguir los offsets de salto/retorno.
- UELib captura errores durante la lectura y puede detenerse ante un token no reconocido. El diagnóstico debe registrar si alcanzó el final del tamaño de script para no confundir una deserialización parcial con evidencia completa.

La comprobación se hará en el paquete Animal original. Se inspeccionarán el token inicial, los saltos y el token Return con sus offsets almacenados. Si el bytecode muestra un salto que evita el bloque de layout, el retorno temprano es real. Si el bytecode contiene el bloque antes del retorno y UELib lo omite o altera, habrá evidencia concreta de un defecto de descompilación.

No se ha cambiado el bytecode, no se ha eliminado el return; y no se ha activado el redimensionamiento. Esta pasada fija el método de comprobación y evita una corrección especulativa.

## Novena pasada: límite real del flujo binario (2026-11-14)

Al revisar el código fuente de UELib se detectó una distinción importante: UStruct.ScriptSize no es el tamaño del bytecode; el límite que usa el propio deserializador es el campo privado ByteScriptSize. La primera versión del diagnóstico comparaba el final de los tokens con ScriptSize, por lo que su indicador COMPLETE podía ser incorrecto.

Se corrigió el diagnóstico para leer ByteScriptSize por reflexión y comparar contra ese límite exacto. Ahora informa ambos tamaños por separado y calcula COMPLETE respecto del flujo binario real. Esto es necesario porque UELib captura internamente excepciones durante la deserialización y puede devolver una lista parcial de tokens sin propagar el error.

La prueba automática está integrada en el workflow, pero aún falta obtener una ejecución terminada y revisar la salida del diagnóstico. No se ha modificado InventoryWnd ni el bytecode original.

## Décima pasada: primer resultado del bytecode original (2026-11-14)

La primera ejecución válida del lector sobre el `Interface.u` original de Animal encontró exactamente `InventoryWnd.function3` (paquete 126/0). UELib informa `SCRIPT_OFFSET=23`, `DATA_SCRIPT_SIZE=1196`, `BYTE_SCRIPT_SIZE=1366` y 248 tokens. La salida inicial de tokens es inequívoca: posición 0, opcode `0x04`, tipo `ReturnToken`, almacenamiento 0 y tamaño 2; inmediatamente después, posición 1, opcode `0x0B`, tipo `NothingToken`. A partir de la posición 2 comienza el cuerpo de instrucciones (`LetToken`). Esto demuestra que el retorno temprano no es únicamente una anomalía textual del descompilador: está presente en el flujo binario original y precede al resto del cuerpo.

La primera impresión intentó llamar `Decompile()` en cada token y UELib lanzó `NullReferenceException` al formatear un `JumpIfNotToken`. No se debe interpretar ese error como corrupción del paquete: la deserialización ya había producido los tokens iniciales y el fallo ocurrió al generar texto. Se retiró esa llamada y se cambió el diagnóstico a una salida estructural segura (posición, opcode, tipo y tamaños), para recorrer todo el flujo sin ejecutar el formateador defectuoso. Queda pendiente confirmar que los 1366 bytes se deserializan completos y revisar destinos de saltos/control de flujo. No se modificó `InventoryWnd`.

### Resultado completo de la pasada de tokens y corrección de offsets

La pasada del 2026-11-14 deserializó 248 tokens de `InventoryWnd.function3` sin error de lectura. El último token observado está en `Position=0x555` (1365), `StoragePosition=0x4AB` (1195), y el flujo declara `BYTE_SCRIPT_SIZE=1366`, `DATA_SCRIPT_SIZE=1196`. Esto aclara por qué la primera métrica `COMPLETE` dio falso: `Position/Size` miden el flujo lógico de instrucciones, mientras `StoragePosition/StorageSize` miden bytes almacenados, y no deben compararse entre sí. Se corrigió el indicador para usar el máximo `Position + Size` contra `ByteScriptSize`, conservando ambas métricas en la salida. La nueva ejecución debe confirmar `COMPLETE=True` antes de cerrar la verificación formal.

La primera instrucción `ReturnToken` seguida por `NothingToken` demuestra que la función retorna sin valor antes del bloque de configuración. El resto del bytecode está presente físicamente y puede decodificarse, pero queda después de ese retorno y no se ejecuta en el flujo normal. No se debe eliminar ni reordenar ese retorno al reconstruir el comportamiento original.

### Deserialización completa confirmada

La nueva métrica basada en offsets lógicos quedó confirmada por GitHub Actions: `DECODED_INSTRUCTION_END=1366`, `BYTE_SCRIPT_SIZE=1366`, `DECODED_STORAGE_END=1196`, `DATA_SCRIPT_SIZE=1196`, `TOKEN_COUNT=248`, `COMPLETE=True`. Por tanto, los 1366 bytes lógicos de bytecode de `InventoryWnd.function3` fueron recorridos completamente por UELib; la diferencia entre 1366 y 1196 es la diferencia entre offsets lógicos y almacenamiento comprimido/compactado, no bytes faltantes.

El flujo contiene además `JumpIfNotToken` en `Position=0x008E`, `JumpToken` en `Position=0x0266` y un segundo `ReturnToken` en `Position=0x0554`. El primer `ReturnToken` está en `Position=0` y va seguido de `NothingToken`; por ello el resto del cuerpo, incluidos esos saltos y el retorno final, queda detrás del retorno inicial y no es alcanzable por ejecución lineal normal. La estructura completa ya está decodificada; la siguiente pasada añade los destinos numéricos de los saltos para documentar el grafo de control sin depender de la salida textual defectuosa de UELib.

## Octava pasada: jerarquía real de `InventoryWnd` en Animal `interface.xdat` (2026-11-14)

Se ejecutó `tools/xdat/report_animal_inventorywnd.py` directamente contra `reference-data/packages/animal/interface.xdat` dentro del workflow de validación. Resultado: `ANIMAL_INVENTORYWND_ANALYSIS_OK controls=53 unique_textures=38`. El JSON y los dos CSV se publican dentro del artefacto `Animal-U-Reconstruction-Validation`, carpeta `Animal-InventoryWnd-XDAT/`.

### Estructura confirmada por XDAT

- Ventana raíz `InventoryWnd`: **566 × 400**, anclaje 1, offsets -8/-70.
- Panel visual de equipo `Equip_Classic`: 206 × 393, posición 5/-1; contiene `ObjectViewport` 204 × 364 y `EquipSlotBg` 204 × 365.
- Inventario principal: `InventoryItem` 339 × 288 en 217/55.
- Paneles alternativos superpuestos en la misma posición y tamaño: `QuestItem`, `EquipmentItem`, `CraftingItem`, `SuppliesItem` y `SearchItem` (todos 339 × 288, 217/55).
- `EquipmentItem`, `CraftingItem`, `SuppliesItem` y `SearchItem` declaran 9 columnas × 8 filas, iconos de 32 × 32, separación 4 × 4 y `maxItemNum=250`.
- `InventoryTab`: 277 × 23, en 214/18; tiene cinco elementos de pestaña visibles en el XDAT: grande/estado, equipo, consumibles, materiales y quest.
- Slots de equipo individualizados: Underwear, Head, Hair, Hair2, Neck, RHand, Chest, LHand, REar, LEar, Gloves, Legs, Feet, RFinger y LFinger; además `HennaItem` 91 × 19.
- Controles de acción confirmados: `CrystallizeButton` 39 × 40, `TrashButton` 39 × 40, `SortButton` 15 × 15, `BtnClose` y `BtnMin` 12 × 12; `SearchBar` 96 × 18 y `CancelBar` 16 × 16.
- Zona inferior: `AdenaText`, `AdenaIcon`, `AdenaBox`, `InvenWeight`, `WeightIcon` y `WeightBG`.

### 38 referencias de textura únicas

El XDAT solicita recursos de cuatro paquetes principales: `Abaddon`, `NphrnTextures`, `L2UI_CH3` e `InterfaceTextures`. Entre las referencias verificadas están `Abaddon.Inventory_Equip_BG`, `Abaddon.Inventory_Equip_Slot_NoBr`, las texturas de pestañas `Abaddon.Inventory_Tab_*`, los botones `Abaddon.Inventory_Btn_Cry*` y `Abaddon.Inventory_Btn_Trash*`, `NphrnTextures.Wnd_Inventory`, `NphrnTextures.Wnd_Inventory_Outline`, `NphrnTextures.ItemWindow_DF_SlotBox_2x2`, `NphrnTextures.Inventory_Btn_Sort` y `L2UI_CH3.InventoryWnd.Inventory_Back`. Se detectaron además 7 referencias vacías/undefined en campos de textura; no se cuentan como recursos válidos.

### Cruce con `function3`

El tamaño raíz del XDAT (566 × 400) coincide con el tamaño compacto que aparece en el bloque posterior de `function3`. Ese bloque propone el modo expandido de 673 × 400 y 12 columnas frente al modo compacto de 566 × 400 y 9 columnas. El XDAT confirma la geometría compacta inicial y los paneles de 9 × 8; no demuestra que la rama expandida esté activa. Como el bytecode original empieza con `ReturnToken`, se mantiene la conclusión: la rutina de cambio de tamaño está presente detrás de un retorno temprano y no debe activarse como parte de una reconstrucción fiel sin evidencia adicional.

### Límites de la evidencia

El XDAT contiene nombres de controles, dimensiones, anclajes, propiedades y referencias de texturas, pero no los píxeles de las texturas ni la definición completa de sus paquetes. El informe no permite reconstruir por sí solo el aspecto visual exacto de cada recurso. Tampoco se modificó `InventoryWnd.uc` ni ningún comportamiento del cliente.

La ejecución completa del workflow `43563099043` terminó correctamente: compilación limpia, reapertura de 160 clases, compilación de prueba editada y verificación de persistencia del cambio.

## Novena pasada: cruce de clasificadores anónimos con controles XDAT y referencia UCC (2026-11-14)

El informe XDAT ya permite asociar de forma directa los destinos del clasificador con controles reales: `function30` enruta al pane `item_6`, que corresponde al control `CraftingItem` (339 × 288, 9 × 8, máximo 250 objetos); `function31` enruta al pane `item_5`, y el destino final `item_7` es otro pane de inventario. Esto confirma el propósito de `function32` como filtro para el panel de crafting, pero no prueba que ambos subtipos 5 y 6 sean recetas: el valor 5 está confirmado como receta; el valor 6 sigue sin resolver.

### `function37` — candidato reforzado

El workflow volvió a localizar en el kit UCC Interlude de referencia la función `HandleAddHennaInfo(string param)` (línea 2378 del archivo `InventoryWnd.uc` del kit). La función Animal `function37(string)` está conectada al evento 2630 y su cuerpo solo llama `UpdateHennaInfo()` y retorna. La coincidencia de evento, firma y efecto con la referencia respalda **`HandleAddHennaInfo` como nombre descriptivo/candidato de alta confianza**, pero no como nombre original demostrado de AnimalGame. Se conserva `function37` para no afirmar una recuperación que el paquete no acredita.

### `function30` — no renombrar por colisión

El mismo kit de referencia contiene `HandleAddItem(string param)` en el evento 2600. Sin embargo, Animal ya tiene una función distinta `HandleAddItem(ItemInfo)` que clasifica determinados subtipos (pociones/elixires y soulshots) para `item_7`. La función anónima `function30(string)` hace la conversión inicial `ParamToItemInfo`, enruta equipo y quest, y después clasifica en los panes de inventario. Aunque ocupa el rol de handler de evento 2600, no se renombra a `HandleAddItem` porque colisionaría con la función existente y no hay evidencia de que el nombre original de Animal fuera el mismo.

No se cambiaron identificadores ni lógica. Se preserva el `InventoryWnd.uc` extraído y compilado como línea base.

### Destinos numéricos de los saltos de `function3`

La pasada estructural del bytecode original ya recupera los campos `CodeOffset` sin invocar el formateador defectuoso de UELib:

- `JumpIfNotToken`: posición `0x008E`, destino `0x0269`.
- `JumpToken`: posición `0x0266`, destino `0x040F`.
- `ReturnToken` final: posición `0x0554`.

El primer `ReturnToken` permanece en posición `0x0000`. Estos destinos permiten seguir el grafo interno del bloque posterior si hace falta, pero no alteran la conclusión de alcanzabilidad desde la entrada: el retorno inicial termina la función antes de llegar a la condición en `0x008E`. Los saltos se conservan como evidencia estructural, no como permiso para reactivar la rutina.

## Décima pasada: comparación automática de funciones anónimas con el UCC Interlude (2026-11-14)

Se añadió `tools/analysis/compare_animal_inventorywnd_reference.py`. El workflow compara tokens de los cuerpos de las 15 funciones anónimas/recuperadas de Animal contra las 52 funciones de `InventoryWnd.uc` del kit UCC Interlude. El script admite el archivo de referencia UTF-16 y guarda `Animal-InventoryWnd-XDAT/reference-function-matches.json` en el artefacto. La ejecución corregida terminó correctamente y la reconstrucción limpia y la prueba de ida/vuelta también pasaron.

### Resultados útiles (similitud de tokens; solo candidatos)

| Función Animal | Mejor candidato de referencia | Similitud | Interpretación segura |
|---|---|---:|---|
| `function37(string)` | `HandleAddHennaInfo(string)` y `HandleUpdateHennaInfo(string)` | 1.0000 | Los cuerpos coinciden exactamente; el evento 2630 favorece `HandleAddHennaInfo`. Candidato de alta confianza, no renombrado. |
| `function44(int)` | `IsLOrREar(int)` y `IsLOrRFinger(int)` | 0.6607 ambos | Empate; no permite distinguir qué pareja de accesorios compara. Mantener anónima. |
| `function26(ItemInfo)` | `IsArrow(ItemInfo)` | 0.6154 | Refuerza que es duplicado de `IsArrow`; sin llamada interna encontrada. Mantenerla. |
| `MethosRecoveredName_1105(ItemInfo)` | `RecoveredName_1266(ItemInfo)` | 0.6689 | Parecido estructural con un helper también recuperado/anónimo; no revela nombre original. |
| `function30(string)` | `HandleAddItem(string)` | 0.3659 | Coincidencia débil y existe `HandleAddItem(ItemInfo)` en Animal; no renombrar. |
| `function31(ItemInfo)` | `IsBowOrFishingRod(ItemInfo)` | 0.4478 | No corresponde a la lógica real (ItemType 0/1/2); descartar como nombre candidato. |
| `function32(ItemInfo)` | `IsBowOrFishingRod(ItemInfo)` | 0.5455 | Coincidencia superficial; no corresponde al filtro ItemSubType 5/6. Descartar. |

El resto de las puntuaciones máximas fueron bajas y no ofrecen identificación fiable. En particular, `function2`, `function13`, `function46`, `function47`, `function3` y `function48` no reciben nombres por similitud automática. La métrica no es prueba semántica: no se usan estos resultados para reemplazar identificadores.

### Corrección metodológica

La comparación sirve para descubrir candidatos, no para restaurar nombres automáticamente. Los resultados exactos o altos se validan contra evento, firma, cuerpo, llamadas y referencias del paquete Animal. Se mantiene la regla de no renombrar ninguna función sin evidencia directa del build AnimalGame.

## Undécima pasada: resolución semántica de helpers duplicados (2026-11-14)

Se revisaron directamente los cuerpos de `function44`, `function26`, `function14` y sus equivalentes en la referencia UCC Interlude. Esta pasada detectó dos duplicaciones importantes y evitó una asignación de nombre potencialmente incorrecta.

### `function26(ItemInfo)` — duplicado exacto de `IsArrow`

El cuerpo de `function26` es `return paramItemInfo.bArrow; return false;`, mientras que la clase Animal ya contiene `IsArrow(ItemInfo)` con el mismo retorno de `bArrow`. El código Animal llama a `IsArrow` desde la ruta de actualización de equipo; no se encontró llamada a `function26`. El comparador token-level también coloca `IsArrow` como candidato principal (0.6154). Conclusión: helper redundante o residual, semántica exacta conocida, pero no se cambia el nombre porque el nombre `IsArrow` ya existe y no hay evidencia de que el helper duplicado tuviera un rol activo.

### `function44(int)` — helper de comparación de accesorios sin nombre resuelto

El cuerpo llama `GetAccessoryServerID(int_1, int_2, int_3, int_4)` y compara el ID recibido únicamente con las dos primeras salidas: primera → `-1`, segunda → `1`, cualquier otra → `0`. En la referencia UCC, `IsLOrREar` usa precisamente las dos primeras salidas y `IsLOrRFinger` las dos últimas; por eso la comparación automática había dado ambos candidatos con empate por similitud de tokens, pero la inspección semántica favorece `IsLOrREar` **solo si la convención de salida de Animal coincide con la del UCC**.

No se renombra: Animal ya contiene una función llamada `IsLOrREar` que compara las salidas tercera y cuarta, mientras que `IsLOrRFinger` usa variables nombradas `LFinger/RFinger` y compara esas mismas salidas tercera/cuarta. Además, no hay llamada interna a `function44`. La contradicción entre la función anónima, la función Animal ya nombrada y la referencia impide afirmar un nombre original sin inspeccionar la implementación/contrato real de `GetAccessoryServerID` en el runtime Animal.

### `function14(ItemInfo)` — no-op confirmado

Su cuerpo solo contiene `return;`. El comparador de similitud no ofrece un candidato convincente; no se asigna nombre. No se ha encontrado evidencia de efecto secundario ni de llamada relevante que permita reconstruir un propósito distinto.

### `MethosRecoveredName_444()` — guardia de set con retorno temprano

Mantiene `vvCanUseSetEquipment = true; return;`. El bloque posterior intenta obtener `UserInfo` y habilitarlo solo para una lista fija de nombres, pero es inalcanzable desde la entrada. Se conserva el retorno y no se reactiva la lista. El helper `MethosRecoveredName_1105(ItemInfo)` consulta esta bandera antes de recorrer piezas del set y solicitar `RequestUseItem` para las piezas que faltan; por tanto, la bandera habilita ese flujo de uso de piezas, no modifica la lógica de equipamiento del servidor.

No se modificó `InventoryWnd.uc`. Estos hallazgos se añaden como anotaciones semánticas; los identificadores permanecen intactos hasta demostrar el nombre del build Animal.

## Duodécima pasada: contrato nativo de GetAccessoryServerID (2026-11-14)

Se contrastó el bytecode/descompilado Animal con la declaración nativa de `GetAccessoryServerID` en `UIScript` del kit Interlude y con el flujo de `EarItemUpdate`/`FingerItemUpdate`.

La firma nativa documentada es `GetAccessoryServerID(out int a_LEar, out int a_REar, out int a_LFinger, out int a_RFinger)`: las cuatro salidas están ordenadas explícitamente como oreja izquierda, oreja derecha, dedo izquierdo y dedo derecho.

En Animal, `function44(int)` compara el ID con las salidas 1 y 2 y devuelve `-1/1/0`. Eso coincide exactamente con la lógica de referencia de `IsLOrREar` del kit Interlude. En cambio, la función Animal que ya se llama `IsLOrREar(int)` compara las salidas 3 y 4, que según el contrato nativo corresponden a dedos. `IsLOrRFinger(int)` también compara las salidas 3 y 4, esta vez con variables explícitamente llamadas `LFinger/RFinger`. Y `EarItemUpdate()` llama a `IsLOrREar`, mientras `FingerItemUpdate()` llama a `IsLOrRFinger`.

Esto deja una inconsistencia concreta en la versión Animal: el helper llamado `IsLOrREar` parece comparar los IDs de dedos, mientras `function44` es el helper que compara los IDs de orejas. La referencia Interlude respalda esa interpretación, pero no demuestra si Animal pretendía conservar dos helpers duplicados, si el nombre del helper existente se desplazó durante la compilación, o si hay una peculiaridad de implementación nativa específica de ese cliente. `function44` no tiene call sites en la clase, por lo que no se cambia el nombre ni se altera `EarItemUpdate`/`FingerItemUpdate`.

**Conclusión operativa:** queda identificada una posible inversión de clasificación orejas/dedos en Animal, con evidencia de la firma nativa y de los consumidores. No es una corrección autorizada para esta reconstrucción: requeriría comprobar el comportamiento real del `GetAccessoryServerID` de Animal en ejecución o disponer de una segunda fuente del mismo build. La fuente reconstruida permanece intacta.

## Decimotercera pasada: clasificación de subtipos y helpers de inventario (2026-11-14)

Se volvió a recorrer el `InventoryWnd.uc` completo del artefacto Animal y las 159 clases descompiladas para rastrear los valores `ItemSubType` 5 y 6, la ruta `function30` y el helper `function47`.

### `function32(ItemInfo)` y `item_6` (CraftingItem)

`function32` devuelve verdadero para `ItemSubType == 5` o `ItemSubType == 6`, y `function30` envía esos objetos a `item_6`, que el XDAT identifica como `CraftingItem` (panel 339×288, 9 columnas × 8 filas, máximo 250 objetos). En `RecipeBookWnd.AddRecipeBookItem`, Animal asigna explícitamente `ItemSubType = 5` a los objetos de receta que añade a `RecipeBookWnd.RecipeItem`. Esto confirma que el subtipo 5 corresponde a receta en ese flujo. La búsqueda global en las 159 clases no encontró otra asignación, comparación o consumidor que explique el subtipo 6. Por tanto, `function32` agrupa recetas conocidas y un segundo subtipo aún no identificado en el panel Crafting; no se debe llamar a ambos «recetas» sin evidencia adicional.

### `function31(ItemInfo)`

Solo clasifica `ItemType` 0, 1 o 2 y envía esos elementos a `item_5`. La búsqueda global no encontró una definición de enum ni una asignación de esos valores que permita recuperar etiquetas de categoría inequívocas. Se conserva el nombre anónimo.

### `function47()` — escritura de configuración sin llamada identificada

El helper escribe `-1` en `TrashGrp/TrashList[int_2]` y retorna. No hay llamadas a `function47` en `InventoryWnd.uc` ni referencias a su nombre en las otras 158 fuentes descompiladas. `function2` carga esa lista desde INI; `function13` activa/desactiva el modo de borrado automático y llama a `function46`; `function46` lee la lista y ejecuta `RequestDestroyItem` para los objetos encontrados. No se encontró un camino que invoque `function47`, así que no se le atribuye una función de «quitar de lista» activa ni se conecta al botón de papelera. Se mantiene sin cambios.

### Resultado de esta pasada

No se renombraron funciones ni se modificó el comportamiento. Se confirmó el subtipo 5 como receta; el subtipo 6 y la enumeración `ItemType` siguen abiertos. Se mantiene la protección: ninguna prueba ejecuta `RequestDestroyItem` ni altera la configuración de borrado.

## Decimocuarta pasada: búsqueda ampliada de referencias de tipos de ítem (2026-11-14)

Se inspeccionó el árbol completo de la rama principal (233 rutas, sin truncamiento) para localizar fuentes, tablas de ítems o datos de cliente que pudieran resolver los valores numéricos de `ItemType` y `ItemSubType`. El repositorio contiene los paquetes `Interface.u` y `interface.xdat` de Animal, Berserker, L2Sudamérica y Silent Dark, además del cliente Interlude de referencia; no contiene `itemname-e.dat`, `itemdata`, `weapongrp`, `armorgrp`, `etcitemgrp` ni una tabla equivalente de definiciones de objetos. Tampoco hay fuentes de `InventoryWnd.uc` alternativas versionadas para esos otros paquetes.

Se amplió la búsqueda externa y apareció una implementación pública de clasificación de inventario para otra interfaz Lineage II: convierte `ItemType` a `EItemType` y `ItemSubType` a `EEtcItemType`, y separa explícitamente activos, armas, armaduras, accesorios, consumibles, elixires, flechas, virotes y recetas. Esta fuente confirma que esos campos son enumeraciones de clasificación y que las recetas son una categoría diferenciada, pero no aporta una tabla numérica fiable aplicable al build Animal. La página consultada es una modificación de terceros, no una fuente oficial ni el mismo paquete AnimalGame: https://maxcheaters.com/topic/244963-edit-file-inventorywnd/

**Conclusión:** la referencia externa no autoriza traducir los valores 0/1/2 de Animal a etiquetas concretas ni identificar el subtipo 6. Se mantienen ambas cuestiones abiertas. Tampoco se ha inferido que la categoría de recetas de otra interfaz use los mismos números que Animal, aunque el valor 5 sí está confirmado por la asignación directa de `RecipeBookWnd` en las fuentes Animal.

No se modificó `InventoryWnd.uc`, no se renombraron helpers y no se alteró ningún comportamiento. La siguiente vía de mayor valor es extraer y comparar las clases `InventoryWnd` de los otros paquetes ya guardados en el repositorio, buscando coincidencias de bytecode y nombres de campos/eventos antes de acudir a referencias genéricas adicionales.

## Decimoquinta pasada: comparación cruzada de paquetes almacenados (2026-11-14)

Se amplió el workflow `validate-animal-interface.yml` para extraer `InventoryWnd.uc` directamente de los paquetes originales Berserker, L2Sudamérica y Silent Dark usando el mismo UCC Interlude y el descompilador del proyecto. Los archivos recuperados y los listados de funciones se publicarán como artefacto `Cross-Package-InventoryWnd` junto con la validación Animal.

La ejecución quedó encolada al registrar esta modificación:
- Run `43564397510`: [Validate Animal Interface U round-trip](https://github.com/Methos48/Methos/actions/runs/43564397510)
- Commit del workflow: `19af06d23a2e2e5ef810b3e94592d300f6d66f1a`.

El paso es deliberadamente tolerante a fallos por paquete: registra si una extracción individual no puede completarse, sin ocultar los resultados de las demás. No cambia ni recompila los paquetes de referencia, no sustituye la fuente Animal y no renombra funciones. La comparación semántica se hará después de inspeccionar los archivos efectivamente extraídos; una coincidencia de otro cliente solo será evidencia comparativa, no prueba automática del nombre original Animal.

## Decimosexta pasada: comparación real con Berserker y Silent Dark (2026-11-14)

Se completó el workflow `43564397510` y se inspeccionó el artefacto `Animal-U-Reconstruction-Validation` (ID `14102101234`). La compilación limpia de Animal, la reapertura de sus 160 clases y la prueba de edición real seguida de compilación y reapertura terminaron correctamente. El marcador `MethosAnimalRoundTripProbe_20261113` sobrevivió al ciclo completo.

### Berserker: misma familia de InventoryWnd que Animal

El paquete Berserker sí se pudo abrir como UE 126/0 y exportó 159/159 clases. Su `InventoryWnd.uc` tiene 72 funciones, igual que Animal. Tras normalizar espacios, la similitud de líneas es 99,69 %. La comparación línea por línea encontró solo estas diferencias:
- Berserker no incluye las 12 declaraciones de propiedades que el proceso de recuperación ya restauró en Animal.
- `OnKeyUp` conserva los valores numéricos 18/17 en vez de los enums `IK_Alt`/`IK_Ctrl`.
- Tres retornos tipados de Berserker aparecen como `return;` donde Animal tiene `return 0` o `return None`; son diferencias de salida/descompilación, no pruebas de una lógica distinta.
- El cuerpo de `function44` es el mismo salvo el retorno final redundante (`return 0` frente a `return;`).

Los cuerpos de las 13 funciones anónimas comparables (`function14`, `function30`, `function31`, `function32`, `function37`, `function44`, `function26`, `function2`, `function13`, `function47`, `function46`, `function3`, `function48`) coinciden exactamente, salvo la diferencia trivial del retorno final en `function44`. Berserker conserva los mismos nombres anónimos; por tanto, confirma que son funciones heredadas de la misma familia de código, pero no recupera sus nombres originales. No se renombra ninguna.

### Silent Dark: variante funcionalmente distinta

El paquete Silent Dark se abrió y exportó correctamente. Su `InventoryWnd.uc` tiene 53 funciones, frente a 72 de Animal/Berserker, y utiliza una arquitectura distinta para búsqueda, botones y filtrado. Tiene nombres explícitos como `HandleAddHennaInfo`, pero su cuerpo no coincide exactamente con `function37` de Animal; tampoco su ruta `HandleAddItem(string)` equivale a la ruta Animal `HandleAddItem(ItemInfo)`. Estos nombres sirven como pistas para nuevas búsquedas, no como evidencia suficiente para renombrar funciones de Animal. Se mantienen los identificadores actuales.

### L2Sudamérica: paquete protegido o no estándar, aún no inspeccionable en crudo

La extracción directa de `reference-data/packages/l2sudamerica/Interface.u` falló con `Interface isn't an UnrealPackage!`. UELib no llegó a leer tablas de paquete. No se fuerza la apertura ni se quita ninguna cabecera a ciegas: primero hay que identificar y verificar la capa de protección de ese archivo mediante el pipeline correspondiente. El archivo original permanece intacto.

### Resultado

- Berserker aporta una segunda copia de la misma familia de código y confirma que las funciones anónimas no son artefactos exclusivos de la extracción Animal.
- Silent Dark permite estudiar una variante diferente, pero sus nombres no se trasladan a Animal sin coincidencia de cuerpo, evento y firma.
- L2Sudamérica queda pendiente de detección/decodificación segura.
- La reconstrucción Animal mantiene la validación de ida y vuelta: compilación limpia, reapertura de 160/160 clases y cambio real persistente después de recompilar y reabrir.
- No se cambió `InventoryWnd.uc` ni se modificó el comportamiento del juego.

## Decimoséptima pasada: identificación del bloqueo de L2Sudamérica (2026-11-14)

Se inspeccionó la cabecera binaria original de `reference-data/packages/l2sudamerica/Interface.u` sin alterar el archivo. Los primeros cuatro bytes son `FF ED FA CE`, firma de NPKscrypt identificada previamente en el análisis de protección del proyecto. Esto explica por qué UELib rechaza el archivo como paquete Unreal cuando se le entrega directamente: todavía está protegido, no es evidencia de que el paquete esté dañado.

La documentación técnica existente `docs/STEP26_NPKSCRYPT_DECRYPTION_PATH.md` reconstruye la forma del transformador de lectura dentro de `npkscrypt.dll`, pero deja pendiente recuperar el estado de clave por archivo y el inicializador/proveedor que lo prepara. Sin esas claves no existe decodificación validada. No se intentó descifrar con valores supuestos ni retirar la cabecera.

Se ajustó el workflow para reconocer `FFEDFACE`, registrar el tipo de protección y omitir la descompilación UELib en crudo para ese paquete. El original se conserva intacto. El análisis de L2Sudamérica queda pendiente de completar el camino de claves NPKscrypt y verificar una decodificación/recodificación exacta antes de comparar su `InventoryWnd`.

**Estado cruzado actualizado:** Berserker es una referencia de la misma familia y confirma el código anónimo, Silent Dark es una variante distinta útil solo como pista, y L2Sudamérica está protegido por NPKscrypt con la inicialización de claves aún sin resolver. No se modificó la fuente Animal ni el comportamiento del juego.

## Decimoctava pasada: nueva evidencia para function37 (2026-11-14)

La comparación semántica detallada con Silent Dark elevó la confianza del candidato para `function37(string)`:

- En Animal, `OnEvent` despacha el evento 2630 a `function37(param)`.
- En Silent Dark, el mismo evento 2630 despacha a `HandleAddHennaInfo(param)`.
- Ambas funciones reciben un `string` y su cuerpo efectivo es `UpdateHennaInfo(); return;`. Silent Dark contiene un segundo `return;` redundante al final.
- La referencia UCC Interlude también ofrece `HandleAddHennaInfo(string)` para la ruta de alta de henna/evento 2630, según la comparación previa.

La coincidencia de evento, firma y operación en dos referencias distintas hace que `HandleAddHennaInfo(string)` sea ahora un candidato de alta confianza para el nombre original de Animal `function37`. Aun así, Silent Dark no es el mismo paquete AnimalGame y Berserker conserva el identificador anónimo; por la regla de reconstrucción estricta, se documenta el candidato pero no se renombra la fuente hasta disponer de evidencia del mismo build.

También se volvió a confirmar que Animal `function26(ItemInfo)` y `IsArrow(ItemInfo)` son duplicados funcionales, mientras que Silent Dark conserva solo la versión nombrada `IsArrow`. Esto no demuestra que el identificador original de `function26` fuese `IsArrow`, ya que Animal mantiene ambas declaraciones.

No se modificó `InventoryWnd.uc` ni el comportamiento del cliente.

## Decimonovena pasada: cruce de eventos y firmas en la variante Silent Dark (2026-11-14)

Se contrastaron los cuerpos completos y los despachos de `OnEvent` de Animal y Silent Dark, no solo la similitud automática.

### `function30(string)` — candidato `HandleAddItem(string)`

Ambos paquetes despachan el evento 2600 con el parámetro string a la función que procesa el alta de un objeto. En Silent Dark, esa función se llama `HandleAddItem(string)`; su cuerpo comienza con `ParamToItemInfo`, después enruta equipo a `EquipItemUpdate`, quest items a su panel y el resto al inventario. Animal `function30(string)` hace exactamente la misma secuencia inicial y añade las rutas de clasificación hacia paneles adicionales mediante `function31` y `function32`. Esto eleva `HandleAddItem(string)` a candidato de alta confianza para el nombre original de `function30`.

No se renombra todavía: Animal ya contiene otra función llamada `HandleAddItem(ItemInfo)`, que actúa como filtro booleano para una categoría. La coexistencia de dos funciones con ese nombre requiere confirmar que el compilador/paquete Animal admite esa sobrecarga o que el segundo nombre también está anonimizado/alterado. No se fuerza una colisión de nombres.

### `function44(int)` — candidato `IsLOrREar(int)`

El cuerpo de Animal `function44` obtiene cuatro IDs con `GetAccessoryServerID` y compara los dos primeros, devolviendo -1/1/0. En Silent Dark, `IsLOrREar(int)` realiza la misma comparación con las variables explícitas `LEar/REar`; `IsLOrRFinger(int)` compara las dos últimas salidas. Esto refuerza que `function44` corresponde semánticamente al helper de orejas.

Animal, sin embargo, ya contiene una función nombrada `IsLOrREar(int)` que compara las salidas tercera y cuarta (los IDs de dedos bajo el contrato nativo), mientras que su `IsLOrRFinger(int)` también compara esas salidas. `function44` no tiene llamadas internas identificadas. Se registra la contradicción y el candidato, pero no se renombra ni se cambia el flujo de `EarItemUpdate`/ `FingerItemUpdate` sin confirmar el contrato en el runtime Animal.

### Resultado

Los eventos 2600 y 2630, sus firmas y los cuerpos de Silent Dark aportan candidatos mucho más fuertes para `function30` y `function37` que la comparación por similitud sola. La coincidencia semántica de `function44` con `IsLOrREar` también queda respaldada por la segunda variante. Los tres identificadores se conservan en la fuente Animal hasta resolver las colisiones/contradicciones del propio paquete. No se modificó `InventoryWnd.uc` ni el comportamiento del cliente.

## Vigésima pasada: verificación del workflow con detección NPKscrypt (2026-11-14)

Se ejecutó de nuevo el workflow tras añadir la detección previa de cabecera protegida. Run `43564770505` terminó en éxito: [Validate Animal Interface U round-trip](https://github.com/Methos48/Methos/actions/runs/43564770505).

Resultados confirmados en los logs:
- Berserker: paquete abierto y 159/159 clases exportadas; `InventoryWnd.uc` recuperado.
- Silent Dark: paquete abierto y 169/169 clases exportadas; `InventoryWnd.uc` recuperado.
- L2Sudamérica: cabecera detectada `FFEDFACE`; el workflow registra NPKscrypt y omite la descompilación cruda por falta de perfil/estado de clave verificado. El archivo original queda intacto.
- Animal: compilación limpia correcta, reapertura de 160/160 clases, compilación de la copia editada correcta y marcador de edición persistente tras reabrir.

La detección evita repetir el error engañoso de tratar el paquete L2Sudamérica protegido como si fuera un UnrealPackage plano. No se intenta descifrarlo sin resolver primero las claves NPKscrypt. La fuente Animal y el comportamiento del juego siguen sin cambios.

## Vigésima primera pasada: referencias globales de las funciones anónimas (2026-11-14)

Se hizo una búsqueda de identificadores y llamadas en los 159 archivos UnrealScript recuperados de Animal, no solo dentro de `InventoryWnd.uc`. El resultado confirma qué funciones tienen referencias reales y cuáles están aisladas:

| Función | Referencias encontradas | Resultado |
|---|---:|---|
| `function14` | 0 | Declaración sin llamadas en las 159 clases |
| `function44` | 0 | Helper aislado; no hay evidencia de uso desde otra clase |
| `function26` | 0 | Duplicado funcional de `IsArrow`, pero sin llamadas propias |
| `function47` | 0 | Rutina que escribe en `TrashList`, sin llamadas en el código recuperado |
| `MethosRecoveredName_1105` | 0 | Rutina de uso de piezas de set, sin llamadas recuperadas |
| `function30` | 1 | Despachada desde `OnEvent` (evento 2600) |
| `function37` | 1 | Despachada desde `OnEvent` (evento 2630) |
| `function31` / `function32` | 1 cada una | Invocadas por la clasificación de objetos de `function30` |
| `function2` | 1 | Invocada durante inicialización |
| `function13` | 1 | Invocada por el botón de descarte; llama a `function46` |
| `function46` | 1 | Invocada por `function13` para ejecutar el descarte automático |
| `function3` | 2 | Invocada durante carga y por el callback de inventario expandido |
| `function48` | 6 | Sus seis llamadas están dentro del bloque posterior al retorno inicial de `function3`, por tanto no son alcanzables desde esa entrada |

Esta búsqueda no prueba que las rutinas sin llamadas sean inútiles: podrían ser callbacks externos o conservarse por compatibilidad con el runtime. Por eso no se borran ni se modifican. Sí permite descartar que `function47` sea un helper activo llamado desde otra clase del código recuperado. Se conserva la fuente intacta y se añaden estas relaciones a la matriz de auditoría.

## Vigésima segunda pasada: contraste de categorías de objetos con Silent Dark (2026-11-14)

Se comparó la clasificación de objetos de Animal con la implementación de `SortItem()` de Silent Dark, que conserva las categorías explícitas:

- Silent Dark clasifica `ItemType 0` como armas, `1` como armaduras y `2` como accesorios.
- Animal `function31(ItemInfo)` devuelve true exactamente para `ItemType 0, 1, 2` y enruta el objeto al control `EquipmentItem` (variable `item_5`). La semántica queda confirmada como filtro conjunto de equipo/armas-armaduras-accesorios; no se recuperó un nombre original exacto, así que se conserva `function31`.
- En Silent Dark, `ItemType 5` se subdivide por `ItemSubType`; `ItemSubType 5` se agrega explícitamente a `RecipeList`.
- Animal `function32(ItemInfo)` acepta `ItemSubType 5` o `6` y enruta a `CraftingItem` (`item_6`). El subtipo 5 como receta queda respaldado por una segunda implementación. La búsqueda de referencias a `ItemSubType 6` en las 169 clases Silent Dark no encontró un uso comparable; el significado del 6 sigue sin resolverse.

Esto permite describir la función31 con mayor certeza y confirma parte del propósito de function32, pero no justifica inventar identificadores originales ni eliminar el subtipo 6. No se modificó la fuente Animal.

## Vigésima tercera pasada: prueba UCC de nombres candidatos (2026-11-14)

Se añadió una prueba aislada que modifica copias temporales de `InventoryWnd.uc`, incluyendo también las referencias del despachador `OnEvent`, sin tocar la fuente Animal original.

Resultados del run `43565701277`:
- `function37(string)` renombrada a `HandleAddHennaInfo(string)`: **compiló correctamente** junto con el resto de las 159 clases. Esto confirma que el nombre candidato es compatible con el contrato de compilación Animal y refuerza la evidencia de evento 2630/cuerpo `UpdateHennaInfo()`. La prueba de reapertura y persistencia del nombre se añadió al siguiente ciclo y queda pendiente.
- `function30(string)` renombrada a `HandleAddItem(string)`: **rechazada por el compilador**. Error exacto: `HandleAddItem conflicts with Function Interface.InventoryWnd.HandleAddItem`, en la declaración existente `HandleAddItem(ItemInfo)`. UCC no admite esa duplicación de nombre en la clase Animal. Por tanto, aunque Silent Dark use `HandleAddItem(string)`, ese nombre no puede trasladarse a Animal mientras se conserve la función tipada `HandleAddItem(ItemInfo)`; se descarta como nombre para `function30` en esta reconstrucción.

La validación base del mismo run volvió a compilar Animal y reabrir 160/160 clases, y la edición de prueba volvió a sobrevivir a compilación y reapertura. El run terminó marcado como fallido únicamente porque la prueba diagnóstica trató el rechazo esperado del candidato `HandleAddItem` como error del paso; se ajustó el workflow para registrar ese rechazo como resultado esperado y continuar. No se cambió `InventoryWnd.uc` ni el comportamiento del cliente.

## Vigésima cuarta pasada: comparación exacta Animal–Berserker (2026-11-14)

Se compararon las fuentes completas `InventoryWnd.uc` extraídas de Animal y Berserker en el artifact validado `Animal-U-Reconstruction-Validation` (run `43564770505`). Berserker aporta una segunda fuente de paquete real, no una plantilla UCC genérica.

Hallazgos medidos:
- Las funciones `function30(string)`, `function37(string)` y `HandleAddItem(ItemInfo)` tienen cuerpos normalizados idénticos en ambas fuentes.
- `function44(int)` también coincide en su lógica; la única diferencia es que Berserker termina con `return;` mientras Animal tiene `return 0;`.
- Berserker conserva exactamente los identificadores anónimos `function30`, `function37` y `function44`. Por tanto, este segundo paquete no recupera los nombres originales y confirma que esos identificadores ya estaban presentes en otra compilación/variante de la misma familia.
- La fuente Animal contiene algunas declaraciones de variables adicionales y diferencias menores de descompilación, pero no hay evidencia para sustituir los nombres anónimos.

Esta comparación refuerza que `HandleAddHennaInfo` es un nombre semánticamente plausible por la variante Silent Dark, pero no demuestra que fuera el nombre original de Animal/Berserker. Se mantiene `function37` en la fuente original hasta validar la prueba de compilación y reapertura, y no se renombran `function30` ni `function44`. No se alteró el código de producción.

## Vigésima quinta pasada: persistencia del nombre de henna tras reapertura (2026-11-14)

La prueba aislada del run `43565930234` confirma:
- La copia temporal con `function37` renombrada a `HandleAddHennaInfo(string)` compiló correctamente.
- El paquete de prueba se reabrió correctamente y UELib exportó las 160/160 clases.
- La fuente `InventoryWnd.uc` reabierta contiene `HandleAddHennaInfo(string)` y ya no contiene la declaración `function37`: **el cambio de nombre persistió en el paquete binario**.
- La prueba independiente de `function30` volvió a confirmar el conflicto de nombre con `HandleAddItem(ItemInfo)`.

El run terminó en fallo únicamente por propagación del código de salida 1 del compilador durante la prueba negativa esperada de `function30`, aunque el script detectó y registró correctamente el conflicto. Se corrigió el cierre del bloque PowerShell y se fuerza un código final satisfactorio tras registrar ese rechazo como resultado esperado. Nuevo run `43566249499` pendiente.

Importante: la persistencia técnica del nombre candidato no demuestra que ese fuera el nombre histórico original de Animal. Por la evidencia cruzada, `HandleAddHennaInfo` es candidato de alta confianza, pero se conserva `function37` en la fuente original hasta obtener confirmación específica del paquete Animal. Ningún archivo de producción fue renombrado.

## Vigésima sexta pasada: validación completa y prueba de nombres (2026-11-14)

El run `43566249499` terminó **SUCCESS**: [Validate Animal Interface U round-trip](https://github.com/Methos48/Methos/actions/runs/43566249499). Artifact `Animal-U-Reconstruction-Validation`, ID `14103220248`.

Resultados comprobados en los logs y el artifact:
- Análisis XDAT: 53 controles y 38 referencias de textura únicas.
- Extracción cruzada: Berserker y Silent Dark se descompilaron; L2Sudamérica fue tratado según su protección NPKscrypt, sin intentar abrirlo como paquete plano.
- Compilación limpia de las 159 fuentes Animal: UCC generó `Interface-clean.u` de 3.338.657 bytes.
- Reapertura limpia: UELib exportó 160/160 clases, 10.483 objetos, cero clases fallidas.
- Prueba de edición real: UCC generó `Interface-edited-test.u` de 3.338.801 bytes; la reapertura confirmó `MethosAnimalRoundTripProbe_20261113` en `ToolTip.uc`.
- Prueba aislada de `function37`: el cambio temporal a `HandleAddHennaInfo(string)` compiló y persistió después de reabrir el paquete; UELib volvió a exportar 160/160 clases.
- Prueba negativa de `function30`: UCC rechazó `HandleAddItem(string)` por colisión con `HandleAddItem(ItemInfo)`; el workflow ahora registra este rechazo como resultado esperado y finaliza correctamente.

El workflow no cambió el `InventoryWnd.uc` de producción. La compilación y reapertura demuestran que el proyecto puede reconstruir el paquete y conservar una modificación real, pero la prueba temporal de henna no convierte por sí sola el nombre candidato en una identificación histórica demostrada. Se mantiene `function37` sin renombrar hasta disponer de evidencia directa del build Animal.

## Vigésima séptima pasada: comparación cruzada automatizada y reproducible (2026-11-14)

Se incorporó `tools/analysis/compare_animal_inventorywnd_packages.py` y un paso al workflow para generar `Animal-InventoryWnd-XDAT/cross-package-function-matches.json` a partir de las fuentes reales de Animal, Berserker y Silent Dark. El script:
- compara los cuerpos de funciones homónimas Animal/Berserker tras normalizar comentarios y espacios;
- clasifica las funciones anónimas de Animal frente a las funciones nombradas de Silent Dark por similitud de tokens;
- marca cuando el nombre candidato ya existe en `InventoryWnd` de Animal, para detectar colisiones antes de probar renombres.

Prueba local con el artifact validado `14103220248`:
- Se extrajeron 72 funciones de Animal, 72 de Berserker y 53 de Silent Dark.
- 68 funciones con el mismo nombre entre Animal y Berserker tienen cuerpos idénticos tras normalización.
- Las cuatro diferencias restantes son pequeñas: constantes de teclas Alt/Ctrl frente a sus códigos numéricos, retornos finales explícitos frente a `return;` en `function44` y `IsLOrREar`, y `return None;` frente a `return;` en `GetCurrentTab`.
- `function37` encuentra `HandleAddHennaInfo` como mejor candidato en Silent Dark; no hay colisión de nombre en Animal. `function30` encuentra `HandleAddItem`, pero ese nombre ya existe en Animal y el compilador confirmó el conflicto. `function44` encuentra `IsLOrREar`, pero ese nombre también existe en Animal y la comparación de IDs mantiene la contradicción ya documentada.

La similitud solo produce candidatos; no restaura nombres automáticamente. El nuevo paso queda incluido en el run `43566703204`, todavía en ejecución al registrar esta pasada.

### Aclaración de la comparación en bruto frente a la fuente restaurada

El run `43566703204` ejecutó el comparador inmediatamente después de descompilar los tres paquetes y **antes** de las restauraciones de compatibilidad. El resultado reproducible del log/JSON fue `animal=72 berserker=72 same_name_exact=72`: las 72 funciones de InventoryWnd tienen el mismo nombre y cuerpo en las dos fuentes brutas.

La cifra local de 68/72 registrada arriba corresponde a comparar el archivo Animal ya restaurado del artifact, no la fuente bruta. Las cuatro diferencias son precisamente correcciones posteriores documentadas por el workflow:
- `OnDBClickItemWithHandle`: códigos 18/17 convertidos a `IK_Alt`/`IK_Ctrl`.
- `function44` y `IsLOrREar`: retorno final incompleto restaurado a `return 0;`.
- `GetCurrentTab`: retorno final restaurado a `return None;`.

Por tanto, la comparación automatizada confirma identidad total Animal–Berserker antes de las correcciones de compatibilidad; las cuatro diferencias posteriores son cambios controlados del proceso de reconstrucción, no diferencias originales entre esos dos paquetes. Berserker no aporta nombres alternativos para las funciones anónimas porque conserva los mismos 15 identificadores anónimos.

## Vigésima octava pasada: referencias externas en Berserker (2026-11-14)

Se recorrieron las 159 clases UnrealScript de Berserker extraídas en el artifact `14103300541`, buscando llamadas a los identificadores anónimos de `InventoryWnd` fuera de su propia declaración.

Resultado:
- `function14`, `function26`, `function44` y `function47`: cero llamadas en las 159 clases.
- `function30`, `function37`, `function31`, `function32`, `function2`, `function13`, `function46` y `function3`: sus referencias aparecen únicamente en `InventoryWnd.uc`, en las mismas rutas internas observadas en Animal.
- `function48`: seis llamadas, todas dentro de `InventoryWnd.uc`; pertenecen al bloque posterior al retorno inicial de `function3`.
- No hay referencias a estos identificadores desde las otras 158 clases de Berserker.

Esto replica la ausencia de referencias externas ya observada en Animal y reduce la posibilidad de que las funciones aisladas tengan consumidores textuales en otra clase de esa variante. No demuestra que el runtime no pueda invocarlas por callbacks/reflexión; por eso se conservan todas las funciones. No se cambió el código.

## Vigésima novena pasada: segunda referencia pública de Interlude para eventos de inventario (2026-11-14)

Se localizó una segunda fuente pública de UnrealScript en `kdo70/a1_l2j`, independiente del UCC de referencia usado por el workflow:
- [InventoryWnd.uc](https://github.com/kdo70/a1_l2j/blob/main/tools/client/Interface/Classes/InventoryWnd.uc)
- [UIEventManager.uc](https://github.com/kdo70/a1_l2j/blob/main/tools/client/NWindow/Classes/UIEventManager.uc)

La tabla de eventos de `UIEventManager.uc` declara explícitamente:
- `EV_UpdateHennaInfo = 260`
- `EV_InventoryAddItem = 2600`
- `EV_InventoryAddHennaInfo = 2630`
- `EV_InventoryToggleWindow = 2631`

El `OnEvent` de esa fuente enruta 2600 a `HandleAddItem(param)`, 2630 a `HandleAddHennaInfo(param)` y 260 a `HandleUpdateHennaInfo(param)`. Los tres handlers y sus firmas string coinciden con la estructura/eventos recuperados de Animal:
- Animal `function30(string)`, evento 2600: candidato `HandleAddItem(string)`.
- Animal `function37(string)`, evento 2630: candidato `HandleAddHennaInfo(string)`, no `HandleUpdateHennaInfo`.
- El evento 260 corresponde al handler de actualización de henna ya nombrado en Animal.

La fuente pública muestra además que `HandleAddHennaInfo` conserva un bloque comentado que analizaba ID/estado y después llama a `UpdateHennaInfo()`; `HandleUpdateHennaInfo` también llama a esa rutina. Como ambos cuerpos efectivos son casi iguales, el número/evento 2630 es la evidencia que distingue el nombre candidato.

Límites importantes:
- Es otra variante Interlude, no el paquete AnimalGame. Se usa como referencia independiente de nombres/eventos, no como fuente para reemplazar código.
- Su `HandleAddItem(string)` no convive con la función booleana `HandleAddItem(ItemInfo)` que existe en Animal; la prueba UCC de Animal ya confirmó la colisión. No se renombra `function30`.
- Sus `IsEquipItem` y `IsQuestItem` tienen cuerpos distintos de Animal `function31`/`function32`; no se asignan esos nombres a los filtros de Animal.
- Su `IsLOrREar` compara los dos primeros IDs de `GetAccessoryServerID`, lo que respalda el papel semántico de Animal `function44`; pero Animal ya tiene una función llamada `IsLOrREar` que compara los dos últimos IDs. La contradicción y la colisión impiden renombrar `function44`.

La evidencia de eventos eleva `HandleAddHennaInfo` a candidato muy sólido para `function37`, pero Animal y Berserker conservan literalmente `function37` en sus paquetes compilados. Por fidelidad histórica, el identificador de producción sigue intacto hasta una fuente del mismo build que demuestre el nombre original.

### Resultado del workflow con el comparador integrado

El run `43566703204` terminó **SUCCESS**: [Validate Animal Interface U round-trip](https://github.com/Methos48/Methos/actions/runs/43566703204). Artifact `Animal-U-Reconstruction-Validation`, ID `14103300541`.

El JSON `Animal-InventoryWnd-XDAT/cross-package-function-matches.json` del artifact confirma `animal=72`, `berserker=72`, `silent_dark=53` y `same_name_exact=72` en la comparación previa a restauraciones. También conserva los candidatos y los indicadores de colisión para las 15 funciones anónimas. Las pruebas de compilación limpia, reapertura 160/160, edición real persistente y renombre temporal de `function37` volvieron a finalizar correctamente.

## Trigésima pasada: correlación automática entre eventos y handlers (2026-11-14)

Se amplió `compare_animal_inventorywnd_packages.py` para extraer automáticamente las rutas `case Event_ID` de `OnEvent` en Animal, Berserker y Silent Dark, y añadirlas al JSON de comparación.

La prueba local con las fuentes extraídas muestra:
- Animal y Berserker: evento 2600 → `function30`; evento 2630 → `function37`.
- Silent Dark: evento 2600 → `HandleAddItem`; evento 2630 → `HandleAddHennaInfo`.
- El candidato del evento 2600 tiene colisión con `HandleAddItem(ItemInfo)` existente en Animal.
- El candidato del evento 2630 no tiene colisión en Animal.

Esta ruta de eventos coincide también con la segunda referencia pública `kdo70/a1_l2j` y su `UIEventManager.uc`, que define 2600 como `EV_InventoryAddItem`, 2630 como `EV_InventoryAddHennaInfo` y 260 como `EV_UpdateHennaInfo`. Es la evidencia más fuerte hasta ahora para identificar `function37` como `HandleAddHennaInfo`, mientras que `function30` no puede tomar `HandleAddItem` por el conflicto de nombre.

Se añadió la impresión de estas rutas al workflow para que el resultado quede visible en cada ejecución. Run `43567163467` pendiente. No se renombró ninguna función de producción.

## Trigésima primera pasada: tercera fuente pública de InventoryWnd (2026-11-14)

Se localizó y revisó una segunda copia pública extensa de la interfaz Interlude:
[ L2jBrasil — InventoryWnd.uc ](https://github.com/L2jBrasil/InterludeClassicInterface/blob/56faed3a11ccf5dd9fc7ac149bdc39160fd967de/src/Interface/Classes/InventoryWnd.uc).

La fuente contiene 48 funciones y confirma de forma independiente:
- `OnEvent`: 2600 → `HandleAddItem`, 2630 → `HandleAddHennaInfo`, 260 → `HandleUpdateHennaInfo`.
- `SortItem`: `ItemType 0` = Weapon, `1` = Armor, `2` = Accessary, `4` = Asset y `5` = EtcItem.
- Dentro de `ItemType 5`, `ItemSubType 5` se clasifica explícitamente como receta.
- `IsEquipItem` solo devuelve `bEquipped`; no es equivalente a Animal `function31`, que filtra ItemType 0/1/2.
- `IsLOrREar` compara los dos primeros resultados de `GetAccessoryServerID`; `IsLOrRFinger` compara los dos últimos. Esto confirma que la contradicción de Animal `function44` frente a su función nombrada `IsLOrREar` es real entre variantes, no un supuesto basado en una única referencia.
- La variante no contiene un helper equivalente que permita asignar nombre original a `function31` o `function32`; el subtipo 6 tampoco aparece en la clasificación de recetas.

La tercera fuente refuerza las conclusiones semánticas de las pasadas anteriores, pero no es el mismo paquete AnimalGame. No se copió código ni se cambiaron identificadores en producción.

### Resultado de la correlación automática de eventos

El run `43567163467` terminó **SUCCESS**: [Validate Animal Interface U round-trip](https://github.com/Methos48/Methos/actions/runs/43567163467). Artifact `Animal-U-Reconstruction-Validation`, ID `14102532523`.

Los logs publicaron las rutas exactas:
- `ANIMAL_EVENT_ROUTE=2600 HANDLER=function30 SILENT_REFERENCE=HandleAddItem NAME_CONFLICT=True`
- `ANIMAL_EVENT_ROUTE=2630 HANDLER=function37 SILENT_REFERENCE=HandleAddHennaInfo NAME_CONFLICT=False`

El mismo run confirmó otra vez compilación limpia, reapertura 160/160, edición real persistente, compilación/reapertura del candidato temporal `HandleAddHennaInfo` y rechazo esperado de `HandleAddItem` por colisión. El workflow completo terminó correctamente y el código de producción sigue sin renombrar funciones.

## Trigésima segunda pasada: evidencia nativa adicional para los slots de accesorios (2026-11-14)

Se revisó el análisis de binarios de otra instalación Interlude en [Elbera — native-inventory-evidence.md](https://github.com/elberacasa/Lineage2Elbera/blob/main/docs/native-inventory-evidence.md#left-and-right-accessory-slots). Ese análisis verifica `UUIScript::execGetAccessoryServerID` dentro de su propio `NWindow.dll` y obtiene esta correspondencia:
- salida 1: LEar, User+0xA0;
- salida 2: REar, User+0x9C;
- salida 3: LFinger, User+0xAC;
- salida 4: RFinger, User+0xA8.

Su InventoryWnd usa los dos primeros resultados para `IsLOrREar` y los dos últimos para `IsLOrRFinger`. Esto coincide con el contrato del UCC Interlude y apoya la lectura semántica de Animal `function44`: compara los dos primeros resultados y, por tanto, sería un helper de orejas bajo ese contrato.

También confirma la anomalía de Animal: la función ya nombrada `IsLOrREar` compara los resultados tercero y cuarto, igual que `IsLOrRFinger`. Si Animal usa el mismo contrato nativo, la primera función nombrada parece estar mal clasificada y `function44` ocuparía el papel semántico de `IsLOrREar`. No es seguro intercambiar ni renombrar estas funciones: el `Interface.u` analizado por Elbera tiene SHA-256 distinto del paquete Animal (`Animal Interface.u = 3780DCAD8DB9C8B7FF305CBAB5C0D1B016E934044C7E3749ECD53229DB1B866B`; Elbera documenta otro fingerprint), y su `NWindow.dll` no es el runtime Animal. Se mantiene la contradicción abierta hasta verificar el contrato del NWindow exacto de AnimalGame.

### Validación final del paso de eventos

El run `43567163467` completó **SUCCESS**: [workflow](https://github.com/Methos48/Methos/actions/runs/43567163467). Artifact `Animal-U-Reconstruction-Validation`, ID `14102532523`. El log imprimió ambas rutas de eventos y las marcas de colisión esperadas. También volvió a pasar la compilación limpia, reapertura de 160/160 clases, edición real persistente y prueba temporal de renombre/persistencia de `HandleAddHennaInfo`. El rechazo de `HandleAddItem(string)` se registró como resultado negativo esperado, sin fallar el workflow.


## Trigésima tercera pasada: búsqueda del runtime nativo exacto de AnimalGame (2026-11-14)

Se continuó la investigación de la contradicción entre Animal `function44` y `IsLOrREar`, esta vez buscando evidencia del runtime nativo correspondiente al mismo cliente, no otra copia de UnrealScript.

Comprobaciones realizadas:
- Se buscó `NWindow.dll` y referencias a `AnimalGame` en el contenido indexado del repositorio `Methos48/Methos`; no apareció un archivo o referencia versionada que permita identificar el DLL nativo de AnimalGame.
- Se hicieron búsquedas públicas por cliente AnimalGame Interlude y por la combinación AnimalGame/`NWindow.dll`. Los resultados encontrados corresponden a clientes/servidores distintos o a documentación general de herramientas; ninguno proporciona un `NWindow.dll` identificable como el mismo build AnimalGame.
- La evidencia nativa de Elbera sigue siendo útil como hipótesis comparativa, pero no identifica el runtime Animal. No se extrapola su layout de memoria a este paquete.

Conclusión de esta pasada:
- No se obtuvo evidencia nueva que autorice intercambiar, renombrar o reescribir `function44` y `IsLOrREar`.
- La contradicción permanece abierta. El dato necesario para cerrarla es el `NWindow.dll` extraído de la misma instalación/build AnimalGame que produjo `reference-data/packages/animal/Interface.u`, junto con su hash SHA-256 y metadatos PE (TimeDateStamp, tamaño de imagen y arquitectura). Con ese binario se podrá comprobar si el contrato de `GetAccessoryServerID` coincide con la referencia externa antes de asignar nombres.
- Mientras no exista ese binario exacto, se conserva el código y se continúa con el inventario descriptivo de funciones y recursos XDAT sin alterar el comportamiento.

No se incorporó ningún DLL de otro cliente, no se modificó `InventoryWnd.uc` y no se cambió ningún identificador de producción.


## Trigésima cuarta pasada: inspección del artefacto y disponibilidad de recursos (2026-11-14)

Se descargó e inspeccionó el ZIP real del workflow `43567163467`, artifact `14102532523` (`Animal-U-Reconstruction-Validation`, SHA-256 del artifact publicado por GitHub: `406af52e018b4b3fe76d6f3862220e6facf656dbbf4af9cb7278ec8594c01bde`).

Inventario del ZIP:
- 1.288 fuentes `.uc` (incluye exportación original, reaperturas y pruebas temporales).
- 3 paquetes `.u`: `Interface-clean.u`, `Interface-edited-test.u` y el paquete temporal de prueba de nombre.
- 2 CSV y 7 JSON de análisis XDAT/comparación.
- **0 archivos `.utx`, 0 `.dll` y 0 `.xdat`**.

Implicaciones comprobadas:
- El artifact sí permite continuar la auditoría de código y de la estructura XDAT mediante los informes exportados.
- No contiene los píxeles/paquetes de texturas necesarios para visualizar las 38 referencias ni el `NWindow.dll` necesario para resolver la contradicción nativa de los accesorios.
- No se debe reconstruir la apariencia de las texturas a partir de sus nombres, ni usar un DLL de otra compilación como sustituto.

Siguiente línea de trabajo: continuar el cruce de controles XDAT con los handlers y funciones del `InventoryWnd.uc`, y mantener en espera únicamente las conclusiones que dependan de los paquetes `.utx` o del DLL exacto. El código de producción permanece intacto.


## Trigésima quinta pasada: cruce control por control entre XDAT y UnrealScript (2026-11-14)

Se cruzaron los 53 nodos contabilizados por el analizador XDAT con los nombres y accesos del `InventoryWnd.uc` extraído directamente del paquete AnimalGame. El CSV contiene 49 nombres únicos porque los cinco elementos internos de `InventoryTab` comparten el nombre genérico `ct0.Tab.TabElement`.

### Coincidencias directas confirmadas

- Los 15 slots `EquipItem_*` del XDAT coinciden uno por uno y en el mismo orden con las 15 asignaciones de `equipItem[0..14]` de `InitHandle()`.
- `HennaItem`, `InventoryItem`, `QuestItem`, `EquipmentItem`, `CraftingItem`, `SuppliesItem` y `AdenaText` se enlazan a handles usados por la lógica del inventario.
- `SearchBar` se enlaza como EditBox y alimenta `FilterInventory`; `CancelBar` ejecuta `RestoreInventory`.
- `SortButton` llama a `SortItem`; `TrashButton` llama al alternador de borrado automático; `CrystallizeButton` se muestra/oculta según `HasCrystallizeAbility()`.
- `BtnClose` y `BtnMin` tienen rutas de cierre/minimización en `OnClickButton`.
- `InventoryTab` aparece como destino de foco. Los cinco nodos TabElement están en XDAT, aunque sus nombres internos genéricos no permiten asignarles una identidad textual independiente.

### Desajustes XDAT–script que requieren resolución

El XDAT Animal actual **no contiene** estos nombres que sí aparecen en el código extraído:
- `InventoryWnd.EnchantButton`
- `InventoryWnd.FastDelete`
- `InventoryWnd.RefineButton`
- `InventoryWnd.BtnExpand`
- `InventoryWnd.b_FastDelete` (referencia de anclaje en el bloque de layout)

El código intenta obtener handles para EnchantButton, FastDelete y RefineButton durante `InitHandle()`; además define una ruta `RefineButton` en `OnClickButton`. `BtnExpand` y `b_FastDelete` aparecen dentro del bloque de layout de `function3`, que comienza con un `return;` y no se ejecuta en la fuente extraída. En contraste, el XDAT sí contiene `CrystallizeButton`, `TrashButton` y `CancelBar`, que son nombres utilizados por el script.

Estos desajustes son evidencia de una posible mezcla de generaciones de UI entre el script y el XDAT, o de referencias heredadas que el script conserva aunque el XDAT ya no las defina. **No se corrigen automáticamente**: antes hay que comprobar si el runtime tolera handles ausentes, si existen aliases globales y si el paquete contiene otra definición de ventana que complete esos controles.

### Nodos XDAT sin referencia textual por nombre en InventoryWnd.uc

`BackTexture`, `Container`, `AdenaBox`, `WeightIcon`, `WeightBG`, `Equip_Classic`, `ObjectViewport`, `EquipSlotBg` y `SearchItem` no aparecen como nombres en el código fuente extraído. La mayoría son recursos/contendedores visuales; `SearchItem` es un ItemWindow real sin enlace nominal encontrado. La ausencia de referencia textual no demuestra que sean inútiles: algunos pueden ser consumidos por el motor de UI, por herencia/configuración XDAT o por APIs que operan sobre la jerarquía.

El nodo `Frame` y el nodo `Tex` sí participan en las referencias de layout del bloque inactivo de `function3`. `Slots2` también aparece allí; el código pide `Slots2`, mientras el XDAT contiene tanto `Slots` como `Slots2`.

### Conclusión y siguiente prueba

El esqueleto de slots de equipo y los paneles principales está alineado con el XDAT. La zona de botones y layout muestra diferencias de nombres concretas que no deben resolverse por intuición. La siguiente prueba segura será rastrear los nombres ausentes y los nodos no enlazados en las demás fuentes del paquete Animal, además de revisar las definiciones de ventana y los aliases del API UI. No se modificó ningún archivo de producción ni el comportamiento del cliente.


## Trigésima sexta pasada: búsqueda global de los controles desajustados (2026-11-14)

Se amplió el cruce anterior a las 160 clases UnrealScript exportadas del paquete AnimalGame y se buscaron tanto los nombres simples como las rutas completas `InventoryWnd.<Control>`.

Resultados:
- `InventoryWnd.EnchantButton`: una sola referencia en todo el paquete, dentro de `InventoryWnd.InitHandle()`.
- `InventoryWnd.FastDelete`: una sola referencia de ruta completa; dos apariciones del nombre simple, ambas en `InventoryWnd.uc` (handle y tooltip).
- `InventoryWnd.RefineButton`: una sola referencia de ruta completa dentro de `InventoryWnd.InitHandle()`; las demás apariciones de `RefineButton` pertenecen a las ventanas `RefineryWnd` y `UnrefineryWnd`, no a InventoryWnd.
- `InventoryWnd.BtnExpand`: dos referencias de ruta completa, ambas dentro del bloque de layout inactivo de `function3`. Las otras apariciones de `BtnExpand` en el paquete pertenecen a `SSQMainBoard`, `QuestTreeWnd` y `RecipeTreeWnd`; no prueban que exista el control de InventoryWnd.
- `InventoryWnd.SearchItem`: cero referencias en las 160 clases.
- `InventoryWnd.CrystallizeButton`, `InventoryWnd.TrashButton` e `InventoryWnd.InventoryTab`: sus referencias encontradas pertenecen a `InventoryWnd.uc`; no hay consumidores externos que expliquen los controles ausentes.
- `InventoryWnd.CancelBar`: una sola referencia, en el callback de botón que restaura el inventario.

### Interpretación

La búsqueda global confirma que los nombres ausentes no están siendo resueltos desde otra clase UnrealScript del mismo paquete. En particular, no hay una clase auxiliar que cree o enlace `EnchantButton`, `FastDelete`, `RefineButton` o `BtnExpand` bajo la ruta `InventoryWnd.*`. Esto refuerza la hipótesis de referencias heredadas o de una discrepancia entre la fuente exportada y el XDAT, pero no permite concluir si los handles nulos causan un fallo: esa respuesta depende del comportamiento del runtime UI y de la versión exacta de sus APIs.

`SearchItem` es el único control interactivo de tipo ItemWindow del XDAT que no tiene ninguna referencia textual encontrada en las 160 clases. Se mantiene como nodo no explicado; no se elimina ni se asigna a una función por semejanza.

La siguiente comprobación será revisar las firmas y comportamiento de `GetHandle`, `ButtonHandle`, `SetFocus` y el registro de callbacks en las fuentes/API disponibles del mismo paquete, para separar referencias obsoletas inocuas de posibles incompatibilidades reales. No se modificó ningún control ni código de producción.


## Trigésima séptima pasada: análisis de riesgo de handles ausentes en InitHandle (2026-11-14)

Se inspeccionó el orden real de ejecución de `InventoryWnd.OnLoad()` y `InitHandle()`, y se comparó con patrones de otras ventanas del mismo paquete.

### Secuencia confirmada

1. `OnLoad()` registra eventos y llama a `InitHandle()`.
2. `InitHandle()` obtiene los handles principales de inventario y los 15 slots de equipo.
3. Después obtiene `InventoryWnd.SortButton`, `InventoryWnd.EnchantButton`, `InventoryWnd.FastDelete` y `InventoryWnd.RefineButton`.
4. Inmediatamente después de obtenerlos ejecuta:
   - `button_1.SetTooltipCustomType(...)`
   - `button_2.SetTooltipCustomType(...)`
   - `button_3.SetTooltipCustomType(...)`
   - `button_4.SetTooltipCustomType(...)`
5. El XDAT inspeccionado contiene `SortButton`, pero no contiene `EnchantButton`, `FastDelete` ni `RefineButton`.

Esto demuestra que tres handles pedidos por `InitHandle()` no tienen un nodo homónimo en el XDAT actual, y que el script intenta utilizarlos inmediatamente. No es solo código antiguo aislado en una función sin llamadas: la secuencia está en la inicialización normal de la ventana.

### Qué se puede y qué no se puede concluir

- **Confirmado:** hay tres discrepancias de nombres entre el script y el XDAT; los tres handles se usan inmediatamente para configurar tooltips.
- **No demostrado:** si `GetHandle()` devuelve un handle inválido tolerado por el runtime, un objeto nulo, un proxy no operativo o si el motor resuelve esos nombres por algún mecanismo externo. Las fuentes UnrealScript exportadas no incluyen la implementación nativa de `GetHandle` ni de `SetTooltipCustomType`.
- En las 160 clases no se encontró un patrón de comprobación `None`/validez aplicado a los handles UI antes de invocar sus métodos. Los ejemplos comparables de `RefineryWnd` también adquieren handles y configuran animaciones/controles sin comprobación visible, pero esto no prueba cómo se comporta el runtime ante un handle ausente.
- `OnClickButton` contiene rutas para `RefineButton` y `BtnExpand`; la primera apunta a abrir/cerrar `AugmentationWnd`, mientras la segunda depende de un bloque de layout que empieza con `return;`. El XDAT no contiene esos botones bajo esos nombres.

### Riesgo y decisión

Se clasifica el desajuste de `EnchantButton`, `FastDelete` y `RefineButton` como **riesgo de inicialización que requiere prueba de runtime**, no como fallo confirmado. No se añadieron controles, no se cambiaron los nombres ni se insertaron comprobaciones especulativas: cualquiera de esas acciones modificaría el comportamiento sin conocer el contrato nativo.

La prueba que resolvería el riesgo requiere ejecutar exactamente este paquete AnimalGame con su runtime original y observar la inicialización de InventoryWnd (log/depurador o captura de error). Si no se dispone del cliente ejecutable, el siguiente mejor insumo es el `NWindow.dll` exacto de esa instalación para inspeccionar la semántica de handles.

No se modificó `InventoryWnd.uc`, XDAT ni ningún paquete de producción.


## Trigésima octava pasada: control huérfano SearchItem y flujo de búsqueda (2026-11-14)

Se inspeccionó el nodo XDAT `SearchItem` y se siguió el flujo completo de búsqueda/restauración de objetos en el fuente Animal.

### Datos directos del XDAT

`SearchItem` es un `ct0.ItemWindow` de 339 × 288, con 9 columnas × 8 filas, máximo 250 objetos, iconos de 32 × 32 y separación de 4 × 4. Sus dimensiones y capacidad coinciden con el panel `CraftingItem`; no es un campo de texto ni un botón. Está ubicado como hijo directo de `InventoryWnd`, en la posición XDAT `children[36]`.

### Flujo de búsqueda en el script

- `OnLoad()` asigna `SearchBox = EditBoxHandle(GetHandle("InventoryWnd.SearchBar"))`.
- `OnKeyUp()` comprueba si SearchBox tiene foco; si queda vacío llama a `RestoreInventory()`; si tiene texto de longitud suficiente llama a `FilterInventory(SearchBox.GetString())`.
- `FilterInventory()` construye una lista filtrada a partir de `m_invenItem` y actualiza ese mismo ItemWindow mediante `Clear()` y `AddItem()`.
- `RestoreInventory()` vacía SearchBox, solicita la lista de inventario, carga el orden guardado y reconstruye la copia del inventario.
- No existe una asignación de handle para `SearchItem`, ni una llamada textual a `GetHandle("InventoryWnd.SearchItem")`, ni un uso por nombre simple dentro de las 160 clases exportadas.

### Conclusión

El control `SearchItem` no participa en el flujo de búsqueda implementado por `InventoryWnd.uc`: el filtro opera sobre el ItemWindow principal `InventoryItem`. Su geometría idéntica a la de `CraftingItem` sugiere que puede ser un panel residual, una superficie reservada para una función retirada o un nodo consumido por lógica nativa/configuración no visible en el fuente; ninguna de esas posibilidades queda demostrada.

No se debe conectar `SearchItem` al filtro ni eliminarlo sin evidencia adicional. Queda clasificado como **nodo ItemWindow huérfano en la capa UnrealScript**, con posible función externa aún desconocida.

La siguiente comprobación segura es comparar su posición, anclajes, orden de hijos y propiedades con el panel `CraftingItem` y con las ventanas equivalentes de las variantes de código ya archivadas, para detectar si es un duplicado estructural o un elemento heredado de otra generación de UI. No se modificó el XDAT ni el código de producción.


## Trigésima novena pasada: comparación estructural exacta de SearchItem (2026-11-14)

Se compararon todas las propiedades decodificadas de `SearchItem` con `CraftingItem` y se verificó su posición dentro del árbol de hijos de `InventoryWnd`.

### Resultado de la comparación

- Ambos nodos son `ct0.ItemWindow`.
- Las propiedades decodificadas son idénticas salvo el campo `name`: `SearchItem` frente a `CraftingItem`.
- Coinciden tamaño (339 × 288), anclajes, desplazamiento (217, 55), grilla (9 × 8), máximo (250), iconos (32 × 32), separación (4 × 4), tipo de popup (`Inventory`), y texturas de contorno superior/inferior.
- No hay diferencias adicionales en el conjunto de propiedades exportadas.

### Posición y orden de hermanos

El orden real de hijos alrededor de los paneles es:

| Índice | Control |
|---:|---|
| 31 | `EquipmentItem` |
| 32 | `CraftingItem` |
| 33 | `SuppliesItem` |
| 34 | `SearchBar` |
| 35 | `CancelBar` |
| 36 | `SearchItem` |

Por tanto, `SearchItem` fue colocado inmediatamente después de los dos controles de búsqueda, pero su configuración no contiene ninguna diferencia respecto de `CraftingItem`. Esto es una pista estructural relevante, no prueba de que el motor lo utilice para mostrar resultados.

### Correlación con el código

El fuente asigna handles a `CraftingItem` y `SuppliesItem`, pero no a `SearchItem`. El filtrado sigue escribiendo los resultados en `InventoryItem`; no hay llamada que cambie el destino a `SearchItem`. La proximidad a `SearchBar`/`CancelBar` y el nombre del control podrían sugerir una implementación de búsqueda incompleta o retirada, pero la evidencia disponible no permite afirmar que esa fuera su función original.

### Conclusión y decisión

`SearchItem` es un **duplicado estructural exacto de CraftingItem con nombre distinto**, añadido como último hermano tras los controles de búsqueda. Se registra esta relación con alta confianza. Su propósito funcional sigue sin demostrarse.

No se cambió el destino de `FilterInventory()`, no se enlazó el control y no se eliminó ningún nodo. El próximo paso será revisar las variantes de InventoryWnd disponibles en el repositorio y los metadatos de origen para establecer si `SearchItem` aparece en otras generaciones o si solo existe en este XDAT de AnimalGame.


## Cuadragésima pasada: contraste con InventoryWnd de Berserker (2026-11-14)

Se revisó el informe de comparación cruzada incluido en el artefacto de reconstrucción, que compara `Animal-Rebuilt/UnrealScript/InventoryWnd.uc` con la variante `Cross-Package-InventoryWnd/berserker/InventoryWnd.uc`.

### Resultado verificable

- El análisis registra 72 funciones en Animal y 72 en Berserker.
- Las 72 funciones con el mismo nombre tienen cuerpos idénticos después de normalizar comentarios y espacios en blanco.
- La coincidencia incluye `OnLoad()`, `InitHandle()`, `OnKeyUp()`, `CopyInventory()`, `RestoreInventory()` y `FilterInventory()`.
- Los hashes SHA-256 de los cuerpos de `InitHandle()` coinciden exactamente: `740126d1ba690e96855d70b2d23f37e629b13e143efb02ba638332eda6fee74d`.
- Por tanto, la ausencia de un handle para `SearchItem` y la ruta de búsqueda que actualiza `InventoryItem` no parecen ser una peculiaridad introducida por la reconstrucción Animal del script: la misma lógica está en la variante Berserker comparada.

### Límite de esta evidencia

Este contraste es entre fuentes UnrealScript, no entre XDAT. No demuestra que el XDAT Berserker tenga el mismo nodo `SearchItem`, ni permite determinar qué versión de interfaz acompaña a cada fuente. Tampoco valida el runtime de los handles ausentes.

El informe de comparación usa hashes de cuerpos normalizados; no se ha afirmado igualdad binaria de archivos completos ni identidad de los paquetes compilados.

### Conclusión

La hipótesis de que `SearchItem` es un nodo residual/no utilizado por la lógica UnrealScript gana apoyo: la variante Berserker comparte íntegramente el flujo de búsqueda Animal, sin que `SearchItem` participe en él. Aun así, su función externa o su presencia en otros XDAT sigue abierta.

Se mantiene el XDAT sin cambios. El siguiente paso será examinar la variante Silent Dark y separar diferencias reales de lógica frente a diferencias de nombres recuperados automáticamente.


## Cuadragésima primera pasada: contraste con InventoryWnd de Silent Dark (2026-11-14)

Se examinó el archivo completo `Cross-Package-InventoryWnd/silent-dark/InventoryWnd.uc` del mismo artefacto y se comparó su estructura con la fuente Animal.

### Diferencias de arquitectura confirmadas

- Animal contiene 72 funciones; Silent Dark contiene 53 según el inventario del análisis del artefacto (52 declaraciones detectables con el patrón de extracción independiente, por una diferencia de conteo que queda anotada y no se fuerza a coincidir).
- Se compararon 47 nombres de función compartidos: 17 tienen cuerpo idéntico tras normalizar comentarios/espacios; el resto difiere.
- Silent Dark no contiene `FilterInventory()`, `RestoreInventory()` ni `OnKeyUp()`. En su lugar tiene `OnChangeEditBox()` y `SearchInventory()`.
- Silent Dark registra el cambio de texto del EditBox y filtra desde `InventoryWnd.Container.SearchBar`. Su búsqueda oculta los paneles de inventario/quest y muestra un ItemWindow de resultados que el script identifica como `InventoryWnd.Container.EquipItem_Underwearss`.
- Animal usa `InventoryWnd.SearchBar` y su función `FilterInventory()` modifica `InventoryItem`; no utiliza el identificador `EquipItem_Underwearss` como destino de búsqueda en el flujo de filtro.
- Silent Dark obtiene los handles principales mediante rutas bajo `.Container.`, y obtiene `SortButton`, `TrashButton`, `btnEnchant` y `SearchBar`. No pide `EnchantButton`, `FastDelete` ni `RefineButton`.
- Silent Dark tampoco contiene referencia a `SearchItem). Su panel de resultados es el control denominado `EquipItem_Underwearss`, una convención heredada/atípica en el código; no se debe equiparar automáticamente al nodo Animal `SearchItem`.

### Correspondencia de eventos

Silent Dark usa el evento 2600 para `HandleAddItem` y el 2630 para `HandleAddHennaInfo`; Animal/Berserker usan esos mismos IDs para `function30` y `function37`. La diferencia de rutas confirma que las variantes pertenecen a generaciones o esquemas de eventos distintos. No se deben copiar handlers entre ellas por coincidencia de número.

### Conclusión

Silent Dark demuestra que existió una arquitectura alternativa de búsqueda: un EditBox con callback de cambio y un ItemWindow de resultados separado, distinto del inventario normal. Esto aporta contexto histórico útil, pero **no demuestra** que el `SearchItem` de XDAT Animal sea ese mismo panel, porque los nombres, rutas, jerarquía y fuente son diferentes.

La hipótesis de que el `SearchItem` de Animal sea un residuo de búsqueda sigue abierta; ahora tiene una comparación de referencia concreta para futuras pruebas. No se renombraron controles ni se modificó código/XDAT de producción.


## Cuadragésima segunda pasada: trazado de la ruta de resultados en Silent Dark (2026-11-14)

Se reabrió el fuente Silent Dark completo para verificar la ruta exacta de escritura de resultados, sin inferirla por el nombre de variables recuperadas.

### Ruta confirmada

1. `InitHandle()` asigna el nombre literal `EquipItem_Underwearss` a una variable.
2. Obtiene el handle de origen desde `InventoryWnd.Container.InventoryItem`.
3. Obtiene el handle de resultados desde `InventoryWnd.Container.EquipItem_Underwearss`.
4. Obtiene el EditBox desde la ruta `InventoryWnd.Container.SearchBar` al ejecutar `SearchInventory()`.
5. Cuando hay texto, limpia el ItemWindow de resultados, recorre los objetos del inventario original y añade coincidencias al destino mediante `UIAPI_ITEMWINDOW.AddItem()`.
6. Durante la búsqueda oculta los paneles normal y de misiones, muestra el destino de resultados y deshabilita las dos pestañas del TabCtrl.
7. Cuando el texto queda vacío, vuelve a mostrar el inventario original, oculta el panel de resultados y reactiva las pestañas.

La ruta de destino concatenada por el código es exactamente `InventoryWnd.container.InventoryItem_1.EquipItem_Underwearss`. La ruta de adquisición del handle es `InventoryWnd.Container.EquipItem_Underwearss`; se conserva esta diferencia de capitalización del fuente y no se normaliza como si el runtime garantizara equivalencia.

### Implicación para Animal

El XDAT Animal contiene `SearchItem`, pero no contiene el nombre `EquipItem_Underwearss` como hijo de `InventoryWnd`; ese nombre sí aparece como uno de los slots de equipo fuera del contenedor, en otra ruta. Animal tampoco contiene la arquitectura de búsqueda de Silent Dark. Por eso no existe evidencia para mapear el `SearchItem` Animal directamente al panel de resultados Silent Dark.

### Límites

El artefacto actual no incluye el XDAT Silent Dark correspondiente. Por ello no se puede comprobar la jerarquía visual, el tipo de control ni las propiedades reales del nodo `EquipItem_Underwearss` de esa variante. Tampoco se ha ejecutado el código en el cliente para validar el comportamiento de las rutas.

Se mantiene la conclusión: Silent Dark documenta un patrón funcional de resultados en un ItemWindow separado; Animal tiene un nodo `SearchItem` estructuralmente duplicado de `CraftingItem`, pero sin vínculo en su script. No se conectó ni renombró ningún control.

### Nota de precisión sobre el inventario de funciones

El recuento de 53 funciones Silent Dark se toma del inventario del artefacto. La extracción independiente por patrón de declaraciones detecta 52; esta diferencia de un elemento permanece como discrepancia de metodología y no se usa para afirmar una cantidad exacta de funciones compiladas.


## Cuadragésima tercera pasada: auditoría de riesgo en los fuentes reabiertos (2026-11-14)

Se comparó el fuente base `Animal-Rebuilt/UnrealScript/InventoryWnd.uc` con `Reopened-Clean`, `Reopened-Edited` y los candidatos de renombrado del artefacto.

### Hallazgos críticos

- `Reopened-Clean/InventoryWnd.uc` y `Reopened-Edited/InventoryWnd.uc` son idénticos byte por byte (SHA-256 `ba569baad297a387df748eb079fd6a1bd9d946bd3ab62c1949c980467cc67435`). No son dos validaciones independientes.
- En el fuente base, el evento 2600 llama a `function30(param)`. La función `function30(string)` procesa el texto del evento y existe además una función distinta `bool HandleAddItem(ItemInfo)`, usada por el filtrado para decidir si un objeto coincide.
- En los fuentes reabiertos, el evento 2600 fue cambiado a `HandleAddItem(param)`, aunque el método `bool HandleAddItem(ItemInfo)` permanece y no se creó una versión `HandleAddItem(string)`. Esto introduce una colisión/incompatibilidad de firma potencial y rompe la correspondencia original entre evento y parser. No se debe aceptar como renombrado seguro.
- El evento 2630 se redirigió de `function37(param)` a `HandleAddHennaInfo(param)`. En el archivo reabierto aparece un wrapper adicional con ese nombre, mientras `function37(string)` sigue presente. El cambio altera la ruta de despacho y necesita validación independiente; el nombre descriptivo por sí solo no demuestra equivalencia.
- La reapertura también modifica los offsets de comentarios de control de flujo, agrega numerosos `return;` finales y altera la representación de las teclas Alt/Ctrl de constantes simbólicas a números. Esas diferencias no deben tratarse como mejoras funcionales ni asumirse inocuas sin recompilar y probar.

### Candidatos de nombres

Los archivos `AddItemCandidate` y `HennaCandidate` son experimentos separados que renombran la declaración y el despacho de una función anónima. No deben confundirse con el resultado `Reopened-Clean`, que conserva `function30`/ `function37` pero redirige parcialmente los eventos.

### Decisión

Se marca `Reopened-Clean` y `Reopened-Edited` como material de investigación, **no como fuente de producción validado**. Se conserva el fuente base como referencia de comportamiento para esta auditoría. No se copió ninguno de estos cambios al paquete ni al código de producción.

Próximo paso: comparar los cuerpos completos de `function30`, `function37`, `HandleAddItem(ItemInfo)` y el wrapper `HandleAddHennaInfo`, y reconstruir una tabla de llamadas/firmas para confirmar qué nombres pueden recuperarse sin alterar el despacho.


## Cuadragésima cuarta pasada: reconstrucción de sobrecargas y corrección del diagnóstico de la pasada 43 (2026-11-14)

Se inspeccionaron directamente los cinco fuentes presentes en el ZIP, incluidos los candidatos `AddItemCandidate` y `HennaCandidate`. Esta revisión permite precisar el despacho y corregir una conclusión anterior.

### Evento 2600: dos funciones con el mismo nombre

En el fuente base existen dos responsabilidades distintas:
- `function30(string param)`: convierte el parámetro del evento con `ParamToItemInfo`, clasifica el objeto y lo incorpora al inventario o equipo.
- `function bool HandleAddItem(ItemInfo info_4)`: predicado usado por el filtro de inventario para decidir si un objeto entra en la lista de resultados.

El candidato `AddItemCandidate` renombra la primera función a `HandleAddItem(string string_2)`, manteniendo la segunda como `HandleAddItem(ItemInfo info_4)`. Por tanto, **no es una colisión incompatible** si el compilador UnrealScript de destino admite sobrecarga por tipos, como sugiere la propia estructura del candidato. La pasada 43 fue demasiado concluyente al calificar el cambio de firma como incompatible: se corrige aquí a «sobrecarga candidata que requiere compilación real para validar».

En cambio, `Reopened-Clean` y `Reopened-Edited` redirigen el evento 2600 a `HandleAddItem(param)`, pero conservan únicamente la declaración `HandleAddItem(ItemInfo)` y no incluyen la sobrecarga `HandleAddItem(string)`. En esos dos archivos sí queda una llamada sin una declaración textual compatible. No son equivalentes al candidato AddItemCandidate.

### Evento 2630: Henna

El fuente base despacha el evento 2630 a `function37(param)`. El candidato `HennaCandidate` cambia el nombre de esa función a `HandleAddHennaInfo(string string_2)` y conserva su cuerpo, que llama a `UpdateHennaInfo()`. Esta transformación mantiene el tipo de parámetro y el comportamiento visible del wrapper.

`Reopened-Clean`/ `Reopened-Edited` también redirigen 2630 a `HandleAddHennaInfo(param)` y añaden un wrapper con ese nombre. Para el evento 2630, por tanto, la ruta sí tiene una declaración string visible. El riesgo principal en estos archivos no es una firma ausente para 2630, sino las alteraciones adicionales del fuente reabierto y la falta de compilación/runtime.

### Estado de validación

- `AddItemCandidate`: renombrado del parser de evento a una sobrecarga `HandleAddItem(string)`; candidato coherente en el texto, aún no compilado.
- `HennaCandidate`: renombrado de `function37(string)` a `HandleAddHennaInfo(string)`; candidato coherente en el texto, aún no compilado.
- `Reopened-Clean` y `Reopened-Edited`: idénticos entre sí; contienen cambios adicionales y el despacho 2600 carece de una sobrecarga string visible. No validar como fuente final.
- Fuente base: sigue siendo la referencia conservadora para preservar comportamiento.

No se modificó producción ni se ha afirmado compatibilidad binaria. El próximo paso es verificar el soporte de sobrecarga y compilar los candidatos por separado con el compilador/runtime correcto, si está disponible.


## Cuadragésima quinta pasada: verificación de llamadas y declaraciones en candidatos (2026-11-14)

Se volvió al ZIP original y se inspeccionaron línea por línea las declaraciones y todas las llamadas de los dos candidatos, para separar el cambio de nombre de la función de la ruta real de evento.

### AddItemCandidate

- El evento 2600 llama a `HandleAddItem(param)`.
- El archivo declara `HandleAddItem(string string_2)`, cuyo cuerpo coincide con el cuerpo original de `function30(string string_2)`: convierte el parámetro con `ParamToItemInfo`, clasifica equipo/quest/ítem y actualiza las ventanas correspondientes.
- El filtro conserva `HandleAddItem(info_4)` con parámetro `ItemInfo`; la llamada del filtro sigue en `if(HandleAddItem(info_4))`.
- Por tanto, las dos llamadas tienen argumentos de tipos distintos y ambas declaraciones existen en el candidato. La ruta de evento 2600 ya no apunta a la función anónima y el predicado del filtro no fue reemplazado.

### HennaCandidate

- El evento 2630 llama a `HandleAddHennaInfo(param)`.
- El archivo declara `HandleAddHennaInfo(string string_2)`; su cuerpo coincide con el wrapper original `function37(string string_2)` y llama a `UpdateHennaInfo()`.
- El evento 260 sigue usando `HandleUpdateHennaInfo(param)`; esa función no se renombró ni se redirigió.
- El cambio se limita al wrapper asociado al evento 2630.

### Independencia y límites de los candidatos

Los dos candidatos son pruebas separadas: `AddItemCandidate` no incorpora el renombrado Henna; `HennaCandidate` no incorpora el renombrado AddItem. Esto evita atribuirles una integración que todavía no existe.

El ZIP no contiene compilador, ejecutable, DLL ni script de build. No fue posible compilar ni probar sobre el cliente. La sintaxis de sobrecarga con `string` e `ItemInfo` está presente en el candidato, pero su aceptación por el compilador concreto permanece sin validar.

### Conclusión

La trazabilidad textual de los dos candidatos queda confirmada:
- AddItem: evento 2600 → wrapper string renombrado; filtro → predicado ItemInfo conservado.
- Henna: evento 2630 → wrapper string renombrado; evento 260 → handler original conservado.

No se combinaron aún ambos cambios ni se modificó producción. El siguiente paso es revisar el inventario del repositorio para localizar el compilador/entorno compatible o una prueba previa de sobrecarga en el mismo dialecto de UnrealScript.


## Cuadragésima sexta pasada: evidencia binaria del paquete HennaCandidate (2026-11-14)

Se volvió a inspeccionar el ZIP de validación y sus tres paquetes Unreal compilados. Apareció evidencia adicional que permite avanzar más allá del análisis textual.

### Inventario de paquetes

| Paquete | Bytes | SHA-256 |
|---|---:|---|
| 'Interface-clean.u' | 3,338,657 | 'b9a443ac57cc39e9099e2ab47683c95070852379ec4a178b9fead8c79e17a255' |
| 'Interface-edited-test.u' | 3,338,801 | '9096389fc5457e97bc8d4b67312781b4965c0fa272cd4bf3fb8d4360bf6ce756' |
| 'InventoryWnd-HennaCandidate.u' | 3,338,681 | 'b4b99ab61d90241e68c9c8afcf86edfc11e6af8694f4ab992967493652cbe1eb' |

### Comprobación de nombres en los paquetes

Se inspeccionaron las cadenas ASCII imprimibles de cada paquete:
- 'Interface-clean.u': contiene 'function37' y 'function30'; no contiene 'HandleAddHennaInfo'.
- 'Interface-edited-test.u': conserva el mismo conjunto relevante de nombres que el paquete limpio; tampoco contiene 'HandleAddHennaInfo'.
- 'InventoryWnd-HennaCandidate.u': contiene 'HandleAddHennaInfo', conserva 'function30' y ya no contiene 'function37'.

Esto demuestra que el artefacto incluye un paquete binario separado que incorpora el renombrado HennaCandidate, no solo el archivo fuente .uc. La diferencia de tamaño frente al paquete limpio es de 24 bytes.

### Qué demuestra y qué no

- **Sí demuestra:** existe un paquete Unreal serializado distinto con el nuevo nombre 'HandleAddHennaInfo' en sus cadenas, y sin la cadena 'function37'.
- **No demuestra:** que el paquete se haya cargado correctamente en el cliente, que el evento 2630 haya despachado al wrapper correcto en runtime, ni que la interfaz haya pasado pruebas funcionales. La presencia de una cadena no prueba por sí sola la tabla de referencias ni el comportamiento.
- No hay .exe, .dll, .bat, .cmd, .ps1 ni compilador dentro del ZIP. Tampoco hay registro de ejecución del paquete en cliente.

### Conclusión

La validación de Henna avanza de «candidato fuente no compilado» a **«paquete candidato compilado/serializado presente, sin prueba de runtime»**. Se mantiene 'Interface-clean.u' como referencia y no se sustituye el paquete de producción. El candidato AddItem sigue sin un paquete binario propio identificable en el artefacto.


## Cuadragésima séptima pasada: comparación binaria de paquetes y límite de atribución (2026-11-14)

Se descargó nuevamente el artefacto de validación 14102532523 y se compararon byte a byte los tres paquetes Unreal incluidos. El objetivo fue comprobar si el paquete HennaCandidate constituye una modificación binaria pequeña y aislada respecto del paquete limpio.

### Resultados reproducibles

| Comparación | Bytes distintos en la zona común | Tramos de diferencias | Diferencia de tamaño |
|---|---:|---:|---:|
| Interface-clean.u vs Interface-edited-test.u | 3,020,362 | 138,420 | 144 bytes |
| Interface-clean.u vs InventoryWnd-HennaCandidate.u | 3,068,536 | 135,457 | 24 bytes |

Hashes SHA-256 verificados:
- Interface-clean.u: `b9a443ac57cc39e9099e2ab47683c95070852379ec4a178b9fead8c79e17a255`
- Interface-edited-test.u: `9096389fc5457e97bc8d4b67312781b4965c0fa272cd4bf3fb8d4360bf6ce756`
- InventoryWnd-HennaCandidate.u: `b4b99ab61d90241e68c9c8afcf86edfc11e6af8694f4ab992967493652cbe1eb`

### Interpretación

- En el paquete HennaCandidate se observa el nombre `HandleAddHennaInfo` y ya no aparece `function37`; el paquete limpio conserva `function37`.
- Sin embargo, el cambio binario no está limitado a los bytes de ese nombre: hay más de 3 millones de bytes distintos respecto del paquete limpio.
- La comparación clean/edited también presenta diferencias masivas de bytes. Esto indica que una reconstrucción/serialización del paquete puede alterar muchas regiones aunque el cambio fuente sea pequeño.
- Por ello, no es válido afirmar que el paquete HennaCandidate contiene únicamente el renombrado Henna ni atribuir todos los cambios binarios a esa función. Las cadenas visibles prueban la presencia/ausencia de nombres, no la integridad de las tablas de exportación, bytecode, referencias ni el comportamiento.

### Decisión de seguridad

Se corrige el grado de confianza de la pasada 46: el artefacto contiene un paquete candidato serializado con el nuevo nombre, pero **no hay validación binaria diferencial aislada ni prueba de carga/runtime**. Se mantiene como candidato experimental. No sustituir `Interface-clean.u` ni desplegar HennaCandidate en el cliente.

Próximo paso: conseguir un parser de paquetes Unreal compatible con esta versión o un volcado de exports/bytecode para comparar la clase InventoryWnd y confirmar que el cambio está limitado a la función esperada.


## Cuadragésima octava pasada: contraste directo HennaCandidate vs HennaCandidate-Reopened (2026-11-14)

Se compararon los dos archivos fuente del ZIP, no solo las cadenas del paquete binario. Esta pasada separa definitivamente el candidato Henna aislado de su variante reabierta.

### HennaCandidate (fuente de prueba aislada)

- Evento 2600 conserva la llamada original `function30(param)`.
- Evento 2630 llama a `HandleAddHennaInfo(param)`.
- Declara `HandleAddHennaInfo(string string_2)`, con el cuerpo del wrapper original `function37`.
- El predicado `HandleAddItem(ItemInfo info_4)` se conserva para el filtro.
- No aparece una declaración `HandleAddItem(string)`, pero tampoco se redirige el evento 2600 hacia ese nombre.

**Conclusión:** el candidato aislado Henna no introduce el problema de firma del evento 2600. El cambio textual visible está limitado al nombre del wrapper de Henna y su llamada en evento 2630.

### HennaCandidate-Reopened (fuente reabierta desde paquete)

- Evento 2630 conserva `HandleAddHennaInfo(param)`.
- Evento 2600 fue cambiado a `HandleAddItem(param)`.
- La única declaración de `HandleAddItem` sigue siendo `function bool HandleAddItem(ItemInfo info_4)`; no existe una sobrecarga string en ese archivo.
- Por tanto, el evento 2600 pasa un `string` a una función declarada con `ItemInfo`. La ruta no tiene una declaración textual compatible.
- Además, la reabertura alteró la estructura del archivo (3,523 a 3,691 líneas), duplicó varios `return` y modificó otros segmentos del switch/eventos. No debe tratarse como un espejo fiel del candidato fuente original.

### Corrección y decisión

El paquete `InventoryWnd-HennaCandidate.u` se asocia con la prueba aislada, pero la inspección de cadenas por sí sola no demuestra equivalencia exacta de bytecode. La fuente aislada es coherente en sus rutas 2600/2630 a nivel de llamadas y tipos visibles; la variante reabierta no lo es para el evento 2600.

Se mantienen tres estados separados:
1. `HennaCandidate/InventoryWnd.uc`: candidato fuente aislado, revisión estática favorable.
2. `HennaCandidate-Reopened/InventoryWnd.uc`: reconstrucción posterior, no apta como fuente final por la redirección 2600.
3. `Interface-clean.u`: referencia limpia que no se modifica.

No se cambia producción. Próximo paso: comparar el export/bytecode de InventoryWnd entre el paquete limpio y HennaCandidate con un parser compatible, para verificar que el paquete serializado corresponde realmente al fuente aislado.


## Cuadragésima novena pasada: diff exacto del fuente HennaCandidate (2026-11-14)

Se reabrió el ZIP de validación y se comparó el fuente base `Animal-Rebuilt/UnrealScript/InventoryWnd.uc` directamente contra `Animal-Rebuilt/InventoryWnd-Name-Probes/HennaCandidate/InventoryWnd.uc`, usando comparación de líneas completa y sin normalizar.

### Resultado exacto

- Fuente base: 72,982 bytes; SHA-256 `e4dfbd39cc3c6b41f01d6ab16461ff7bd7f53638014151bac72097396bc484eb`.
- HennaCandidate: 72,998 bytes; SHA-256 `4302dde976865d1f743d5dcb817d6cbe7f76dbc514cb5b45dcac755cffbf2ca0`.
- El diff contiene exactamente **dos líneas reemplazadas**, sin inserciones ni eliminaciones:
  1. En el despacho del evento 2630: `function37(param);` → `HandleAddHennaInfo(param);`.
  2. En la declaración del wrapper: `function function37(string string_2)` → `function HandleAddHennaInfo(string string_2)`.

No cambian las líneas del evento 2600, el parser `function30`, el predicado `HandleAddItem(ItemInfo)`, ni el cuerpo del wrapper. El cambio fuente está, por tanto, aislado de forma verificable a dos identificadores relacionados con Henna.

### Relación con el paquete binario

El paquete candidato real está en `Animal-Rebuilt/InventoryWnd-Name-Probes/InventoryWnd-HennaCandidate.u` (3,338,681 bytes), no en una carpeta `Reopened`. Su tamaño es 24 bytes mayor que `Interface-clean.u` (3,338,657 bytes). La comparación byte a byte del paquete sigue mostrando diferencias extensas por la serialización/reconstrucción; ese dato no invalida el diff fuente aislado, pero tampoco permite demostrar que el binario corresponda exactamente a ese fuente.

### Conclusión

La fuente `HennaCandidate/InventoryWnd.uc` queda confirmada como un cambio mínimo y aislado de dos líneas respecto del fuente base. Es la mejor base para continuar la reconstrucción estática. La validación binaria/compilación y la prueba en cliente siguen pendientes; no desplegar todavía el paquete candidato ni modificar el paquete limpio.


## Quincuagésima pasada: comparación de cadenas del paquete candidato (2026-11-14)

Se compararon las cadenas ASCII imprimibles (secuencias de 4 o más caracteres) de `Interface-clean.u` y `InventoryWnd-HennaCandidate.u`, conservando multiplicidad y comparando el conjunto completo, no solo buscando el nombre nuevo.

### Identidad de los paquetes

- `Interface-clean.u`: 3,338,657 bytes; SHA-256 `b9a443ac57cc39e9099e2ab47683c95070852379ec4a178b9fead8c79e17a255`.
- `InventoryWnd-HennaCandidate.u`: 3,338,681 bytes; SHA-256 `b4b99ab61d90241e68c9c8afcf86edfc11e6af8694f4ab992967493652cbe1eb`.

### Resultado de comparación completa de cadenas

Ambos paquetes contienen exactamente 84,308 secuencias ASCII imprimibles de longitud mínima 4. Al comparar sus multiconjuntos, las únicas diferencias son estas tres cadenas reemplazadas:

- Solo paquete limpio: `function37`.
- Solo paquete candidato: `HandleAddHennaInfo`.
- Solo paquete limpio: `function37(param);`.
- Solo paquete candidato: `HandleAddHennaInfo(param);`.
- Solo paquete limpio: `function function37(string string_2)`.
- Solo paquete candidato: `function HandleAddHennaInfo(string string_2)`.

No aparece ninguna otra cadena imprimible añadida, eliminada o con multiplicidad alterada. Esto coincide exactamente con los dos reemplazos del fuente documentados en la pasada 49 y aporta evidencia fuerte de que el paquete candidato es una variante textual acotada al wrapper Henna.

### Límite de esta comprobación

La prueba cubre cadenas imprimibles completas del paquete, no el bytecode UnrealScript ni la ejecución del evento. No sustituye un parser/desensamblador compatible con paquetes UE2 versión 126, una compilación reproducible o una prueba en cliente. Por tanto, no se declara validación binaria completa ni se autoriza instalación.

### Decisión

Mantener `InventoryWnd-HennaCandidate.u` como candidato preferido para la siguiente fase de inspección. `Interface-clean.u` sigue siendo la referencia intacta. No modificar ni desplegar el paquete limpio.


## Quincuagésima primera pasada: ubicación exacta de las tres referencias binarias de Henna (2026-11-14)

Se volvió a inspeccionar el contenido bruto de ambos `.u` del ZIP, localizando todas las apariciones byte a byte y revisando su contexto. Esta pasada precisa la conclusión de la comparación de cadenas de la pasada 50.

### Tres apariciones en cada paquete

En `Interface-clean.u`, `function37` aparece exactamente tres veces:
1. Tabla de nombres del paquete (offset 24,049): entrada de nombre `function37`.
2. Texto fuente serializado (offset 2,306,047): llamada `function37(param);` dentro del evento 2630.
3. Texto fuente serializado (offset 2,352,466): declaración `function function37(string string_2)`.

En `InventoryWnd-HennaCandidate.u`, `HandleAddHennaInfo` aparece exactamente tres veces en las posiciones correspondientes:
1. Tabla de nombres (offset 24,049): entrada `HandleAddHennaInfo`.
2. Texto fuente serializado (offset 2,306,055): llamada `HandleAddHennaInfo(param);` dentro del evento 2630.
3. Texto fuente serializado (offset 2,352,482): declaración `function HandleAddHennaInfo(string string_2)`.

Los contextos del evento 2600 siguen conteniendo `function30(param)` en ambos paquetes. La entrada `function30` y su declaración también permanecen presentes. El nombre `HandleAddItem` conserva sus apariciones y no sustituye a la ruta del evento 2600.

### Lectura técnica

La tabla de nombres y las dos referencias de texto del wrapper concuerdan con el cambio de fuente de dos líneas. No se observó una cuarta referencia residual a `function37` en el candidato, ni una referencia a `HandleAddHennaInfo` en el paquete limpio.

Esto verifica consistencia de nombres y texto serializado, pero el paquete incluye datos compilados/serializados que no se pueden validar completamente leyendo estas cadenas. No se ha decodificado el bytecode de las funciones ni probado la ejecución en el cliente. Se mantiene el bloqueo de instalación hasta contar con una herramienta compatible o una prueba controlada.

### Estado

`InventoryWnd-HennaCandidate.u` continúa siendo el candidato estático preferido. `Interface-clean.u` permanece intacto. No se modificó ningún archivo de producción.
