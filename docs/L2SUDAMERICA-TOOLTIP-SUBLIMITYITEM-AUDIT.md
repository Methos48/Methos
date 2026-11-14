# Auditoría técnica — ToolTip y SublimityItem

**Paquete:** Interface.u de L2Sudamerica (Interlude).  
**Fuentes examinadas:** `ToolTip.uc`, `Tooltip.xml`, `SublimityItem.uc`.  
**Método:** inspección estática de fuentes exportadas y referencias cruzadas. Sin recompilación ni prueba en cliente.

## 1. ToolTip: responsabilidad y ciclo

`ToolTip` hereda de `UICommonAPI`. Declara dos variables de trabajo:
- `CustomTooltip`: acumulador que se devuelve al motor.
- `DrawItemInfo`: registro temporal de una línea/elemento del tooltip.

OnLoad registra solamente el evento **2920**. OnEvent lo deriva a la función principal, que:
1. Limpia el acumulador: SimpleLineCount=0, MinimumWidth=0 y vacía DrawList.
2. Lee `TooltipType` y `SourceType`; si falta cualquiera, termina sin producir tooltip.
3. Selecciona un generador según el tipo.
4. Devuelve el resultado mediante `ReturnTooltipInfo`.

La ventana XML `Tooltip` no contiene controles hijos ni geometría visible propia: es un contenedor nativo con tamaño absoluto 0×0, ancla TOP_LEFT. El dibujo real se compone dinámicamente en CustomTooltip.DrawList.

## 2. Tipos de tooltip reconocidos

El selector contiene estas rutas explícitas:

| TooltipType | Generador / función | Función general |
|---|---|---|
| Text | MethosRecoveredName_757 | Texto o ID de cadena del sistema |
| Description | MethosRecoveredName_757 | Texto descriptivo con ancho mínimo y formato multilínea |
| Action | MethosRecoveredName_782 | Descripción de acción |
| Skill | MethosRecoveredName_780 | Información de skill |
| NormalItem | MethosRecoveredName_777 | Información normal de item |
| Shortcut | MethosRecoveredName_761 | Información de shortcut según tipo |
| AbnormalStatus | MethosRecoveredName_779 | Buff/debuff |
| RecipeManufacture | MethosRecoveredName_774 | Fabricación de receta |
| Recipe | MethosRecoveredName_760 | Receta |
| RecipePrice | MethosRecoveredName_760 | Receta con precio |
| Inventory | MethosRecoveredName_783 | Item del inventario |
| InventoryPrice1 | MethosRecoveredName_783 | Item con precio/moneda |
| InventoryPrice2 | MethosRecoveredName_783 | Item con precio alternativo |
| InventoryPrice1HideEnchant | MethosRecoveredName_783 | Variante de inventario que oculta encantamiento |
| InventoryPrice1HideEnchantStackable | MethosRecoveredName_783 | Variante que trata apilables y encantamiento |
| InventoryPrice2PrivateShop | MethosRecoveredName_783 | Precio en tienda privada |
| PartyMatch | MethosRecoveredName_772 | Información de búsqueda de grupo |
| QuestInfo | MethosRecoveredName_766 | Información de quest |
| QuestList | MethosRecoveredName_771 | Elemento de lista de quest |
| RaidList | MethosRecoveredName_767 | Elemento de lista de raid |
| ClanInfo | MethosRecoveredName_773 | Información de clan |
| ManorSeedInfo / ManorCropInfo / ManorSeedSetting / ManorCropSetting / ManorDefaultInfo / ManorCropSell | MethosRecoveredName_765 | Datos del sistema Manor |
| Valor numérico | MethosRecoveredName_761 | Shortcut identificado por ID numérico |

El selector también depende de un valor `SourceType` convertido a enum `ETooltipSourceType`. La función de texto verifica un bit específico del tipo de fuente antes de aceptar Text/ID; no debe asumirse que todos los orígenes admiten todas las variantes.

## 3. Construcción de elementos gráficos

La lista de dibujo se compone con registros `DrawItemInfo`:
- DIT_TEXT y campos de texto/ID.
- Color RGBA.
- Desplazamiento vertical.
- Indicador de salto de línea.
- Indicador de una sola línea.
- Datos de icono/ítem para las ramas correspondientes.

