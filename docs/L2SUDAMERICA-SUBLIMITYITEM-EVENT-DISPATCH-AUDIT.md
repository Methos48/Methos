# Auditoría técnica — SublimityItem: despachador común de eventos

**Cliente:** L2Sudamerica / Lineage II Interlude.  
**Fuente examinada:** decoded-u/sources-include-tail/SublimityItem.uc del artefacto L2Sudamerica-Interface-Audit.  
**Método:** lectura estática y contraste con UISublimityCommonAPI.uc, SublimityShortcutWnd.uc y SublimityShortcut.uc.  
**Límite:** no se modificó ni compiló ningún paquete; no se ejecutó el cliente.

## 1. Hallazgo arquitectónico

SublimityItem implementa un despachador común OnEvent(int, string) que convierte IDs de evento en llamadas a métodos virtuales de la clase concreta.

Patrón observado:

evento global + payload string → switch de SublimityItem.OnEvent → parser de campos → hook virtual → implementación de la ventana derivada.

Esto explica por qué las clases derivadas pueden registrar pocos eventos y recibir una familia más amplia de mensajes a través de la clase base. La presencia de un caso en el switch demuestra que el dispatcher conoce el ID; no demuestra que cada derivada implemente el hook.

## 2. Eventos comunes y payloads reconstruidos

| ID | Campos leídos en la base | Hook invocado | Observación |
|---:|---|---|---|
| 1910 | TotalPoint, AddPoint, PeriodType, RemainTime, PointType | _946 | Cinco enteros confirmados; significado funcional pendiente |
| 700 | — | _945 | Sin payload parseado |
| 710 | QuestID, Level, Completed | _944 | Tres enteros |
| 720 | — | _943 | Sin payload parseado |
| 1520 | QuestID | _942 | Un entero |
| 90 | Command | _728 | Texto de comando |
| 40 / 50 | — | _940 / _939 | Eventos sin payload |
| 950 / 960 / 970 | Max + SkillID_n, SkillLevel_n, RemainTime_n, Name_n, IconName_n, Description_n | _938 / _937 / _936 | Tres listas de StatusIconInfo |
| 630 | ShortcutID | _730 | Hook de shortcut; cuerpo exportado vacío |
| 631 | ShortcutID | _729 | Ruta distinta de 630 |
| 640 | ShortcutPage | _731 | Página |
| 660 | OnOff | _908 | Semántica exacta pendiente |
| 650, 590, 600, 610, 620 | — | _735, _904, _902, _736, _736 | Sin payload parseado |
| 150, 160, 180 | — | _897, _895, _893 | Sin payload parseado |
| 2530 | shopID | _935 | Identificador de tienda |
| 2540 | index, type, ID, slotBitType, itemType, itemCount, OutputRefineryOp1/2 | _932 | Ocho enteros; datos de slot/objeto |
| 2550 | ID, refineryOp1/2, ClassID, count, enchant, inputRefineryOp1/2 | _930 | Ocho enteros; ruta de datos de objeto/refinería |
| 2560 | — | _929 | Sin payload parseado |
| 25100 | BuffLimit | _855 | Inicio de lista de buffs |
| 25101 | Category, SkillID, SkillLevel, Available | _852 | Elemento de lista de buffs |
| 25102 | — | _849 | Fin de lista |
| 1710 / 1720 | — | _948 / _947, solo si _63() | Respuestas compartidas de diálogo con comprobación de propietario |
| 100000 | — | _877 | Hook sin payload |
| 100001 | AttemptsLeft | _876 | Contador |
| 100002–100005 | — | _875, _874, _873, _872 | Hooks sin payload |
| 100006 / 100007 | Time, Tex1, Tex2, Tex3, Tex4 | _871 / _870 | Tiempo y cuatro índices de textura |
| 100008 | — | _869 | Hook sin payload |
| 100009 | SkillID, CastTime | _868 | Datos de casteo |
| 100010 | — | _867 | Hook sin payload |
| 100011 | MessageCount | _865 | Contador de mensajes |
| 10002 | QuestID, Level | _864 | Caso singular 10002, no 100012 |
| 200000 | Count | _863 | Contador |
| 100012 / 100013 | — | _891 / _889 | Hooks sin payload |
| 100014 / 100015 | — | _861 / _860 | Hooks sin payload |
| 100016 | payload string completo | _859 | No parsea campos localmente |
| 100017 / 100018 / 100019 | — | _858 / _857 / _856 | Hooks sin payload |
| 25000 | Time | _887 | Temporización |
| 25001 | key, state, X, Y | _885 | Tecla/estado/coordenadas |
| 25002 | key, state | _737 | Tecla/estado; SublimityShortcut registra este evento |
| 25003 | X, Y, Z, FOV | _847 | Parámetros con nombres de cámara/campo visual |
| 25004 | key, state, X, Y | _884 | Segunda ruta tecla/estado/coordenadas |

