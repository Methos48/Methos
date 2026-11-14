# L2Sudamerica — auditoría profunda de estado, objetivo, grupo, casteo y buffs

**Estado:** análisis estático cruzado de UnrealScript recuperado y XML XDAT.  
**Archivos:** `StatusWnd`, `TargetStatusWnd`, `PartyWnd`, `SkillCastWnd`, `AbnormalStatusWnd`.  
**Límite:** no se ha ejecutado el cliente ni validado visualmente en juego. Los nombres `MethosRecoveredName_N` son símbolos saneados por el proceso de recuperación; no se presentan como nombres originales.

## 1. StatusWnd — estado propio

### Estructura XDAT
- Ventana 176 × 68, anclaje superior izquierdo en (0,0).
- Elementos principales: `UserName`, `StatusWnd_LevelTextBox`, `CPBar`, `HPBar`, `MPBar`, `ed_texHero` y tres texturas de fondo.
- Los tamaños negativos de algunos controles son valores relativos del layout XDAT; no deben interpretarse como dimensiones físicas negativas.

### Fuente y flujo
- `OnLoad` registra eventos 70, 180, 190, 200, 210, 220, 230 y 240.
- La rutina de actualización obtiene `UserInfo` mediante `GetPlayerInfo`; guarda `UserInfo.nID` y actualiza CP/HP/MP, experiencia/nivel y nombre.
- Usa `UIAPI_STATUSBARCTRL.SetPoint` para CP/HP/MP y `SetPointExp` para EXP.
- El icono de estado muestra textura Hero si `bHero`; si no, muestra Noble cuando `bNobless`; en caso contrario queda vacío.
- Evento 180 reconstruye toda la información del jugador.
- Eventos 200, 210, 220, 230 y 240 pasan un payload con `ServerID`; si corresponde al jugador local o el estado interno indica que faltaba información, vuelve a consultar `GetPlayerInfo`.
- Evento 70 procesa campos `Type` y valores asociados; el significado exacto de cada tipo debe cruzarse con el catálogo global de eventos.
- Clic izquierdo en una zona concreta del marco ejecuta `RequestSelfTarget()`. Es evidencia directa de que la ventana de estado propio permite seleccionarse a sí mismo.

## 2. TargetStatusWnd — objetivo

### Estructura XDAT
- Ventana base 176 × 46, anclada arriba al centro.
- Variante expandida usa fondo de experiencia de 126 px de alto.
- Controles: `UserName`, `RankName`, datos de clan/alianza y emblemas, `NpcInfo`, barras `barHP`/`barMP`, botones `FastInviteBtn`, `FastTradeBtn`, `btnClose`, y subventana de skill/casteo `BackTex2` con `SkillName` y texturas del slider.
- El layout contiene una zona `BackTex2.tb_slider` con tres piezas de textura (Left/Center/Right), y una segunda composición de fondo.

### Eventos y lógica observada
- Registra eventos 980, 990, 190, 210, 200, 220 y 300.
- Al cargar inicia temporizadores 1000 y 2000 ms. Los timers gestionan refrescos/ocultación de información temporal; no equivalen a un sondeo del servidor cada segundo.
- El evento principal de actualización procesa identificadores de objetivo y una tabla extensa de IDs de NPC/objetos especiales (entre ellos 2039, 2150–2164, 26050–26064, 22036–22038, 2033, 2008, 2009 y 22369). La tabla alimenta presentaciones especiales del objetivo; su significado individual debe contrastarse con datos de NPC y no inferirse solo por número.
- Maneja estados de objetivo jugador/NPC, texto de nombre/rango, clan/alianza, barras de HP/MP, colores de nombre y tooltips.
- Botones:
  - `FastInviteBtn` llama a `RequestInviteParty(UserInfo.Name)`.
  - `FastTradeBtn` llama a `RequestStartTrade(targetID)`.
  - `btnClose` ejecuta `RequestTargetCancel()`.
