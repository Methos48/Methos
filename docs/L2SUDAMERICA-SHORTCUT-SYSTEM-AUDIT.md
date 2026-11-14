# Auditoría técnica — ShortcutWnd y Shortcut: barras, páginas y atajos

**Cliente:** L2Sudamerica / Lineage II Interlude.  
**Fuentes:** ShortcutWnd.uc (57.582 bytes, 1.712 líneas), Shortcut.uc (40.765 bytes, 1.288 líneas), ShortcutWnd.xml y Shortcut.xml del artefacto 43579091099.  
**Método:** análisis estático de eventos, páginas, layouts, persistencia y comandos. Sin modificar binarios ni probar en cliente.

## 1. Arquitectura

El sistema se divide en dos clases:
- **ShortcutWnd:** presenta barras de accesos directos, páginas, slots, expansión, rotación, bloqueo, layout horizontal/vertical y panel joypad.
- **Shortcut:** procesa comandos globales de interfaz relacionados con atajos, chat y apertura de ventanas; conserva configuración de paneles de teclas y bindings.

La UI usa UIAPI_SHORTCUTITEMWINDOW.UpdateShortcut para refrescar slots y ShortcutAPI.SetShortcutPage para cambiar página. La lógica de ejecución del acceso directo reside en el sistema de atajos del cliente; estas clases no reimplementan la ejecución de skills/items.

## 2. ShortcutWnd — eventos

OnLoad registra:
- 630: cambio de página principal.
- 640: actualización de shortcut individual.
- 660: limpieza/actualización de shortcuts.
- 650: actualización global que fuerza refresco de layout.
- 590, 600, 610, 620: cambios de página en barras secundarias.
- 150: evento de ciclo de interfaz/estado.

OnEvent enruta los payloads a funciones específicas. Los eventos de páginas leen ShortcutPage; los de slot leen ShortcutID. El evento 1008 lee OnOff y muestra/oculta JoypadBtn.

### Página principal
- ShortcutPage debe estar entre 0 y 9; fuera del rango se ignora.
- El texto de página muestra índice + 1.
- Cada página contiene 12 slots.
- El primer índice de shortcut para una página se calcula como página × 12.
- Los 12 slots se refrescan mediante UIAPI_SHORTCUTITEMWINDOW.UpdateShortcut.

### Actualización de slot
El evento ShortcutID calcula el slot visual mediante división entera por 12 más 1. Si el índice está en la página visible de cualquiera de las cinco barras, actualiza el slot correspondiente. El control limpia valores auxiliares del índice al recibir la actualización. La comparación de página usa intervalos de 12 elementos.

**Precaución:** ShortcutID es un índice global de shortcut y el número del slot visual es un índice local 1..12. No son valores intercambiables.

## 3. Cinco páginas/barras simultáneas

ShortcutWnd mantiene cinco páginas activas:
- barra principal;
- barra secundaria 1;
- barra secundaria 2;
- barra secundaria 3;
- barra secundaria 4.

Cada barra tiene un estado de página independiente, entre 0 y 9, y 12 slots. El sistema de páginas de ShortcutAPI cambia la página principal; las barras secundarias refrescan sus grupos de 12 slots localmente con UpdateShortcut.

Los botones Prev/Next calculan página anterior/siguiente y hacen wrap-around entre 0 y 9. Para cada barra se actualiza su PageNumTextBox y se refrescan los 12 controles.

El código contiene un conjunto de arrays/estados para las cinco páginas visibles. El valor de página no debe confundirse con el identificador de shortcut global.

## 4. Layouts y presentación XDAT

### Barras horizontales y verticales
- Barra vertical base: **46 × 512**, anclada CENTER_RIGHT.
- Barra horizontal base: **512 × 46**, anclada BOTTOM_RIGHT.
- Slots: 12 controles Shortcut, cada uno de 36 × 36.
- Los fondos BackDropH*_1..12 son texturas de 36 × 36, con espaciado aproximado de 37 px y separaciones mayores en determinados grupos.
- Controles: PrevBtn/NextBtn, PageNumTextBox, RotateBtn, JoypadBtn, LockBtn/UnlockBtn, ExpandButton/ReduceButton y btnKeyOption.

