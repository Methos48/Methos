# L2Sudamerica — auditoría profunda de comercio, almacén, tienda y envíos

**Estado:** análisis estático cruzado U + XDAT. No se han modificado los binarios originales.  
**Fuentes:** artifact `L2Sudamerica-Interface-Audit`, extracción `sources-include-tail` y XML `include-tail-xml/windows`.

## 1. Alcance y método

Se examinaron siete pares de archivos:

| Clase U | Fuente U | XML XDAT | Funciones |
|---|---:|---:|---:|
| TradeWnd | 12.334 B / 439 líneas | 12.467 B | 18 |
| WarehouseWnd | 19.945 B / 590 líneas | 18.016 B | 17 |
| ShopWnd | 26.736 B / 660 líneas | 15.895 B | 13 |
| PrivateShopWnd | 59.331 B / 1.505 líneas | 18.931 B | 21 |
| DeliverWnd | 13.230 B / 403 líneas | 15.939 B | 16 |
| MultiSellWnd | 16.628 B / 440 líneas | 12.079 B | 15 |
| SelectDeliverWnd | 1.893 B / 77 líneas | 6.442 B | 5 |

Las cifras de funciones corresponden a declaraciones detectadas en el fuente descompilado, no a una garantía de que se hayan recuperado nombres originales. Muchos identificadores siguen saneados como `MethosRecoveredName_...`; no se les asigna una semántica por intuición.

## 2. Geometría y controles XDAT

Las dimensiones siguientes son los valores absolutos que figuran en los XML exportados:

| Ventana | Tamaño | Anclaje / desplazamiento | Controles relevantes |
|---|---:|---|---|
| TradeWnd | 256×471 | CENTER / (0,0) | InventoryList 239×139; MyList y OtherList 239×108; TargetName; OKButton; CancelButton; InvenWeight |
| WarehouseWnd | 256×401 | CENTER_LEFT / (0,0) | TopList 239×139; BottomList 239×104; botones Up/Down/OK/Cancel; textos de cantidades y precios; peso |
| ShopWnd | 256×401 | CENTER_LEFT / (0,0) | TopList 239×139; BottomList 239×104; Up/Down/OK/Cancel; precio, adena y peso |
| PrivateShopWnd | 256×401 | CENTER_LEFT / (0,0) | TopList y BottomList; StopButton, MessageButton, OKButton; CheckBulk; precio, adena y peso |
| DeliverWnd | 256×401 | CENTER_LEFT / (0,-50) | TopList 239×139; BottomList 239×104; Up/Down/OK/Cancel; precio, adena y peso |
| MultiSellWnd | 512×401 | CENTER_LEFT / (0,-70) | ItemList 240×314; ItemInfo y NeededItem 244×150; ItemCountEdit; OK/Cancel |
| SelectDeliverWnd | 256×201 | No se ha verificado el anclaje en esta pasada | SelectDeliverCombo; Text1; OK/Cancel |

Los nombres de controles proceden de los elementos `<name>` del XML, no de inferencias visuales.

## 3. TradeWnd — intercambio entre jugadores

### Evidencia U
- Registra los eventos 1710, 1720, 1950, 1960, 1970, 1980, 1990 y 2000.
- Implementa `OnSendPacketWhenHiding()`, que llama `RequestTradeDone(false)`. Por tanto, el cierre/ocultación dispone de una ruta explícita de finalización/cancelación del intercambio.
- El flujo de añadir objetos utiliza `RequestAddTradeItem(ServerID, cantidad)`.
- En el arrastre desde `InventoryList` a `MyList`, los objetos apilables pueden enviarse con su cantidad total; los no apilables se envían con cantidad 1.
- El clic/doble clic sobre inventario consulta el objeto por índice y obtiene `ItemInfo`; se comprueba el tipo apilable antes de elegir la cantidad.
- XML distingue `InventoryList`, `MyList` y `OtherList`, además del nombre del otro participante (`TargetName`).

### Interpretación y límites
La UI separa claramente inventario propio, oferta propia y oferta remota. El envío de una solicitud no demuestra que el servidor la acepte; el resultado depende de eventos de red posteriores. Los ocho IDs registrados deben correlacionarse con productores de eventos para identificar con seguridad cada actualización y rechazo.

## 4. WarehouseWnd — depósito y retirada

