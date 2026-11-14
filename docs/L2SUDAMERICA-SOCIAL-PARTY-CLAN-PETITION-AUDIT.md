# L2Sudamerica — auditoría de clan, alianzas, party matching, peticiones y servicios sociales

Estado: auditoría estática de fuentes UnrealScript y XML XDAT del artefacto L2Sudamerica-Interface-Audit. No se modificaron binarios. Las funciones MethosRecoveredName_* son nombres saneados; no se les asigna semántica sin evidencia. Los eventos son IDs internos de interfaz y no equivalen automáticamente a opcodes de red.

## 1. Alcance y fuentes

Se revisaron las fuentes U y XML de:
- ClanWnd, ClanDrawerWnd, GMClanWnd, InviteClanPopWnd.
- UnionWnd, UnionDetailWnd.
- PartyWnd, PartyWndCompact, PartyWndOption, SaveLoadParty.
- PartyMatchWnd, PartyMatchRoomWnd, PartyMatchWaitListWnd, PartyMatchOutWaitListWnd, PartyMatchMakeRoomWnd, PartyMatchWndCommon.
- PetitionWnd, UserPetitionWnd, PetitionFeedBackWnd.
- BoardWnd, MailBtnWnd y SSQMainBoard.

Fuentes destacadas: ClanWnd.uc (73.639 bytes/1.785 líneas), ClanDrawerWnd.uc (93.659/2.460), PartyWnd.uc (36.448/993), PartyWndCompact.uc (25.221/760), PartyMatchRoomWnd.uc (20.318/589), PartyMatchWnd.uc (12.540/374), UserPetitionWnd.uc (5.790/201), SSQMainBoard.uc (66.612/1.437).

## 2. Geometría XDAT y controles

| Ventana | Dimensiones | Anclaje | Offset | Controles con nombre |
|---|---:|---|---:|---:|
| ClanWnd | 280×411 | TOP_LEFT | (0,65) | 32 |
| ClanDrawerWnd | 256×512 | TOP_LEFT | (0,0) | 182 |
| UnionWnd | 226×271 | TOP_LEFT | (200,200) | 11 |
| UnionDetailWnd | 226×251 | TOP_LEFT | (400,400) | 5 |
| PartyWnd | 176×416 | TOP_LEFT | (0,152) | 46 |
| PartyWndCompact | 71×26 | TOP_LEFT | (0,152) | 20 |
| PartyWndOption | 256×131 | TOP_LEFT | (200,131) | 4 |
| PartyMatchWnd | 550×486 | TOP_LEFT | (0,76) | 13 |
| PartyMatchRoomWnd | 550×486 | TOP_LEFT | (0,76) | 21 |
| PartyMatchWaitListWnd | 246×466 | TOP_LEFT | (0,0) | 10 |
| PartyMatchOutWaitListWnd | 246×466 | TOP_LEFT | (0,0) | 16 |
| PartyMatchMakeRoomWnd | 356×141 | CENTER | (0,0) | 13 |
| SaveLoadParty | 164×48 | CENTER | (0,-80) | 5 |
| PetitionWnd | 310×412 | TOP_CENTER | (106,0) | 5 |
| UserPetitionWnd | 256×401 | CENTER_LEFT | (0,0) | 9 |
| PetitionFeedBackWnd | 256×401 | BOTTOM_RIGHT | (0,0) | 8 |
| BoardWnd | 646×530 | CENTER | (0,0) | 6 |
| MailBtnWnd | 32×32 | BOTTOM_LEFT | (79,-5) | 2 |
| SSQMainBoard | 256×401 | CENTER | (0,0) | 3 |

Los controles se extrajeron de los elementos name dentro del árbol XML. El número de controles nombrados no equivale al total de nodos, porque también hay elementos decorativos/estructurales y nodos sin nombre.

## 3. ClanWnd — resumen, miembros y guerras

### Eventos
Registra 150, 160, 320, 330, 340, 400, 410, 420, 440, 450 y 480. La fuente asocia las rutas de carga/actualización a datos de clan, miembros, autorizaciones, grado y guerras; algunos IDs exactos necesitan correlacionarse con los productores que aún no están documentados.

### Controles principales
El XML incluye ClanNameText, ClanMasterNameText, ClanAgitText, ClanStatusText, ClanLevelText, ClanCurrentNum, ComboboxMainClanWnd, ClanMemberList, ClanMemInfoBtn, HeroBtn, ClanMemAuthBtn, ClanBoardBtn, ClanInfoBtn, ClanPenaltyBtn, ClanQuitBtn, ClanWarInfoBtn y ClanWarDeclareBtn.

