# Auditoría técnica — Siege, Manor y Olympiad (L2Sudamerica)

**Evidencia:** fuentes UnrealScript descifradas y XML XDAT del artefacto del workflow 43579091099. Análisis estático; no se modificaron binarios ni se probó en cliente.

## 1. Alcance y límites

Se estudiaron SiegeInfoWnd; ManorInfoWnd, ManorShopWnd, ManorCropInfoChangeWnd, ManorCropInfoSettingWnd, ManorCropSellChangeWnd, ManorCropSellWnd, ManorSeedInfoChangeWnd y ManorSeedInfoSettingWnd; OlympiadBuffWnd/1/2, OlympiadControlWnd, OlympiadPlayerWnd/1/2, OlympiadTargetWnd y ObserverWnd.

Estas clases presentan información y preparan solicitudes. No implementan ni certifican las reglas del servidor. Una solicitud emitida no demuestra que el servidor la haya aceptado.

## 2. SiegeInfoWnd

### Estructura XDAT
- Ventana raíz 256 × 401, anclada CENTER_LEFT.
- Encabezado: txtCastleName, txtOwnerName, txtClanName, txtAllianceName, texClan y texAlliance.
- Panel de fecha SiegeInfoWnd_Date: cboTime, txtCurTime y txtSiegeTime.
- Panel atacante SiegeInfoWnd_Party1: lstClan 242 × 200, txtCount, btnAttackApply y btnAttackCancel.
- Panel defensor SiegeInfoWnd_Party2: lstClan 242 × 200, txtCount, btnDefenseApply, btnDefenseCancel, btnDefenseReject y btnDefenseConfirm.
- TabCtrl 224 × 23; fondos con recursos L2UI_ch3.SiegeWnd.

### Eventos y flujo
OnLoad registra 1710 y 1450–1490.
- 1450: procesa información general del asedio/castillo.
- 1460: actualización de lista atacante.
- 1470: actualización de lista defensora.
- 1480 y 1490: rutas de actualización/confirmación de listas de espera.
- 1710: despacha una acción según estado interno seleccionado.

Las etiquetas de arriba reflejan la función consumidora y el contexto; el payload exacto debe verificarse contra el productor.

### Solicitudes
La rama 1710 llama a SiegeAPI según estado:
| Estado | API | Argumentos observados |
|---:|---|---|
| 1 | RequestJoinCastleSiege | (castleID, 1, 1) |
| 2 | RequestJoinCastleSiege | (castleID, 1, 0) |
| 3 | RequestJoinCastleSiege | (castleID, 0, 1) |
| 4 | RequestJoinCastleSiege | (castleID, 0, 0) |
| 5 | RequestConfirmCastleSiegeWaitingList | (castleID, pendingID, 0) |
| 6 | RequestConfirmCastleSiegeWaitingList | (castleID, pendingID, 1) |
| 7 | RequestSetCastleSiegeTime | (castleID, selectedSiegeTime) |

La selección de cboTime toma el valor correspondiente del array de horarios, muestra un mensaje de confirmación y activa estado 7. Las funciones de carga invocan RequestCastleSiegeAttackerList(castleID) y RequestCastleSiegeDefenderList(castleID).

**No confirmado:** significado preciso de los dos flags de RequestJoinCastleSiege. Se documentan las cuatro combinaciones transmitidas, pero no se les asigna semántica sin contrato API/productor adicional.

### Secuencia
1. OnLoad registra eventos, enlaza controles e inicializa estado.
2. Se cargan datos de castillo y listas.
3. Los eventos actualizan encabezado, listas y contadores.
4. El usuario elige pestaña/acción u horario.
5. La UI establece estado interno y dispara 1710.
6. La rama 1710 valida IDs/estado y llama a SiegeAPI.
7. La confirmación real depende del servidor y de sus eventos de respuesta.

Pendiente: productores 1450–1490, formato LVDataRecord, campos de crest/alianza, nombres originales FName, permisos y significado de flags.

## 3. Familia Manor

### ManorInfoWnd
- XML 600 × 326; texManorInfoTop 600 × 28; ManorInfoTabCtrl 424 × 23.
- Registra 2650–2655.
- btnBuySeed envía bypass manor_menu_select?ask=1&state=-1&time=0.
- btnSellCrop envía ask=2.
- Pestaña 0 abre SeedInfo; pestaña 1 abre CropInfo; pestaña 2 envía ask=5.
- SeedInfo consulta ask=3; CropInfo consulta ask=4.
- Los combos aportan ManorID (GetReserved) y periodo (índice seleccionado). Se construye manor_menu_select?ask=<3|4>&state=<ManorID>&time=<periodo>.
- Para CropInfo hay fallback al ManorID almacenado cuando el combo devuelve -1; se valida ID positivo antes del bypass.

