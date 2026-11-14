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


## Addendum C — overrides identificados por firma y cadena de despacho

Se compararon automáticamente las firmas de función de la clase base contra las dos derivadas, no solo los nombres de clase. Resultado:

| Derivada | Funciones propias exportadas | Firmas que coinciden con la base |
|---|---:|---|
| SublimityShortcut | 3 | MethosRecoveredName_737(int, int) |
| SublimityShortcutWnd | 34 | MethosRecoveredName_728(string), _729(int), _730(int), _731(int), _735() |

Son overrides candidatos por identidad y firma de fuente. La confirmación definitiva de los flags virtual/override exige revisar export table/bytecode.

### Despacho desde OnEvent

| Evento | Parser intermedio | Hook final |
|---:|---|---|
| 630 | _911 | _730 |
| 631 | _913 | _729 |
| 640 | _909 | _731 |
| 660 | _907 | _908 |
| 650 | _905 | _906 |
| 590 | _903 | _904 |
| 600 | _901 | _902 |
| 610 | _899 | _900 |
| 620 | _898 | _897 |
| 150 | _896 | _895 |
| 160 | _894 | _893 |
| 180 | _892 | _891 |
| 90 | _923 | _728 |

Los parsers _911 y _913 leen ShortcutID; _909 lee ShortcutPage. Los eventos restantes pasan por parsers propios; sus campos se deben revisar individualmente.

### Cadena de eventos de shortcut

- Evento 631 → parser _913 → hook _729(int). SublimityShortcutWnd implementa _729 y delega en _744(ShortcutID, true).
- Evento 630 → parser _911 → hook _730(int). SublimityShortcutWnd implementa _730 y delega en _744(ShortcutID, false).
- Evento 640 → parser _909 → hook _731(int). SublimityShortcutWnd implementa _731 y delega en _345(0, ShortcutPage).

Por tanto, 631 y 630 activan la misma rutina de ventana con un booleano opuesto. El significado exacto de ese booleano requiere seguir _744 y sus actualizaciones posteriores.

### Override candidato 737

SublimityShortcut registra 25002. La base parsea key y state y llama a _737(key,state). SublimityShortcut declara la misma firma, pero ambos cuerpos exportados son solo return. Esto confirma una coincidencia de firma, pero no recupera la acción efectiva de teclado. No debe afirmarse que el evento no hace nada en el cliente original.

### Corrección

El inventario textual muestra un override candidato en SublimityShortcut y cinco en SublimityShortcutWnd. Esto corrige la afirmación previa de que no había coincidencias nominales en SublimityShortcutWnd.

**Pendiente de alta prioridad:** leer el serial data de los exports de función en bruto para obtener FunctionFlags, NativeIndex, parámetros/defaults y bytecode. El lector actual UnrealPackageIndex valida y expone tablas principales y metadatos de export, pero mantiene opacos los serial payloads de objetos; no reconstruye todavía flags de UFunction.


## Addendum D — verificación directa de los cuerpos de las dos derivadas

Se volvió al código fuente exportado del artefacto, no solo al resumen de firmas. Este contraste permite corregir una conclusión anterior.

### 1. Cinco hooks de SublimityShortcutWnd sí tienen implementación

La clase derivada contiene cuerpos funcionales para las cinco firmas:
- `MethosRecoveredName_735()`: limpia la ventana, reconstruye estado, restaura configuración y recorre cuatro paneles.
- `MethosRecoveredName_729(int)`: llama a `MethosRecoveredName_744(id, true)`.
- `MethosRecoveredName_730(int)`: llama a `MethosRecoveredName_744(id, false)`.
- `MethosRecoveredName_731(int)`: llama a `MethosRecoveredName_345(0, page)`.
- `MethosRecoveredName_728(string)`: implementa un switch amplio de nombres de controles de shortcut y acciones asociadas.