### Barras expandidas
XDAT contiene instancias adicionales verticales y horizontales (_1, _2, _3) para mostrar varias filas/barras. El script muestra u oculta esas instancias según Is1ExpandShortcutWnd, Is2ExpandShortcutWnd, Is3ExpandShortcutWnd e Is4ExpandShortcutWnd.

La función de layout actualiza anclajes para alternar orientación y mantener la barra complementaria pegada al borde inferior/derecho de la otra. No es un simple cambio de textura: mueve anclajes, oculta una orientación y muestra la otra.

### Joypad
- ShortcutWndJoypad: 172 × 82, anclado CENTER_RIGHT; cuatro slots visibles en forma de cruz.
- ShortcutWndJoypadExpand: 432 × 82, anclado CENTER_RIGHT; amplía el panel y contiene slots adicionales.
- El código alterna fondos/posiciones y texturas de JoypadLButtonTex/JoypadRButtonTex para representar la orientación activa.
- El modo joypad muestra cuatro shortcuts en la vista compacta; la expandida presenta más slots.

La geometría XDAT de las instancias secundarias contiene controles con tamaño heredado/omitido, por lo que no se deben reconstruir sus dimensiones suponiendo que cada XML hijo sea autónomo.

## 5. Preferencias persistentes

Al cargar ShortcutWnd, lee:
- Game/IsLockShortcutWnd
- Game/Is1ExpandShortcutWnd
- Game/Is2ExpandShortcutWnd
- Game/Is3ExpandShortcutWnd
- Game/Is4ExpandShortcutWnd
- Game/IsShortcutWndVertical

También lee Custom/ShortcutMovable para el bloqueo de movimiento de la barra. El botón btnMove alterna SetDraggable y guarda el booleano.

Al alternar orientación, expande/contrae barras o cambia el bloqueo, el script persiste la preferencia con SetOptionBool. OnDefaultPosition limpia estados de expansión y restaura posición/layout.

**Distinción:** IsLockShortcutWnd controla el estado Lock/Unlock del panel; ShortcutMovable controla la posibilidad de arrastrar la ventana. Son preferencias separadas.

## 6. Atajos de UI y comandos globales — clase Shortcut

Shortcut registra el evento 90 y lee de Option.ini:
- Key/Panel1, Panel2, Panel3;
- Key/UseBind1, UseBind2, UseBind3.

Si un número de panel es cero, se corrige localmente a 1. La función de actualización escribe de nuevo las tres opciones de panel y los tres flags de binding; también persiste Game/EnterChatting.

El evento 90 lee el campo Command y procesa comandos como:
- CloseAllWindow;
- ShowChatWindow;
- SetPrevChatType / SetNextChatType;
- ShowStatusWnd, ShowSkillWnd, ShowActionWnd, ShowQuestWnd, ShowClanWnd, ShowMinimapWnd;
- HandleShowOptionWnd;
- ForceExit → RequestExit;
- otros comandos de navegación/UI que se continúan en el código.

Estos comandos abren/cambian ventanas y estados del cliente. No equivalen a solicitudes de juego al servidor salvo que una rama invoque explícitamente una API.

## 7. Interacción con la interfaz personalizada

ShortcutWnd contiene controles de personalización:
- btnMove alterna el arrastre y cambia textura/tooltip de candado.
- btnKeyOption abre/cierra InterfaceAI_KeySettingWnd.
- ExpandButton/ReduceButton cambian el número de barras expandidas.
- RotateBtn alterna horizontal/vertical.
- JoypadBtn alterna vista joypad.
- LockBtn/UnlockBtn cambian la preferencia de bloqueo.
- OnEnterState aplica Custom/ShortcutMovable y actualiza tooltip/texture.

