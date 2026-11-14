# L2Sudamerica — auditoría de mascotas, invocaciones, pociones y shots

Estado: análisis estático de fuentes UnrealScript decompiladas y XML XDAT. No se ha modificado ni recompilado ningún binario. Los identificadores MethosRecoveredName_#### son nombres provisionales del decompilador, no nombres originales confirmados.

## 1. Evidencia revisada

Fuentes y XML del artefacto L2Sudamerica-Interface-Audit:
- AutoPotionsWnd.uc: 41.761 bytes, 1.531 líneas; XML 13.819 bytes.
- AutoPotionOptionWnd.uc: 19.269 bytes, 374 líneas.
- PotionsConfigWnd.uc: 2.761 bytes, 83 líneas; XML 28.423 bytes.
- NPHRN_PotionsWnd.uc: 34.365 bytes, 1.030 líneas.
- AutoShotItemWnd.uc: 19.089 bytes, 633 líneas; XML 18.223 bytes.
- PetWnd.uc: 22.787 bytes, 685 líneas; XML 50.488 bytes.
- PetStatusWnd.uc: 7.610 bytes, 271 líneas; XML 11.117 bytes.
- SummonedWnd.uc: 9.529 bytes, 261 líneas; XML 39.782 bytes.
- SummonedStatusWnd.uc: 8.919 bytes, 313 líneas; XML 11.154 bytes.

## 2. AutoPotionsWnd — pociones automáticas integradas

Hereda de UICommonAPI. Registra eventos 150, 230, 190, 210, 950 y 960. En OnLoad inicializa controles, oculta indicadores y programa timer 10215 a 5.000 ms. Su anclaje por defecto es ShortcutWnd.ShortcutWndVertical, BottomLeft, desplazamiento (-130,+30).

Lee objetos desde InventoryWnd.InventoryItem mediante ClassID y obtiene ServerID para solicitar su uso. Las cajas visibles corresponden a MP, HP, CP, CP pequeño, elixir HP y elixir CP.

### ClassID de objetos encontrados
La rutina de refresco busca 8639, 8627, 5591, 5592 (alternativa 6540), 1539 y 728 (alternativa 6539). La identificación nominal debe contrastarse con datos de la misma crónica/servidor; no se asignan nombres por suposición.

### Umbrales y temporizadores
La lógica obtiene UserInfo y calcula porcentajes HP/MP/CP:
- Una rama de CP compara CP actual con 10% del CP máximo.
- Una rama de HP compara HP actual con 40% del HP máximo.
- Otras ramas comparan el porcentaje entero de CP, HP o MP con PotionsConfigWnd.CPBox, HPBox y MPBox.
- Al cumplirse el criterio, programa un timer y llama RequestUseItem(ServerID).

Retardos literales observados:
| Timer | Retardo |
|---|---:|
| 10198 | 1.000 ms |
| 10199 | 1.000 ms |
| 10200 | 100 ms |
| 10203 | 500 ms |
| 10201 | 14.000 ms |
| 10202 | 1.000 ms |
| 10215 | 5.000 ms |
| 10210 | 2.000 ms |
| 4443 | 1.500 ms |

No se han resuelto aún los nombres semánticos de cada timer ni los significados completos de eventos 190/210/230. La fuente acredita que el cliente envía solicitudes de uso de objeto; no demuestra que el servidor acepte cada solicitud ni que se ignore el cooldown.

### Reposición de buffs
La rutina recorre un payload con Max y SkillID_n, clasifica efectos mediante predicados internos y consulta la opción Custom/BuffPotions. Si faltan ciertos efectos reconocidos (entre ellos Acumen, Haste y WindWalk), programa el timer 4443 a 1.500 ms. La rutina de uso solo intenta activar objetos si HP actual es mayor que cero y usa RequestUseItem(ServerID). Hay ramas condicionales distintas; no es una regla universal de reponer cualquier buff.

Esto prueba lógica de reintento temporizado de la interfaz, no reducción de cooldown del servidor. No cambiar intervalos sin reconstruir eventos, cooldowns y respuestas.

