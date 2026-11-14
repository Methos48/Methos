# Auditoría técnica — EventMatch, Replay, Duel, Hero Tower y utilidades especiales

**Cliente:** L2Sudamerica / Lineage II Interlude.  
**Fuente:** UnrealScript exportado y XML XDAT del artefacto 43579091099.  
**Alcance:** EventMatch GM/Observer, replay local, duelos, Hero Tower, herramientas GM, reportes, puntos PC Café y cupones.  
**Método:** correlación estática de clases, eventos, llamadas API, payloads y controles. No se modificaron binarios ni se ejecutó el cliente.

## 1. Cobertura

| Subsistema | Clases | Hallazgo |
|---|---|---|
| EventMatch | EventMatchGMWnd, EventMatchGMFenceWnd, EventMatchGMMsgWnd, EventMatchObserverWnd, EventMatchSpecialMsgWnd, EventMatchGMCommandWnd | Paneles GM/Observer; varias acciones GM aparecen como funciones vacías |
| Replay | ReplayListWnd, ReplayLogoWnd | Administración de archivos locales .L2R |
| Duel | DuelManager | Invitaciones, respuesta y panel de participantes |
| Hero Tower | HeroTowerWnd | Ranking, diarios y envío de texto de héroe |
| GM | GMWnd, GMSnoopWnd, GMQuestWnd y ventanas GM auxiliares | Herramientas administrativas |
| Report | ReportWnd | Eventos de reporte/estado y ramas de reinicio/salida |
| PC Café | PCCafeEventWnd, CouponEventWnd | Presentación de puntos y solicitud de canje |

Las ventanas no sustituyen las reglas del servidor. Las herramientas GM son administrativas y no deben probarse fuera de cuentas/servidores autorizados.

## 2. EventMatch — panel de administración

### 2.1 EventMatchGMWnd

XML: raíz 700 × 500, centrada. Controles:
- Equipos: Team1NameEditBox, Team2NameEditBox, Team1LeaderNameEditBox, Team2LeaderNameEditBox.
- Configuración: OptionFileEditBox, CommandFileEditBox.
- Marcador: Team1ScoreEditBox, Team2ScoreEditBox, MatchIDTextBox.
- Mensaje: AnnounceEditBox.
- Listas Team1ListCtrl y Team2ListCtrl, ambas 300 × 150.
- Botones CreateEventMatchButton, SetTeam1LeaderButton, LockTeam1Button, SetTeam2LeaderButton, LockTeam2Button, PauseButton, StartButton, SetScoreButton, SendAnnounceButton, ShowCommandWndButton, SendGameEndMsgButton, SetFenceButton, Team1FirecrackerButton y Team2FirecrackerButton.

OnLoad enlaza los controles y registra 2180, 2190, 2250 y 2210.
- 2180: ruta de inicialización/limpieza de estado de partida.
- 2190: actualización de datos de evento.
- 2250: actualización de miembros/listas por TeamID.
- 2210: actualización de MatchID, MatchStat, FenceStat, Team1Name, Team2Name, Team1Stat y Team2Stat.

En la ruta 2250 consulta EventMatchAPI.GetPartyMemberCount(TeamID), elige la lista de equipo y rellena registros de usuarios. La ventana consulta EventMatchAPI para datos del evento.

**Hallazgo importante:** varias funciones llamadas por los botones principales (crear evento, asignar líderes, bloquear equipos, pausar, iniciar, fijar score, anunciar, mensaje final, valla y fuegos artificiales) están presentes como funciones vacías que solo retornan en la fuente exportada. El XML prueba que los controles existen, pero esta fuente no permite afirmar que esas acciones estén implementadas funcionalmente. No completar la semántica por el texto de los botones.

### 2.2 Panel de comandos

La función ShowCommandWnd:
- lee la ruta de CommandFileEditBox;
- ejecuta RefreshINI sobre ese archivo;
- lee sección Cmd, clave CmdCnt y entradas Cmd0..CmdN;
- construye HTML con enlaces cmd para mostrarlos en EventMatchGMCommandWnd.HtmlCtrl.

El archivo de comandos es externo. No se documentan comandos reales ni se recomienda ejecutar comandos GM. No está cerrado qué manejador procesa cada enlace ni qué autorización aplica.

### 2.3 EventMatchGMFenceWnd

XML 500 × 170, centrada. Controles XEditBox, YEditBox, ZEditBox, XLengthEditBox, YLengthEditBox y botones OKButton, CancelButton, MyLocationButton.
- MyLocationButton copia GetPlayerPosition() a X/Y/Z.
- OK valida XLength/YLength entre 100 y 5000, convierte coordenadas a Vector y llama a una función de EventMatchGMWnd que está vacía en esta exportación.
- Cancel oculta la ventana.

