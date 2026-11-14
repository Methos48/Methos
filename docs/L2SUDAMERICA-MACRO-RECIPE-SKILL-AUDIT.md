# L2Sudamerica — auditoría de macros, recetas y aprendizaje de habilidades

**Estado:** análisis estático de fuentes U descompiladas y XML XDAT. No se modificó ningún binario. Las variables saneadas no se renombran sin evidencia.

## 1. Alcance

Se cruzaron nueve ventanas y sus fuentes:
- MacroListWnd, MacroEditWnd, MacroInfoWnd
- RecipeBookWnd, RecipeManufactureWnd, RecipeBuyListWnd, RecipeBuyManufactureWnd, RecipeShopWnd
- SkillTrainListWnd, SkillTrainInfoWnd

Además, se revisaron las funciones de MagicSkillWnd para establecer la separación entre lista de habilidades aprendidas y aprendizaje de habilidades.

## 2. Macros

### MacroListWnd
- Fuente: 14.565 bytes, 448 líneas, 22 funciones.
- XML: lista `MacroItem`, contador, botones de añadir, ayuda, editar y eliminar.
- Registra eventos 1710, 1230, 1240 y 1250.
- Al entrar al estado solicita la lista con `MacroAPI.RequestMacroList()`.
- La selección de un elemento ejecuta `RequestUseMacro(ClassID)`.
- El borrado usa `RequestDeleteMacro(id)`; la ruta observada puede eliminar el macro seleccionado antes de abrir/actualizar el editor.
- El editor es una dependencia directa: `GetScript("MacroEditWnd")`.

### MacroEditWnd
- Fuente: 15.649 bytes, 560 líneas, 27 funciones.
- XML define nombre, nombre corto, icono, información, doce filas de texto/comando y botones guardar/cancelar.
- Registra eventos 150, 1710, 1260 y 1270.
- Maneja edición de comandos, arrastrar/soltar objetos y habilidades, cambio de texto y temporizador.
- Al guardar construye los parámetros de macro: identificador, nombre, icono, grupo/orden, descripción y `CommandList`; finalmente llama `MacroAPI.RequestMakeMacro(...)`.
- El código persiste un valor de agrupación/orden con `SetINIInt("MacroList", Name, valor, "MacroGrp")`. Es persistencia local de configuración, distinta de la solicitud de macro al sistema.
- Hay una rama condicionada por `GetOptionInt("Neophron", "MacroUpdate")` que llama `RequestDeleteMacro(-1)`. Su semántica exacta y compatibilidad deben rastrearse antes de tocarla.

### MacroInfoWnd
- Fuente pequeña: 2.032 bytes, 90 líneas, 9 funciones.
- Muestra información/descripción y controles de confirmación/cancelación.
- No registra eventos de red ni realiza solicitudes API propias en el código examinado.

**Conclusión de macros:** el flujo se divide entre listado/uso/borrado y edición/serialización/guardado. La UI puede recibir comandos por drag-and-drop, pero la semántica exacta de cada tipo de comando debe reconstruirse siguiendo `CommandList`, sus productores y los eventos 1230–1270.

## 3. Libro de recetas y fabricación propia

### RecipeBookWnd
- Fuente: 5.948 bytes, 178 líneas, 9 funciones.
- Registra 820, 830, 2070 y 1710.
- Doble clic sobre una receta solicita `RecipeAPI.RequestRecipeItemMakeInfo(recipeID)`.
- El borrado llama `RequestRecipeItemDelete(id)`.
- El XML tiene lista `RecipeItem`, contador y botón de papelera.
- Por tanto, el libro local es una lista administrable, pero los detalles de fabricación se solicitan mediante un flujo separado.