Los métodos homónimos de SublimityItem son stubs `return;`; la derivada proporciona la lógica efectiva de la ventana. Esto confirma el reemplazo funcional en el nivel del código fuente exportado. Los flags UFunction del paquete aún no se han leído, así que no se afirma una verificación binaria del flag `FUNC_Virtual`.

### 2. Significado observable de 630 y 631

La función común `MethosRecoveredName_744(id, bool)`:
1. Recorre cuatro páginas/paneles guardados en el array de estado.
2. Si el ID pertenece a la página del panel, llama dos veces a `UIAPI_SHORTCUTITEMWINDOW.UpdateShortcut`, una para cada orientación/representación.
3. Llama a `MethosRecoveredName_746(id, !bool)`.

La función 746 elige:
- true: `MethosRecoveredName_814(id)`, que muestra las texturas de ambos formatos de barra para ese shortcut.
- false: `MethosRecoveredName_816(id)`, que oculta esas texturas.

Por tanto, la semántica observable es **actualizar las dos representaciones visuales del shortcut y encender/apagar su textura**, no una acción del servidor. Según el evento, 631 pasa true y termina en el argumento false de 746; 630 pasa false y termina en true. El sentido visual exacto (mostrar/ocultar) queda así invertido entre ambos eventos.

### 3. Evento 640 y páginas

`MethosRecoveredName_345(panel, page)` valida panel entre 0 y 3 y página entre 0 y 9, guarda el valor en el array de cuatro páginas y en la opción `Game/ShortcutPage_<panel>`, y reconstruye ese panel mediante `MethosRecoveredName_752(panel)`.

La ruta de evento 640 llama con panel 0 y la página recibida. El mismo método es reutilizado por botones de navegación de los otros tres paneles.

### 4. Evento 90 y controles

`MethosRecoveredName_728(string)` implementa la selección por nombre de control, incluyendo controles de los grupos `MyShortcutItem...` y comandos de ventana. Esto confirma que el evento 90 llega a una lógica de UI real en SublimityShortcutWnd, no a un simple stub.

### 5. SublimityShortcut: diferencia crítica

La clase `SublimityShortcut` registra 25002 y declara `MethosRecoveredName_737(int,int)`, pero ese cuerpo exportado sí es únicamente `return;`. A diferencia de SublimityShortcutWnd, no se ha encontrado una implementación derivada funcional de ese hook en la fuente recuperada. La acción de teclado asociada a 25002 permanece sin resolver y puede depender de bytecode ausente, nativo o de una ruta externa.

### 6. Corrección del estado anterior

La auditoría anterior dejó abierta la interpretación del booleano de 630/631 y no había seguido el cuerpo de _744. Ya está resuelto por la fuente exportada: ambos eventos actualizan el shortcut en dos formatos y cambian la visibilidad de las texturas de forma opuesta. También queda corregida la ambigüedad sobre los cinco hooks de SublimityShortcutWnd: sus cuerpos no están vacíos en la derivada.

**Pendiente binario:** extraer los flags UFunction y el bytecode bruto de estos seis métodos para cotejar el export textual con la serialización original. No se ha compilado ni ejecutado el cliente.


## Addendum E — investigación específica de SublimityShortcut y evento 25002

Se revisaron de nuevo los fuentes `SublimityShortcut.uc`, `SublimityItem.uc`, `Shortcut.uc` e `InterfaceAI_KeySettingWnd.uc` del paquete L2Sudamerica.

### 1. Ruta real del evento 25002

La cadena recuperada es:

`RegisterEvent(25002)` en `SublimityShortcut.OnLoad`
→ `SublimityItem.OnEvent` caso 25002
→ `MethosRecoveredName_878(payload)`
→ parseo de `key` y `state`
→ `MethosRecoveredName_737(key,state)`.

