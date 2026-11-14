# Auditoría técnica — SublimityShortcut y barra extendida de cuatro paneles

**Cliente:** L2Sudamerica / Lineage II Interlude.  
**Fuentes:** SublimityShortcutWnd.uc (fuente exportada), SublimityShortcut.uc y referencias cruzadas con ShortcutWnd/Shortcut.  
**Método:** análisis estático. No se modificaron binarios ni se realizaron pruebas en cliente.

## 1. Separación respecto al sistema estándar

Este conjunto es distinto de `ShortcutWnd` y `Shortcut`:
- `SublimityShortcutWnd` y `SublimityShortcut` heredan de `SublimityItem`, no de UICommonAPI.
- El panel extendido reutiliza controles y rutas visuales con nombres `ShortcutWnd.ShortcutWndHorizontal/Vertical`.
- Llama a la misma API nativa `NWindow.ShortcutAPI.SetShortcutPage` y al mismo `UIAPI_SHORTCUTITEMWINDOW.UpdateShortcut`.

Por tanto, no es correcto tratarlo como un segundo motor de atajos independiente. Es una capa alternativa de organización/presentación que comparte el backend de shortcuts del cliente.

## 2. Cuatro paneles administrados

Constantes visibles:
- 4 paneles secundarios, índices internos 0..3.
- 12 slots por página.
- 10 páginas por panel, índices 0..9.
- 120 índices globales de shortcut considerados por el array de estado.

El estado principal usa:
- `ShortcutPage_0` a `ShortcutPage_3` para las páginas de los cuatro paneles.
- `ShortcutExpandCount` para la cantidad de paneles expandidos.
- `ShortcutIsVertical` para orientación.
- `IsLockShortcutWnd` para bloqueo.
- `ShortcutStub` como marcador de inicialización.

Cada panel tiene PrevBtn/NextBtn, y el primer panel utiliza SetShortcutPage. Los paneles secundarios actualizan su página local y refrescan los controles de ambas orientaciones.

## 3. Inicialización y persistencia

La función de inicialización verifica `Game/ShortcutStub`. Si no es 100, inicializa:
- ShortcutStub = 100;
- ShortcutExpandCount = 0;
- ShortcutIsVertical = false;
- IsLockShortcutWnd = false;
- ShortcutPage_0..3 = 0..3.

Después carga los valores desde Game INI. Esta inicialización es un esquema de compatibilidad/persistencia propio de SublimityShortcutWnd; no debe confundirse con las claves Is1ExpandShortcutWnd..Is4ExpandShortcutWnd del ShortcutWnd estándar.

Al guardar orientación se escribe ShortcutIsVertical. El número de paneles expandidos se guarda como ShortcutExpandCount. El bloqueo reutiliza IsLockShortcutWnd, compartido con la barra estándar: dos sistemas pueden afectar la misma preferencia.

## 4. Refresco y asignación de slots

El refresco recorre 120 índices. Para cada índice comprueba si pertenece al intervalo de página de alguno de los cuatro paneles. Si corresponde:
- actualiza el ShortcutItemWindow horizontal;
- actualiza el ShortcutItemWindow vertical;
- alterna la textura indicadora F1..F12 según el estado guardado del índice.

El intervalo de pertenencia se calcula a partir de la página del panel y bloques de 12. La función de refresco de página actualiza PageNumTextBox para las dos orientaciones y solicita UpdateShortcut para los 12 slots visibles.

Las funciones de mostrar/ocultar textura F recorren los cuatro paneles y solo actúan sobre el panel cuyo rango contiene el índice global.

## 5. Eventos y botones

OnLoad registra eventos:
- 650;
- 631;
- 630;
- 640;
- 90.

La ventana esconde los botones JoypadBtn de ambas orientaciones. OnShow fija el foco en las texturas F1Tex..F12Tex.

Botones identificados:
- PrevBtn/NextBtn del panel 0;
- PrevBtn2/NextBtn2;
- PrevBtn3/NextBtn3;
- PrevBtn4/NextBtn4;
- RotateBtn;
- ExpandButton/ReduceButton;
- LockBtn/UnlockBtn.

El evento 631 existe en esta implementación pero no en el registro de ShortcutWnd estándar. Su payload y productor deben seguirse antes de asignarle semántica.

## 6. SublimityShortcut: evidencia incompleta

La clase `SublimityShortcut`:
- hereda de SublimityItem;
- registra evento 25002;
- declara arrays de 256 enteros y dos arrays de 256 strings;
- declara un struct de tres enteros;
- inicializa un estado a -1;
- contiene una función que retorna inmediatamente sin ejecutar lógica visible;
- contiene una función booleana que combina bits 16, 17 y 18 del argumento.

La fuente exportada no contiene una implementación funcional completa de manejo de atajos para esta clase. No se debe rellenar el hueco suponiendo qué significa el evento 25002 ni los arrays. Puede haber lógica en la clase padre, bytecode, código nativo o partes no recuperadas.

## 7. Diferencias con ShortcutWnd estándar

| Característica | ShortcutWnd estándar | SublimityShortcutWnd |
|---|---|---|
| Barras/paneles | Barra principal + cuatro extensiones visuales | Cuatro paneles administrados en un array |
| Estado de expansión | Is1ExpandShortcutWnd..Is4ExpandShortcutWnd | ShortcutExpandCount |
| Orientación | IsShortcutWndVertical | ShortcutIsVertical |
| Página | UI principal + estados de paneles | ShortcutPage_0..3 |
| Eventos | 590, 600, 610, 620, 630, 640, 650, 660, 150 | 630, 631, 640, 650, 90 |
| Joypad | Sí, compacto/expandido | Los botones Joypad se ocultan |
| Backend | ShortcutAPI / UIAPI_SHORTCUTITEMWINDOW | Las mismas APIs |

No asumir equivalencia de preferencias: comparten algunas claves y controles, pero usan esquemas de expansión y orientación distintos.

## 8. Riesgos de compatibilidad

1. No reemplazar ShortcutExpandCount por los cuatro booleanos del sistema estándar sin migración explícita.
2. No borrar ShortcutPage_0..3: la ventana los usa para restaurar cada panel.
3. No modificar el cálculo de rangos de 12 slots ni el límite 120 sin revisar los controles XML y API nativa.
4. No eliminar el evento 631 hasta localizar su productor.
5. No cambiar IsLockShortcutWnd sin considerar que se comparte con la barra estándar.
6. No inferir la semántica del evento 25002 desde el nombre SublimityShortcut.
7. No afirmar que se han recuperado los 256 registros internos: solo se identificaron arrays declarados en fuente.

## 9. Pendientes

- Recuperar `SublimityItem` y analizar herencia/dispatch de eventos.
- Identificar el productor y payload de evento 631 y 25002.
- Seguir `OnEvent` completo y las funciones de arrastre/slot de SublimityShortcutWnd.
- Comparar XML de ShortcutWnd para identificar controles concretos de los paneles 0..3.
- Comprobar convivencia real con ShortcutWnd estándar, especialmente claves compartidas.
- Obtener bytecode de SublimityShortcut para resolver funciones stub y arrays.
- Validar en cliente las cuatro páginas, persistencia y actualización de shortcuts.

## 10. Estado

**Confirmado estáticamente:** arquitectura de cuatro paneles, 12 slots por página, diez páginas, 120 índices considerados, claves de persistencia, eventos registrados, controles de navegación y reutilización del backend nativo.

**Pendiente:** semántica de eventos 631/25002, clase base SublimityItem, bytecode incompleto y pruebas de coexistencia. No se ha compilado ni cargado una modificación.