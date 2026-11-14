# L2Sudamerica — auditoría de mapas, radar, misiones y navegación

Estado: análisis estático de fuentes UnrealScript y XML XDAT extraídos del artefacto L2Sudamerica-Interface-Audit. No se han modificado binarios ni se ha probado una edición en cliente. Los identificadores MethosRecoveredName_* siguen siendo nombres saneados no resueltos.

## 1. Alcance y archivos

| Clase U | Tamaño fuente | Líneas | Eventos registrados | XML XDAT |
|---|---:|---:|---|---:|
| MinimapWnd | 22.974 B | 756 | 1780, 520, 1790–1890, 2420 | 21.681 B |
| MinimapWnd_Expand | 20.864 B | 669 | 520, 1790–1890, 2420, 2900 | 19.073 B |
| MinimapControl | 1.286 B | 49 | — | 3.483 B |
| RadarContainerWnd | 1.479 B | 65 | 1730, 1740, 1750 | 2.466 B |
| RadarMapWnd | 12.475 B | 405 | 2900, 110, 2420, 520, 1790–1830, 1710, 3030 | 15.043 B |
| RadarWnd | 6.859 B | 231 | 110 | 25.129 B |
| QuestTreeWnd | 37.227 B | 902 | 700, 710, 720, 730, 1710, 2600, 2610 | 14.131 B |
| QuestListWnd | 6.299 B | 172 | 2840, 2850 | 5.343 B |
| QuestBtnWnd | 952 B | 45 | 1520 | 2.667 B |
| TownMapWnd | 1.257 B | 43 | 1770 | 5.973 B |
| ZoneTitleWnd | 2.189 B | 57 | 2420 | 4.526 B |
| GMQuestWnd | 2.675 B | 135 | 2320, 2330, 2340, 2350 | 14.132 B |

También se inspeccionó GuideWnd.xml (29.526 B); su análisis de código/relaciones queda pendiente.

## 2. Geometría XDAT confirmada

Los valores siguientes proceden de los campos absolutos y anclajes de cada XML:

| Ventana | Tamaño | Anclaje | Offset |
|---|---:|---|---:|
| MinimapWnd | 512×512 | TOP_LEFT → TOP_LEFT | (0,0) |
| MinimapWnd_Expand | 0×0 en campos absolutos | TOP_LEFT → TOP_LEFT | (0,0) |
| MinimapControl | 0×0 en campos absolutos | CENTER → CENTER | (-256,-256) |
| RadarMapWnd | 204×204 | TOP_RIGHT → TOP_RIGHT | (0,0) |
| RadarContainerWnd | 102×102 | TOP_RIGHT → TOP_RIGHT | (0,0) |
| RadarWnd | 16×16 | TOP_RIGHT → TOP_RIGHT | (-10,85) |
| QuestTreeWnd | 280×450 | TOP_LEFT → TOP_LEFT | (50,55) |
| QuestListWnd | 600×326 | CENTER → CENTER | (0,0) |
| QuestBtnWnd | 32×32 | BOTTOM_LEFT → TOP_LEFT | (42,-5) |
| TownMapWnd | 334×354 | CENTER_LEFT → CENTER_LEFT | (0,0) |
| ZoneTitleWnd | 500×200 | CENTER → CENTER | (0,0) |

Los 0×0 de MinimapWnd_Expand/MinimapControl son valores almacenados en esos nodos XML; el código U modifica tamaños en ejecución. No interpretarlos como ventanas necesariamente invisibles.

## 3. MinimapWnd — minimapa principal

### Inicialización y dependencias
- Hereda de UICommonAPI.
- Carga preferencia de localización English_TownMap desde Localization.ini.
- Obtiene GetPartyMemberCount() y mantiene estado para miembro seleccionado, filtros y visibilidad.
- Obtiene handles de MinimapWnd_Expand y GuideWnd y referencia directa a RadarMapWnd mediante GetScript.
- Oculta BtnClose y BtnResize de MinimapControl al cargar.
- Registra un conjunto de eventos de party, targets, quests, ubicación/zona y cursed weapons.