### Evidencia U
- Registra 1710, 2070, 2100 y 2110.
- Mantiene dos ItemWindow, expuestos en XML como `TopList` y `BottomList`.
- El envío construye un payload con `num`, y para cada fila añade `dbID[i]` y `count[i]`.
- La operación de depósito termina en `RequestWarehouseDeposit(payload)`; la retirada termina en `RequestWarehouseWithdraw(payload)`.
- El código tiene ramas diferentes según el modo/estado interno saneado (`MethosRecoveredName_2065`). No se asigna significado a sus valores sin rastrear dónde se inicializa y modifica.
- La ventana lee un parámetro `warehousePrivate`; queda pendiente rastrear el origen y los valores posibles.

### Implicación
El identificador usado en el payload se obtiene de `ItemInfo.Reserved`, no se construye a partir del índice de lista. El índice solo forma parte de la clave del parámetro. Esta distinción importa para evitar confundir posición visual con identidad de objeto.

## 5. ShopWnd — compra, venta y vista previa

### Evidencia U
- Registra 1710, 2080 y 2090.
- La inicialización recibe parámetros `type`, `merchant` y `adena`.
- La compra construye un payload con cantidad de entradas y pares `classID[i]` / `count[i]`, y llama `RequestBuyItem(payload)`.
- La venta construye `merchant`, `num` y, por fila, `serverID[i]`, `classID[i]` y `count[i]`; termina en `RequestSellItem(payload)`.
- La vista previa de objetos llama `RequestPreviewItem(payload)`, con datos del comerciante y los ClassID de los elementos seleccionados.
- Hay una rama de confirmación/mensaje cuando la lista de venta está vacía; el texto de sistema se obtiene mediante `GetSystemMessage(1157)`.

### Distinción importante
Comprar y vender no usan exactamente la misma identidad de objeto: la compra usa ClassID y cantidad; la venta incorpora también ServerID. La vista previa es otra solicitud independiente, no equivale a una compra.

## 6. PrivateShopWnd — tienda privada

### Evidencia U
- Es el mayor fuente de este grupo: 59.331 bytes y 1.505 líneas.
- Registra 1710, 2070, 2120 y 2130.
- El XML presenta dos listas, botón de detener, botón de mensaje, botón OK y casilla `CheckBulk`.
- `OnSendPacketWhenHiding()` existe y se ha de estudiar junto con `OnHide()` para establecer el cierre exacto.
- El código diferencia estados internos 3 y 4 en la función que termina la tienda: llama `RequestQuitPrivateShop("buy")` o `RequestQuitPrivateShop("sell")`.
- Se detecta lógica para procesar elementos en `TopList` y `BottomList`, además de actualizar precio y Adena.
- Hay una variable booleana de estado para la opción de compra/venta por lote, coherente con el control XML `CheckBulk`; su efecto exacto requiere completar el seguimiento de todas sus asignaciones y usos.

### Pendiente específico
No se debe afirmar aún que todos los flujos de publicación, modificación, cancelación y mensaje privado estén reconstruidos. La función de extracción de `ItemInfo` y el conjunto de eventos 2120/2130 requieren trazado completo y correlación con el productor.

## 7. DeliverWnd y SelectDeliverWnd — envío de paquetes

### Evidencia U: DeliverWnd
- Registra 1710, 2160 y 2170.
- El payload incluye `num`; para cada elemento seleccionado agrega `dbID[i]` a partir de `ItemInfo.Reserved` y `count[i]` desde `ItemInfo.ItemNum`.
- Ejecuta `RequestPackageSend(payload)` y oculta DeliverWnd.
- XML define listas superior/inferior, controles de cantidad, precio/Adena y peso.

### Evidencia U: SelectDeliverWnd
- Registra eventos 2140 y 2150.
- Al confirmar, obtiene el índice seleccionado del ComboBox y lee su valor reservado.
- Si hay una selección válida, llama `RequestPackageSendableItemList(valorReservado)`.
- Después oculta la ventana.

### Flujo demostrado parcialmente
`SelectDeliverWnd` selecciona un destino o contexto de envío y solicita la lista de objetos que se pueden enviar; `DeliverWnd` prepara y solicita el envío de los objetos seleccionados. La relación exacta entre el valor reservado del ComboBox y el receptor necesita analizar el productor del evento 2140 y el código que llena el ComboBox.

