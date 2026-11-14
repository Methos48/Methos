# Auditoría técnica — DialogBox y contrato de confirmaciones compartidas

**Cliente:** L2Sudamerica / Lineage II Interlude.  
**Fuente:** DialogBox.uc (14.757 bytes, 547 líneas), UICommonAPI.uc y DialogBox.xml del artefacto 43579091099.  
**Método:** análisis estático de estados, eventos, controles, wrappers y consumidores. Sin modificar binarios ni probar en cliente.

## 1. Papel en la arquitectura

DialogBox es un diálogo global compartido por numerosas ventanas. UICommonAPI ofrece wrappers para abrirlo, cerrarlo, identificar a qué ventana pertenece, asignar un ID de confirmación, configurar el editor y recuperar la respuesta. DialogBox conserva el estado transitorio y emite dos eventos globales:
- **1710:** confirmación/OK.
- **1720:** cancelación.

La ventana que abrió el diálogo debe comprobar que la respuesta le pertenece antes de ejecutar la acción. UICommonAPI implementa esa protección comparando la identidad guardada con `string(self)`.

## 2. Tipos de diálogo confirmados

La enumeración de UICommonAPI contiene ocho tipos:

| Valor | Constante | Comportamiento observado |
|---:|---|---|
| 0 | DIALOG_OKCancel | Botones OK y Cancel |
| 1 | DIALOG_OK | Botón OK centrado |
| 2 | DIALOG_OKCancelInput | OK/Cancel y campo de entrada |
| 3 | DIALOG_OKInput | OK centrado y campo de entrada |
| 4 | DIALOG_Warning | OK/Cancel; variante de presentación/semántica |
| 5 | DIALOG_Notice | OK centrado; variante de presentación/semántica |
| 6 | DIALOG_NumberPad | OK/Cancel, entrada numérica y teclado numérico |
| 7 | DIALOG_Progress | OK/Cancel y barra temporizada |

La diferencia funcional entre Warning/Notice y los tipos 0/1 no se debe exagerar: el código observado cambia el modo de presentación, botones y anclaje, pero ambos reutilizan la infraestructura común.

## 3. Estado interno

DialogBox conserva:
- texto de mensaje;
- identidad del script que abrió el diálogo;
- tipo activo;
- indicador de diálogo en uso;
- ID de confirmación;
- valores auxiliares para edición/tiempo;
- tipo de acción por defecto (ninguna, OK o Cancel);
- longitud máxima previa del EditBox para restaurarla;
- handles DialogReadingText y DialogBoxEdit.

### Exclusión mutua
Al abrir, si el indicador de diálogo activo ya es verdadero, la función escribe `Error!! DialogBox in Use` y retorna. No encola ni reemplaza la petición. Si está libre, muestra la ventana, prepara el tipo, asigna mensaje, enfoca el diálogo y guarda la identidad del llamador.

**Implicación:** no debe asumirse que varias ventanas pueden abrir confirmaciones simultáneas. Un segundo intento puede ser descartado mientras el diálogo actual está activo.

## 4. Secuencia de apertura

La función de apertura recibe tres argumentos: tipo, texto y nombre/identidad del llamador. La secuencia estática es:
1. comprobar que el diálogo no esté ocupado;
2. mostrar DialogBox;
3. ocultar controles de los modos anteriores y configurar el tipo nuevo;
4. asignar el mensaje;
5. establecer foco;
6. si el EditBox está visible, enfocarlo y aplicar límite de longitud temporal cuando corresponda;
7. guardar la identidad del llamador;
8. marcar el diálogo como ocupado.

El helper de inicialización reinicia identidad, indicador ocupado, tipo de edición, valores de estado y acción por defecto; el límite máximo especial se inicializa en -1.

## 5. Confirmación, cancelación y teclado

### OK
- Si el EditBox está visible, guarda su texto como resultado; si no, guarda cadena vacía.
- Oculta la ventana.
- Desmarca el indicador ocupado.
- Emite ExecuteEvent(1710).

### Cancel
- Oculta la ventana.
- Desmarca el indicador ocupado.
- Emite ExecuteEvent(1720).

El evento global no incluye en esta función un payload explícito con el ID. El ID se mantiene como estado consultable por UICommonAPI. Los consumidores deben recuperar el ID y comprobar la identidad del diálogo.

### Tecla Enter
OnKeyUp detecta Enter (código 13) y ejecuta la acción por defecto:
- 1 → OK.
- 2 → Cancel.
- 0 → Cancel.

Después reinicia la acción por defecto a 0. No se observa que Enter confirme automáticamente por defecto; la ruta predeterminada inicial es Cancel.

### Progreso
Al finalizar el timer del control DialogProgress, OnProgressTimeUp llama a la ruta de cancelación, por lo que el vencimiento produce evento 1720, no 1710. OnHide detiene el control de progreso si el tipo activo es 7.

## 6. Entrada y teclado numérico

- OnChangeEditBox solo procesa el campo DialogBoxEdit cuando el tipo activo es DIALOG_NumberPad.
- Lee la cadena del EditBox y actualiza DialogReadingText mediante ConvertNumToTextNoAdena si la cadena no está vacía.
- Los botones num0..num9 añaden el dígito correspondiente.
- numAll carga el valor auxiliar de referencia si es distinto de cero.
- numBS simula retroceso.
- numC establece el texto a `0`.

