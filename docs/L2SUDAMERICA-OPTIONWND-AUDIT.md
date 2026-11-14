# L2Sudamerica — auditoría profunda de OptionWnd y pestañas

**Estado:** análisis estático de fuente UnrealScript recuperada y XML XDAT decodificado.  
**Alcance:** `OptionWnd.uc`, `OptionWnd.xml`, `OptionTab0.xml` a `OptionTab4.xml`.  
**No realizado:** ejecución dentro del cliente, recompilación UCC, edición de binarios ni prueba de persistencia en servidor.

## 1. Identificación y geometría

- Clase: `OptionWnd extends UIScript`.
- Ventana raíz XDAT: `OptionWnd`, 389 × 420, anclada al centro.
- Contiene cinco botones de pestaña: `Tab_Btn0` … `Tab_Btn4`, además de `BtnClose`.
- Las cinco páginas son ventanas XDAT independientes de 256 × 432:
  - `OptionTab0`: vídeo/gráficos.
  - `OptionTab1`: audio.
  - `OptionTab2`: juego y comportamiento general.
  - `OptionTab3`: opciones adicionales de interfaz.
  - `OptionTab4`: indicadores flotantes de daño y eventos.
- La clase enlaza controles de las páginas mediante rutas como `OptionWnd.Cb_ShowDamage`, aunque los controles se encuentran físicamente en los XML de pestaña. No confundir la ruta lógica de UIAPI con el nombre del archivo XML.

## 2. Inventario de controles por pestaña

### OptionTab0 — vídeo (38 nodos nombrados)

Combos: `ResBox`, `RefreshRateBox`, `GammaBox`, `TextureBox`, `ModelBox`, `AnimBox`, `TerrainBox`, `CharBox`, `PawnBox`, `ReflectBox`, `AABox`, `CaptureBox`, `HDRBox`, `WeatherEffectComboBox`.

Checks: `FrameBox`, `ShadowBox`, `DecoBox`, `TriBox`, `GPUAnimationCheckBox`.

Botones: `VideoOKBtn`, `VideoCancelBtn`, `VideoApplyBtn`. También hay etiquetas `text0` … `text13`.

La clase obtiene resoluciones mediante `GetResolutionList`, refrescos mediante `GetRefreshRateList`, shader mediante `GetShaderVersion`, antialiasing mediante `GetMultiSample`, y comprueba capacidades con `IsEnableEngSelection` y `CanUseHDR`. La lista AA se habilita/deshabilita según el nivel de multisample disponible; HDR se agrega solo cuando `CanUseHDR()` es verdadero. El selector de idioma también depende del idioma actual y de `IsEnableEngSelection()`.

### OptionTab1 — audio (14 nodos nombrados)

Sliders: `EffectVolumeSliderCtrl`, `MusicVolumeSliderCtrl`, `SystemVolumeSliderCtrl`, `TutorialVolumeSliderCtrl`.

Check: `mutecheckbox`.

Botones: `AudioOKBtn`, `AudioCancelBtn`, y un botón llamado `VideoApplyBtn` dentro de este XML (nombre inusual que debe conservarse hasta verificar referencias y no renombrarse por intuición).

La casilla de mute deshabilita los cuatro sliders cuando está marcada y los vuelve a habilitar al desmarcarla.

### OptionTab2 — juego (26 nodos nombrados)

Comboboxes: `LanguageBox`, `LootingBox`.

Checks: `NameBox0` … `NameBox5`, `ZoneNameBox`, `GametipBox`, `CursorBox`, `ArrowBox`, `EnterChatBox`, `OldChatBox`, `KeyboardBox`, `JoypadBox`, `CameraBox`, `DuelBox`, `DropItemBox`, `OpacityBox`.

Botones: `GameOKBtn`, `GameCancelBtn`, `GameApplyBtn`, `WindowInitBtn`.

La lógica de `NameBox2` habilita o deshabilita `NameBox3`, `NameBox4` y `NameBox5` según su estado. El checkbox `SystemMsgBox` también gobierna la disponibilidad de `DamageBox` y `ItemBox`; estos tres nombres aparecen en la lógica UC aunque no forman parte de los controles enumerados de `OptionTab2.xml`, por lo que su pertenencia y creación deben rastrearse en los XML restantes (probablemente el subsistema de filtros de chat) antes de asignarlos a una pestaña.

### OptionTab3 — opciones adicionales (22 nodos nombrados)

