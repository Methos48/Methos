# L2Sudamerica — auditoría profunda de DetailStatusWnd, ClanWnd y ClanDrawerWnd

**Estado:** análisis estático cruzado de las fuentes UnrealScript recuperadas y los XML XDAT.
**Cobertura:** panel de atributos del personaje, ventana de clan y panel ampliado de administración del clan.
**No realizado:** ejecución en cliente, validación de permisos contra servidor, recompilación ni edición de binarios.

## 1. DetailStatusWnd — atributos detallados del personaje

### Geometría y estructura XDAT
- Ventana raíz: 348 × 465, anclada arriba a la izquierda.
- El XML contiene 103 nombres de controles.
- Incluye una variante/estructura clásica de 340 × 457 dentro del árbol recuperado.
- La interfaz separa información de identidad, clan, nivel/clase, recursos, estadísticas de combate, atributos básicos y datos sociales.

### Controles identificados
- Identidad: txtName1, txtName2, txtHeadPledge, texPledgeCrest, txtPledge, texHero, txtLvHead, txtClass, txtHeadRank, txtRank.
- Recursos y progreso: texHP, texMP, texExp, texCP, texWeight, txtHP, txtMP, txtExp, txtCP, txtWeight, txtSP.
- Combate: encabezados y valores para ataque físico, defensa física, precisión, crítico, velocidad de ataque, ataque mágico, defensa mágica, evasión, velocidad de movimiento y velocidad de casteo.
- Atributos: STR, DEX, CON, INT, WIT y MEN.
- Social: criminalidad/PK, PvP, socialidad y puntos de sufragio restantes.
- Contenedores y grupos: Container, DetailStatusWndClassic, grupos de recursos y grupos de estadísticas.

### Fuente y eventos
- 831 líneas, 36,711 bytes, 22 funciones recuperadas.
- Registra eventos 180, 260, 3000, 190, 200, 210, 220, 230 y 240.
- Evento 180 reconstruye el panel completo.
- Eventos 200–240 comparten una rutina de actualización parcial basada en el payload; cada ID debe correlacionarse con el evento global que lo produce antes de asignarle con seguridad HP/MP/CP/EXP u otras estadísticas.
- Evento 260 procesa una actualización adicional mediante una rutina propia.
- Evento 3000 lee el campo Index y vuelve a entrar en un estado interno de actualización.
- Obtiene datos del personaje mediante GetPlayerInfo.
- OnClickButton solo maneja BtnClose, que oculta la ventana.
- OnShow y OnHide ejecutan rutinas auxiliares de presentación/estado; falta resolver completamente esas funciones recuperadas.

**Conclusión parcial:** DetailStatusWnd es un panel de lectura de datos del personaje, no una interfaz que solicite cambios de estadísticas. No se encontró en el manejador de botones una acción para alterar atributos.

## 2. ClanWnd — resumen y accesos a funciones de clan

### Geometría y controles XDAT
- Ventana raíz 280 × 411.
- XML: 32 nombres de controles.
- Resumen: nombre del clan, líder, castillo/agit, estado, nivel y miembros actuales.
- Selector ComboboxMainClanWnd y lista ClanMemberList.
- Botones: ClanMemInfoBtn, HeroBtn, ClanMemAuthBtn, ClanBoardBtn, ClanInfoBtn, ClanPenaltyBtn, ClanQuitBtn, ClanWarInfoBtn, ClanWarDeclareBtn, ClanWarCancleBtn, ClanAskJoinBtn, ClanAuthEditBtn, ClanTitleManageBtn y BtnClose.

### Eventos y despacho
Registra 11 eventos: 320, 330, 420, 400, 440, 410, 450, 150, 160, 340 y 480. El OnEvent despacha explícitamente:
- 420 → limpia el contenido.
- 320 → actualiza una sección del resumen/listado.
- 410 → actualiza datos de miembros.
- 440 → procesa otra respuesta de información de clan.
- 400 → actualiza la lista/estado de guerras.
- 450 → procesa respuesta de la lista de grados/autorizaciones.
- 330 → actualiza una sección de clan.
- 480 → procesa otra respuesta de gestión.
- 340 → actualiza una sección adicional.

