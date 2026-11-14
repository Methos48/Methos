# L2Sudamerica — auditoría estructural de InventoryWnd

Fecha: 2026-11-14  
Origen: `reference-data/packages/l2sudamerica/Interface.u` y `interface.xdat`.  
Estado: fuentes U y XML obtenidos tras descifrado verificado; nombres originales de FName numéricos conservados en el mapa de nombres.

## Base técnica comprobada

- `Interface.u`: Unreal 123/37; 4,987 nombres, 793 imports, 9,433 exports; 10,226 objetos.
- UELib exportó 150/150 clases, con cero errores.
- `interface.xdat`: 169 ventanas, 8 shortcuts; XML → XDAT con coincidencia binaria exacta.
- `InventoryWnd.uc` y `windows/InventoryWnd.xml` proceden de esos dos mismos originales L2Sudamerica, no de otra interfaz.

## InventoryWnd — inventario de código

- 27 declaraciones `var` a nivel de clase.
- 26 constantes.
- 60 funciones exportadas por el decompilador.
- 12 códigos de evento registrados distintos.
- 52 nodos XDAT con nombre dentro del árbol, contando la raíz `InventoryWnd`; 51 son controles/ventanas descendientes.

### Variables y correspondencia con controles XDAT

Los nombres `MethosRecoveredName_N` son identificadores seguros asignados por el decompilador porque el FName original del paquete es numérico/no válido como identificador UnrealScript. No son nombres semánticos originales. La correspondencia de los handles sí se puede recuperar por las llamadas `GetHandle(...)` del código.

| Variable de clase | Tipo | Correspondencia / interpretación basada en el código |
|---|---|---|
| `MethosRecoveredName_1322` | string | Prefijo/nombre de ventana utilizado para formar rutas como `.InventoryItem` y para mostrar/ocultar la ventana; su valor de inicialización no aparece en el cuerpo exportado. |
| `MethosRecoveredName_2527` | ItemWindowHandle | `InventoryItem`, inventario principal. |
| `MethosRecoveredName_2505` | ItemWindowHandle | `QuestItem`, inventario de quest. |
| `MethosRecoveredName_2575[15]` | ItemWindowHandle[15] | Los 15 slots de equipo, en el orden detallado abajo. |
| `MethosRecoveredName_2391` | ItemWindowHandle | `HennaItem`. |
| `MethosRecoveredName_2571` | TextBoxHandle | `AdenaText` (el mismo número se reutiliza en variables locales de otras funciones; esas variables locales no son este campo). |
| `MethosRecoveredName_2392` | ItemWindowHandle | `EquipmentItem`. |
| `MethosRecoveredName_2393` | ItemWindowHandle | `CraftingItem`. |
| `MethosRecoveredName_2394` | ItemWindowHandle | `SuppliesItem`. |
| `MethosRecoveredName_2453` | ButtonHandle | `InventoryWnd.SortButton`. |
| `MethosRecoveredName_2395` | ButtonHandle | Se solicita `InventoryWnd.EnchantButton`; no aparece un control con ese nombre en el XML exportado. Pendiente resolver. |
| `MethosRecoveredName_2396` | ButtonHandle | Se solicita `InventoryWnd.FastDelete`; no aparece un control con ese nombre en el XML exportado. Pendiente resolver. |
| `MethosRecoveredName_2397` | ButtonHandle | Se solicita `InventoryWnd.RefineButton`; no aparece un control con ese nombre en el XML exportado. Pendiente resolver. |
| `MethosRecoveredName_2398` | ButtonHandle | `InventoryWnd.TrashButton`; cambia sus texturas entre modo normal/automático. |
| `MethosRecoveredName_2399` | ButtonHandle | `InventoryWnd.CancelBar`, botón de borrar/cancelar búsqueda. |
| `MethosRecoveredName_2400` | EditBoxHandle | `InventoryWnd.SearchBar`. |
| `MethosRecoveredName_2401` | ItemWindowHandle | `InventoryWnd.SearchItem`, lista filtrada de búsqueda. |
| `MethosRecoveredName_2455` | array<int> | Orden guardado del inventario: se llena con `ItemInfo.ClassID` y se pasa a `SaveInventoryOrder` / `LoadInventoryOrder`. |
| `MethosRecoveredName_2402` | Vector | Posición del clic/arrastre, obtenida con `GetClickLocation()` y pasada a `RequestDropItem`. |
| `MethosRecoveredName_4023` | array<ItemInfo> | Cola temporal de objetos que comparten los slots de oreja; se resuelve en los índices 8/9 del arreglo de equipo. |
| `MethosRecoveredName_4022` | array<ItemInfo> | Cola temporal de objetos que comparten los slots de anillo; se resuelve en los índices 13/14. |
| `MethosRecoveredName_2504` | bool | Bandera booleana usada en lógica de recorrido/selección. Hay variables locales que la sombrean; significado global exacto pendiente. |
| `MethosRecoveredName_2454` | int | Entero usado en rutinas de lista/eliminación y Henna; no se asigna un significado único sin rastrear todas las rutas. |
| `MethosRecoveredName_2403` | bool | Estado del modo de anclaje de búsqueda: cambia entre posiciones superior/derecha e inferior/derecha. |
| `MethosRecoveredName_2404` | TextureHandle | `InventoryWnd.BlackSquare`, fondo/placa que acompaña a la barra de búsqueda. |
| `MethosRecoveredName_2405` | ButtonHandle | `InventoryWnd.EnableButton`, botón superpuesto a la zona de búsqueda. |
| `MethosRecoveredName_2406` | ButtonHandle | `InventoryWnd.BtnInfo`, tooltip de ayuda del borrado rápido. |