- `Cb_ShowMiniMap`: activa `Options/ShowMiniMap`; muestra/oculta `RadarMapWnd` y `RadarMapDrag`.
- `Cb_PartyIcons`: `Options/CustomPartyIcons`.
- `Cb_SkillCast`: `Options/SkillCast`.
- `Cb_ChatTransparency_`: control visual de transparencia de chat; el valor se almacena en `Options/ChatSlider`.
- `Cb_DualCount`: `Options/ShowPvP/Pk`; muestra/oculta `MenuWnd.txtPVP2` y `MenuWnd.PVP1`.
- `Cb_Time`: `Options/ShowTime`; muestra/oculta `MenuWnd.Time` y `MenuWnd.Current`.
- `Cb_AutoPotions`: `Options/AutoPotions`; muestra/oculta `AutoPotionsWnd`.
- `Cb_AutoSS`: `Options/AutoSS`; muestra/oculta `AutoShotItemWnd`.
- `Cb_FastLearn`: `Options/FastLearn`.
- `Cb_PartyControl`: `Options/PartyControl`; muestra/oculta `SaveLoadParty`.
- `SD_ChatTransparency`: slider vinculado a `ChatSlider`.
- `SD_BarTransparency`: slider vinculado a `BarSlider`.
- `Ed_BuffIcons`: campo de edición relacionado con el tamaño de iconos de buff; el valor se lee como `Options/Buff Size`.
- Etiquetas: `TX_Chat`, `TX_ChatValue`, `TX_SkillBar`, `TX_BarValue`, `TX_BuffIcons`, `TX_Credits`, `TX_Credits_2`.

El XML incluye 8 checks y 2 sliders. La fuente inicializa los handles de checkbox para estas opciones y los valores se consultan al reconstruir el estado de la interfaz.

### OptionTab4 — daño/eventos (27 nodos nombrados)

- Maestro: `Cb_ShowDamage` → `Options/ShowDamage`.
- Grupo normal: `Cb_Damage`, `Cb_HP`, `Cb_MP`, `Cb_CP`, `Cb_EXP`, `Cb_Miss`, `Cb_Resist`, `Cb_Block`, `Cb_Critical`, `Cb_Critical2`, `Cb_Overhit`, `Cb_Successful`, `Cb_Failed`, `Cb_Spoiled`.
- Grupo enemigo: `Cb_EDamage`, `Cb_EMiss`, `Cb_EResist`, `Cb_ECritical`.
- Botones: `OnBtn`, `OffBtn`.
- Etiquetas: `MainText`, `MainText2`, `TX_Credits_3`, `TX_Credits_4`.

Los checks escriben en la sección `Damage` las claves `Normal Damage`, `HP+`, `MP+`, `CP+`, `EXP+`, `Miss`, `Resist`, `Block`, `Critical`, `Magic Critical`, `OverHit`, `Successful`, `Failed`, `Spoiled`, `Enemy Damage`, `Enemy Miss`, `Enemy Resist` y `Enemy Critical`. Son filtros/configuración de visualización; no constituyen evidencia de que el cliente cambie daño real ni mecánicas del servidor.

## 3. Eventos registrados por OptionWnd

En `OnLoad` se registran exactamente estos eventos:

| ID | Evidencia observada |
|---:|---|
| 510 | Lee `Video/IsKeepMinFrameRate`, actualiza `FrameBox`, llama rutinas de estado de frame y refresca controles relacionados. |
| 520 | Llama a `MethosRecoveredName_370()`; el significado del evento requiere resolver esa rutina y su productor. |
| 1550 | Marca un flag interno verdadero y llama a `MethosRecoveredName_370()`. |
| 1560 | Marca el flag interno falso y llama a `MethosRecoveredName_370()`. |
| 150 | Habilita `Tab_Btn3` y `Tab_Btn4` y reconstruye opciones adicionales mediante `MethosRecoveredName_1285()`. |

También registra estados `GamingState` y `LoginState`. Esto prueba que la ventana tiene rutas de estado distintas según contexto de juego/login, pero no basta para deducir todas las restricciones de cada pestaña.

## 4. Inicialización y configuración gráfica

Durante `OnLoad`:

1. Obtiene shader y resoluciones.
2. Ejecuta `SetOptionBool("Game", "HideDropItem", false)`. Es una escritura explícita durante la carga; debe investigarse si es un valor temporal, un restablecimiento intencional o una peculiaridad de esta interfaz. No asumir que es persistencia permanente.
3. Rellena `ResBox` con ancho × alto y profundidad de color.
4. Construye `RefreshRateBox` con las frecuencias disponibles.
5. Consulta multisampling y configura `AABox` (0: deshabilitado; 1: dos opciones; 2: tres opciones según ramas observadas).
6. Rellena idioma según `GetLanguage()`; el inglés puede habilitarse o no según `IsEnableEngSelection()`.
7. Si `CanUseHDR()` es verdadero, agrega las opciones HDR con IDs de texto 1230, 1231 y 1232.
8. Llama a rutinas internas de inicialización/carga de opciones.

No se han asignado todavía nombres semánticos fiables a las funciones recuperadas como `MethosRecoveredName_409`, `638`, `637`, etc. Esos identificadores son marcadores de recuperación, no nombres originales demostrados.

## 5. Persistencia y límites de lo demostrado

La fuente usa las APIs `GetOptionBool`, `SetOptionBool`, `GetOptionInt`, `SetOptionInt` y controles UIAPI. En esta auditoría se ha confirmado el nombre de las secciones y claves que aparecen explícitamente en las llamadas, pero no el archivo físico, backend o momento exacto de escritura. Para resolverlo hay que rastrear las APIs nativas/importadas y comparar valores antes/después en cliente.

La rutina `MethosRecoveredName_1285()` vuelve a leer múltiples claves de `Options` y `Damage` y actualiza checks/sliders/campo de tamaño de buff. Esto confirma que las opciones adicionales se reconstruyen desde el almacén de configuración del cliente. La lista incluye las claves citadas en la sección 2; el mapeo completo de cada handle a su variable recuperada sigue pendiente porque el descompilado ha saneado nombres.

## 6. Relaciones con otros subsistemas

- `ShowMiniMap` controla RadarMapWnd y RadarMapDrag.
- `CustomPartyIcons` debe cruzarse con `PartyWnd` y sus ventanas compactas.
- `SkillCast` debe cruzarse con `SkillCastWnd`.
- `ShowPvP/Pk` y `ShowTime` modifican controles de `MenuWnd`.
- `AutoPotions` abre/cierra `AutoPotionsWnd`; no prueba por sí solo el algoritmo de uso automático.
- `AutoSS` abre/cierra `AutoShotItemWnd`.
- `PartyControl` abre/cierra `SaveLoadParty`.
- `ShowDamage` y las opciones `Damage/*` conectan con `CustomDamageWnd`; la orientación exacta de cada canal (daño propio/recibido) requiere correlacionar IDs de eventos con mensajes y pruebas controladas.
- Transparencias de chat y barra deben cruzarse con los widgets consumidores para distinguir valor almacenado de efecto final.

## 7. Próximas verificaciones necesarias

1. Desofuscar las variables `MethosRecoveredName_4593` … `4623` correlacionando orden de declaraciones, inicialización y controles.
2. Resolver funciones recuperadas 370, 409, 638, 637, 418, 632, 631, 630, 286, 439, 440, 1285 y 1519 por sus efectos y llamadas.
3. Localizar los controles `SystemMsgBox`, `DamageBox` e `ItemBox` en todos los XML y documentar su ventana propietaria.
4. Rastrear productores de eventos 510, 520, 1550, 1560 y 150.
5. Identificar la implementación real de las APIs de opciones y su persistencia.
6. Comparar cada opción con el consumidor que la aplica, sin inferir que una casilla cambia una mecánica de juego.
7. Validar en cliente, sin modificar el paquete original, una vez que exista un entorno de pruebas controlado.

## 8. Archivos de evidencia

- `decoded-u/sources-include-tail/OptionWnd.uc`
- `decoded-xdat/interface.xdat.include-tail-xml/windows/OptionWnd.xml`
- `decoded-xdat/interface.xdat.include-tail-xml/windows/OptionTab0.xml`
- `decoded-xdat/interface.xdat.include-tail-xml/windows/OptionTab1.xml`
- `decoded-xdat/interface.xdat.include-tail-xml/windows/OptionTab2.xml`
- `decoded-xdat/interface.xdat.include-tail-xml/windows/OptionTab3.xml`
- `decoded-xdat/interface.xdat.include-tail-xml/windows/OptionTab4.xml`

**Conclusión:** OptionWnd no es solo un panel de ajustes gráficos: es el centro de configuración que conecta vídeo, audio, filtros de chat, minimapa, indicadores de estado, automatizaciones de interfaz, party, barras y daño flotante. El mapa anterior está basado en llamadas y controles encontrados; quedan pendientes la resolución de símbolos recuperados, productores de eventos, persistencia física y validación dentro del cliente.
