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