## 8. MultiSellWnd — intercambio múltiple

### Evidencia U
- Registra 1710, 2530, 2540, 2550 y 2560.
- Conserva estructuras para ID, nombre, icono, tipo de cristal/objeto, opciones de refinamiento y arrays de materiales/recetas.
- XML contiene una lista de recetas, panel de información del resultado, panel de materiales necesarios, cantidad editable y botones OK/Cancel.
- Al confirmar, prepara `ShopID`, `ItemID`, `RefineryOp1`, `RefineryOp2` e `ItemCount`, y llama `RequestMultiSellChoose(payload)`.
- El ItemID se obtiene del primer elemento de la lista de resultado y sus datos de refinamiento se transportan por separado.

### Implicación
La selección de una receta no es una operación de inventario local: la UI envía al backend la receta/resultado elegido, opciones de refinamiento y cantidad. La validación y el resultado final corresponden al servidor.

## 9. Mapa de llamadas confirmado

| Ventana | Solicitud UI/API observada | Datos principales observados |
|---|---|---|
| TradeWnd | RequestAddTradeItem | ServerID, cantidad |
| TradeWnd | RequestTradeDone(false) | cierre/cancelación desde ocultación |
| WarehouseWnd | RequestWarehouseDeposit | dbID, count, num |
| WarehouseWnd | RequestWarehouseWithdraw | dbID, count, num |
| ShopWnd | RequestBuyItem | classID, count, num |
| ShopWnd | RequestSellItem | merchant, serverID, classID, count, num |
| ShopWnd | RequestPreviewItem | merchant y ClassID seleccionados |
| PrivateShopWnd | RequestQuitPrivateShop | modo buy/sell |
| DeliverWnd | RequestPackageSend | dbID, count, num |
| SelectDeliverWnd | RequestPackageSendableItemList | valor reservado de ComboBox |
| MultiSellWnd | RequestMultiSellChoose | ShopID, ItemID, RefineryOp1/2, ItemCount |

Este mapa documenta las llamadas encontradas en el código cliente. No implica conocer el formato binario de los paquetes ni que el servidor procese las solicitudes con éxito.

## 10. Relaciones y preguntas para la siguiente pasada

1. Localizar todos los productores de eventos 1950–2000, 2070–2170 y 2530–2560. Crear una tabla productor → payload → consumidor → cambio visual.
2. Seguir las variables saneadas de modo/estado hasta su asignación inicial y cada transición; recuperar nombres solo cuando haya evidencia cruzada.
3. Comparar los payloads de compra, venta, depósito, retirada y envío con los tipos de ItemInfo y los callbacks de inventario.
4. Auditar el cierre de TradeWnd y PrivateShopWnd: ocultación, cancelación y estado de red.
5. Seguir el llenado de SelectDeliverCombo y resolver el significado de sus valores reservados.
6. Seguir los datos de receta de MultiSellWnd hasta el origen de la lista y validar cómo se muestran materiales, resultados y refinamiento.
7. Identificar recursos/strings de UI asociados a los controles, confirmaciones y errores.
8. Repetir la comparación en cliente de prueba; no cambiar mecánicas ni binarios originales.

## 11. Evidencias del artifact

- `decoded-u/sources-include-tail/TradeWnd.uc`
- `decoded-u/sources-include-tail/WarehouseWnd.uc`
- `decoded-u/sources-include-tail/ShopWnd.uc`
- `decoded-u/sources-include-tail/PrivateShopWnd.uc`
- `decoded-u/sources-include-tail/DeliverWnd.uc`
- `decoded-u/sources-include-tail/SelectDeliverWnd.uc`
- `decoded-u/sources-include-tail/MultiSellWnd.uc`
- `decoded-xdat/interface.xdat.include-tail-xml/windows/{TradeWnd,WarehouseWnd,ShopWnd,PrivateShopWnd,DeliverWnd,SelectDeliverWnd,MultiSellWnd}.xml`

**Conclusión:** el grupo de comercio confirma una arquitectura cliente que prepara listas y payloads tipados para solicitudes explícitas de intercambio, depósito/retirada, compra/venta, tienda privada, envío y multisell. Se documentan las estructuras y llamadas observadas, pero quedan pendientes la correlación de productores de eventos, la recuperación de identificadores saneados y la validación en ejecución.