Los nombres de hooks se conservan como identificadores saneados. No se asignan nombres funcionales inventados. Los campos y tipos son evidencia directa de ParseInt, ParseString y ParseFloat.

## 3. Listas de estados alterados

La ruta 950/960/970 llama a un parser común:
- Lee Max.
- Itera desde 0 hasta Max-1 mediante wrappers nativos __NFUN_150__ y __NFUN_165__.
- Por cada índice lee SkillID, SkillLevel, RemainTime, Name, IconName y Description.
- Fija Size=24, BackTex=L2UI.EtcWndBack.AbnormalBack y bShow=true.
- Configura bEtcItem y bShortItem según la variante del evento.

Esto demuestra que el mismo formato alimenta tres presentaciones diferenciadas. La diferencia de UI depende de los hooks concretos de la clase derivada.

## 4. Lista de buffs

Los eventos 25100, 25101 y 25102 forman una secuencia Start / Item / End por la estructura y los nombres literales enviados a la función de notificación:
- OnBuffListStart
- OnBuffListItem
- OnBuffListEnd

La clase parsea BuffLimit al inicio y Category, SkillID, SkillLevel y Available por elemento. La notificación ocurre solo si el booleano de instancia _3846 está activo. El hook que consume cada elemento aparece como stub en esta exportación. Está confirmado el contrato del payload, no el efecto final sobre la ventana.

## 5. Enlace con Shortcut

Queda confirmado que SublimityItem.OnEvent tiene un caso explícito para 631 y extrae ShortcutID, mientras que 630 también extrae ShortcutID pero llama a otro hook. No es correcto colapsar ambos IDs.

SublimityShortcutWnd registra 631 además de 630, 640 y otros eventos. SublimityShortcut registra 25002, cuyo payload base es key/state. Esto conecta las auditorías previas, pero todavía no identifica quién produce los eventos ni la semántica de cada hook.

## 6. Persistencia y temporizador compartido

UISublimityCommonAPI crea un objeto SublimityTimerActor durante su inicialización y expone wrappers para preferencias por personaje. SublimityTimerActor.uc exportado solo contiene la declaración de clase extends Actor notplaceable; no se recuperó su implementación de estados/timers. Los eventos 25000 y 100006/100007 no autorizan a inventar intervalos, callbacks ni garantías de temporización.

## 7. Limitación crítica de la exportación

La mayoría de los hooks de SublimityItem terminan inmediatamente con return. Esto incluye rutas importantes de shortcut, quests, tienda, refinamiento, buffs, cámara y temporizadores. El dispatcher y los parsers están presentes, pero el comportamiento final de muchas rutas reside en overrides no implementados en la fuente exportada, bytecode no reconstruido o código nativo.

Un hook vacío en esta exportación no demuestra que la función original sea vacía. Antes de editar estas clases se requiere contrastar bytecode y tabla de exports/imports.

## 8. Próximos pasos

1. Extraer la tabla de exports/funciones de SublimityItem y derivadas para localizar cuerpos bytecode y flags de override.
2. Buscar los IDs 630, 631, 640, 650, 660, 25000–25004, 25100–25102, 2530–2560 y 100000–100019 en todas las clases U para identificar productores.
3. Correlacionar payloads 2540/2550 con InventoryWnd, Trade/Warehouse/Shop y Refinería sin asumir equivalencia con eventos estándar.
4. Identificar derivadas que implementan hooks de estados alterados, buffs, quests, cámara y mensajes.
5. Resolver SublimityTimerActor mediante bytecode o comparación con otra compilación.
6. Validar en un cliente de prueba sin tocar solicitudes ni reglas del servidor.

## 9. Estado

