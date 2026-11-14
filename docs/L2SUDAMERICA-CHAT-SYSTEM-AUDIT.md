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


## Seguimiento específico: daño hecho y daño recibido (14-11-2026)

Se inspeccionó directamente `CustomDamageWnd.uc` del artefacto auditado, incluyendo `OnLoad`, `OnEvent`, el parser del evento 580, `SetValue`, `OnTimer` y el manejo de ventanas/timers.

### Flujo confirmado

- `CustomDamageWnd` registra los eventos 580, 40 y 280. El evento 580 es el que procesa los mensajes de combate: lee los campos `Index`, `Param1` y `Param2`.
- La visualización global está condicionada por `Options/ShowDamage`.
- Hay dos controles independientes en OptionWnd: `Cb_Damage` y `Cb_EDamage`. OptionWnd los obtiene como CheckBoxHandle separados.
- Rama `Cb_Damage`: índices 35 y 1130; toma el valor numérico de `Param1` y lo envía al canal interno 1.
- Rama `Cb_EDamage`: índices 36 y 37; toma `Param2` y lo envía al canal interno 2.
- La misma rama `Cb_EDamage` acepta además los índices 296, 297, 333, 686 y 812, usando `Param1`.
- Los canales 1 y 2 usan grupos de ventanas/dígitos distintos. La rutina `SetValue` convierte el valor en dígitos y sufijos, y `OnTimer` anima/mueve/oculta las ventanas. No es una simple modificación del texto de SystemMsgList.
- El evento 40 oculta las 30 ventanas de daño; el evento 280 está registrado, pero no tiene rama explícita en el switch OnEvent mostrado, por lo que su función sigue sin aclararse.

### Interpretación y cautelas

La estructura del código confirma que el cliente separa dos clases de daño mediante opciones distintas, pero los nombres internos `Cb_Damage` / `Cb_EDamage` por sí solos no bastan para asignar definitivamente cuál corresponde a daño recibido y cuál a daño hecho. Tampoco se deben traducir todavía los índices adicionales a crítico, curación u otros eventos sin consultar la tabla de mensajes exacta de L2Sudamerica.

### Próxima comprobación

1. Recuperar la tabla `SystemMsg`/mensajes que corresponde exactamente a esta compilación y resolver los textos de los índices 35, 1130, 36, 37, 296, 297, 333, 686 y 812.
2. Inspeccionar los controles y textos de `OptionWnd.xml` para confirmar la presentación visible de las dos opciones.
3. Contrastar las dos ramas con capturas/prueba controlada: recibir un golpe y golpear un objetivo, sin modificar el cliente original.
4. Documentar el significado exacto de los índices y los parámetros antes de cambiar colores, posición, tamaño o filtros.

**Estado:** flujo de eventos y separación de canales identificados estáticamente. La asignación semántica definitiva «recibido/hecho» y la prueba dentro del cliente siguen pendientes. No se ha editado ni recompilado ningún binario.


### Confirmación de etiquetas visibles en OptionWnd.uc

La revisión de los manejadores de cambios de opciones resolvió los nombres persistidos:

- `Cb_Damage` guarda `Damage / Normal Damage`.
- `Cb_EDamage` guarda `Damage / Enemy Damage`.
- `Cb_HP`, `Cb_MP`, `Cb_CP`, `Cb_EXP`, `Cb_Miss`, `Cb_Resist`, `Cb_Block`, `Cb_Critical`, `Cb_Critical2`, `Cb_Overhit`, `Cb_Successful`, `Cb_Failed` y `Cb_Spoiled` son opciones independientes del mismo panel.
- La activación maestra es `Options / ShowDamage`; las dos opciones de daño no sustituyen ese interruptor general.

Esto confirma que la interfaz distingue explícitamente **Normal Damage** y **Enemy Damage**. La lectura funcional más probable es daño normal generado por el jugador frente a daño atribuido al enemigo, pero se mantiene pendiente comprobar la orientación exacta (hecho/recibido) contrastando los textos de SystemMsg para los índices 35, 1130, 36 y 37. Los nombres de opciones, aislados, no demuestran quién es el atacante en cada mensaje.


## Auditoría profunda — pipeline de daño y controles (14-11-2026)

### Flujo de números flotantes

La inspección de `CustomDamageWnd.uc` confirma:

- `OnLoad` registra 580, 40 y 280. Inicializa 30 grupos visuales, cada uno con 10 texturas de dígitos: 300 handles de textura en total.
- Cada grupo `CustomDamage1Wnd`…`CustomDamage30Wnd` tiene diez elementos `DigitXY`; las texturas usan familias `Abaddon.DamageIndicator1`…`DamageIndicator30`.
- Evento 580 solo se procesa si `Options/ShowDamage` está activo.
- Parser: `Index`, `Param1`, `Param2`.
- Índices 35 y 1130: opción `OptionWnd.Cb_Damage`, número desde `Param1`, canal visual 1.
- Índices 36 y 37: opción `OptionWnd.Cb_EDamage`, número desde `Param2`, canal visual 2.
- Índices 296, 297, 333, 686 y 812: también `Cb_EDamage`, número desde `Param1`, canal 2.
- El código no acumula un total: cada evento crea un indicador, y cuando no hay slot disponible acumula el valor en una cola por canal y programa un timer.
- Cada indicador se ancla a coordenadas relativas a la ventana de juego, mueve su posición y se oculta con temporizadores. Hay lógica de dirección/desplazamiento y offsets distintos para los canales y para eventos especiales.
- `SetValue` separa dígitos, usa prefijos `#+`, `#`, y sufijos `M`, `B`, `R` para ciertas categorías. El número de texturas disponibles limita la representación visual por indicador.
- Evento 40 oculta los 30 grupos. El 280 está registrado, pero no tiene caso explícito en el switch de `OnEvent`; no asignar significado todavía.

### Panel de filtros del chat

`ChatFilterWnd.xml` incluye controles separados para sistema, daño, objetos y canales: `CheckBoxSystem`, `CheckBoxDamage`, `CheckBoxItem`, Normal, Shout, Pledge, Party, Trade, Whisper, Ally, Union, Hero, además de `SystemMsgBox`, `DamageBox`, `ItemBox` y Command. La presencia de estos controles confirma que el registro textual puede filtrarse por categorías independientemente de los números flotantes de CustomDamageWnd.

### Pendiente semántico

Las opciones están identificadas como `Normal Damage` y `Enemy Damage`, pero aún no se ha demostrado con la tabla de mensajes de esta misma compilación cuál de las ramas es daño causado por el jugador y cuál recibido por él. No cambiar nombres, colores ni orientación hasta resolver los IDs y verificar en cliente.