Las funciones auxiliares:
- limpian y añaden entradas al DrawList;
- configuran color y altura;
- administran SimpleLineCount y MinimumWidth;
- construyen cadenas de coste mediante MakeCostString;
- obtienen color numérico con GetNumericColor;
- convierten números a texto;
- consultan datos de ItemInfo y refinamiento.

No es un widget HTML ni una ventana convencional compuesta por controles XML: es una estructura de dibujo que entrega información al renderizador nativo.

## 4. Inventario, precio y refinamiento

La rama Inventory obtiene ItemInfo mediante el parser común y utiliza `NWindow.UIDATA_ITEM.GetRefineryItemName` para formar el nombre que incluye opciones de refinamiento.

También consulta `NWindow.UIDATA_REFINERYOPTION`:
- GetQuality para obtener calidad de una opción.
- GetOptionDescription para recuperar su descripción y formato.
- Aplica color a cada descripción de opción.
- Procesa RefineryOp1 y RefineryOp2 por separado.

Las variantes de precio construyen información de coste:
- InventoryPrice1 usa el ID de cadena 469 y una presentación de coste.
- InventoryPrice2 / InventoryPrice2PrivateShop usa el ID 468.
- Si Price es positivo, presenta el coste; la fuente también tiene ramas para precio cero.
- La variante PrivateShop puede mostrar cantidad/reserva cuando el item es apilable y Reserved es positivo.
- Hay tratamiento especial de ClassID 57 para mostrar cantidad entre paréntesis.

También existen variantes que omiten parte de la representación del encantamiento o adaptan el texto para items apilables. No se deben fusionar sin conservar sus diferencias de tipo.

## 5. Tipos de shortcuts y estados

La ruta Shortcut discrimina `EShortCutItemType` y contiene ramas para tipos 1, 2, 3, 5 y 4. Por ello el tooltip de un acceso directo no es una simple copia del tooltip de un item: interpreta el tipo de contenido del slot y consulta los datos nativos correspondientes.

La ruta AbnormalStatus es independiente y construye información de estados alterados. Su fuente y el sistema AbnormalStatusWnd deben analizarse juntos para completar el contrato de duración, iconos y descripciones.

## 6. Datos de entrada identificados

Las claves Parse* literales encontradas en ToolTip incluyen:
`TooltipType`, `SourceType`, `Text`, `ID`, `ClassID`, `Name`, `AdditionalName`, `Description`, `ItemNum`, `ItemType`, `ItemSubType`, `ConsumeType`, `CrystalType`, `Weight`, `Price`, `Enchanted`, `RefineryOp1`, `RefineryOp2`, `Level`, `MpConsume` y `Reserved`.

No todas las claves se leen en todas las ramas; algunas proceden del parser ItemInfo compartido. El orden y la presencia de campos deben conservarse al generar payloads.

## 7. SublimityItem: clase base de utilidades

`SublimityItem` hereda de `UISublimityCommonAPI`. Tiene 1.106 líneas, 4 variables de instancia y una gran cantidad de funciones recuperadas con nombres numéricos. Es una clase base compartida por ventanas Sublimity, incluida la implementación extendida de shortcuts.

Variables:
- dos booleanos;
- dos enteros.

La fuente contiene funciones de:
- adaptación de eventos a métodos internos;
- lectura de payloads de status icons;
- estado/contador/puntos;
- quest y comandos;
- shortcuts y páginas;
- estados de item/refinería;
- actualización de ventanas, posiciones y textos;
- callbacks de sistemas adicionales.

No todas las funciones tienen lógica visible: hay varios cuerpos que retornan inmediatamente. Esos stubs no se deben completar por intuición ni considerar prueba de que el runtime original carece de implementación.

## 8. Despacho de eventos de SublimityItem

El OnEvent exportado despacha estos grupos:

| Evento | Función destino | Datos que la función lee / acción visible |
|---|---|---|
| 1910 | MethosRecoveredName_928 | TotalPoint, AddPoint, PeriodType, RemainTime, PointType |
| 700 / 710 / 720 / 1520 | 927 / 926 / 925 / 924 | QuestID, Level, Completed; destrucción/actualización de quest |
| 90 | 923 | Command |
| 40 / 50 | 940 / 939 | Sin payload visible |
| 950 / 960 / 970 | 922 / 921 / 920 | Listas de StatusIconInfo, con variantes de flags |
| 630 / 631 / 640 / 660 / 650 / 590 / 600 / 610 / 620 | 911 / 913 / 909 / 907 / 905 / 903 / 901 / 899 / 898 | Familia relacionada con shortcuts/paneles; los contratos completos dependen de cada función |
| 150 / 160 / 180 | 896 / 894 / 892 | Actualizaciones auxiliares |
| 2530 / 2540 / 2550 / 2560 | 918 / 917 / 916 / 915 | Payloads específicos del subsistema |
| 25100 / 25101 / 25102 | 854 / 850 / 848 | Payloads específicos del subsistema |
| 1710 / 1720 | 948 / 947 | Ejecuta solo si la instancia satisface MethosRecoveredName_63() |
| 100000–100011, 10002, 200000, 100012–100019 | varias | Eventos extendidos de Sublimity; requieren rastreo de productores |
| 25000–25004 | 886 / 882 / 878 / 846 / 881 | Familia relacionada con eventos extendidos |

**Precaución:** el evento 40 aparece dos veces en el switch exportado (al principio y al final). En un switch convencional la segunda rama sería inalcanzable. Debe contrastarse con bytecode/exports originales antes de atribuir intención o editar el orden.

## 9. Relación con Shortcut y otros sistemas

La coincidencia de eventos 590–660 entre ShortcutWnd estándar y SublimityItem no demuestra que ambos reciban exactamente los mismos payloads ni que los procesen igual. La clase base actúa como adaptador compartido para varias ventanas Sublimity; los eventos 631 y 25000–25004 son pistas de extensiones propias.

ToolTip también se consume indirectamente desde ventanas y controles que construyen `CustomTooltip` manualmente. Por ejemplo, ClanDrawerWnd, InventoryWnd, OptionWnd, PCCafeEventWnd, QuestTreeWnd y RecipeBuyListWnd crean sus propios DrawList. Esto es distinto del evento global 2920 que entrega el tooltip dinámico a ToolTip.

## 10. Riesgos de modificación

1. No renombrar ni eliminar funciones numéricas hasta resolver referencias, bytecode y herencia.
2. No alterar el orden de limpieza y ReturnTooltipInfo: evita mezclar líneas de tooltips consecutivos.
3. No omitir SourceType al enviar evento 2920.
4. No tratar InventoryPrice1 y InventoryPrice2 como formatos intercambiables.
5. No eliminar consultas UIDATA_ITEM/UIDATA_REFINERYOPTION: son puentes nativos.
6. No asumir que los stubs SublimityItem son vacíos en el paquete binario original; validar contra bytecode.
7. No deducir semántica de eventos 100000+, 25000+ o 25100+ solo por su numeración.
8. No confundir CustomTooltip construido manualmente por otras ventanas con la ventana global ToolTip.

## 11. Pendientes

- Resolver todos los nombres numéricos de ToolTip y SublimityItem con referencias cruzadas/FNames.
- Construir una tabla de consumidores y productores del evento 2920, incluyendo payloads completos.
- Profundizar cada variante de tooltip de skill, shortcut, abnormal status, quest, raid, party match, clan y Manor.
- Recuperar `UISublimityCommonAPI` y analizar herencia, eventos y wrappers nativos.
- Contrastar stubs y ramas duplicadas con bytecode de Interface.u.
- Identificar los productores y consumidores de los eventos extendidos 100000+, 25000+ y 25100+.
- Verificar XML/controles de cada ventana que invoca CustomTooltip.
- Pruebas en cliente de cada tipo y de cambio rápido entre tooltips.

## 12. Estado

**Confirmado estáticamente:** ciclo global ToolTip/evento 2920, catálogo de tipos, acumulación de DrawItemInfo, variantes de inventario/precio/refinería, jerarquía SublimityItem y mapa inicial de su despacho de eventos.

**Pendiente:** contratos nativos, payloads completos, resolución de nombres, bytecode, productores de eventos y validación en cliente. No se modificaron binarios ni se compiló una interfaz.