El parser confirma que el payload contiene al menos dos enteros denominados literalmente `key` y `state`. El método 737 aparece como `return;` tanto en SublimityItem como en SublimityShortcut. No hay lógica visible de tecla/estado en esas dos implementaciones exportadas.

### 2. Estructura de datos de SublimityShortcut

La clase declara:
- Un struct con tres enteros (campos saneados 4025–4027).
- Tres arrays de 256 elementos: uno de enteros y dos de strings.
- Tres enteros escalares adicionales (4030–4032).
- En OnLoad, solo se observa que 4030 se inicializa a -1.

No se asigna semántica a estos campos por sus nombres saneados. Las fuentes disponibles no muestran referencias adicionales a esos arrays/escalares dentro de los 151 fuentes UC, aparte de las declaraciones y la inicialización indicada. Es una señal de que la lógica pudo quedar fuera de la exportación recuperada, no prueba de que los campos estén inactivos en el binario original.

### 3. Método 1576: clasificación bit a bit pendiente

`MethosRecoveredName_1576(int)` devuelve una expresión formada por tres llamadas anidadas a `__NFUN_154__(valor, 16/17/18)` combinadas mediante `__NFUN_132__`. El decompilador agrega un `return false;` posterior inalcanzable.

Sin resolver con certeza los intrínsecos 154 y 132 no se puede etiquetar el resultado como máscara, combinación AND/OR u otra operación. Queda como expresión recuperada, no como lógica interpretada.

### 4. No confundirlo con el sistema Shortcut clásico

Existe otra clase independiente, `Shortcut`, que registra el evento 90 y maneja los comandos de teclas configurables. Su configuración lee y guarda:
- `Option.ini [Key] Panel1/Panel2/Panel3`
- `Option.ini [Key] UseBind1/UseBind2/UseBind3`
- `Game/EnterChatting`

La ventana `InterfaceAI_KeySettingWnd` presenta tres casillas UseBind y tres selectores de panel, y persiste los cambios a través de `Shortcut.MethosRecoveredName_1017`.

La clase Shortcut traduce los nombres de comandos:
- `UserShorcutKey_1..12` → `/useshortcut <Panel1> <slot>`
- `UserShorcutKey_Q,W,E,R,T,Y,U,I,O,P,[,]` → `/useshortcut <Panel2> <slot 1..12>`
- `UserShorcutKey_F1..F12` → `/useshortcut <Panel3 actual> <slot 1..12>`
- Hay rutas paralelas `UserShorcutKeyForce_...` que ejecutan `/useshortcutforce`.

En F1–F12, cuando UseBind3 está activo, el código lee el número de página de la barra horizontal si está visible; de lo contrario, lee el textbox de la barra vertical. Si UseBind3 está desactivado, el comando usa el valor de Panel3 cargado desde Option.ini. En las rutas de 1–12 y Q..], el comando solo se ejecuta si UseBind1 o UseBind2 respectivamente está activo.

Esta clase es el sistema clásico de asignación de teclas y no es la implementación de `SublimityShortcut`. La existencia de rutas `/useshortcutforce` tampoco demuestra que el cliente pueda saltarse restricciones del servidor; solo acredita qué comando de cliente se envía.

### 5. Hallazgo de arquitectura

Hay dos sistemas separados en el mismo paquete:
1. `Shortcut` + `InterfaceAI_KeySettingWnd`: asignación de teclas por Option.ini y emisión de comandos de acceso directo.
2. `SublimityShortcut` + `SublimityItem`: receptor del evento 25002 con payload key/state, arrays de 256 elementos y lógica de callback no recuperada.

No se debe trasladar la lógica de la clase Shortcut a SublimityShortcut ni asumir que ambos comparten el mismo registro de teclas. No se ha encontrado una llamada directa que conecte la ruta 25002 con los comandos de la clase Shortcut.

### 6. Estado de recuperación

