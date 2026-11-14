# Auditoría técnica — UICommonAPI: capa compartida de interfaz

**Clase:** UICommonAPI  
**Archivo fuente:** decoded-u/sources-include-tail/UICommonAPI.uc  
**Tamaño exportado:** 184,436 bytes; 9,105 líneas.  
**Contexto:** base compartida de múltiples ventanas del cliente L2Sudamerica.  
**Método:** análisis estático de funciones, serialización de datos, diálogos, ventanas, clasificación y ordenación de inventario. No se modificó el paquete ni se probó en cliente.

## 1. Papel arquitectónico

UICommonAPI extiende UIScript y concentra utilidades usadas por muchas ventanas. No es una ventana visual individual: funciona como una capa común que reduce duplicación entre InventoryWnd, Trade/Warehouse/Shop, macros, diálogo, herramientas de eventos y ventanas personalizadas.

La clase contiene:
- utilidades de resolución y posicionamiento de ventanas;
- wrappers sobre DialogBox;
- wrappers de mostrar/ocultar/enfocar ventanas;
- conversión de cadenas de evento a estructuras ItemInfo y LVDataRecord;
- helpers de mensajes y tooltips;
- lectura/escritura de preferencias INI para macros;
- ordenación de inventario por familias de objetos;
- wrappers de operaciones primitivas del bytecode/engine.

El archivo contiene muchas funciones con nombres saneados MethosRecoveredName_* y UnknownFunction*. Esos nombres se conservan como identificadores técnicos: no se les inventa un nombre semántico sin pruebas cruzadas.

## 2. Tipos y constantes

### Enumeración de diálogos
La enumeración interna de tipo de diálogo contiene ocho modos:
0. DIALOG_OKCancel
1. DIALOG_OK
2. DIALOG_OKCancelInput
3. DIALOG_OKInput
4. DIALOG_Warning
5. DIALOG_Notice
6. DIALOG_NumberPad
7. DIALOG_Progress

La enumeración de botón predeterminado contiene:
- EDefaultNone = 0
- EDefaultOK = 1
- EDefaultCancel = 2

Constantes observadas: 10002, 19000, 11223344, 2940 y 2930. No se asigna semántica a las constantes que no está confirmada por uso.

## 3. Mensajes, colores y geometría

- Una función recibe texto y lo añade al chat con color SYSTEM mediante AddSystemMessage.
- Otra función permite color explícito: Red=(255,0,0), Green=(0,255,0), Blue=(0,0,255), Default=(176,153,121).
- El helper de centrado obtiene resolución actual y rectángulo de una ventana; la mueve a (resolución/2 − tamaño/2) en X/Y.
- Las funciones ShowWindow, HideWindow e IsShowWindow delegan directamente en UIAPI_WINDOW.
- El wrapper de mostrar con foco realiza ShowWindow y SetFocus consecutivamente.

Estas utilidades no cambian el anclaje XDAT por sí mismas; reposicionan el handle solicitado en tiempo de ejecución.

## 4. Conversión de payload a ItemInfo

La función de parseo de ItemInfo consume una cadena key/value y llena los campos siguientes:

**Identidad y presentación**
- classID → ClassID
- level → Level
- name → Name
- additionalName → AdditionalName
- iconName → IconName
- description → Description
- iconNameEx1..iconNameEx4 → IconNameEx1..IconNameEx4

**Identidad de instancia y estado**
- itemType → ItemType
- serverID → ServerID
- itemNum → ItemNum
- slotBitType → SlotBitType
- enchanted → Enchanted
- blessed → Blessed
- damaged → Damaged
- equipped → bEquipped (si la clave existe)
- arrow → bArrow (si existe)
- recipe → bRecipe (si existe)

**Economía y atributos**
- price, reserved, defaultPrice
- refineryOp1, refineryOp2
- currentDurability, durability, weight
- materialType, weaponType, armorType, ItemSubType
- physicalDamage, magicalDamage, shieldDefense, shieldDefenseRate
- crystalType, randomDamage, critical, hitModify, attackSpeed
- mpConsume, avoidModify, soulshotCount, spiritshotCount
- physicalDefense, magicalDefense, mpBonus, consumeType

Si Enchanted está en el rango 1..30, se construye ForeTexture con el patrón Abaddon.Inventory_ENCHANTNUMBER_SMALL_<valor>.

**Importancia:** este parser define un contrato de datos compartido. Si una ventana depende de ServerID, ClassID, ItemNum o ItemType, no son campos intercambiables. Un cambio visual debe conservar todos los campos y sus claves exactas.

### Limitación detectada
Otra función de UICommonAPI declarada para obtener ItemInfo desde una cadena aparece vacía en la fuente exportada. No asumir que realiza el mismo parseo ni rellenar el hueco sin localizar sus llamadas y el bytecode/export correspondiente.

## 5. Conversión de payload a LVDataRecord

El parser de registros de lista lee:
- szReserved;
- nReserved1, nReserved2 y nReserved3;
- MaxColumn;
- para cada columna: szData_n, szReserved_n, nReserved1_n, nReserved2_n y nReserved3_n.

A partir de MaxColumn dimensiona LVDataList y llena cada columna por índice. Esto permite que una lista conserve tanto el texto visible como datos internos reservados por fila/columna.