- La fuente implementa clics sobre el marco y el control de nombre; quedan por resolver todas las ramas de interacción para documentar exactamente cada gesto.
- Se observan funciones específicas para colorear nombres (YELLOW, SYSTEM, AMBER, WHITE, Gray, Magenta, Blue, COMMANDER, Ivory, Red). No asumir que todos esos colores son configurables por el usuario.

## 3. PartyWnd — grupo

### Estructura XDAT
- Ventana 176 × 416, anclada arriba a la izquierda con desplazamiento Y=152.
- Controles globales `btnBuff` y `btnCompact`.
- Tiene filas `PartyStatusWnd0` … `PartyStatusWnd9` en XML (diez instancias visibles en el árbol recuperado), mientras que la fuente declara arrays de nueve elementos y recorre índices 0…8. Esta discrepancia debe conservarse como hallazgo abierto: puede deberse a una fila adicional del XML, índice especial o diferencia entre el control de fondo y los miembros reales.
- Cada fila enlaza nombre, icono de clase, icono de líder, estados buff/debuff, barras CP/HP/MP y textura blackout.
- El código ancla los controles de iconos y nombre de cada fila en tiempo de carga.

### Eventos
- Registra 1000, 1140, 1150, 1160, 1170, 1180 y 40.
- Los eventos 1140, 1150 y 1160 se delegan a rutinas separadas de actualización; 1170 y 1180 también tienen ramas propias.
- Al entrar en estado reinicia flags internos y llama la rutina que reconstruye el grupo.
- Lee `Game/SmallPartyWnd` y `Options/CustomPartyIcons`; esta última controla el uso de iconos personalizados.
- El botón `BtnBuff` controla la visualización/acción relacionada con buffs; `BtnCompact` alterna el modo compacto. El detalle completo de cada subrutina queda pendiente de desofuscación.

### Interacciones de miembros
- La fuente contiene `RequestAttack(memberID, UserInfo.Loc)` y `RequestAction(memberID, UserInfo.Loc)` en una ruta de clic.
- Clic derecho ejecuta `RequestAssist(memberID, UserInfo.Loc)`.
- Los miembros se identifican por IDs guardados en un array; el nombre visual no es la clave de las peticiones.
- Hay lógica de selección de modo/acción (valores internos 0, 1 y 2), además de drag/selection con eventos de botón abajo/arriba.
- La fila del grupo es por tanto un control interactivo, no solo un panel informativo.

## 4. SkillCastWnd — barra de casteo propio

### Estructura XDAT
- Ventana 254 × 29, anclada abajo al centro con desplazamiento Y=-310.
- Contiene `SkillGauge`, `w_slider`, `w_slider2`, textura `t_Slider`, texto `tb_Title`, `t_Highlight`, `t_Frame` y `t_Movable`.
- Puede dejarse movible según `Interface/enableOwnCastMovable`; si no, se fija al ancla inferior central.

### Eventos y temporización
- Registra eventos 290 y 580; la rama 580 analiza `Index` y descarta índices mayores o iguales a 27, y luego continúa por una ruta compartida con el evento 50 para ocultar/resetear la barra. El flujo exacto del switch debe revisarse al reconstruir el bytecode fuente.
- Evento 290 comprueba `Options/SkillCast`; si está habilitado, procesa el payload de casteo.
- Usa timer ID 1 para ocultar la barra después del intervalo indicado por el evento; al terminar, oculta gauge y ventana y mata el timer.
- La rutina de inicio obtiene información del jugador y skill, actualiza nombre y progreso visual, y anima el slider mediante temporizador. La rutina contiene llamadas a funciones de usuario y a `UserInfo`; los parámetros exactos requieren desofuscar variables locales.

## 5. AbnormalStatusWnd — iconos de estados alterados

### Estructura XDAT
- Ventana contenedora 38 × 26, desplazada 176 px desde el borde superior izquierdo.
- Un control `StatusIcon` de 26 × 26.
- Aunque el XML raíz es pequeño, la lógica UC gestiona una lista de iconos/estados con paginación, agrupación y filtros; el contenedor no representa el tamaño total de la lista que puede desplegarse.