### RecipeManufactureWnd
- Fuente: 16.793 bytes, 481 líneas, 17 funciones.
- XML muestra objeto resultado, nombre, coste/consumo de MP, tasa de éxito, cantidad, mensajes, casilla de creación automática, árbol de recetas y botón de fabricación.
- Registra 840, 210, 2600, 2610 y 580.
- `BtnPrev` solicita volver al libro mediante `RequestRecipeBookOpen(recipeID)`.
- La fabricación normal llama `RequestRecipeItemMakeSelf(recipeID)`.
- Hay una ruta con temporizador que vuelve a solicitar la fabricación cada 250 ms, condicionada por el estado de creación automática. No debe interpretarse como eliminación del cooldown o garantía de fabricación: el servidor sigue validando cada solicitud.
- La ventana consulta MP y actualiza barra/valores visuales; el resultado de fabricación llega por eventos y no se deduce del botón.

## 4. Fabricación por tienda de recetas

### RecipeBuyListWnd
- Fuente: 14.703 bytes, 317 líneas, 9 funciones.
- Registra 780, 790 y 210.
- El XML incluye `MainTree`, MP, Adena y nombre.
- Al seleccionar una entrada del árbol, obtiene un identificador y llama `RequestRecipeShopMakeInfo(shopID, recipeID)`.
- Inicializa el texto de Adena en cero; los valores reales se actualizan mediante eventos.

### RecipeBuyManufactureWnd
- Fuente: 10.596 bytes, 256 líneas, 8 funciones.
- Registra 800, 210, 2600 y 2610.
- El botón anterior llama `RequestRecipeShopSellList(shopID)`.
- El botón de fabricación llama `RequestRecipeShopMakeDo(shopID, recipeID, cantidad)`.
- El XML comparte el patrón visual de fabricación: resultado, coste de MP, tasa, cantidad y controles de fabricación.

### RecipeShopWnd
- Fuente: 15.029 bytes, 453 líneas, 18 funciones.
- Registra 850, 860, 870 y 1710.
- Gestiona dos listas de objetos, botones para mover elementos arriba/abajo, finalizar tienda, mensaje e iniciar.
- Las llamadas observadas son:
  - `RequestRecipeShopListSet(payload)`: publicar/actualizar la lista de recetas ofrecidas.
  - `RequestRecipeShopMessageSet(mensaje)`: definir el mensaje de la tienda.
  - `RequestRecipeShopManageQuit()`: finalizar la gestión.
- `OnSendPacketWhenHiding()` también solicita salir de la gestión y limpia la ventana. El cierre visual no es un simple ocultamiento sin efectos: dispara una solicitud explícita.
- La construcción del payload de lista se realiza tras recorrer las entradas; falta recuperar completamente el esquema y validar los eventos 850/860/870 con sus productores.

**Distinción importante:** el flujo de receta propia (`RequestRecipeItemMakeSelf`) y el flujo de tienda de recetas (`RequestRecipeShopMakeDo`) son APIs separadas. No se deben fusionar ni asumir que usan las mismas reglas.

## 5. Aprendizaje y encantamiento de habilidades

### SkillTrainListWnd
- Fuente: 12.897 bytes, 351 líneas, 8 funciones.
- Registra eventos 2010, 2020 y 2030.
- XML usa `SkillTrainListTree` y textos de SP.
- La selección del nodo distingue opciones de aprendizaje:
  - Si `OptionWnd.Cb_FastLearn` está activado, llama directamente `RequestAcquireSkill(skillType, skillID, level)`.
  - Si no, solicita primero información con `RequestAcquireSkillInfo(skillType, skillID, level)`.
  - Para la opción de encantamiento solicita `RequestExEnchantSkillInfo(skillType, skillID)`.
- Esto demuestra que la opción rápida cambia el paso de confirmación en la UI; no demuestra que omita requisitos del servidor.

### SkillTrainInfoWnd
- Fuente: 11.261 bytes, 314 líneas, 10 funciones.
- Registra 2040, 2050 y 2060.
- XML muestra icono, nombre, descripción, coste SP y botones aprender/volver.
- En confirmación, las opciones de aprendizaje llaman `RequestAcquireSkill(type, id, level)`.
- La opción de encantamiento llama `RequestExEnchantSkill(type, id)`.
- Los datos de descripción/coste/estado se reciben y presentan a través de los eventos registrados; no son calculados exclusivamente por el XML.