Los números ask se describen como rutas concretas observadas, no como semántica protocolaria universal.

### ManorShopWnd
- Hereda de ShopWnd; registra 2680, 2690 y 1710.
- 2680 procesa lista de semillas; 2690 procesa lista de compra/procurement de cosechas; 1710 despacha la acción seleccionada.
- Compra: obtiene item de TopList, valida stackability/cantidad y llama RequestBuySeed(payload).
- Procura de cosechas: construye campos indexados classID[n] y count[n] para filas seleccionadas y llama RequestProcureCrop(payload).
- Reutiliza validaciones y mensajes de la infraestructura ShopWnd. No sustituir el flujo por compra directa sin conservar cantidades y checks.

### Configuración de cosechas
ManorCropInfoSettingWnd:
- XML 600 × 326; ManorCropInfoSettingListCtrl 586 × 199; gasto total y botones btnChangeSell, btnSetToday, btnStop, btnOk y btnCancel.
- Eventos 2665, 2666, 2667, 2668 y 1710.
- 2665 recibe ManorID/ManorName; 2666 procesa filas; 2667 limpia/cierra; 2668 procesa actualización.
- Estado interno 777 pone a cero campos editables de filas; 888 copia valores base a campos editables.
- btnStop activa 777; btnSetToday activa 888; btnOk compone payload y cierra; btnCancel cierra.
- RequestSetCrop(payload) transmite la configuración.
- ManorCropInfoChangeWnd: 256 × 201; nombre de cosecha, cantidad de compra, límite/precio y recompensa, OK/Cancel.

ManorSeedInfoSettingWnd es análoga: XML 600 × 326, lista 586 × 199, gasto total y botones de edición/estado/confirmación. Registra 2656–2659 y 1710. ManorSeedInfoChangeWnd mide 256 × 201 y presenta volumen de venta, límite y precio.

Los índices LVDataRecord y constantes 0–12 forman parte de las columnas/estados; no se renombrarán por intuición sin contrastar el productor y el payload.

### Venta de cosechas
ManorCropSellWnd:
- XML 600 × 326; ManorCropSellListCtrl 586 × 199.
- Eventos 2640, 2645 y 2646. 2640 alterna/carga la ventana; 2645 recibe filas; 2646 procesa actualización adicional.
- btnChangeSell toma la fila seleccionada, envía manor_menu_select?ask=9&state=<CropID>&time=0 y emite evento local 2649 con CropName, RewardType1 y RewardType2.
- btnSell recorre filas con cantidad positiva y construye CropCnt, CropServerID[n], CropID[n], ManorID[n] y SellCount[n]; llama RequestProcureCropList(payload) y cierra.
- Los IDs se leen de campos reservados del registro. No reemplazar CropServerID por ClassID.

ManorCropSellChangeWnd:
- XML 336 × 306; datos de recompensa, lugar de compra, volumen y botón Max.
- El evento 2649 enlaza selección de cultivo y editor; falta completar correlación de payload/consumidor.

### Pendientes Manor
Encontrar productores ExecuteEvent de 2640–2690 y 2650–2668; definir todos los campos de filas; identificar contrato de ask=1..9 y state/time; comprobar límites de presupuesto/cantidad; probar únicamente en entorno controlado. El servidor es autoridad sobre transacciones y límites.

## 4. Olympiad

### OlympiadPlayerWnd
Se instancia por índice 1/2 y deriva nombres OlympiadPlayer1Wnd/2Wnd y OlympiadBuff1Wnd/2Wnd. Registra:
- 920: datos de participante.
- 910: limpieza.
- 290: notificación de skill/acción.
- 280: notificación de combate/daño.

El evento 920 lee IsPlayer, PlayerNum, ID, Name, ClassID, MaxHP, CurHP, MaxCP y CurCP. Descarta registros no correspondientes al participante esperado. Si cambia el ID, emite evento local 930 con PlayerNum y PlayerID para actualizar buffs.

El evento 290 lee AttackerID y SkillID, filtra IDs inválidos y rango de skill, y agrega texto al log. El evento 280 lee AttackerID, AttackerName, DefenderID, Critical, Miss y ShieldDefense; produce mensajes según si el atacante/defensor coincide con el participante representado.