**Confirmado estáticamente:** despachador OnEvent, mapa de IDs, campos de payload, parser de StatusIconInfo, secuencia de lista de buffs y separación entre eventos 630/631.

**Pendiente:** productores, overrides efectivos, bytecode, comportamiento de timers, contratos nativos y prueba en cliente. Sin modificaciones, compilación ni prueba de runtime.


## Addendum A — contraste de registros y productores visibles en UnrealScript

Se recorrieron las 151 fuentes UC del artefacto buscando RegisterEvent y ExecuteEvent para las familias de IDs descritas arriba. Resultado estático:

- 631: registro localizado en SublimityShortcutWnd.OnLoad.
- 25002: registro localizado en SublimityShortcut.OnLoad.
- 2530, 2540, 2550 y 2560: registros localizados en MultiSellWnd.OnLoad. Esta clase implementa su propio OnEvent y procesa esos cuatro IDs directamente; no hereda de SublimityItem. Por tanto, su coincidencia numérica con los casos del dispatcher SublimityItem no demuestra que MultiSellWnd use esos hooks heredados.
- 100009 y 100010: ExecuteEvent localizados en UISublimityCommonAPI. La API emite 100009 con SkillID y CastTime, y 100010 sin payload.
- No se encontró en las fuentes UC revisadas un productor ExecuteEvent explícito para el resto de IDs del dispatcher. Esto no excluye productores nativos, eventos generados por el cliente o llamadas con IDs indirectos/variables.

### Implicación

Hay que mantener separados tres conceptos: (1) el ID reconocido por SublimityItem.OnEvent, (2) la clase que registra/consume el ID, y (3) el lugar que lo produce. La búsqueda textual solo ha resuelto parte de (2) y (3). Los IDs 2530–2560 ilustran por qué una coincidencia numérica entre dos ventanas no prueba una relación de herencia.

**Nuevo pendiente:** recorrer los bytecode de las funciones que llaman a ExecuteEvent/DispatchEvent y las referencias nativas, además de revisar registros heredados en cada clase derivada.


## Addendum B — jerarquía real de herencia en las 150 fuentes exportadas

Se inspeccionaron las declaraciones `class ... extends ...` de las 150 fuentes UC incluidas en el artefacto y se contrastaron con los nombres de funciones de SublimityItem.

Resultado directo:
- `SublimityItem extends UISublimityCommonAPI`.
- `SublimityShortcut extends SublimityItem`: 3 funciones exportadas; registra 25002.
- `SublimityShortcutWnd extends SublimityItem`: 34 funciones exportadas; registra 650, 631, 630, 640 y 90.
- No aparece otra clase con `extends SublimityItem` entre las 150 fuentes examinadas.
- `SublimityShortcut` declara `MethosRecoveredName_737`, que también existe en la base con la misma firma de dos enteros. Es un candidato claro a override. No se debe contar como función independiente sin revisar los flags y el índice de función exportado.
- En `SublimityShortcutWnd` no se observó una coincidencia nominal exacta con los hooks de la base en el inventario textual; falta verificar los flags de función y el dispatch.

### Corrección importante sobre el alcance

La frase anterior “varias clases derivadas pueden registrar pocos eventos” era una generalización. La evidencia de este paquete es más precisa: hay exactamente dos derivadas directas exportadas de SublimityItem en el conjunto de 150 clases. Los otros consumidores de APIs Sublimity no son por eso derivados de SublimityItem.

### Lectura funcional prudente

- SublimityShortcut registra únicamente 25002 y declara un método con la misma identidad saneada que un hook de la base. Esto lo convierte en un candidato claro para seguir el camino del evento de teclado.
- SublimityShortcutWnd registra cinco eventos y declara métodos con nombres numéricos recuperados. Para asociar cada evento con cada método hay que revisar el cuerpo de OnEvent y el bytecode/dispatch virtual, no deducirlo solo por el número.
- La tabla de eventos de la clase base no debe interpretarse como si todos los eventos fueran usados por estas dos derivadas. Muchos casos pueden ser compartidos por otras clases a través de UISublimityCommonAPI o ser residuos de una jerarquía mayor no presente en esta exportación.

**Pendiente:** comparar todas las firmas y flags de función del export table para confirmar virtual/override, y reconstruir la relación exacta entre OnEvent, los hooks de la base y las dos derivadas.