### Operaciones visibles
- RequestClanMemberInfo(clanType, memberName): solicita información de un miembro seleccionado.
- RequestClanMemberAuth(clanType, memberName): solicita información/autorización del miembro.
- RequestClanGradeList(): obtiene grados disponibles.
- RequestClanWarList(page, mode): solicita listas de guerras por página/modo.
- RequestClanDeclareWar() y RequestClanWithdrawWar(): acciones de guerra.
- RequestClanLeave(clanID, clanName): salida del clan.
- ClanPenaltyBtn ejecuta el comando de acción pledgepenalty.

La UI mantiene flags para evitar solicitudes repetidas de información/grades/wars. Los detalles de actualización dependen de eventos entrantes; el código no autoriza a asumir que el cambio se produjo al enviar la solicitud.

## 4. ClanDrawerWnd — administración del clan

Es el módulo más extenso de esta familia: 93.659 bytes, 2.460 líneas, 50 funciones y 10 eventos registrados (160, 350, 360, 370, 380, 430, 460, 470, 490, 500). El XML contiene 182 controles nombrados y varios grupos/pestañas de administración.

### Operaciones de administración identificadas
- Invitar a un miembro seleccionado a la party: RequestInviteParty(nombre).
- Expulsar miembro: RequestClanExpelMember(clanType, nombre).
- Asignar/eliminar aprendiz: RequestClanAssignPupil(nombreMaestro, nombreAprendiz), RequestClanDeletePupil(nombre).
- Cambiar apodo/nombre de clan de un miembro: RequestClanChangeNickName(nombre, nuevoNombre); la cadena vacía aparece en rutas de borrado.
- Cambiar grado: RequestClanChangeGrade(nombre, grado).
- Registrar/quitar cresta: RequestClanRegisterCrest / RequestClanUnregisterCrest.
- Registrar/quitar emblema: RequestClanRegisterEmblem / RequestClanUnregisterEmblem.
- Consultar lista de guerras: RequestClanWarList(página, modo).
- Declarar/retirar guerra con clan concreto: RequestClanDeclareWarWidhClanName(nombreClan), RequestClanWithdrawWarWithClanName(nombreClan). La grafía Widh está tal cual aparece en el símbolo.
- Reorganizar miembros: RequestClanReorganizeMember(modo, nombre, destino/posición, dato auxiliar).
- Editar autorizaciones: RequestEditClanAuth(grado, arrayAutorizaciones); consultar una autorización con RequestClanAuth(authID).

La clase tiene numerosas ventanas internas para cambio de grado, asignación de aprendices, emblemas, autorizaciones y guerras. Varias operaciones están precedidas por selección de registro, lectura de controles y/o confirmaciones. No deben activarse automáticamente ni eliminarse confirmaciones en una modificación visual.

### Seguridad y límites
La UI expone acciones administrativas, pero los permisos y resultados finales dependen del servidor. La lista de permisos mostrada localmente no prueba que el usuario tenga permiso efectivo. Los métodos Request* solo acreditan solicitudes salientes.

## 5. UnionWnd y UnionDetailWnd — canal de comando

UnionWnd registra eventos 1360, 1370, 1380, 1390, 1395 y 1400. Presenta la party líder/propietaria, tipo de loot/routing, cantidad de miembros y lista de parties. Controles: lstParty, txtOwner, txtRoutingType, txtCountInfo, btnRefresh, btnBan, btnOut y btnMemberInfo.

Solicitudes:
- RequestCommandChannelInfo(): información del canal.
- RequestCommandChannelPartyMembersInfo(): miembros.
- RequestCommandChannelBanParty(party): expulsión/bloqueo de party.
- RequestCommandChannelWithdraw(): salir del canal.

UnionDetailWnd registra evento 1420, muestra maestro y lista de miembros. Desde la lista usa RequestAction o RequestAttack sobre el miembro seleccionado según la ruta de interacción. La acción concreta debe preservarse como comportamiento de juego, no reinterpretarse como simple navegación.

## 6. PartyWnd y PartyWndCompact — party en pantalla

### Geometría y modo
PartyWnd normal mide 176×416 y PartyWndCompact 71×26; ambos tienen slots nombrados PartyStatusWnd0..7 y un slot adicional/estructura de líder en el árbol. El código implementa arrays de nueve posiciones. Esta diferencia entre la cantidad de controles/slots visibles y el recorrido del código requiere revisar XML completo y condiciones de render antes de afirmar capacidad exacta.

PartyWndOption mide 256×131; checkbox ShowSmallPartyWnd guarda/consulta Game/SmallPartyWnd. PartyWnd también lee Options/CustomPartyIcons.

### Eventos
Ambas ventanas registran 1000, 1140, 1150, 1160, 1170, 1180 y 40. El código reacciona a altas/bajas/actualizaciones de miembros, barras y estados; los productores y payloads completos de cada ID quedan abiertos.