### Tabla de eventos consumidos
| Evento | Acción observada |
|---:|---|
| 1780 | Actualiza estado SSQ a partir del payload |
| 520 | Lee PartyMemberCount |
| 1790 | Añade target al minimapa |
| 1800 | Elimina target |
| 1810 | Elimina todos los targets |
| 1820 | Muestra indicadores de quest |
| 1830 | Oculta indicadores de quest |
| 1840 | Centra/ajusta vista a posición del jugador |
| 1850 | Actualiza lista/selección de cursed weapons |
| 1860 | Actualiza iconos/posiciones de cursed weapons |
| 2420 | Actualiza nombre de zona actual |
| 1870 | Muestra BtnReduce |
| 1880 | Oculta BtnReduce |
| 1890 | Actualiza texto de hora/estado si el flag interno lo permite |

Los números son IDs de evento de interfaz. Productores y payloads completos no están todos identificados.

### Funciones de mapa confirmadas
- Agrega y elimina targets mediante UIAPI_MINIMAPCTRL.AddTarget/DeleteTarget.
- Limpia la capa de targets con DeleteAllTarget.
- Alterna marcadores de quest con SetShowQuest(true/false).
- Centra el mapa con AdjustMapView usando GetPlayerPosition().
- Usa GetPartyMemberLocation para centrar en un miembro de party.
- Obtiene la ubicación de quest con GetQuestLocation y puede centrar el minimapa.
- Procesa cursed weapons con MiniMapAPI.RequestCursedWeaponList() y RequestCursedWeaponLocation(); dibuja iconos con DrawGridIcon y evita solapamientos mediante IsOverlapped.
- Actualiza el nombre de zona mediante GetCurrentZoneName().
- Ajusta el modo/tamaño del minimapa en runtime: se observan dimensiones 512×512 y 1024×1024 para MinimapWnd, y 512×32/1024×32 para MinimapControl según la ruta.

### Interacción y persistencia
La ventana expone botones de posición propia, posición de party, target, expansión, pursuit y guía. Los nombres exactos de todas las acciones requieren terminar de resolver funciones saneadas. Hay flags booleanos de estado y selección. No se debe asumir que las coordenadas/targets se guardan permanentemente: la persistencia de estas variables no está demostrada en esta pasada.

## 4. MinimapWnd_Expand y MinimapControl

MinimapWnd_Expand replica parte del flujo del minimapa para una vista ampliada:
- Registra eventos de party/targets/quest/posición y cursed weapons, además del evento 2900.
- Solicita lista y ubicación de cursed weapons al mostrarse.
- Obtiene la lista de resoluciones y referencias a controles de minimapa.
- Tiene rutas de selección de miembros/targets, actualización de SSQ y dibujo de iconos.
- Incluye RequestCursedWeaponLocation() explícito.

MinimapControl es una barra/control auxiliar con OnClickButton, OnShow, OnHide y OnDefaultPosition. El código principal manipula su tamaño y limpia su anclaje en el cambio entre modos de minimapa. El comportamiento detallado de cada botón requiere recuperar los nombres originales del mapa FName.

## 5. RadarMapWnd — mapa mundial y zonas

### Ciclo y eventos
- Registra 2900, 110, 2420, 520, 1790, 1800, 1810, 1820, 1830, 1710 y 3030.
- Al mostrarse activa el estado de tick, ajusta posición por defecto, oculta RadarContainerWnd y enfoca RadarMapDrag.
- Al ocultarse desactiva el estado de tick.
- En el estado activo, OnTick centra el mapa en el jugador cuando el flag interno está habilitado.

### Zonas y leyenda
El evento 110 lee ZoneCode; 2420 lee ZoneName. La función de leyenda configura textura y tooltip para estos códigos:
- 15: Ordinary Field, textura gris.
- 12: Peace Zone, azul.
- 11: Siege Warfare Zone, naranja.
- 9: Buff Zone, verde.
- 8: Danger Area, rojo.
- 13: SSQ Zone, gris.
- 14: Combat Zone, verde.

Al código 12 se le asigna círculo de 32×32; a los demás códigos reconocidos, círculo de 256×256. Los nombres en inglés son cadenas literales de esta fuente; no se ha probado si son idénticos a los nombres de zonas usados por el servidor.

### Targets y quest
- Evento 1790 parsea X/Y/Z como float y agrega target.
- Evento 1800 parsea X/Y/Z como enteros, convierte a Vector y elimina target.
- Evento 1810 elimina todos los targets.
- Eventos 1820 y 1830 muestran/ocultan la capa de quest.
- Evento 520 actualiza PartyMemberCount.
- El mapa utiliza UIAPI_MINIMAPCTRL para centrar y administrar marcadores.