### Orden exacto de los 15 slots de equipo

| Índice | Control XDAT |
|---:|---|
| 0 | `EquipItem_Underwear` |
| 1 | `EquipItem_Head` |
| 2 | `EquipItem_Hair` |
| 3 | `EquipItem_Hair2` |
| 4 | `EquipItem_Neck` |
| 5 | `EquipItem_RHand` |
| 6 | `EquipItem_Chest` |
| 7 | `EquipItem_LHand` |
| 8 | `EquipItem_REar` |
| 9 | `EquipItem_LEar` |
| 10 | `EquipItem_Gloves` |
| 11 | `EquipItem_Legs` |
| 12 | `EquipItem_Feet` |
| 13 | `EquipItem_RFinger` |
| 14 | `EquipItem_LFinger` |

El código aplica la textura `L2UI.InventoryWnd.Icon_dualcap` a determinados slots y gestiona por separado los objetos de oreja y anillo, porque dos piezas pueden compartir una misma posición lógica según `SlotBitType`.

## Controles XML presentes

El XML contiene estos controles principales:

- Equipamiento: los 15 slots anteriores.
- Inventario: `InventoryItem`, `QuestItem`, `EquipmentItem`, `CraftingItem`, `SuppliesItem`, `SearchItem`, `HennaItem`.
- Búsqueda: `SearchBar`, `CancelBar`, `BlackSquare`, `EnableButton`, `BtnInfo`.
- Acciones: `SortButton`, `TrashButton`, `CrystallizeButton`, `BtnClose`, `BtnMin`, `BtnExpand`.
- Datos/estado: `AdenaText`, `ItemCount`, `InvenWeight`, `AdenaIcon`, `WeightIcon`, `WeightBG`.
- Estructura visual: `BackTexture`, `Container`, `Tex`, `Slots`, `Slots2`, `Frame`, `AdenaBox`, `Title`, `Equip_Classic`, `ObjectViewport`, `EquipSlotBg`.

### Parámetros de ItemWindow

Los paneles de inventario principales exportan configuración de 9 columnas × 8 filas, máximo 250 ítems, iconos de 32 × 32 y separación de 4 × 4. El código cambia dinámicamente `SearchItem` entre 9 y 12 columnas y entre 339 y 447 píxeles de ancho, manteniendo 288 píxeles de alto.

## Eventos y puntos de entrada

Los eventos registrados son: `150`, `180`, `260`, `1710`, `2570`, `2580`, `2590`, `2600`, `2610`, `2620`, `2630`, `2631`.

Puntos de entrada reconocibles: `OnLoad`, `OnEvent`, `OnShow`, `OnHide`, `OnDBClickItemWithHandle`, `OnRClickItemWithHandle`, `OnClickButton`, `OnSelectItemWithHandle`, `OnDropItem`, `OnDropItemSource` y `OnChangeEditBox`. El resto de funciones se mantiene en el fuente exportado; sus identificadores numéricos no se renombran por intuición.

## Pendientes concretos de esta auditoría

1. Resolver los tres handles `EnchantButton`, `FastDelete` y `RefineButton` que no aparecen en el árbol XML de esta exportación.
2. Rastrear los usos globales frente a locales de `MethosRecoveredName_2504`, `_2454` y `_2403` para cerrar su significado sin confundir variables sombreadas.
3. Relacionar los identificadores numéricos del mapa `methos_name_map.json` con fuentes de referencia compatibles, si existen, sin reemplazar nombres basándose solo en similitud.
4. Continuar el inventario estructural del resto de clases y ventanas con el mismo criterio de evidencia.

## Artefactos de origen