Los eventos 150 y 160 están registrados, pero no tienen rama explícita en el switch mostrado. Debe investigarse si se consumen indirectamente o si el registro quedó por compatibilidad.

### Acciones demostradas por código
- Información de miembro: selecciona un registro y llama RequestClanMemberInfo(clanType, memberName); después activa el estado de detalle en ClanDrawerWnd.
- Autorizaciones de miembro: RequestClanMemberAuth(clanType, memberName); abre el estado de autorización.
- Tablero de clan: ejecuta evento 1190 con Index=3.
- Penalizaciones: ejecuta el comando pledgepenalty.
- Salida del clan: RequestClanLeave(...).
- Gestión de guerras: RequestClanWarList(0,0), RequestClanDeclareWar(), RequestClanWithdrawWar().
- Gestión de autorizaciones/grados: RequestClanGradeList().
- Acceso a gestión de emblemas/título y a ventana de héroes.
- La solicitud para unirse al clan se delega a una rutina interna.

La UI solicita operaciones mediante funciones Request; el resultado y la autorización final pertenecen al servidor. La presencia de un botón no garantiza que cualquier miembro tenga permiso para usarlo.

## 3. ClanDrawerWnd — panel de administración ampliado

### Geometría y escala
- Ventana raíz 256 × 512.
- XML de 198,169 bytes y 182 nombres de controles.
- Es un contenedor multipágina: los nombres y grupos de controles muestran módulos para miembros, autorizaciones, información, habilidades, guerras, emblemas y opciones administrativas.
- El árbol XDAT incluye subventanas anidadas para cuadros de edición, confirmación y selección; 182 nombres no equivalen a 182 controles visibles simultáneamente.

### Módulos identificados
- Miembros: controles Clan1_* (nombre, grado, título, caballería/knighthood y selección).
- Cambiar nombre de miembro: Clan1_ChangeMemberNameWnd, Clan1_ChangeNameTextEditbox y botones asignar/cancelar/eliminar.
- Cambiar grado: Clan1_ChangeMemberGradeNameWnd y Clan1_MemberGradeList.
- Asignación de aprendiz: Clan1_AssignApprenticeWnd y Clan1_AssignApprenticeList.
- Autorizaciones: ClanMemberAuthWnd y familias Clan2_Check...
- Información de clan: ClanInfoWnd y controles Clan3_*.
- Lista de guerras: Clan8_DeclaredListCtrl y Clan8_GotDeclaredListCtrl.
- Habilidades: ClanSkillWnd.
- Emblemas/crest: Clan7_RegEmbBtn, Clan7_RmEmbBtn, Clan7_RegEmb2Btn y controles asociados.
- Hay controles de selección, listas, checkboxes y tooltips adicionales que requieren un inventario XML completo por módulo.

### Eventos registrados
Registra 10 eventos: 350, 360, 370, 380, 430, 460, 490, 500, 160 y 470. El código contiene un despachador extenso que alimenta diferentes estados de gestión; falta elaborar una tabla evento→rutina→datos de entrada→control actualizado para cada caso.

### Operaciones de miembros y clan encontradas
- Invitar al miembro seleccionado al grupo: RequestInviteParty(nombre).
- Expulsar miembro: RequestClanExpelMember(clanType, nombre).
- Eliminar aprendiz: RequestClanDeletePupil(nombre, ...).
- Cambiar apodo/título de miembro: RequestClanChangeNickName(nombre, texto); una ruta envía cadena vacía para borrar el título.
- Cambiar grado: RequestClanChangeGrade(nombre, grado).
- Asignar aprendiz: RequestClanAssignPupil(clanType, nombre).
- Reorganizar miembro: RequestClanReorganizeMember(...).
- Editar permisos: RequestEditClanAuth(...).
- Gestionar guerra: RequestClanWarList, RequestClanWithdrawWarWithClanName y RequestClanDeclareWarWidhClanName.
- Registrar/quitar crest y emblema: RequestClanRegisterCrest, RequestClanUnregisterCrest, RequestClanRegisterEmblem y RequestClanUnregisterEmblem.
- Consultar detalle de miembro: RequestClanMemberInfo(...).
- Cambiar caballería/knighthood y gestionar lista de miembros asignables.
- Las operaciones sensibles se disparan desde botones/selecciones concretas y dependen de las respuestas y permisos del servidor.