### Interacciones sobre miembros
- RequestAssist(memberObjectID, playerLocation) al usar la zona/acción de asistencia.
- RequestAction(memberObjectID, playerLocation) para la acción de seguimiento/orden.
- Si se mantiene Alt y el personaje está en PK mode, la ruta correspondiente usa RequestAttack(memberObjectID, playerLocation).
- PartyWndCompact añade RequestTargetUser en su flujo de selección.
- Se actualizan CP/HP/MP, icono de clase, líder, buffs/debuffs y tooltips; los iconos personalizados dependen de Options/CustomPartyIcons.

Las solicitudes llevan ID de objeto y ubicación local del jugador. No se debe alterar el ID, la condición PK ni los modificadores de teclado en una personalización de la interfaz.

## 7. SaveLoadParty — guardar/cargar lista de miembros

La ventana mide 164×48 y registra eventos 150 y 580. Persiste la lista en el archivo de opciones bajo sección PartyList:
- Count: cantidad de miembros almacenados.
- Member_0...Member_n: nombres de miembros.

Al guardar recorre la party y escribe nombres con SetINIString y SetINIInt. Al cargar lee Count y cada Member_n mediante GetINIString y envía RequestInviteParty(nombre) para cada miembro. La rutina utiliza temporizadores/estado para espaciar o controlar el proceso. Es una automatización de invitaciones normales; no prueba que la invitación sea aceptada ni que el miembro esté disponible. No se debe confundir con una función de reclutamiento del servidor.

## 8. Party Matching — búsqueda y salas

### PartyMatchWnd
Registra 1540, 1550 y 1570. Los filtros de ubicación/nivel se inicializan en índice 1. La lista de salas parsea PageNum y RoomCount y, por sala, RoomNum, Leader, ZoneID, MinLevel, MaxLevel, CurMember, MaxMember y RoomName. Presenta zona mediante GetZoneNameWithZoneID.

Botones:
- Refresh, Prev, Next: refrescar y paginar.
- MakeRoom: abre PartyMatchMakeRoomWnd.
- AutoJoin: RequestJoinPartyRoomAuto().
- WaitListButton: abre la lista de espera.
- Doble clic de registro: flujo de entrada a sala con RequestJoinPartyRoom.
- Al ocultar se ejecuta RequestExitPartyMatchingWaitingRoom() en OnSendPacketWhenHiding.

### PartyMatchRoomWnd
Registra 1550, 1560, 1580, 1590, 1600, 1630 y 40. Presenta número/nombre de sala, ubicación, miembros, loot, límites de nivel, lista de party y chat interno. Las operaciones visibles incluyen:
- RequestPartyMatchWaitList(...): pedir lista de espera.
- RequestInviteParty(nombre): invitar a party.
- RequestBanFromPartyRoom(nombre): expulsar de sala.
- RequestWithdrawPartyRoom(): salir de sala.
- RequestDismissPartyRoom(): cerrar/deshacer sala.

El cierre/ocultación tiene un hook OnSendPacketWhenHiding, por lo que no debe eliminarse como si fuera un callback cosmético.

### WaitListWnd y OutWaitListWnd
Ambas páginas registran eventos 1550/1610/1620 o 1610/1620, y muestran lista paginada. RequestPartyMatchWaitList(page,minLevel,maxLevel,mode) pide los datos. OutWaitListWnd lee min/max level desde EditBox y usa mode=1; la lista de espera de la sala usa mode=0. Al seleccionar registro se usa RequestAskJoinPartyRoom(roomID).

### PartyMatchMakeRoomWnd
El formulario valida el máximo y mínimo de nivel; el máximo se limita en el código al rango 1..80. Envía RequestManagePartyRoom(roomID/operación, límite miembros, nivel mínimo, nivel máximo, título). Otra rama envía RequestAskJoinPartyRoom para una sala seleccionada. El mapeo exacto de cada argumento posicional necesita completar la lectura de la función entera antes de modificar el payload.

## 9. Peticiones de soporte — Petition

### UserPetitionWnd
Mide 256×401. Controles: PetitionTypeComboBox, PetitionMultiEdit, OKButton, CancelButton, ViewInfoTextBox, CategoryTextBox, InputPetitionTextBox y HelpHtmlCtrl. Registra eventos 1221 y 1921.
- Carga texto de ayuda HTML desde recursos L2text o desde un string recibido.
- Selecciona categoría/tipo en combo.
- Valida longitud del texto (límite 255 observado en el flujo) y usa RequestPetition(tipo, texto).
- Cancelar utiliza RequestPetitionCancel().

### PetitionWnd
Mide 310×412, presenta PetitionChatWindow y PetitionChatEditBox, además de CancelButton y FeedBackButton. Registra eventos 1920, 1930 y 1940. La ruta de cancelación usa RequestPetitionCancel(); la ventana de feedback se abre desde el botón correspondiente.