### Transparencia y opciones
- BtnTrans conmuta Alpha entre 255 y 130 con transición de 0,5 s.
- Guarda el booleano RadarMap.Transparency mediante SetOptionBool/GetOptionBool.
- Al abrir aplica el estado de transparencia guardado.
- El botón BtnType consulta RadarMap.Render, pero el código visible asigna true inmediatamente antes de guardar; queda pendiente entender el propósito real y si hay una rama de render adicional.
- OnDefaultPosition ancla RadarMapDrag en TOP_RIGHT (-2,+2), coloca RadarMapWnd respecto a ese drag handle, limpia el anclaje del drag handle y lo deja AlwaysOnTop.

## 6. RadarWnd y RadarContainerWnd

RadarWnd registra evento 110 y contiene lógica de color, OnTick, consulta de estado y actualización de radar. La pasada de esta ficha no ha recuperado todavía el mapeo completo de cada variable ni la API exacta de dibujo. Su XML es 25.129 bytes aunque el nodo raíz mide 16×16: el contenido interno debe analizarse como composición de controles y no deducirse del tamaño raíz.

RadarContainerWnd registra eventos 1730, 1740 y 1750; sus funciones procesan cada uno en tres rutas diferenciadas. La semántica de esos eventos y su relación con RadarWnd queda abierta.

## 7. QuestTreeWnd — árbol principal de misiones

### Estructura y datos
- Hereda de UICommonAPI; constante 25, usada como denominador del contador de quests.
- Control principal QuestTreeWnd.MainTree; tooltip QuestToolTip.
- Arrays paralelos conservan rutas/nodos, textos, IDs y datos de quests.
- Al mostrarse enfoca controles y solicita RequestQuestList().
- Evento 700 limpia y crea nodo raíz; 710 procesa datos de quest; 720 actualiza contador; 730 procesa selección/expansión.
- Eventos 2600/2610 leen classID y actualizan cantidades de objetos de quest en los nodos.

### Construcción de árbol
La fuente usa UIDATA_QUEST para consultar:
- GetQuestName
- GetQuestType
- GetMaxLevel / GetMinLevel
- GetQuestJournalName
- GetTargetName / GetTargetLoc
- GetQuestDescription
- GetQuestItem
- IsMinimapOnly

También usa UIDATA_ITEM.GetItemTextureName/GetItemName y GetInventoryItemCount. El árbol puede mostrar nivel, descripción, target y requisitos/objetos con sus cantidades.

### Target NPC y minimapa
El checkbox chkNpcPosBox controla el envío de información de objetivo:
- Obtiene el nodo expandido y extrae QuestID/Level/Completed.
- Obtiene nombre y posición de target desde UIDATA_QUEST.
- Si IsMinimapOnly es true, llama SetQuestTargetInfo(true,false,false,...).
- En la otra rama llama SetQuestTargetInfo(true,true,true,...).
- Si no hay selección válida o se desactiva el checkbox, limpia el target con SetQuestTargetInfo(false,false,false,"",Vector,0).

Esto enlaza directamente el árbol de quests con los marcadores del minimapa. No significa que el cliente calcule el objetivo; la ubicación procede de UIDATA_QUEST.

### Abortar/destruir quest
BtnAbort entra en un flujo de confirmación. En el evento 1710, si las condiciones de confirmación interna se cumplen, llama RequestDestroyQuest(QuestID), limpia el target y elimina el nodo. El efecto final depende de la respuesta/validación del servidor; el código solo acredita la solicitud y la limpieza local posterior.

### Contador de objetos
Cuando recibe eventos 2600/2610 con classID, recorre los IDs de objetos asociados a los nodos y consulta GetInventoryItemCount para actualizar texto. El contador visual muestra formato actual/25. El valor 25 es límite/denominador del contador de esta ventana, no necesariamente límite global de quests del servidor.

## 8. QuestListWnd — lista auxiliar

Registra eventos 2840 y 2850:
- 2840 limpia lista, muestra ventana y toma foco.
- 2850 parsea QuestID, QuestName, QuestRequirement, QuestLevel, QuestType, QuestNpcName y QuestDecription (la clave aparece escrita así en el código).
- Inserta cinco campos en ListCtrl; usa iconos distintos para tipos 0/2 y 1/3.
- El ID se conserva en nReserved1 y descripción en szReserved.
- btnLoc consulta GetStartNPCID/GetStartNPCLoc, resuelve nombre mediante UIDATA_NPC.GetNPCName y establece SetQuestTargetInfo(true,true,true,...).