El workflow [Audit L2Sudamerica Interface.u and interface.xdat](https://github.com/Methos48/Methos/actions/runs/43579091099) produjo `InventoryWnd.uc`, `InventoryWnd.xml`, el mapa de nombres y el resto de las clases/XML. Los binarios originales permanecen inmutables.


### Seguimiento de los tres handles no encontrados en XML

Se revisaron todas las fuentes `.uc` y todos los XML exportados, no solo el archivo de InventoryWnd:

- `EnchantButton`: aparece en `GetHandle` y recibe un tooltip, pero no tiene nodo XML coincidente ni rama `OnClickButton` propia en esta clase.
- `FastDelete`: aparece en `GetHandle` y recibe tooltip; tampoco hay nodo XML ni rama `OnClickButton`. El código además posiciona un nombre distinto, `InventoryWnd.b_FastDelete`, mediante `SetAnchor` en la rutina de expansión. Ninguno de esos dos nombres existe en el XML extraído.
- `RefineButton`: aparece en `GetHandle`, recibe tooltip y sí tiene rama `OnClickButton` que alterna la ventana `AugmentationWnd`; aun así, el control no está declarado en el XML de InventoryWnd.

Esto apunta a una discrepancia entre la capa UnrealScript y el árbol XDAT exportado, o a controles creados por otra capa/versión. No se renombraron ni se inventaron controles para resolverla.


## Continuación paralela — 2026-11-14

Esta auditoría corresponde solo a `InventoryWnd` de L2Sudamerica. La auditoría `docs/INVENTORYWND-RECONSTRUCTION-AUDIT.md` es del paquete AnimalGame y permanece separada.

### Estado consolidado
- [x] 27 variables de clase relacionadas con tipos y controles cuando existe evidencia.
- [x] Orden de los 15 slots de equipo recuperado.
- [x] Controles XDAT, parámetros principales de ItemWindow, eventos y puntos de entrada registrados.
- [x] Discrepancia de EnchantButton, FastDelete y RefineButton documentada sin inventar nodos XML.
- [ ] Resolver FName numéricos y variables globales pendientes con evidencia de la misma build.
- [ ] Inspeccionar índices de export/bytecode para detectar referencias externas a funciones anónimas.
- [ ] Comparar geometría, anclajes y estados de controles con otras ventanas del mismo XDAT.
- [ ] Probar cambios solo en una copia, con reconstrucción exacta; no editar el original.

No se restauran nombres ni se modifican funciones por semejanza con AnimalGame u otras interfaces. No se ha cambiado el comportamiento del cliente.


## Auditoría profunda — InventoryWnd (14-11-2026)

### Geometría y composición XDAT

- Ventana principal: 566 × 400, anclada `CENTER_RIGHT` a `CENTER_RIGHT`, desplazamiento (-8, -70).
- Zona de equipamiento `Equip_Classic`: 206 × 393, con fondo `Abaddon.Inventory_Equip_BG` y máscara de slots `Abaddon.Inventory_Equip_Slot_NoBr`.
- Zona de inventario: ItemWindow de 339 × 288, origen (217,55); QuestItem, EquipmentItem, CraftingItem y SuppliesItem comparten la misma zona y se alternan por pestañas/estado.
- Búsqueda: SearchBar 96 × 18, CancelBar 16 × 16, SearchItem 339 × 288, BlackSquare y EnableButton. Se anclan dinámicamente al viewport superior o inferior derecho según el estado de expansión.
- Equipo: 15 slots independientes, HennaItem, peso, Adena y contador de objetos.
- Botones/acciones visibles: SortButton, BtnClose, BtnMin, BtnExpand, CrystallizeButton, TrashButton, RefineButton, EnchantButton, FastDelete y BtnInfo.

### Correspondencia de slots de equipo

Orden interno del array de 15 handles:
0 Underwear; 1 Head; 2 Hair; 3 Hair2; 4 Neck; 5 RHand; 6 Chest; 7 LHand; 8 REar; 9 LEar; 10 Gloves; 11 Legs; 12 Feet; 13 RFinger; 14 LFinger.

Las posiciones XML son independientes del orden del array; no reordenar el array al rediseñar la presentación.

### Ciclo de vida y eventos

`OnLoad` registra 2570, 2580, 2590, 2600, 2610, 2620, 2630, 2631, 260, 180, 1710 y 150. Inicializa handles, tooltips y estado de búsqueda. `OnShow` comprueba si el personaje tiene habilidad de cristalizar, actualiza contenido, peso y contador, muestra SortButton y configura anclajes de búsqueda; `OnHide` ejecuta limpieza.

La tabla de eventos enruta:
- 2570 → actualización/ordenación del inventario.
- 2580 → mostrar/actualizar la ventana.
- 2590 → ocultar/cerrar o restaurar estado asociado.
- 2600 → actualizar datos de inventario.
- 2610 → actualización de objetos/estado de pestañas.
- 2620 → refresco de contenido.
- 2630 → actualización adicional parametrizada.
- 2631 → alternar/actualizar un estado de ventana.
- 1710 → refresco de contador/estado.
- 180 → sincronización del jugador/estado.
- 150 → evento de UI/visibilidad.
- 260 aparece duplicado en el switch original; conservar constancia de la duplicación hasta identificar si es artefacto de decompilación.

### Interacciones y seguridad de operaciones

- Doble clic y clic derecho convergen en una rutina común de selección/uso.
- Selección contempla modificadores de teclado; hay una ruta explícita que solicita destruir un objeto con Ctrl+Alt.
- Drag & drop diferencia inventario, equipo, crafting, supplies, quest, slots de equipamiento, inventario de mascota, TrashButton y CrystallizeButton.
- Reordenamiento local usa `GetIndexAt`, `FindItemWithServerID` y `SwapItems`; mover desde equipo puede emitir `RequestUnequipItem`.
- Mover desde inventario de mascota usa `PetAPI.RequestGetItemFromPet`, con validaciones de stack y límites.
- Tirar objetos usa rutas de confirmación y solicitudes con ServerID/cantidad; cristalizar comprueba habilidad y si el objeto es cristalizable.
- Ordenar, expandir/contraer, búsqueda y filtro de categorías son funcionalidades distintas; no asumir que la búsqueda modifica el inventario del servidor.
- Se encontraron persistencias locales/INI para la lista de descarte rápido; revisar a fondo antes de modificar el comportamiento de FastDelete/Trash.

### Límites y siguiente fase

La geometría y rutas principales están recuperadas, pero varias funciones conservan nombres numéricos saneados. Falta resolver cada una contra llamadas API y referencias de bytecode, documentar el significado de todos los IDs de evento con sus productores, mapear cada control XML a su handle/callback y validar el flujo de búsqueda/ordenación/descartar con pruebas unitarias o capturas controladas. No se ha editado ni recompilado ningún binario.


## Segunda pasada — lógica interna del inventario

### Capacidad y equipo

- El contador visible se calcula como inventario normal + objetos de misión + cantidad de objetos en los 15 slots de equipo habilitados.
- El límite se obtiene de `UIDATA_PLAYER.GetInventoryLimit()`; no es un número fijo de la interfaz.
- La rutina de recuento ignora los slots de equipo deshabilitados.
- Al refrescar el inventario, limpia las listas visuales y los arrays auxiliares de crafting/supplies antes de reconstruirlos.
- Hay tratamiento especial para accesorios duplicados: al retirar un objeto de un slot, el código puede trasladar el objeto de mano derecha a mano izquierda cuando su SlotBitType lo requiere.
- Los arrays de pendientes para orejas y anillos se resuelven contra el estado de equipamiento y rellenan los slots correspondientes.

### Orden personalizado

- Al ocultar la ventana, guarda la secuencia de ClassID mediante `SaveInventoryOrder`.
- La rutina complementaria carga el orden con `LoadInventoryOrder`.
- Al reconstruir el inventario, primero coloca los objetos que coinciden con la lista guardada y después completa el resto; la ordenación es local/visual, no una petición al servidor.
- El botón SortButton llama a la rutina de ordenación; no debe confundirse con cambiar el orden de equipamiento.

### Operaciones pendientes y confirmaciones

La interfaz guarda un contexto temporal (tipo de operación, ServerID, cantidad y ubicación de clic) antes de mostrar el mensaje de confirmación. El evento 1710 recupera ese contexto y ejecuta la solicitud correspondiente:

| Contexto interno | Acción ejecutada |
|---|---|
| 1111 / 2222 | Usar objeto |
| 3333 | Soltar 1 |
| 4444 | Soltar cantidad elegida |
| 5555 | Soltar cantidad total |
| 6666 | Destruir 1 |
| 8888 | Destruir cantidad elegida |
| 7777 | Destruir cantidad total |
| 9999 | Cristalizar 1 |
| 10000 | Recuperar cantidad elegida desde inventario de mascota |

El flujo distingue soltar del inventario, destruir con TrashButton y cristalizar. No sustituir ni saltar los cuadros de confirmación al rediseñar.

### Restricciones al abrir

Antes de solicitar `RequestItemList`, comprueba ventanas incompatibles como WarehouseWnd, TradeWnd, ShopWnd, MultiSellWnd y DeliverWnd. La apertura del inventario puede ser rechazada mientras esas ventanas están activas. La rutina también consulta PrivateShopWnd; hay que verificar el resto de su condición completa antes de modificarla.

### Próxima investigación

Resolver nombres numéricos de las funciones de clasificación, búsqueda y filtros; rastrear el productor de cada evento 2570–2631; completar la condición de compatibilidad de ventanas; y mapear controles de Enchant/FastDelete/Refine con su callback real. La lógica de operaciones destructivas queda marcada como zona de riesgo: cualquier cambio futuro debe preservar ServerID, cantidades, validaciones y confirmaciones.