Mantiene cola circular de cinco mensajes. HP/CP se dibujan redimensionando texturas con proporción Cur/Max y ancho de referencia 326, alto 6; máximo cero produce ancho cero. No calcula daño ni modifica estado de combate.

### OlympiadTargetWnd
- XML 172 × 46; txtName, texCPBack/texHPBack y texCP/texHP.
- Eventos 900, 910 y 920.
- Presenta nombre y barras del objetivo; no se encontró una solicitud de combate propia.

### Buffs
OlympiadBuffWnd registra 910, 930 y 940; mantiene 12 slots (constantes 12) mediante UIAPI_STATUSICONCTRL. Los eventos limpian/actualizan iconos. OlympiadBuff1Wnd y OlympiadBuff2Wnd son clases mínimas; la lógica compartida reside en OlympiadBuffWnd.
Pendiente: esquema de payload de 930/940 y productores.

### OlympiadControlWnd
- XML 102 × 56; btnStop y btnOtherGame.
- btnStop → OlympiadAPI.RequestOlympiadObserverEnd().
- btnOtherGame → OlympiadAPI.RequestOlympiadMatchList().
La ventana ofrece control de observador/lista; no demuestra gestión de inscripción o resultados.

## 5. ObserverWnd
- XML 102 × 32; TexBack y BtnEnd 76 × 23.
- Registra 2450, 2460 y 150.
- BtnEnd llama RequestObserverModeEnd().
- Eventos 2450/2460 actualizan estado; 150 coordina visibilidad con estado de interfaz.
No confundir esta API general con OlympiadAPI.RequestOlympiadObserverEnd().

## 6. Tabla de geometría XDAT

| Ventana | Tamaño raíz | Controles |
|---|---:|---|
| SiegeInfoWnd | 256 × 401 | TabCtrl, listas atacante/defensor, selector hora y botones |
| ManorInfoWnd | 600 × 326 | TabCtrl, vistas SeedInfo/CropInfo |
| ManorCropInfoSettingWnd | 600 × 326 | lista, gasto y botones de estado |
| ManorSeedInfoSettingWnd | 600 × 326 | lista, gasto y botones de estado |
| ManorCropInfoChangeWnd | 256 × 201 | cantidades, límite, precio y recompensa |
| ManorSeedInfoChangeWnd | 256 × 201 | volumen, límite y precio |
| ManorCropSellWnd | 600 × 326 | lista de cosechas y botones |
| ManorCropSellChangeWnd | 336 × 306 | recompensa, lugar y volumen |
| OlympiadControlWnd | 102 × 56 | Stop, OtherGame |
| OlympiadTargetWnd | 172 × 46 | nombre y CP/HP |
| ObserverWnd | 102 × 32 | End |

ManorShopWnd hereda ShopWnd; no se declara aquí una geometría independiente que sustituya la estructura heredada.

## 7. Dependencias y límites de modificación

- Siege: NWindow.SiegeAPI, controles de listas/combos/textos y datos de clan/crest.
- Manor: RequestBypassToServer, RequestBuySeed, RequestProcureCrop, RequestProcureCropList, RequestSetCrop y UIAPI_LISTCTRL/COMBOBOX.
- Olympiad: NWindow.OlympiadAPI, UIAPI_TEXTBOX/WINDOW/STATUSICONCTRL y eventos de combate/estado.
- Observer: RequestObserverModeEnd y eventos de estado global.

No modificar argumentos de solicitudes, IDs de objeto/castillo/Manor, cantidades, confirmaciones, filtros de participante ni mensajes basados en eventos. Los cambios visuales deben preservar nombres de controles y contratos de eventos.

## 8. Próximos pasos y estado

1. Seguir productores de SiegeAPI/eventos 1450–1490; cerrar flags y payloads.
2. Construir matriz de productores/consumidores de eventos Manor 2640–2690 y 2650–2668.
3. Definir cada columna LVDataRecord en semillas/cosechas/ventas.
4. Seguir eventos Olympiad 900/910/920/930/940 y 280/290 hasta su productor.
5. Extraer posiciones/anclajes y recursos completos de todos los controles.
6. Resolver las seis clases exportadas ausentes del CSV de funciones.

**Estado:** inventariado y extraído; mapeo y análisis estático de flujos principales completados; productores, payloads restantes y FNames pendientes. No se ha compilado UCC un paquete modificado, no se ha reprotegido una modificación ni se ha probado carga real en cliente.

## 10. Addendum — correlación adicional de eventos internos (14-11-2026)

