# Base de conocimiento técnica — Interface.u e interface.xdat de L2Sudamerica

**Proyecto:** Methos (repositorio independiente).  
**Cliente de referencia:** L2Sudamerica, Lineage II Interlude.  
**Fecha de inicio:** 2026-11-14.  
**Objetivo:** construir conocimiento verificable y acumulativo de cada paquete `.u`, clase UnrealScript, ventana, control, recurso, evento, opción, dependencia y estructura XDAT. Esta documentación es una base de ingeniería para futuras mejoras compatibles con el cliente; no es una autorización para alterar mecánicas del servidor.

> Regla principal: no inventar nombres ni semánticas. Separar evidencia directa, inferencia y preguntas abiertas. Mantener los binarios originales inmutables y trabajar siempre en copias.

## 1. Paquete de referencia y evidencia

Los archivos originales de L2Sudamerica están versionados en:

- `reference-data/packages/l2sudamerica/Interface.u` — 987,258 bytes.
- `reference-data/packages/l2sudamerica/interface.xdat` — 940,381 bytes.

El workflow [L2Sudamerica Interface Audit #43579091099](https://github.com/Methos48/Methos/actions/runs/43579091099) produjo el artefacto `L2Sudamerica-Interface-Audit` (artifact ID `14107214383`). Ese artefacto contiene los paquetes descifrados, fuentes UELib, XML de XDAT, mapas de nombres, inventarios CSV/JSON y logs.

### 1.1 Protección y descifrado

- Protección identificada: SmartCrypt 1.5.2.2 + Lineage2Ver111.
- Marcador SmartCrypt: `FF ED FA CE`; selector seed `0x01`; estado `0x00`; clave interna de 24 bytes.
- El workflow verificó round-trip exacto de ambas capas de protección para los dos binarios originales.
- `Interface.u` descifrado: 987,230 bytes; Unreal/Licensee 123/37; SHA-256 `877a6873fecfcb141b603010524f4601634ab5f31de5ae3371a1705b76f5dbc9`.
- `interface.xdat` descifrado: 940,353 bytes; SHA-256 `ce63c0c1972812621d4553b7e2dba8041bd917e3c1d220a25cdd11599777bafb`.
- XDAT XML → XDAT fue reconstruido con coincidencia binaria exacta en el pipeline auditado.

**Límite:** este round-trip demuestra fidelidad de descifrado/reconstrucción sin cambios. No demuestra todavía que una modificación de UnrealScript compile, que un paquete editado se pueda reproteger o que el cliente lo cargue.

## 2. Inventario cuantitativo del paquete

El artefacto contiene los siguientes inventarios estructurados:

| Recurso | Cantidad |
|---|---:|
| Clases UnrealScript exportadas | 150 |
| Clases con funciones presentes en inventario CSV | 144 |
| Declaraciones de variables de clase | 1,028 |
| Constantes | 372 |
| Firmas de funciones | 2,245 |
| Ventanas XDAT | 169 |
| Shortcuts XDAT | 8 |
| Controles XDAT con nombre | 4,092 |

Los CSV son índices de navegación, no una sustitución de las fuentes completas. La diferencia entre 150 clases exportadas y 144 clases presentes en el inventario de funciones no debe interpretarse como seis clases vacías sin revisar cada export y el criterio del extractor.

### 2.1 Archivos de inventario del artefacto

- `structured-inventory/class-structure-inventory.json`: estructura por clase.
- `structured-inventory/class-function-inventory.csv`: clase, línea y firma de función.
- `structured-inventory/class-variable-inventory.csv`: clase, línea, tipo, identificador saneado, FName original, longitud de array y declaración.
- `structured-inventory/class-constant-inventory.csv`: constantes por clase.
- `structured-inventory/xdat-control-inventory.csv`: ventana, XML, tipo de elemento, nombre, dimensiones, anclajes y coordenadas.
- `decoded-u/sources-include-tail/`: fuentes UnrealScript exportadas.
- `decoded-u/methos_name_map.json`: mapa de FNames y nombres recuperados/conservados.
- `decoded-xdat/interface.xdat.include-tail-xml/windows/`: XML de ventanas.
- `decoded-xdat/interface.xdat.include-tail-xml/shortcuts/`: XML de shortcuts.
- `decoded-xdat/interface.xdat.include-tail-xml/__xdat_meta.json`: metadatos del XDAT.

## 3. Qué significa conocer un archivo U

Cada paquete `.u` debe documentarse en capas, no solo como una lista de clases:

1. **Cabecera y versión:** package flags, Unreal version, licensee version, GUID, generación, nombres/imports/exports.
2. **Tabla de nombres (FName):** índice, texto, número/instancia, flags y usos. Conservar el índice original aunque se recupere un nombre legible.
3. **Imports:** paquete, clase, objeto y ruta externa que la clase necesita.
4. **Exports:** índice, clase padre, super, outer, nombre, flags, serial size/offset y datos serializados.
5. **Clases:** herencia, estados, interfaces, variables, constantes, structs, enums, delegates y defaults.
6. **Funciones:** firma, parámetros, retorno, flags, bytecode, llamadas, referencias y flujo de control.
7. **Propiedades:** tipo, tamaño, array dimension, flags, categoría, replication y valor por defecto.
8. **Dependencias:** llamadas a APIs del cliente, paquetes importados, ventanas, controles, texturas y sonidos.
9. **Eventos:** ID, productor, consumidor, estructura de parámetros y efecto visible.
10. **Persistencia:** INI, opciones, variables guardadas, estado de sesión y restauración.
11. **Riesgos:** operaciones de inventario/servidor, acciones destructivas, seguridad, validaciones y comportamiento que no debe alterarse.
12. **Confianza:** evidencia directa, inferencia fuerte, hipótesis o desconocido.

Para cada función, el objetivo es registrar: qué la llama, cuándo se ejecuta, qué lee, qué modifica, qué APIs invoca, qué eventos emite, qué controles actualiza, qué condiciones la bloquean y qué efectos secundarios tiene.

## 4. Qué significa conocer un archivo XDAT

Cada `.xdat` debe tratarse como un árbol de ventanas y controles con relaciones, no como una colección de coordenadas:

1. Cabecera, versión, tablas, offsets, índices y regiones de datos.
2. Ventanas: nombre, padre, tamaño, posición, anclaje, visibilidad, orden Z y flags.
3. Controles: clase/tipo, nombre, parent, dimensiones, offsets, anclajes, estado y propiedades.
4. Recursos visuales: texturas, atlas, fuentes, colores, materiales y referencias.
5. Enlaces con UnrealScript: `GetHandle`, `FindWindow`, callbacks, eventos y nombres exactos.
6. Interacciones: click, doble click, drag/drop, hover, teclado, rueda, foco y tooltips.
7. Layout adaptable: anclaje al viewport, escala, resolución, expansión, minimización y ventanas móviles.
8. Estados: visible/oculto, habilitado/deshabilitado, seleccionado, pestaña activa y modos alternativos.
9. Persistencia: posiciones, opciones, filtros y restauración.
10. Compatibilidad: controles desconocidos, IDs sin resolver, duplicados, referencias rotas y datos que el parser no comprende.

Para cada control registrar tanto la definición XDAT como el handle y la función que lo utiliza en U. No asumir que dos controles de nombre parecido son equivalentes.

## 5. Grafo de conocimiento U ↔ XDAT

La unidad de análisis futura será la relación cruzada:

`Package → Class → Function/Event → Handle → Window → Control → Resource`

Además:
- `Event → Producer → Payload → Consumer → UI effect`
- `Option → INI key → callback → runtime branch`
- `User action → validation → client API/request → confirmation → result event`
- `Window → parent/anchor → sibling windows → screen layout`

Un nombre recuperado no se considera confirmado por sí solo. Se necesita evidencia de uso: referencia de bytecode, handle, texto de interfaz, API o coincidencia estructural.

## 6. Prioridad de investigación

La investigación debe cubrir todos los archivos, en paralelo por familias y sin abandonar el inventario completo:

### A. Núcleo visual y navegación
`MainWnd`, `MenuWnd`, `SystemMenuWnd`, `ShortcutWnd`, `ActionWnd`, `OptionWnd`, `DialogBox`, `ToolTip`, `LoadingWnd`, `Logo`, `OnScreenMessageWnd`.

### B. Combate e información en pantalla
`CustomDamageWnd`, `SystemMsgWnd`, `ChatWnd`, `ChatFilterWnd`, `TargetStatusWnd`, `TargetOptionsWnd`, `SkillCastWnd`, `AbnormalStatusWnd`, `StatusWnd`, `DetailStatusWnd`, `PartyWnd`, `PetStatusWnd`, `SummonedStatusWnd`.

### C. Inventario y objetos
`InventoryWnd`, `ItemEnchantWnd`, `WarehouseWnd`, `PrivateShopWnd`, `TradeWnd`, `DeliverWnd`, `MultiSellWnd`, `RecipeBookWnd`, `RecipeManufactureWnd`, `RefineryWnd`, `UnrefineryWnd`, `HennaListWnd`, `HennaInfoWnd`, `PetWnd`.

### D. Habilidades, macros y accesos
`MagicSkillWnd`, `SkillTrainListWnd`, `SkillTrainInfoWnd`, `MacroListWnd`, `MacroEditWnd`, `MacroInfoWnd`, `Shortcut`, `SaveLoadParty`.

### E. Social y comunidad
`ClanWnd`, `ClanDrawerWnd`, `UnionWnd`, `UnionDetailWnd`, `PartyMatchWnd`, `PartyMatchRoomWnd`, `PartyMatchWaitListWnd`, `Messenger` y ventanas de correo/solicitudes.

### F. Mundo, mapas y misiones
`MinimapWnd`, `RadarWnd`, `RadarMapWnd`, `TownMapWnd`, `QuestListWnd`, `QuestTreeWnd`, `GuideWnd`, `ZoneTitleWnd`.

### G. Sistemas especiales
Olympiad, Siege, Manor, Seven Signs, Observer, GM, Petition, Replay, EventMatch y ventanas específicas de L2Sudamerica.

La lista anterior es una agrupación de trabajo, no una enumeración exhaustiva. La fuente autoritativa es el inventario completo de 150 clases y 169 ventanas del artefacto.

## 7. Fichas por archivo y estado

Cada clase U y cada ventana XDAT tendrá una ficha individual o una sección claramente identificable con:

- Identidad y hash/origen del paquete.
- Ruta exacta dentro del artefacto.
- Clase padre e imports relevantes.
- Variables, constantes, structs y enums.
- Lista completa de funciones y análisis de cada una.
- Eventos registrados y tabla de productores/consumidores.
- Handles U ↔ controles XDAT.
- Árbol visual, dimensiones, anclajes y recursos.
- Opciones/INI y estados persistentes.
- APIs y solicitudes que invoca.
- Dependencias con otras clases/ventanas.
- Casos de uso y secuencias de interacción.
- Pruebas realizadas y resultado.
- Preguntas abiertas, nivel de confianza y riesgos.

Estados normalizados:
- **Inventariado:** localizado y contado.
- **Estructura extraída:** clase/XML disponible.
- **Mapeado:** variables, funciones, controles y relaciones listados.
- **Analizado:** flujo funcional explicado con evidencia.
- **Contrastado:** confirmado mediante referencias cruzadas u otra muestra.
- **Probado:** round-trip o prueba controlada reproducible.
- **Modificado:** cambio aislado, con diff y justificación.
- **Validado:** recompilado/reconstruido, reprotegido y probado en cliente.

No elevar el estado por intuición ni por tener un archivo exportado.

## 8. Reglas de preservación y experimentación

- Los binarios originales y sus hashes nunca se sobrescriben.
- Toda edición se realiza en una copia identificada por hash y versión.
- Cada modificación tendrá diff, motivo, dependencias, riesgo y plan de reversión.
- Mantener separado código de cliente, configuración y datos que pertenecen al servidor.
- No cambiar protocolos, cálculos de combate, cooldowns, validaciones ni resultados del servidor bajo el pretexto de modificar la interfaz.
- Los controles que envían solicitudes al servidor deben conservar argumentos, ServerID, cantidades, permisos y confirmaciones.
- Reconstruir XDAT y comparar binariamente cuando no hubo cambios; ante cambios, comparar estructura y validar referencias.
- La compilación UCC, reprotección y carga real son pruebas separadas; ninguna sustituye a las otras.

## 9. Situación inicial de ventanas ya investigadas

- **InventoryWnd:** geometría, 15 slots, categorías, búsqueda, eventos principales, capacidad, orden local, drag/drop y flujos de confirmación documentados. Pendiente resolver nombres saneados, productores de eventos y callbacks completos.
- **ChatWnd:** canales, entrada, pestañas, handles y vínculo con SystemMsgList documentados.
- **SystemMsgWnd:** contenedor de mensajes, anclajes y coordinación con filtros/ventanas auxiliares documentados.
- **ChatFilterWnd:** controles de filtros de sistema, daño, objetos y canales inventariados.
- **CustomDamageWnd:** dos opciones de daño, índices de evento, parámetros, canales visuales, 30 grupos de indicadores y temporizadores identificados. Pendiente demostrar sin ambigüedad la orientación exacta daño hecho/recibido contra la tabla de mensajes y prueba controlada.

Estas fichas están en:
- [Auditoría general de L2Sudamerica](L2SUDAMERICA-INTERFACE-AUDIT.md)
- [InventoryWnd](L2SUDAMERICA-INVENTORYWND-AUDIT.md)
- [Chat, System y daño](L2SUDAMERICA-CHAT-SYSTEM-AUDIT.md)

## 10. Próximas entregas de conocimiento

1. Generar un catálogo maestro de las 150 clases U y 169 ventanas XDAT, con estado y enlaces cruzados.
2. Producir fichas por clase, comenzando por las ventanas prioritarias, sin dejar de inventariar las demás.
3. Resolver los FNames saneados mediante referencias de bytecode, imports, texto de interfaz y comparación entre paquetes.
4. Construir mapas de eventos con productores, consumidores, parámetros y efectos.
5. Construir un índice de recursos visuales: texturas, fuentes, atlas y nombres referenciados.
6. Analizar los paquetes de referencia adicionales por separado para detectar diferencias y recuperar nombres/estructuras, sin transferir conclusiones automáticamente a L2Sudamerica.
7. Mantener un registro de avances y preguntas abiertas en GitHub en cada sesión.

**Estado de esta base:** esquema maestro creado; inventarios y artefacto de referencia disponibles. La comprensión completa de cada clase, función, control y recurso es un trabajo continuo. No se declara terminada la auditoría general ni la capacidad de recompilar y cargar cambios.

## Auditorías de ventanas completadas parcialmente

- [OptionWnd y sus cinco pestañas](L2SUDAMERICA-OPTIONWND-AUDIT.md): análisis UC + XDAT de configuración gráfica, audio, opciones de juego, funciones de interfaz y daño flotante. Incluye inventario de controles, claves de configuración, eventos registrados, conexiones entre ventanas y pendientes de validación.
- [StatusWnd, TargetStatusWnd, PartyWnd, SkillCastWnd y AbnormalStatusWnd](L2SUDAMERICA-STATUS-PARTY-CAST-AUDIT.md): cruce de fuentes UC y XML XDAT, geometría, eventos, acciones de grupo, casteo y estados alterados.
- [DetailStatusWnd, ClanWnd y ClanDrawerWnd](L2SUDAMERICA-DETAIL-CLAN-AUDIT.md): atributos del personaje, resumen del clan, miembros, autorizaciones, rangos, aprendices, guerras y emblemas.


## Auditoría de comercio y almacén

- [Comercio, almacén, tienda privada, envíos y multisell](L2SUDAMERICA-TRADE-WAREHOUSE-SHOP-AUDIT.md) — siete ventanas cruzadas U/XDAT; geometría, controles, eventos registrados, payloads y solicitudes observadas. La correlación de productores y la validación en cliente siguen pendientes.


## Auditoría de macros, recetas y habilidades

- [Macros, recetas y aprendizaje de habilidades](L2SUDAMERICA-MACRO-RECIPE-SKILL-AUDIT.md) — análisis cruzado de diez ventanas U/XDAT, solicitudes API, eventos registrados, edición de macros, fabricación y aprendizaje/encantamiento. La correlación de productores y la validación en cliente siguen pendientes.


## 10. Auditoría de mascotas, invocaciones, pociones y shots

Nueva ficha técnica: [L2SUDAMERICA-PET-SUMMON-POTION-AUDIT.md](L2SUDAMERICA-PET-SUMMON-POTION-AUDIT.md). Incluye AutoPotionsWnd, AutoPotionOptionWnd, PotionsConfigWnd, NPHRN_PotionsWnd, AutoShotItemWnd, PetWnd, PetStatusWnd, SummonedWnd y SummonedStatusWnd; documenta eventos, temporizadores, umbrales HP/MP/CP, ClassID observados, solicitudes de objetos y preguntas abiertas. La auditoría es estática y no implica compilación ni prueba en cliente.


## 11. Auditoría de mapas, radar, misiones y navegación

Nueva ficha: [L2SUDAMERICA-MAP-QUEST-NAVIGATION-AUDIT.md](L2SUDAMERICA-MAP-QUEST-NAVIGATION-AUDIT.md). Documenta MinimapWnd/Expand, MinimapControl, RadarMapWnd, RadarWnd, RadarContainerWnd, QuestTreeWnd, QuestListWnd, QuestBtnWnd, TownMapWnd, ZoneTitleWnd y GMQuestWnd; incluye geometría XDAT, eventos, APIs, vínculo QuestAPI↔minimapa, leyenda de zonas y preguntas abiertas. Análisis estático, sin modificaciones ni pruebas de carga.


## 12. Auditoría social, clan, party matching y peticiones

Nueva ficha: [L2SUDAMERICA-SOCIAL-PARTY-CLAN-PETITION-AUDIT.md](L2SUDAMERICA-SOCIAL-PARTY-CLAN-PETITION-AUDIT.md). Incluye ClanWnd/ClanDrawerWnd, UnionWnd/UnionDetailWnd, PartyWnd normal/compacta, PartyMatch y salas/listas de espera, SaveLoadParty, Petition, BoardWnd, MailBtnWnd y SSQMainBoard. Registra geometría XDAT, controles, eventos, solicitudes administrativas, operaciones de party, persistencia INI y preguntas abiertas. Análisis estático; no se editaron paquetes.


## 13. Auditoría de asedios, Manor y Olympiad

Nueva ficha: [L2SUDAMERICA-SIEGE-MANOR-OLYMPIAD-AUDIT.md](L2SUDAMERICA-SIEGE-MANOR-OLYMPIAD-AUDIT.md). Documenta SiegeInfoWnd y sus solicitudes SiegeAPI; nueve ventanas/rutas de Manor para semillas, cosechas, configuración y venta; y paneles Olympiad/Observer. Incluye eventos, payloads observados, geometría XDAT, dependencias y preguntas abiertas. Análisis estático; no se modificaron ni probaron binarios.


## 14. Auditoría de sistemas especiales

Nueva ficha: [L2SUDAMERICA-SPECIAL-SYSTEMS-AUDIT.md](L2SUDAMERICA-SPECIAL-SYSTEMS-AUDIT.md). Incluye EventMatch GM/Observer, replay local, DuelManager, HeroTower, herramientas GM, ReportWnd, PC Café y cupones. Se identificaron los controles XDAT, eventos, payloads y solicitudes; también se marcaron funciones GM de EventMatch que están vacías en la fuente exportada, sin asumir una implementación inexistente. Análisis estático, sin cambios ni pruebas en cliente.


## 15. Auditoría de UICommonAPI

Nueva ficha: [L2SUDAMERICA-UICOMMONAPI-AUDIT.md](L2SUDAMERICA-UICOMMONAPI-AUDIT.md). Analiza la capa compartida UICommonAPI: wrappers de ventana/diálogo, parser de ItemInfo, parser LVDataRecord, mensajes, eventos internos, ordenación local de inventario y wrappers de primitivas. Se marcan funciones con cuerpo vacío en la exportación y las dependencias que requieren análisis de bytecode. Análisis estático; sin cambios en binarios.


## 16. Auditoría de DialogBox

Nueva ficha: [L2SUDAMERICA-DIALOGBOX-AUDIT.md](L2SUDAMERICA-DIALOGBOX-AUDIT.md). Documenta los ocho tipos de diálogo, exclusión de instancia simultánea, identidad del llamador, ID de confirmación, eventos globales 1710/1720, acción predeterminada de Enter, entrada numérica, barra temporizada y estructura XDAT. Se identifican riesgos de concurrencia y consumo de respuestas entre ventanas. Análisis estático; sin modificar ni probar binarios.


## 17. Auditoría del sistema de shortcuts

Nueva ficha: [L2SUDAMERICA-SHORTCUT-SYSTEM-AUDIT.md](L2SUDAMERICA-SHORTCUT-SYSTEM-AUDIT.md). Documenta ShortcutWnd y Shortcut: cinco barras/páginas, 12 slots por página, diez páginas, orientación horizontal/vertical, panel joypad, expansión, bloqueo/movimiento, persistencia y comandos globales de UI. Incluye eventos 590–660 y riesgos de no confundir índice global ShortcutID con slot local. Análisis estático; sin cambios ni pruebas en cliente.


### Addendum a la auditoría del sistema de shortcuts — bindings y teclas rápidas

La ficha de Shortcut fue ampliada con el cruce de `Shortcut.uc` e `InterfaceAI_KeySettingWnd.uc`: tres bindings independientes (teclas 1–12, QWERTY y F1–F12), paneles configurables por INI, comandos `/useshortcut` y `/useshortcutforce`, y la frontera no reconstruida de `NWindow.ShortcutAPI`. También se registraron dos puntos que requieren prueba/bytecode: la rama Force de Bind3 y etiquetas duplicadas en el switch de comandos. No se atribuye a “force” capacidad de saltarse restricciones del servidor.


## 18. Auditoría de SublimityShortcut

Nueva ficha: [L2SUDAMERICA-SUBLIMITY-SHORTCUT-AUDIT.md](L2SUDAMERICA-SUBLIMITY-SHORTCUT-AUDIT.md). Documenta la variante de cuatro paneles heredada de SublimityItem, sus 120 índices, páginas, persistencia propia y diferencias con ShortcutWnd estándar. Se marcan como pendientes los eventos 631/25002 y la lógica incompleta de SublimityShortcut; no se atribuyen semánticas no demostradas.


## 19. Auditoría de ToolTip y SublimityItem

Nueva ficha: [L2SUDAMERICA-TOOLTIP-SUBLIMITYITEM-AUDIT.md](L2SUDAMERICA-TOOLTIP-SUBLIMITYITEM-AUDIT.md). Cubre el evento global 2920, el catálogo de variantes de tooltip, construcción dinámica DrawList, inventario/precios/refinería, y el despacho de eventos de la clase base SublimityItem. Se mantienen pendientes los productores, contratos nativos, bytecode y ramas duplicadas/stubs.


## 20. Auditoría de UISublimityCommonAPI y SublimityTimerActor

Nueva ficha: [L2SUDAMERICA-UISUBLIMITY-COMMONAPI-AUDIT.md](L2SUDAMERICA-UISUBLIMITY-COMMONAPI-AUDIT.md). Documenta la jerarquía común Sublimity, eventos, actor temporizador, preferencias separadas por personaje y las rutas bypass PIN/bot-check. Estas últimas se identifican como solicitudes al servidor y quedan fuera de cambios visuales; no se ejecutaron.