## 3. NPHRN_PotionsWnd — sistema alternativo de cinco ranuras

Es una implementación independiente de AutoPotionsWnd:
- Mantiene cinco entradas en un array de estructura con ServerID, intervalo, ClassID/configuración, umbral, controles visuales y dos booleanos.
- Evento 230 evalúa ranuras 3 y 2; evento 190 evalúa 4 y 1; evento 210 evalúa 0.
- Compara HP/MP/CP actual con el umbral configurado y exige HP actual distinto de cero.
- Si se cumple, marca la ranura activa, programa un timer usando el intervalo propio y llama RequestUseItem(ServerID).
- El timer vuelve a evaluar la ranura; el timer especial 5 puede usar un objeto adicional cuyo ServerID se conserva por separado.
- Arrastrar un objeto lo asigna a una ranura si su ClassID coincide con la configuración admitida.
- La opción Unload/PotionsWnd puede ocultar la ventana en OnShow.
- Lee opciones INI; faltan por recuperar exhaustivamente las claves y valores por defecto.

Hallazgo: hay al menos dos motores de automatización de pociones en el paquete. No asumir que comparten configuración o temporizadores.

## 4. AutoPotionOptionWnd y PotionsConfigWnd

AutoPotionOptionWnd registra eventos 200, 220 y 240; solicita RequestItemList() en una ruta de interacción; lee/escribe INI mediante GetINIBool, SetINIBool y SetINIInt; gestiona checks, clics, drag/drop y estado de ventana. Funciona como panel de configuración/selección, no como motor principal de consumo.

PotionsConfigWnd tiene una ventana, tres EditBox y dos ButtonHandle. Registra evento 150 y lee/escribe enteros y cadenas INI. AutoPotionsWnd consulta los EditBox CPBox, HPBox y MPBox durante la evaluación. Pendiente confirmar límites, validación, claves INI y defaults.

## 5. AutoShotItemWnd — shots automáticos

Tiene 22 funciones y 19 variables, usa una estructura de cuatro elementos y registra eventos 2600, 2610, 280, 290, 3030 y 580. Usa RequestUseItem para activar objetos y timers 0, 1, 2 y 1000; el timer 1000 aparece con 300 ms. Consulta GetUserInfo, GetPlayerInfo y GetSkillInfo, mantiene referencia a StatusWnd y maneja clic, clic derecho, drag/drop y actualización visual.

No se asigna aún significado exacto a cada slot, timer o evento. Falta reconstruir payloads de 280/290/580/2600/2610/3030. Tampoco se concluye que la interfaz active shots ignorando estados del personaje.

## 6. PetWnd — inventario y acciones de mascota

Hereda de UIScript y tiene 31 funciones. Registra eventos 1010, 1020, 1030, 1060, 1070, 1080, 1130, 1320, 1330, 1710, 1900 y 250.

Al abrir/actualizar solicita RequestPetInventoryItemList() y RequestPetActionList(). Dar objetos a la mascota utiliza RequestGiveItemToPet(ServerID,cantidad), tanto cantidad completa como una unidad. Usar un objeto de mascota utiliza RequestPetUseItem(ServerID). Cambiar el nombre utiliza RequestChangePetName(nombre).

Incluye interacciones con acciones, inventario, doble clic, clic derecho y drag/drop. Mantiene ID de mascota, estado booleano y cadena; usa confirmaciones internas 1111 y 2222. Los eventos 1010–1080 y 1320/1330 deben mapearse a nombres simbólicos y productores.

Las funciones Request son solicitudes del cliente: el código no demuestra validación del servidor, éxito de operación ni restricciones por tipo de mascota. ServerID y cantidades son parámetros de solicitud, no autoridad local.

## 7. PetStatusWnd y SummonedStatusWnd