### Matriz de permisos
La rutina OnClickCheckBox procesa tres familias:
- Familia 1: checks individuales Clan6_Check10... y maestro Clan6_Check100; el maestro activa/desactiva en bloque nueve checks.
- Familia 2: checks Clan6_Check20... y maestro Clan6_Check200; el bloque recorre cinco checks.
- Familia 3: checks Clan6_Check30... y maestro Clan6_Check300; el bloque recorre nueve checks.
La selección de un checkbox maestro propaga el valor a cada checkbox de la familia. La semántica de cada índice de permiso no debe adivinarse: hay que mapear IDs y textos de autorización del XML con los datos que recibe el servidor.

### Temporizadores y estados
- ClanDrawerWnd implementa OnTimer, OnShow, Clear, HideWindow y un sistema de estados internos.
- El temporizador no demuestra por sí solo una consulta periódica al servidor; hay que inspeccionar el cuerpo completo y los IDs de timer antes de afirmarlo.
- El código usa LVDataRecord y LVDataList para transportar nombre, tipo de clan y otros campos entre listas y solicitudes. Los índices deben rastrearse antes de asignarles significado.

## 4. Relaciones entre ventanas
1. ClanWnd actúa como resumen y punto de entrada.
2. Seleccionar un miembro y pulsar información o autorización genera una solicitud y cambia el estado de ClanDrawerWnd.
3. ClanDrawerWnd presenta la información extendida y expone operaciones administrativas.
4. Las listas usan registros con campos reservados para mantener el identificador/tipo de clan y el nombre seleccionado; las peticiones no dependen solo del texto visible.
5. DetailStatusWnd es independiente de la administración del clan, aunque comparte datos de identidad, nivel, clase, recursos y estadísticas.

## 5. Riesgos y límites
- Los nombres MethosRecoveredName_N y __NFUN_N__ son sustitutos; no son símbolos originales.
- No atribuir significado semántico a cada código de evento sin localizar su productor.
- No asumir que todos los botones son utilizables por todos los rangos.
- La UI muestra y solicita; la autoridad final sobre expulsión, grados, permisos, guerras y cambios de clan corresponde al servidor.
- El número de nombres XDAT incluye controles anidados/ocultos; no equivale a controles visibles al mismo tiempo.
- No se ha confirmado persistencia local ni se ha probado una petición real en servidor.

## 6. Próximas verificaciones
1. Resolver el mapa completo de eventos 320–500 entre ClanWnd y ClanDrawerWnd.
2. Decodificar los campos LVDataRecord usados por las solicitudes de clan.
3. Cruzar cada checkbox de autorización con su texto/ID de sistema y función de servidor.
4. Reconstruir los estados de ClanDrawerWnd y la visibilidad de cada subventana.
5. Mapear los IDs de clanType y sus valores válidos.
6. Inspeccionar las funciones completas de guerras, caballerías, aprendices, emblemas y habilidades de clan.
7. Construir el índice de recursos de crest/emblem/texturas referenciadas.
8. Verificar el comportamiento en cliente de pruebas, sin modificar el original ni alterar reglas del servidor.

## 7. Evidencias
- decoded-u/sources-include-tail/DetailStatusWnd.uc
- decoded-u/sources-include-tail/ClanWnd.uc
- decoded-u/sources-include-tail/ClanDrawerWnd.uc
- decoded-xdat/interface.xdat.include-tail-xml/windows/DetailStatusWnd.xml
- decoded-xdat/interface.xdat.include-tail-xml/windows/ClanWnd.xml
- decoded-xdat/interface.xdat.include-tail-xml/windows/ClanDrawerWnd.xml

**Conclusión:** el conjunto documenta el panel de estadísticas del personaje y una interfaz de clan extensa, con lectura de miembros, autorizaciones, rangos, aprendices, guerras, habilidades y emblemas. Se ha demostrado el flujo UI→Request para varias operaciones; no se ha demostrado su resultado real ni sus permisos en servidor.