Una búsqueda global de los archivos UC del paquete encontró productores locales que permiten cerrar parte del flujo Manor y Olympiad. Esto mejora la trazabilidad respecto a la primera lectura de cada consumidor.

### 10.1 Eventos Manor confirmados entre ventanas

| Productor | Evento emitido | Consumidor registrado | Campos observados |
|---|---:|---|---|
| ManorCropSellWnd | 2649 | ManorCropSellChangeWnd | CropName, RewardType1, RewardType2 |
| ManorCropSellChangeWnd | 2646 | ManorCropSellWnd | SellCntString, ManorID, ManorName, CropRemainCntString, CropPriceString, ProcureTypeString |
| ManorCropInfoSettingWnd | 2670 | ManorCropInfoChangeWnd | CropName, TomorrowVolumeOfBuy, TomorrowLimit, TomorrowPrice, TomorrowProcure, MinCropPrice, MaxCropPrice |
| ManorCropInfoChangeWnd | 2668 | ManorCropInfoSettingWnd | TomorrowAmountOfPurchase, TomorrowPurchasePrice, TomorrowProcure |
| ManorSeedInfoSettingWnd | 2660 | ManorSeedInfoChangeWnd | SeedName, TomorrowSalesVolume, TomorrowLimit, TomorrowPrice, MinCropPrice, MaxCropPrice |
| ManorSeedInfoChangeWnd | 2659 | ManorSeedInfoSettingWnd | TomorrowSalesVolume, TomorrowPrice |

Los productores y consumidores se identificaron por llamadas explícitas ExecuteEvent y registros RegisterEvent en fuentes UC. Esta tabla representa eventos internos de interfaz; no son necesariamente paquetes de red ni respuestas del servidor.

**Consecuencia:** la edición de cosecha/semilla es un ciclo local de ida y vuelta: la lista seleccionada arma el contexto para el editor; el editor valida/recoge campos; emite el evento local; la ventana de configuración aplica los valores a la fila correspondiente. Los cambios definitivos se envían por las solicitudes de configuración de Manor, no por esos eventos locales aislados.

### 10.2 Detalle del payload de venta editada

ManorCropSellChangeWnd recoge:
- SellCntString: cantidad de venta escrita.
- ManorID: ID del Manor asociado a la fila/lugar seleccionado.
- ManorName: nombre de la ciudad/Manor.
- CropRemainCntString: cantidad restante disponible.
- CropPriceString: precio por unidad.
- ProcureTypeString: tipo de procurement.

Ese paquete se emite como evento local 2646. ManorCropSellWnd consume 2646 y actualiza la fila/lista; por tanto, el editor no ejecuta directamente la venta final en esta ruta.

### 10.3 Detalle de edición de configuración

- La lista de cosechas emite 2670 con identidad de cultivo y parámetros del siguiente periodo; ManorCropInfoChangeWnd usa esos valores para abrir/precargar la edición.
- El editor de cosechas emite 2668 al aceptar; la lista consume el evento y actualiza la fila.
- La lista de semillas emite 2660 con SeedName, TomorrowSalesVolume, TomorrowLimit, TomorrowPrice, MinCropPrice y MaxCropPrice.
- El editor de semillas emite 2659 con TomorrowSalesVolume y TomorrowPrice; la lista consume el resultado.

Los validadores de los editores comparan cantidades y precios con rangos extraídos de la fila y muestran mensajes de sistema antes de emitir el evento. Es una validación de UI; la autoridad final sigue siendo el servidor.

### 10.4 Payload OlympiadBuffWnd

El consumidor de evento 940 parsea:
- PlayerID
- Max
- SkillID_[n]
- SkillLevel_[n]
- RemainTime_[n]
- Name_[n]
- IconName_[n]
- Description_[n]

El índice recorre Max elementos; la UI agrega filas cada 12 entradas y asigna iconos de tamaño 24, fondo L2UI.EtcWndBack.AbnormalBack. El panel se muestra si Max > 0 y ajusta su ancho al número de columnas de 12. El evento 930 solo actualiza el PlayerID del participante mostrado; el 910 limpia los iconos y reinicia el estado.

### 10.5 Límites aún abiertos

- No se identificó todavía el origen externo de los eventos de datos del servidor 2645, 2647, 2650–2659, 2665–2667, 2680/2690.
- El origen del evento Olympiad 940 debe seguirse fuera de estas clases; aquí se conoce el formato del consumidor, no el productor final.
- Las rutas de venta/configuración Manor requieren prueba en un entorno controlado para separar validación local de aceptación del servidor.