Ambas consultan UIDATA_PET.GetPetID(), GetIsPetOrSummoned() y GetPetInfo(), ocultan la ventana cuando no hay entidad compatible y actualizan barras/estados. Al hacer clic en una región del marco obtienen ubicación del jugador y llaman RequestAction(petID, playerLocation); es una solicitud de acción/orden, no movimiento local demostrado.

PetStatusWnd registra 250, 1000, 1040, 1050 y 1130. SummonedStatusWnd registra 250, 1000, 1100, 1110, 1120 y 1130; tiene una variable booleana adicional y un método de actualización adicional. La diferencia funcional completa sigue pendiente.

## 8. SummonedWnd — estado y acciones de invocación

Registra 250, 1090, 1130, 1340, 1350 y 1900. Al mostrarse y ante el evento 1900 solicita RequestSummonedActionList(). Lee PetInfo y presenta nombre, nivel, HP/MP, ataque/defensa física, hit, crítico, velocidad de ataque, ataque mágico, defensa mágica, evasión, movimiento, casteo y consumo de shots.

La lista se muestra en SummonedWnd.SummonedWnd_Action.SummonedActionWnd. Al hacer clic obtiene ClassID y ejecuta DoAction(ClassID). Limpia/oculta datos al cerrarse o perder la entidad. La barra HP usa ancho máximo 125 y alto 12; MP sigue el mismo patrón.

## 9. Mapa parcial de eventos y APIs

| Clase | Eventos registrados | APIs/operaciones visibles |
|---|---|---|
| AutoPotionsWnd | 150, 190, 210, 230, 950, 960 | RequestUseItem, timers, opciones |
| AutoPotionOptionWnd | 200, 220, 240 | RequestItemList, INI |
| PotionsConfigWnd | 150 | lectura/escritura INI |
| NPHRN_PotionsWnd | 50, 110, 150, 190, 210, 230, 2600, 2610 | RequestUseItem, timers, opciones |
| AutoShotItemWnd | 280, 290, 580, 2600, 2610, 3030 | RequestUseItem, timers |
| PetWnd | 250, 1010, 1020, 1030, 1060, 1070, 1080, 1130, 1320, 1330, 1710, 1900 | RequestPetInventoryItemList, RequestPetActionList, RequestGiveItemToPet, RequestPetUseItem, RequestChangePetName |
| PetStatusWnd | 250, 1000, 1040, 1050, 1130 | GetPetInfo, RequestAction |
| SummonedWnd | 250, 1090, 1130, 1340, 1350, 1900 | RequestSummonedActionList, DoAction |
| SummonedStatusWnd | 250, 1000, 1100, 1110, 1120, 1130 | GetPetInfo, RequestAction |

Los números son eventos internos de interfaz. No deben convertirse en nombres de protocolo sin localizar sus productores.

## 10. Próximas verificaciones

1. Resolver nombres originales de símbolos MethosRecoveredName_#### mediante name map y referencias cruzadas.
2. Identificar ClassID de pociones y shots usando datos de esta misma versión.
3. Recuperar OnTimer, OnEvent, clasificación de buffs y rutas de activación/desactivación completas.
4. Mapear productores y payloads de eventos 190, 210, 230, 950, 960, 2600, 2610, 280, 290, 580, 1010–1080 y 1320/1330/1340/1350.
5. Reconstruir claves INI y defaults de ambos sistemas.
6. Extraer geometría y controles de los XML.
7. Comparar PetWnd/SummonedWnd con APIs de mascota/acción sin inferir comportamiento de servidor.
8. Validar cambios solo en entorno de prueba.

## 11. Conclusión provisional

El paquete tiene subsistemas independientes para inventario/órdenes de mascota, estado de mascota, estado/acciones de invocación, shots automáticos y al menos dos motores de pociones. AutoPotionsWnd implementa umbrales HP/MP/CP y reposición temporizada de ciertos buffs; NPHRN_PotionsWnd añade cinco ranuras con intervalos propios. Las activaciones observadas terminan en solicitudes normales de uso de objeto (RequestUseItem); no hay evidencia aquí de bypass de cooldown ni de modificación del servidor.