Confirma captura/validación local de coordenadas y dimensiones, no creación real de valla en servidor.

### 2.4 EventMatchGMMsgWnd y EventMatchSpecialMsgWnd

Ambas registran evento 2270 y leen Type (entero) y Message (texto). Presentan mensajes temporales y programan timer de 5 segundos. GMMsg XML 720 × 88; SpecialMsg XML 720 × 320, anclada TOP_CENTER con offset Y=390 y textura MsgTex. Falta correlacionar todos los valores Type con sus productores.

### 2.5 EventMatchObserverWnd

Composición XML:
- TopWnd con TeamName1/TeamName2 y texturas de score.
- Player1Wnd/Player2Wnd con barras CP/HP/MP, nivel/clase, nombre y panel de buffs.
- Party1Wnd/Party2Wnd, cada uno con nueve filas PartyMember.
- MsgWnd con seis entradas por lado y campos Attacker/Defender/Skill.

Registra 2220, 2240, 2230, 2250, 2260, 290 y 90.
- 2220: datos/actualización de usuario.
- 2240: refresca marcador.
- 2230: refresca nombres de equipo.
- 2250: actualiza grupo/lista del TeamID indicado.
- 2260: lee UserID y TeamID y selecciona miembro.
- 290: lee AttackerID, DefenderID y SkillID; resuelve UserInfo y SkillInfo para formar mensaje.
- 90: comandos de interfaz.

El evento 90 reconoce:
- EventMatchShowPartyWindow: mostrar/ocultar paneles de equipos.
- EventMatchLockPosition: alternar draggable de paneles de jugador, buffs y equipos.
- EventMatchInitPosition: restaurar anclajes de los paneles.
- EventMatchToggleShowClassOrName: alternar entre clase y nombre.
- EventMatchSwitchMessageMode: ciclar modos 0→1→2→0.

Se representan nueve miembros por equipo. Barras y buffs usan datos recibidos; los buffs se reconstruyen resolviendo SkillInfo localmente. La selección llama EventMatchAPI.SetSelectedUser(team,index). También consulta GetTeamName, GetScore y GetPartyMemberCount. Es presentación/selección de observador; no calcula el resultado del combate.

## 3. Replay — archivos locales

ReplayListWnd XML 316 × 350, centrada. Contiene ReplayListCtrl 300 × 273, chkLoadCamInst, chkLoadChatData y botones btnOK, btnDel, btnCancel.
- Al mostrarse vacía la lista y ejecuta GetFileList en ..\REPLAY para archivos .L2R.
- Guarda índice y nombre en LVDataList[0] y LVDataList[1].
- Doble click/btnOK llama BeginReplay(nombre, loadCamInst, loadChatData).
- btnDel construye ..\REPLAY\<nombre>.L2R, llama EraseReplayFile y recarga.
- btnCancel cambia a LoginState.

Administra archivos locales, no solicita al servidor iniciar un replay. Los efectos internos de los dos flags deben seguirse en el motor/cliente.

## 4. DuelManager — invitación y panel

XML 176 × 368, anclado CENTER_RIGHT. Nueve filas PartyStatusWnd0..8 con PlayerName, ClassIcon, LeaderIcon y barras CP/HP/MP.
Registra 2700, 2710, 2720, 2730, 2740, 2750, 1710 y 1720.
- 2700 lee userName y type. Si Game/IsRejectingDuel está activo, responde automáticamente mediante RequestDuelAnswerStart(type, rejectFlag, 0). Si no, muestra mensaje según type 0/1 y abre confirmación interna ID 1111.
- 1710/1720 procesan respuesta pendiente y llaman RequestDuelAnswerStart(duelType, rejectFlag, 1) o con último argumento 0.
- 2710 activa panel; 2730 desactiva, limpia y oculta.
- 2740 lee userName, ID, class, level, currentHP/maxHP, currentMP/maxMP y currentCP/maxCP.
- Mantiene hasta nueve IDs y actualiza la fila; RequestTargetUser se usa en una ruta de selección.

No asignar semántica definitiva a los argumentos 0/1 de RequestDuelAnswerStart sin contrato adicional. Sí se observa que IsRejectingDuel participa en la respuesta automática y manual.

## 5. HeroTowerWnd — ranking y diario

XML 600 × 326; lstHero 586 × 255, txtDiary, btnReg y btnShowDiary. Registra 880 y 890.
- Evento 880 limpia, muestra, enfoca y actualiza la lista.
- Payload: Max y arrays Name_n, ClassId_n, PledgeName_n, PledgeCrestId_n, AllianceName_n, AllianceCrestId_n y WinCount_n.
- Las filas presentan nombre, clase, clan/alianza, crest y victorias.
- Doble click/btnShowDiary toma ClassID de nReserved1 y envía bypass _diary?class=<ClassID>&page=1.
- UIDATA_PLAYER.IsHero() determina si se muestran txtDiary y btnReg.
- btnReg llama HeroTowerAPI.RequestWriteHeroWords(texto) y limpia el editor.