El teclado numérico es una interfaz de entrada local. La validación final del valor y su rango corresponde a la ventana llamadora y/o al sistema que procesa la operación.

## 7. Temporizador de progreso

DIALOG_Progress requiere un parámetro de duración distinto de cero. Si es cero, escribe un error de depuración. Si es válido:
1. SetProgressTime;
2. Reset;
3. Start.

El diálogo se ancla BottomCenter solo para progreso; los demás tipos se anclan CenterCenter. Al finalizar, la ruta de timeout cancela y emite 1720. No interpretar el progreso como confirmación de éxito de una operación externa.

## 8. XDAT: estructura visual

Raíz:
- **DialogBox:** 434 × 128, anclada CENTER/CENTER, posición 0,0.
- DialogBody: panel de contenido y base de tamaño utilizada por el script.
- DialogText: texto principal.
- DialogReadingText: representación textual auxiliar, usada por teclado numérico.
- DialogBoxEdit: campo de entrada.
- OKButton, CenterOKButton y CancelButton.
- NumberPad: teclado con botones num0..num9, numAll, numBS y numC.
- DialogProgress: barra de progreso.

El script obtiene los rectángulos de DialogBody y NumberPad para cambiar el ancho total en modo numérico. No fija un ancho literal en código: suma el ancho de ambos rectángulos.

Los botones OK/Cancel se configuran con IDs de texto 1337 y 1342 al cargar, pero UICommonAPI permite reasignar sus nombres mediante SetButtonName.

## 9. Wrappers UICommonAPI y correspondencia

| Wrapper UICommonAPI | Acción DialogBox |
|---|---|
| MethosRecoveredName_25(tipo,texto) | Abre el diálogo y registra `string(self)` |
| MethosRecoveredName_131() | Cierra/oculta el diálogo y reinicia |
| MethosRecoveredName_91() | Establece acción predeterminada OK |
| MethosRecoveredName_63() | Comprueba si el diálogo activo pertenece al llamador |
| MethosRecoveredName_33(id) | Establece ID de confirmación |
| MethosRecoveredName_72() | Recupera ID de confirmación |
| MethosRecoveredName_138() | Recupera texto introducido |
| MethosRecoveredName_352(texto) | Establece texto del EditBox |
| MethosRecoveredName_713(tipo) | Configura tipo de edición |
| MethosRecoveredName_68(valor) | Configura valor auxiliar A |
| MethosRecoveredName_62(valor) | Configura valor auxiliar B |
| MethosRecoveredName_217(valor) | Configura valor auxiliar C |
| MethosRecoveredName_551(valor) | Configura valor auxiliar D |
| MethosRecoveredName_172() | Recupera valor auxiliar B |
| MethosRecoveredName_187() | Recupera valor auxiliar C |
| MethosRecoveredName_552() | Recupera valor auxiliar D |
| MethosRecoveredName_553(valor) | Configura límite de longitud (solo acepta 0 o positivo) |

Los nombres MethosRecoveredName_* son nombres de recuperación de esta exportación. La tabla describe la correspondencia observada, no propone renombrar funciones sin cotejar la tabla de nombres original.

## 10. Consumidores y riesgos

- UICommonAPI concentra la interfaz de llamada y acceso al estado.
- PrivateShopWnd contiene una ruta explícita que, si DialogBox ya está visible, llama a la cancelación (evento 1720) antes de continuar con su propio flujo.
- DeliverWnd guarda un handle a DialogBox y debe examinarse junto con sus eventos de confirmación.
- GMWnd también guarda un handle DialogBox para operaciones de sus formularios.
- En el resto del código, los consumidores suelen referenciar wrappers de UICommonAPI en vez de DialogBox directamente; una búsqueda por el nombre de clase no basta para localizar todos los llamadores.

**Riesgos de modificación:**
1. quitar la comprobación de ocupado permitiría que un diálogo sobrescriba otro;
2. cambiar 1710/1720 rompe confirmaciones de múltiples ventanas;
3. perder la identidad del llamador permite que una ventana consuma una respuesta ajena;
4. cambiar el valor por defecto de Enter altera la seguridad de acciones confirmables;
5. alterar el timeout de progreso puede convertir una operación pendiente en una cancelación prematura o tardía;
6. no restaurar MaxLength puede afectar diálogos posteriores;
7. cambiar el parser de número o el texto auxiliar puede alterar cantidades introducidas.

## 11. Próximos pasos

1. Recorrer todos los consumidores de wrappers MethosRecoveredName_25, _63, _33, _72, _138 y eventos 1710/1720.
2. Construir tabla por ventana: tipo de diálogo, ID, acción OK/Cancel, validación y solicitud resultante.
3. Completar DeliverWnd, GMWnd y PrivateShopWnd con la secuencia exacta de confirmación.
4. Comparar DialogBox con versiones originales Interlude/Gracia/Freya/H5 cuando se disponga de paquetes verificados.
5. Conservar los ocho tipos y el contrato global de eventos en cualquier cambio de UI.

## 12. Estado

**Confirmado estáticamente:** los ocho tipos, exclusión de diálogo único, identidad del llamador, ID, eventos 1710/1720, comportamiento de Enter, entrada numérica, timeout y estructura XDAT principal.

**Pendiente:** matriz completa de consumidores, payloads de confirmación por ventana, posiciones de cada subcontrol y validación real en cliente. No se ha compilado ni cargado un paquete modificado.