**Implicación para las auditorías anteriores:** cuando una ventana usa nReserved1 para almacenar ClassID, ServerID u otro identificador, la semántica procede del productor de ese LVDataRecord, no del nombre genérico nReserved1. Debe rastrearse por ventana.

## 6. Diálogos compartidos

UICommonAPI obtiene el script DialogBox y expone wrappers para:
- mostrar/cerrar el diálogo actual;
- consultar si el diálogo activo pertenece a la instancia llamadora;
- establecer el ID de confirmación;
- establecer tipo de edición;
- obtener texto de entrada;
- asignar mensaje;
- consultar índice/botón elegido;
- configurar tiempo/estado del diálogo.

Las funciones delegan en DialogBox, no implementan el dibujo del cuadro en UICommonAPI. Los diálogos concretos y su temporización deben analizarse en DialogBox.uc.

La existencia de un diálogo activo se comprueba comparando el identificador/instancia devuelto por DialogBox con la cadena de la instancia actual. Esto ayuda a evitar que una ventana responda accidentalmente al cuadro de confirmación abierto por otra.

## 7. Eventos internos y helpers

- Un helper construye parámetros QuestID y Level y emite ExecuteEvent(10002, payload).
- Otro helper escribe mensajes de depuración en SublimityDebugWnd.DebugChat en rojo.
- La lectura de grupo de macros consulta GetINIInt en la sección MacroList del archivo MacroGrp.
- Existe un mapeo de índice a icono de macro, que empieza por Icon.skill0003, Icon.skill0255 e Icon.skill0001; debe documentarse completo en una ficha de macros si se requiere el catálogo de iconos.

## 8. Ordenación del inventario

La función de ordenación recorre todos los ítems de un ItemWindowHandle, obtiene ItemInfo y clasifica en arrays temporales según EItemType y, para consumibles, ItemSubType. Después escribe los grupos de vuelta mediante SetItem, en secuencia.

La fuente evidencia que:
- se conservan los objetos ItemInfo completos al moverlos;
- se agrupan por ItemType;
- dentro del grupo de consumibles se subdivide por ItemSubType;
- la salida reconstruye el contenido de la ventana con índices consecutivos.

La lista de subtipos observados incluye 22, 21, 20, 19, 3, 24 y 2. No se asignan nombres de categoría a esos números sin cruzarlos con el enum del cliente y la tabla de objetos. La función opera sobre el orden visual/local del ItemWindow; no hay evidencia de que envíe una operación al servidor.

## 9. Wrappers de primitivas y nombres saneados

El archivo contiene un bloque amplio de wrappers UnknownFunction que delegan en primitivas __NFUN_* del entorno UnrealScript:
- concatenación/comparación y operaciones de cadenas;
- comparaciones booleanas/numéricas;
- incremento/decremento y operaciones enteras;
- división/multiplicación y comparaciones float;
- comparaciones de Name y handles;
- operaciones sobre arrays y referencias.

Son wrappers de compatibilidad/decompilación. No deben eliminarse ni renombrarse a ciegas: son utilizados por el código exportado para reconstruir operaciones originales. El nombre UnknownFunction no implica que la operación sea desconocida para el motor; la semántica de cada una puede resolverse por el wrapper y el opcode subyacente.

## 10. Funciones incompletas y puntos de riesgo

Se identificaron funciones declaradas sin cuerpo funcional visible, entre ellas el parser alternativo ItemInfo y un helper de macro. No se declara que estén sin implementar en el binario original: la exportación/decompilación puede haber perdido cuerpo o no haber reconstruido un opcode. Se deben contrastar con bytecode y referencias antes de intervenir.

Riesgos:
- alterar claves de payload puede romper inventario, comercio, almacén y tooltips;
- confundir ClassID y ServerID puede afectar selección/operaciones de objetos;
- cambiar el orden de clasificación puede alterar solo la presentación, pero hay que comprobar si alguna ventana interpreta el índice como estado;
- modificar helpers de diálogo puede provocar que una ventana consuma la respuesta de otra;
- eliminar wrappers UnknownFunction puede impedir recompilación o cambiar la semántica.

## 11. Dependencias y próximos pasos

1. Construir un grafo de referencias de UICommonAPI hacia todas las clases que llaman sus helpers.
2. Resolver los nombres MethosRecoveredName_* mediante tabla de nombres y referencias cruzadas.
3. Mapear ItemType/ItemSubType contra enums y uso en InventoryWnd, WarehouseWnd, TradeWnd, ShopWnd, PetWnd y macros.
4. Comparar los dos parsers ItemInfo y buscar consumidores de cada uno.
5. Documentar DialogBox.uc como dependencia directa de UICommonAPI.
6. Seguir ExecuteEvent(10002) hasta sus consumidores.
7. Comprobar por bytecode las funciones con cuerpo vacío en la exportación.
8. Mantener el orden de inventario como comportamiento local hasta tener evidencia contraria.

## 12. Estado

**Inventariado y analizado parcialmente:** capa común, diálogos, ventanas, mensajes, conversión ItemInfo/LVDataRecord, ordenación local y wrappers de primitivas.  
**Pendiente:** grafo completo de llamadas, resolución de FNames, tabla completa de enums de ítems, productores/consumidores de eventos, revisión de bytecode y prueba de compilación/carga.

No se ha modificado ni recompilado Interface.u.