### MagicSkillWnd (contexto)
- Fuente: 11.264 bytes, 351 líneas, 12 funciones.
- Registra 160, 1900, 1280 y 1290.
- Solicita `RequestSkillList()` y maneja clic, clic derecho, mostrar/ocultar y limpieza.
- Se trata de la lista/uso de habilidades disponibles, distinta de las ventanas de aprendizaje y encantamiento.

## 6. Matriz de solicitudes observadas

| Sistema | API | Parámetros identificables |
|---|---|---|
| Macros | RequestMacroList | Sin parámetros visibles |
| Macros | RequestUseMacro | ClassID |
| Macros | RequestDeleteMacro | ID; también aparece -1 en ruta especial |
| Macros | RequestMakeMacro | ID, nombre, icono, grupo/orden, descripción, CommandList |
| Libro de recetas | RequestRecipeItemMakeInfo | RecipeID |
| Libro de recetas | RequestRecipeItemDelete | ID de receta |
| Fabricación propia | RequestRecipeBookOpen | RecipeID |
| Fabricación propia | RequestRecipeItemMakeSelf | RecipeID |
| Tienda de recetas | RequestRecipeShopMakeInfo | ShopID, RecipeID |
| Tienda de recetas | RequestRecipeShopSellList | ShopID |
| Tienda de recetas | RequestRecipeShopMakeDo | ShopID, RecipeID, cantidad |
| Tienda de recetas | RequestRecipeShopListSet | Payload de lista |
| Tienda de recetas | RequestRecipeShopMessageSet | Mensaje |
| Tienda de recetas | RequestRecipeShopManageQuit | Sin parámetros visibles |
| Habilidades | RequestAcquireSkillInfo | Tipo, ID, nivel |
| Habilidades | RequestAcquireSkill | Tipo, ID, nivel |
| Encantamiento | RequestExEnchantSkillInfo | Tipo, ID |
| Encantamiento | RequestExEnchantSkill | Tipo, ID |
| Lista de habilidades | RequestSkillList | Sin parámetros visibles |

## 7. Eventos y dependencias aún sin resolver

1. Identificar productores y payloads de eventos 780–870 (recetas), 820/830/840 y 2010–2060 (habilidades).
2. Recuperar la estructura interna de `CommandList` de macros: tipos de comando, campos y límites de filas.
3. Seguir las fuentes de datos de RecipeTreeWnd y los nodos de árbol que alimentan las listas de fabricación/tienda.
4. Resolver el estado de creación automática y el temporizador de 250 ms: inicio, parada, condiciones y respuesta de cada intento.
5. Trazar el valor de `Cb_FastLearn` y confirmar qué confirmaciones visuales omite, sin atribuirle efectos sobre reglas del servidor.
6. Analizar los eventos de respuesta y errores para separar solicitud enviada, aceptación, rechazo, coste y resultado.
7. Relacionar cada control XML con el código que lo actualiza y sus recursos de icono/textura.

## 8. Evidencias

Fuentes U:
- `MacroListWnd.uc`, `MacroEditWnd.uc`, `MacroInfoWnd.uc`
- `RecipeBookWnd.uc`, `RecipeManufactureWnd.uc`, `RecipeBuyListWnd.uc`, `RecipeBuyManufactureWnd.uc`, `RecipeShopWnd.uc`
- `SkillTrainListWnd.uc`, `SkillTrainInfoWnd.uc`, `MagicSkillWnd.uc`

XML XDAT:
- `windows/MacroListWnd.xml`, `MacroEditWnd.xml`, `MacroInfoWnd.xml`
- `windows/RecipeBookWnd.xml`, `RecipeManufactureWnd.xml`, `RecipeBuyListWnd.xml`, `RecipeBuyManufactureWnd.xml`, `RecipeShopWnd.xml`
- `windows/SkillTrainListWnd.xml`, `SkillTrainInfoWnd.xml`

**Conclusión:** el bloque confirma tres familias separadas: macros (lista/edición/uso), recetas (libro/fabricación/tienda) y habilidades (consulta/aprendizaje/encantamiento). Se identificaron sus principales APIs y rutas de UI, pero aún faltan los productores de eventos, la recuperación de nombres originales y pruebas de ejecución controladas.