El script también gestiona un efecto LoopedTex y un conjunto de controles de firma ShortcutWndSign/ShortcutWndSignV. Su función visual exacta requiere seguir las funciones de animación y productores.

## 8. Límites y riesgos

1. No cambiar el cálculo página × 12 ni el rango 0..9 sin modificar de forma coherente el sistema ShortcutAPI y todos los paneles.
2. No confundir ShortcutID global con slot local.
3. No alterar nombres de controles Shortcut1..12: el código arma las rutas por concatenación de strings.
4. No borrar las instancias secundarias _1/_2/_3: representan las barras adicionales del sistema.
5. No fusionar IsLockShortcutWnd y ShortcutMovable: tienen usos distintos.
6. Conservar los eventos 590–660 y sus payloads.
7. No cambiar la ruta de RequestExit del comando ForceExit.
8. Las barras son presentación/configuración; no hay evidencia de que cambiar su layout altere cooldowns, validación de skills o mecánicas del servidor.

## 9. Pendientes

- Completar análisis de Shortcut.uc: todos los comandos 90, navegación, chat y configuración de teclas.
- Seguir productores de eventos 590–660 y el origen de ShortcutPage/ShortcutID.
- Analizar ShortcutAPI y la persistencia de shortcuts del cliente.
- Revisar los cuatro niveles de expansión y la instancia _4, cuyo estado se lee/oculta pero cuyo XML requiere correlación adicional.
- Identificar uso de ShortcutWndSign/SignV y LoopedTex.
- Comparar layouts con versiones originales Interlude, Gracia, Freya y H5 cuando se disponga de muestras verificadas.
- Validar en cliente la interacción drag/drop, páginas y persistencia.

## 10. Estado

**Confirmado estáticamente:** cinco barras/páginas visibles, 12 slots por página, diez páginas por barra, layouts horizontal/vertical, joypad compacto/expandido, preferencias persistentes y comandos globales de UI.

**Pendiente:** productores de eventos, ShortcutAPI completa, todos los comandos de Shortcut.uc, firmas visuales y prueba de cliente. No se ha recompilado ni cargado una modificación.

## 11. Addendum — asignación real de teclas rápidas

Se cruzaron `Shortcut.uc` con `InterfaceAI_KeySettingWnd.uc`. Esto permite precisar que el sistema no se limita a dibujar barras: también traduce comandos de teclado en comandos de atajo del cliente.

### 11.1 Tres bindings independientes

La ventana de configuración lee y guarda en el archivo Option, sección `Key`:
- `Panel1` + `UseBind1`
- `Panel2` + `UseBind2`
- `Panel3` + `UseBind3`

Los valores Panel1/2/3 se normalizan a 1 cuando el valor leído es 0. Los tres checks se cargan desde INI y se guardan al pulsar OK. Cancel simplemente oculta la ventana; no persiste los cambios temporales.

En la UI, los controles son `checkUseBind1`, `checkUseBind2`, `checkUseBind3`, `checkEnterChat`, y los combos `comboPanel1`, `comboPanel2`, `comboPanel3`. La opción EnterChatting se persiste mediante `SetOptionBool("Game","EnterChatting",...)`, separada de la sección Key.

### 11.2 Familias de teclas

| Binding | Teclas/comandos | Página | Comando ejecutado |
|---|---|---|---|
| Bind1 | 1–12 | Panel1 | `/useshortcut Panel1 slot` |
| Bind2 | Q, W, E, R, T, Y, U, I, O, P, [, ] | Panel2 | `/useshortcut Panel2 slot` |
| Bind3 | F1–F12 | Panel3 o página visible | `/useshortcut page slot` |

La página para F1–F12 se obtiene leyendo PageNumTextBox de la barra horizontal si está visible; de lo contrario lee la vertical. La fuente muestra una conversión/selección directa de texto a entero; el valor exacto se interpreta junto al formato visual del número de página y debe verificarse en ejecución.

### 11.3 Variantes Force

