# L2Sudamerica — auditoría de ChatWnd, SystemMsgWnd y CustomDamageWnd

Fecha: 2026-11-14. Origen: artefacto verificado del workflow [43579091099](https://github.com/Methos48/Methos/actions/runs/43579091099), derivado de los originales L2Sudamerica.

## Alcance

- `ChatWnd`: ventana principal de chat, canales y entrada de texto.
- `SystemMsgWnd`: panel que contiene `SystemMsgList`, donde se muestran mensajes del sistema y eventos de combate enviados a esa lista.
- `CustomDamageWnd`: indicadores numéricos flotantes de daño; es distinto del registro de texto.

No se mezcla código de AnimalGame ni de otras interfaces.

## ChatWnd — estructura comprobada

Fuente `ChatWnd.uc`: 50,072 bytes, 1,341 líneas. XML `ChatWnd.xml`: 17,654 bytes.

- 30 declaraciones var (incluidos miembros del struct) y 31 funciones.
- Registra eventos 540, 1500, 550, 560, 570, 571, 572, 2450 y 1710.
- Handles: `NormalChat`, `TradeChat`, `PartyChat`, `ClanChat`, `AllyChat`, `SystemMsgWnd.SystemMsgList`, `ChatTabCtrl`, `ChatEditBox`, `ChatWnd` y `MessengerBtn`.
- Canales de chat: normal, trade, party, clan y ally; el sistema es una lista separada conectada desde ChatWnd.
- Tamaño base: 348 × 187, anclado abajo a la izquierda, Y = -16.
- ChatEditBox: 293 × 16, longitud máxima 100.
- ChatTabCtrl: 320 × 23.
- Controles XML: ChatFilterBtn, PartyMatchingBtn, MessengerBtn, ChatEditBox, TabBackgroundTexture, ChatTabCtrl y btnAllBlock.
- El código gestiona pestañas, split/merge, envío de mensajes y estados/eventos de chat.

## SystemMsgWnd — estructura comprobada

Fuente `SystemMsgWnd.uc`: 1,424 bytes, 55 líneas, 5 funciones. XML: 3,452 bytes.

- Ventana 348 × 125, anclada abajo a la izquierda.
- Contiene BackTexture con `L2UI_CH3.ChatWnd.chatting_system` y `ChatWindow SystemMsgList`.
- Registra evento 150 y muestra/oculta la ventana según `Game/SystemMsgWnd`.
- Al mostrarse, ancla ChatFilterWnd sobre SystemMsgWnd; al ocultarse, lo devuelve sobre ChatWnd.
- Reubica TutorialBtnWnd, QuestBtnWnd y MailBtnWnd con respecto al panel activo.
- ChatWnd obtiene el handle `SystemMsgWnd.SystemMsgList`, confirmando el vínculo funcional entre ambas ventanas.

Las categorías exactas de muertes, golpes, daño, drops y avisos deben rastrearse por sus IDs/tipos de mensaje y opciones del cliente. No se atribuye una categoría sin verificar la tabla de mensajes y el código que la entrega.

## CustomDamageWnd — indicador visual relacionado

Fuente: 22,723 bytes. XML: 299,705 bytes.

- 17 funciones; eventos 40 y 580.
- XML con ventana raíz, subventanas numeradas `CustomDamage1Wnd` en adelante y texturas Digit...
- Los recursos `Abaddon.DamageIndicatorN` confirman que dibuja indicadores numéricos de daño sobre el juego, no mensajes de texto.
- La semántica de cada evento/variante requiere rastrear OnEvent, SetValue y temporizadores antes de modificar.

## Próximas tareas

1. Trazar los eventos y llamadas que actualizan canales y SystemMsgList; clasificar mensajes de muertes, golpes, daño y drops con evidencia.
2. Resolver variables saneadas usando mapa FName y referencias del mismo paquete.
3. Documentar scroll, filtros, pestañas, anclajes, colores y opciones persistentes.
4. Trazar CustomDamageWnd hasta texturas y temporizadores para distinguir variantes solo cuando el código lo demuestre.
5. Probar cambios solo en copias y exigir round-trip XDAT exacto.

## Límites

Inspección estática de fuentes y XML del artefacto automatizado. No se editó ningún archivo, no se recompiló Interface.u, no se cambió XDAT y no se probó en cliente. Los originales permanecen intactos.