### PetitionFeedBackWnd
Mide 256×401, ofrece radio buttons VeryGood, Good, Normal, Bad, VeryBad y campo MultiEdit. Envía RequestPetitionFeedBack(valor, comentario). El comentario se limita a 512 caracteres en la comprobación observada. Añade el comentario como mensaje de sistema en una ruta local.

Petition es un sistema de soporte al usuario; no confundirlo con chat normal ni alterar el envío de datos de la petición.

## 10. BoardWnd, MailBtnWnd y SSQMainBoard

### BoardWnd
Mide 646×530. Contiene HtmlViewer, TabCtrl, fondos y bookmark. Registra eventos 1190 y 1200. Al abrir configura page lock, orden de pestañas, limpia HtmlViewer y ejecuta RequestBBSBoard(). El contenido HTML se carga/actualiza a través de datos del sistema; la página tiene protección de navegación (PageLock). No debe inferirse que el HTML sea local ni seguro por el solo hecho de representarse en el cliente.

### MailBtnWnd
Botón 32×32 anclado abajo a la izquierda, offset (79,-5), registra evento 1530. El evento actualiza/gestiona el botón de correo; el código de esta clase no expone el cliente de correo completo. Queda pendiente localizar el resto de ventanas y APIs de mail en el inventario global.

### SSQMainBoard
Mide 256×401 y registra eventos 740, 750, 760 y 770. Presenta pestañas y estado Seven Signs. Usa SSQAPI.RequestSSQStatus con parámetros 1, 2, 3 y 4 según pestaña/acción; las respuestas se procesan en SetSSQStatus y funciones auxiliares que construyen el árbol y actualizan texto/estado. El significado exacto de cada parámetro debe confirmarse con la API/consumidores antes de renombrarlo.

## 11. Mapa de dependencias y operaciones

| Subsistema | API/solicitud principal | Función |
|---|---|---|
| Clan resumen | RequestClanMemberInfo, RequestClanWarList | cargar miembro/guerras |
| Clan administración | RequestClanExpelMember, ChangeGrade, AssignPupil, EditClanAuth | acciones administrativas |
| Clan emblemas | Register/UnregisterCrest, Register/UnregisterEmblem | gestión de insignias |
| Canal de comando | RequestCommandChannelInfo, BanParty, Withdraw | administración del canal |
| Party UI | RequestAssist, RequestAction, RequestAttack | acciones sobre miembros |
| Party matching | RequestPartyRoomList, JoinPartyRoom, ManagePartyRoom | buscar/crear/entrar a salas |
| Wait list | RequestPartyMatchWaitList, RequestAskJoinPartyRoom | lista y solicitud de ingreso |
| SaveLoadParty | RequestInviteParty + INI PartyList | invitaciones desde lista local |
| Petition | RequestPetition, RequestPetitionCancel, RequestPetitionFeedBack | soporte |
| BBS | RequestBBSBoard | tablón |
| Seven Signs | RequestSSQStatus | datos SSQ |

## 12. Preguntas abiertas y siguientes pruebas

1. Resolver FNames saneados y nombres originales de las funciones internas de ClanWnd/ClanDrawerWnd/PartyMatch.
2. Identificar productores, payloads y respuestas de eventos de clan 150–500, command channel 1360–1420, party 1000–1180, party matching 1540–1630 y petition 1920–1940.
3. Completar firma semántica de RequestManagePartyRoom y RequestPartyMatchWaitList con lectura integral de sus funciones.
4. Resolver el número real de miembros/slots de party y la diferencia entre PartyWnd XML y arrays U.
5. Localizar ventanas de mail/messenger/friends restantes; esta pasada no halló clases con esos nombres exactos, lo que no demuestra que no existan bajo otros nombres.
6. Analizar InviteClanPopWnd, GMClanWnd, PartyMatchWndCommon y las funciones de GM con mayor detalle.
7. Revisar todos los 182 controles de ClanDrawerWnd y relacionarlos con sus callbacks.
8. Confirmar claves INI completas y defaults de SaveLoadParty y PartyWndOption.
9. Validar las operaciones solo con un cliente de pruebas y sin alterar permisos o reglas del servidor.

## 13. Conclusión provisional

La interfaz de L2Sudamerica tiene una capa de clan extensa con administración de miembros, rangos, aprendices, permisos, emblemas y guerras; un canal de comando separado; dos presentaciones de party; búsqueda de grupos con salas y listas de espera; almacenamiento local de miembros para invitaciones; y un subsistema de peticiones de soporte. Muchas acciones visibles son solicitudes al servidor y algunas son administrativas o destructivas. Las personalizaciones deben preservar confirmaciones, argumentos, permisos y respuestas, y distinguir siempre el cambio visual de la operación real.