### Eventos
- Registra 950, 960, 970, 4444, 40, 50, 310 y 1900.
- 950, 960 y 970 se dirigen a tres rutinas distintas de actualización de estados.
- 40 y 50 ejecutan rutas de limpieza/actualización al cambiar el estado de juego.
- 310 actualiza la presentación asociada a objetos/estados; 1900 procesa otra ruta de actualización.
- 4444 actualiza el estado de la ventana.
- Inicializa varios índices en -1 y una lista dinámica de IDs; esto es consistente con seguimiento de slots/estados, pero no basta para asignar significado exacto a cada variable saneada.
- Lee `Options/Buff Size` en múltiples rutas, por lo que el tamaño de iconos es una opción compartida y aplicada durante la reconstrucción de la lista.
- Declara constantes 4, 12, 1250 y 4443. No asignarles significado semántico definitivo hasta encontrar todos sus usos y el productor/consumidor correspondiente.

## 6. Relaciones entre ventanas

- `StatusWnd` obtiene los datos del personaje local mediante `GetPlayerInfo`.
- `TargetStatusWnd` representa el objetivo actual y expone acciones de invitar, comerciar y cancelar objetivo.
- `PartyWnd` muestra hasta nueve miembros según los arrays del script y ofrece acciones por ID (atacar, acción, asistir); el XML contiene diez filas y queda abierta la discrepancia.
- `SkillCastWnd` depende de la opción `Options/SkillCast`, y su posición puede depender de `Interface/enableOwnCastMovable`.
- `AbnormalStatusWnd` consume eventos de estado y la preferencia `Options/Buff Size`.
- `OptionWnd` configura varias de estas funciones: `SkillCast`, `CustomPartyIcons`, `Buff Size`, y las opciones de party.
- La auditoría de `OptionWnd` anterior describe las claves de configuración; este documento registra sus consumidores observados.

## 7. Pendientes para cerrar estas cinco ventanas

1. Resolver nombres y tipos de las variables saneadas con flujo de datos, no por intuición.
2. Reconstruir productores y payloads de los eventos 40, 50, 70, 180–240, 290, 300, 310, 950–990, 1000, 1140–1180, 1900 y 580.
3. Resolver la discrepancia entre diez filas XDAT de PartyWnd y arrays 0…8 del script.
4. Mapear los IDs de NPC especiales de TargetStatusWnd con tablas de NPC y recursos.
5. Describir el algoritmo completo de paginación/orden de buffs y debuffs.
6. Verificar la rutina de casteo y temporización, incluyendo interrupción, finalización y ocultación.
7. Comprobar geometría/anclajes en el cliente sin editar los originales.
8. No cambiar lógica del servidor ni presentar acciones de UI como cambios de mecánica.

## 8. Fuentes de evidencia

- `decoded-u/sources-include-tail/StatusWnd.uc`
- `decoded-u/sources-include-tail/TargetStatusWnd.uc`
- `decoded-u/sources-include-tail/PartyWnd.uc`
- `decoded-u/sources-include-tail/SkillCastWnd.uc`
- `decoded-u/sources-include-tail/AbnormalStatusWnd.uc`
- `decoded-xdat/interface.xdat.include-tail-xml/windows/StatusWnd.xml`
- `decoded-xdat/interface.xdat.include-tail-xml/windows/TargetStatusWnd.xml`
- `decoded-xdat/interface.xdat.include-tail-xml/windows/PartyWnd.xml`
- `decoded-xdat/interface.xdat.include-tail-xml/windows/SkillCastWnd.xml`
- `decoded-xdat/interface.xdat.include-tail-xml/windows/AbnormalStatusWnd.xml`

**Conclusión:** estas cinco ventanas forman el núcleo de estado del personaje, objetivo, grupo, casteo y efectos. Se han cruzado scripts y estructuras XDAT; la auditoría no declara estas ventanas terminadas porque aún faltan productores de eventos, desofuscación de símbolos y validación en cliente.