Existe una segunda familia de comandos:
- `UserShorcutKeyForce_1..12` para Bind1;
- `UserShorcutKeyForce_Q..]` para Bind2;
- `UserShorcutKeyForce_F1..F12` para Bind3.

Estas rutas construyen `/useshortcutforce <panel/página> <slot>` en vez de `/useshortcut`. En Bind1 y Bind2 se comprueba el flag UseBind correspondiente antes de ejecutar. En Bind3 se lee la página visible cuando UseBind3 está activo, pero la llamada a ExecuteCommand aparece fuera de ese condicional en la fuente exportada; por tanto, el flag parece condicionar la selección de página y no necesariamente impedir la ejecución. Este detalle debe preservarse como evidencia literal y probarse antes de corregirlo.

No se afirma que la palabra `force` ignore cooldowns, restricciones o validaciones del servidor. La fuente solo demuestra que emite un comando cliente distinto; su semántica final pertenece al motor/protocolo y debe comprobarse por separado.

### 11.4 Comandos globales de UI y duplicados

El evento 90 de Shortcut lee el campo `Command`. Entre las acciones confirmadas:
- alternar ChatWnd y, si está habilitado, SystemMsgWnd;
- recorrer el canal/tab activo del ChatWnd;
- abrir/cerrar ventanas de estado, skills, acciones, quests, clan, minimapa y opciones;
- ejecutar RequestExit para ForceExit;
- llamar métodos de MenuWnd para algunos paneles y shortcuts personalizados.

**Hallazgo que requiere cautela:** el switch exportado contiene etiquetas repetidas como `ShowActionWnd` y `ShowClanWnd` en dos ramas. En un switch convencional la primera coincidencia hace inalcanzable la segunda. No se debe reordenar ni eliminar sin comprobar el bytecode original y el comportamiento del motor; puede ser duplicación real o artefacto de recuperación.

### 11.5 ShortcutAPI: frontera nativa

Las fuentes UC llaman a `NWindow.ShortcutAPI.SetShortcutPage` y `NWindow.UIAPI_SHORTCUTITEMWINDOW.UpdateShortcut`, pero no contienen la implementación de esas clases nativas. La fuente de UI solo demuestra cuándo y con qué argumentos se invocan. Quedan fuera de lo que puede afirmarse estáticamente sobre el motor:
- almacenamiento interno de la lista de atajos;
- semántica exacta del índice de página;
- protocolo de `/useshortcut` y `/useshortcutforce`;
- cooldown, validación de skill/item y tratamiento de objetivos;
- eventos nativos que originan 590–660.

Por tanto, no se puede declarar que ShortcutAPI esté reconstruida ni que los eventos 590–660 tengan productores identificados dentro de estas fuentes. Es probable que parte del contrato pertenezca al runtime de NWindow/cliente.

### 11.6 Ventana de configuración

`InterfaceAI_KeySettingWnd` obtiene la instancia Shortcut mediante GetScript("Shortcut"), carga los seis valores INI y al pulsar OK llama a `Shortcut.MethosRecoveredName_1017` con EnterChatting, tres flags y tres paneles. Ese método actualiza la memoria de Shortcut y persiste todas las claves. Cancel solo oculta la ventana.

### 11.7 Pendientes añadidos

1. Obtener bytecode/exports de NWindow.ShortcutAPI y UIAPI_SHORTCUTITEMWINDOW o una referencia compatible que incluya sus definiciones.
2. Confirmar el formato 0-based/1-based de PageNumTextBox frente al argumento de `/useshortcut`.
3. Verificar en cliente la diferencia entre `/useshortcut` y `/useshortcutforce`; no asumir bypass de restricciones.
4. Comprobar la rama Bind3 Force cuando UseBind3 está desactivado.
5. Revisar las etiquetas repetidas de Shortcut.OnEvent en bytecode.
6. Localizar los productores de eventos 590–660 en código nativo u otras fuentes del cliente.

**Estado del addendum:** flujo de configuración y traducción de teclas confirmado estáticamente. API nativa, semántica de comandos y validación en cliente siguen pendientes.