| Elemento | Estado |
|---|---|
| Registro del evento 25002 | Confirmado |
| Campos key/state del payload | Confirmados |
| Firma y despacho al hook 737 | Confirmados |
| Cuerpo funcional del hook 737 | No recuperado; stub en ambas clases |
| Uso de arrays de 256 | No resuelto |
| Semántica del struct de tres enteros | No resuelta |
| Intrínsecos 154/132 del método 1576 | Pendiente |
| Sistema clásico Shortcut / Option.ini | Lógica fuente recuperada |
| Relación entre ambos sistemas | No demostrada |

**Próxima tarea técnica:** resolver los intrínsecos `__NFUN_154__` y `__NFUN_132__` desde el registro nativo y buscar referencias de los índices saneados en el mapa de nombres del paquete. Después, analizar los exports de SublimityShortcut y SublimityItem a nivel serializado. No se ha compilado ni ejecutado el cliente.


## Addendum F — inferencia contextual de los intrínsecos 154 y 132

Se hizo un barrido de los 151 fuentes UC del artefacto original. El resultado cuantitativo:
- `__NFUN_154__`: 1.041 llamadas distribuidas en 95 archivos.
- `__NFUN_132__`: 238 llamadas distribuidas en 41 archivos.

No se dispone todavía de Core.u/Engine.u/NWindow.u del mismo runtime para generar y consultar el registro nativo exacto. Por eso las identificaciones siguientes son inferencias contextuales fuertes, no resolución por nombre del registro.

### 1. Token 154: comparación de igualdad (candidato muy fuerte)

En RecipeBuyListWnd, el evento `MethosRecoveredName_3518` se compara contra constantes 780, 790 y 210 para seleccionar ramas distintas del dispatcher. En otros lugares se compara un ID o longitud con un entero. En CustomEtcWnd se utiliza para probar pertenencia de un entero a valores discretos.

La interpretación que mejor explica todos esos contextos es igualdad (`==`). Debe confirmarse con el descriptor nativo del runtime, porque el token 154 no se ha resuelto por su registro.

### 2. Token 132: OR lógico (candidato muy fuerte)

En CustomEtcWnd se agrupan pruebas de igualdad de IDs para activar una misma categoría; por ejemplo, dos IDs de texto comparados por separado y unidos en una condición. También se combinan grupos mayores de comparaciones para seleccionar un comportamiento. La interpretación más consistente es OR lógico (`||`).

En SublimityShortcut, el método 1576 combina tres pruebas de token 154 con valores 16, 17 y 18 mediante token 132. Si las inferencias anteriores se confirman, el método devuelve true cuando el argumento es 16, 17 o 18.

### 3. Reconstrucción condicional de 1576

`MethosRecoveredName_1576(x)` equivale probablemente a:

`return (x == 16) || (x == 17) || (x == 18);`

Esto es una reconstrucción semántica provisional basada en 1.279 usos contextuales de ambos tokens. No debe marcarse como verdad binaria hasta consultar NativeFunctionRegistryBuilder con los paquetes exactos Core/Engine/NWindow de L2Sudamerica.

### 4. Implicación funcional limitada

El método 1576 parece clasificar tres valores enteros específicos como un grupo. No hay evidencia suficiente para nombrar ese grupo como teclas, modificadores, estados de pulsación, categorías de shortcut ni códigos virtuales. El parámetro no tiene un nombre original recuperado.

### 5. Acción técnica pendiente

Para cerrar la inferencia se requiere:
1. Obtener Core.u, Engine.u y NWindow.u del mismo cliente/runtime L2Sudamerica.
2. Ejecutar NativeFunctionRegistryBuilder y filtrar NativeToken 154 y 132.
3. Confirmar Name, Owner, ReturnType, Flags y si IsOperator.
4. Correlacionar el descriptor exacto con los callsites del paquete Interface.u.
5. Solo entonces restaurar el nombre real y semántica del método 1576.

No se han alterado binarios ni se ha compilado o ejecutado el cliente.