No se debe corregir silenciosamente la clave QuestDecription en el análisis: es la grafía observada y podría ser un contrato del productor de evento.

## 9. QuestBtnWnd y GMQuestWnd

QuestBtnWnd escucha evento 1520, parsea QuestID, muestra botón y comienza efecto visual con BeginEffect. Al hacer clic oculta el botón y muestra QuestTreeWnd.

GMQuestWnd registra eventos 2320, 2330, 2340 y 2350. Su función exacta requiere una pasada dedicada; no se mezcla con el flujo normal de QuestTreeWnd sin evidencia.

## 10. TownMapWnd — mapa local

Registra evento 1770. El payload contiene:
- TownMapName: nombre de textura asignada a TownMapWnd.TownMapTex.
- UserPosX y UserPosY: coordenadas que se usan para anclar UserTex en la esquina superior izquierda de TownMapTex.

Luego muestra TownMapWnd. XML: 334×354, centrado verticalmente a la izquierda. La ventana usa una textura de mapa y un marcador de posición del usuario; el origen de esos datos y las unidades de coordenadas quedan pendientes.

## 11. ZoneTitleWnd — aviso de cambio de zona

Registra evento 2420 y parsea ZoneName, SubZoneName1 y SubZoneName2. El código visible muestra ZoneName en dos TextBox (frontal y sombra), mide texto, calcula posición según resolución y centra el aviso horizontalmente. El tamaño se calcula como ancho del texto + 100 y alto 200. Activa timer de 4.000 ms; OnTimer mata el timer y oculta la ventana.

Aunque parsea subzonas, esta función de render solo utiliza ZoneName. No afirmar que SubZoneName1/2 se muestran.

## 12. Mapa de dependencias U ↔ APIs ↔ XDAT

- MinimapWnd / MinimapWnd_Expand → UIAPI_MINIMAPCTRL → controles Minimap.
- MinimapWnd → MiniMapAPI → cursed weapon list/location.
- QuestTreeWnd / QuestListWnd → QuestAPI.SetQuestTargetInfo → capa de quest del minimapa.
- QuestTreeWnd → QuestAPI.RequestQuestList / RequestDestroyQuest.
- QuestTreeWnd → UIDATA_QUEST y UIDATA_ITEM → datos de misiones/objetos.
- QuestListWnd → UIDATA_QUEST y UIDATA_NPC → NPC inicial y posición.
- RadarMapWnd → UIAPI_MINIMAPCTRL → mapa, targets, capas y leyenda de zonas.
- TownMapWnd → UIAPI_TEXTURECTRL + UIAPI_WINDOW → textura y marcador de posición.
- ZoneTitleWnd → evento de zona + UIAPI_TEXTBOX/ WINDOW + timer.

## 13. Preguntas abiertas prioritarias

1. Resolver nombres originales de variables y funciones saneadas en todos estos archivos.
2. Encontrar productores y payloads completos de eventos 520, 700–730, 110, 1520, 1730–1890, 2420, 2600/2610, 2840/2850 y 2900/3030.
3. Identificar quién solicita TownMapWnd y cómo se construye TownMapName/UserPosX/UserPosY.
4. Reconstruir todos los botones y callbacks de MinimapWnd y MinimapWnd_Expand.
5. Analizar RadarWnd y RadarContainerWnd con sus XML internos y funciones completas.
6. Resolver la persistencia de opciones del radar y las preferencias de minimapa.
7. Analizar GuideWnd y su relación con el botón OpenGuideWnd.
8. Verificar las ventanas/controles hijos de cada XML, texturas y referencias U exactas.
9. Separar las operaciones puramente visuales de solicitudes que cambian estado de quest o envían acciones al servidor.

## 14. Conclusión provisional

La interfaz tiene dos escalas de mapa (minimapa y mapa ampliado), una vista RadarMap con leyenda de zonas y targets, un mapa local por textura y una cadena completa de seguimiento de misiones. QuestTreeWnd no es solo un listado: reconstruye un árbol desde UIDATA_QUEST, actualiza cantidades de objetos, selecciona NPC/ubicaciones y alimenta QuestAPI.SetQuestTargetInfo, que a su vez controla marcadores del minimapa. El flujo de abandono de misión llama RequestDestroyQuest y requiere tratarse como operación sensible, no como simple cambio visual.