La UI limita el editor al jugador identificado como Hero; la autorización final debe validarla servidor.

## 6. Herramientas GM

### GMWnd
XML 256 × 276, anclado BOTTOM_RIGHT con offset Y=-51. Campo de texto y botones Tele, Move, Recall, DetailStatus, Inventory, MagicSkill, Quest, Info, Store, Clan, Petition, SendHome, NPCList, ItemList, SkillList, ForcePetition y ChangeServer. Registra 2280, 1710, 1720 y 980. Es un menú de herramientas GM; las acciones se delegan a otras ventanas/APIs.

### GMSnoopWnd
Cuatro paneles SnoopWnd1..4; cada uno puede presentar chat y filtros. Registra 2410. El evento parsea SnoopID, CharacterName, SnoopName, CreatureID, Type y ChatMessage. Cancel llama GMAPI.RequestSnoopEnd(SnoopID). Herramienta de observación de chat para GM, no chat normal.

### GMQuestWnd
XML 281 × 450, anclado TOP_LEFT (0,100); MainTree 253 × 290, BtnAbort y chkNpcPosBox. Registra 2320, 2330, 2340 y 2350. Usa GMAPI.RequestGMCommand(4,texto) y maneja datos ClassID/ItemCount. Los resultados dependen de permisos y respuesta del servidor.

## 7. ReportWnd

XML 206 × 321, centrado. Contiene BtnCancel, BtnExecute, BtnInit, campos de experiencia, Adena y tiempo, títulos y texturas. Registra 2930, 2940, 580 y 3030. Procesa Index y Param1; existen rutas ExecRestart y ExecQuit, temporizadores de 1 segundo y referencias a estado de InventoryWnd. No asignar semántica definitiva a cada índice sin localizar su productor. No activar estas ramas durante pruebas visuales.

## 8. PC Café y cupones

### PCCafeEventWnd
XML 150 × 56, anclado TOP_RIGHT (-10,260). Controles HelpButton, TitleTextBox, PointTextBox y PointAddTextBox. Registra 1910. Payload: TotalPoint, AddPoint, PeriodType, RemainTime y PointType. Actualiza la presentación de puntos, periodo y tiempo; no se encontró aquí una solicitud que otorgue puntos.

### CouponEventWnd
XML 300 × 261, anclado CENTER_LEFT (120,0). Cinco EditBox input1..input5, botones inputnumber/resetbtn y campos de aviso. Registra 530. inputnumber concatena/valida el código y llama RequestPCCafeCouponUse(código); resetbtn limpia campos. Canje y validación final dependen del servidor.

## 9. Matriz de llamadas

| Componente | Llamada observada | Efecto |
|---|---|---|
| ReplayListWnd | GetFileList / BeginReplay / EraseReplayFile | Listar, reproducir y borrar .L2R local |
| DuelManager | RequestDuelAnswerStart | Responder a invitación/confirmación |
| DuelManager | RequestTargetUser | Seleccionar objetivo |
| HeroTowerWnd | HeroTowerAPI.RequestWriteHeroWords | Enviar diario de héroe |
| HeroTowerWnd | RequestBypassToServer(_diary...) | Solicitar diario por ClassID |
| GMSnoopWnd | GMAPI.RequestSnoopEnd | Finalizar observación GM |
| GMQuestWnd | GMAPI.RequestGMCommand(4,...) | Solicitud de comando GM |
| CouponEventWnd | RequestPCCafeCouponUse | Solicitar canje |
| EventMatchObserverWnd | EventMatchAPI.GetScore/GetTeamName/GetPartyMemberCount/SetSelectedUser | Leer datos y selección del observador |

## 10. Riesgos y pendientes

- No modificar solicitudes GM, comandos, IDs, argumentos de duelo, rutas replay ni payloads de cupones.
- No inventar implementación para funciones vacías de EventMatch GM.
- Un control XML no prueba que su acción esté implementada.
- Seguir productores de eventos 2180/2190/2210/2250/2270, 2220–2260, 880/890, 1910, 530 y 2930/2940/3030.
- Seguir BeginReplay y los flags de cámara/chat en el motor.
- Confirmar contrato de RequestDuelAnswerStart y estados 1710/1720.
- Revisar geometría y recursos de todos los controles.

**Estado:** inventario y estructura XDAT confirmados; eventos y llamadas principales mapeados; flujos estáticos descritos. No se compiló un paquete modificado, no se reprotegió una edición ni se probó carga en cliente.