# Auditoría técnica — UISublimityCommonAPI y SublimityTimerActor

**Paquete:** Interface.u de L2Sudamerica / Interlude.  
**Fuentes:** `UISublimityCommonAPI.uc`, `SublimityTimerActor.uc`, referencias cruzadas en `SublimityShortcutWnd.uc`.  
**Método:** análisis estático. No se enviaron solicitudes al servidor ni se ejecutó el cliente.

## 1. Posición en la jerarquía

`UISublimityCommonAPI` hereda de `UICommonAPI`. `SublimityItem` hereda de esta clase, por lo que ToolTip no participa en esta rama de herencia: ToolTip deriva directamente de UICommonAPI.

La clase es pequeña (137 líneas) pero funciona como capa común para las ventanas Sublimity. Centraliza:
- identificadores de eventos;
- creación de un actor temporizador;
- envío de eventos internos;
- solicitudes bypass al servidor;
- persistencia de preferencias individualizadas por personaje.

## 2. Constantes de eventos

Declara constantes para:
- 631;
- 25000–25004;
- 25100–25102;
- 100000–100022;
- 200000.

La tabla completa aparece en el código fuente. Son constantes de contrato para el subsistema Sublimity; la mera declaración no prueba que cada evento tenga productor o consumidor en este paquete. El evento 631 y varios 25000/100000 se consumen en SublimityItem.

## 3. Temporizador Sublimity

La función de inicialización instancia:
`new (none, "SublimityTimerActor") Class'Interface.u.include-tail.SublimityTimerActor'`

El actor exportado:
- hereda de Actor;
- está marcado `notplaceable`;
- no contiene propiedades ni funciones adicionales en el fuente recuperado.

La función que envía datos de casteo construye un payload con:
- `SkillID`;
- `CastTime`;

y ejecuta el evento interno **100009**. Otra función ejecuta el evento **100010** sin payload. No se debe atribuir al actor temporizador un comportamiento de timer concreto sin recuperar su bytecode o el mecanismo nativo que consume esos eventos.

## 4. Solicitudes PIN y bot-check

La clase incluye tres métodos que generan bypasses explícitos:
- `request_pin?pin=<valor>`;
- `create_pin?pin=<valor>`;
- `request_bot_check?pin=<valor>`.

El método bot-check oculta primero la ventana `SublimityBotCheckWnd` y luego llama a RequestBypassToServer.

Esto demuestra que la interfaz construye y envía solicitudes al servidor; **no** demuestra qué valida el servidor, si el PIN es correcto, qué respuesta devuelve ni qué privilegios habilita. No se debe cambiar ni automatizar este flujo como si fuera una opción puramente visual. Queda fuera de una modificación de interfaz inocua sin autorización y especificación del servidor.

También existe una función que construye un evento 100009 con SkillID y CastTime, que es una señal de temporización de casteo; no modifica por sí misma el tiempo real del servidor.

## 5. Preferencias por personaje

Cuatro funciones implementan wrappers de lectura/escritura de Option INI:
- lectura int;
- lectura bool;
- escritura int;
- escritura bool.

La clave real se compone así:
`<PlayerName>_<PreferenceName>`

El nombre del personaje se obtiene con GetPlayerInfo. Si no hay información de jugador:
- lecturas int devuelven 0;
- lecturas bool devuelven false;
- escrituras no guardan nada.

Esto explica por qué SublimityShortcutWnd puede guardar `ShortcutExpandCount`, `ShortcutIsVertical`, `ShortcutPage_0..3` y `ShortcutStub` bajo claves individualizadas por personaje, aunque en la llamada solo aparezca el nombre lógico de la preferencia.

## 6. Dependencias confirmadas

- `SublimityItem` hereda de UISublimityCommonAPI y utiliza su capa de eventos.
- `SublimityShortcutWnd` utiliza los wrappers de preferencias por personaje para persistir expansión, orientación y páginas.
- El actor SublimityTimerActor es creado por UISublimityCommonAPI; el actor exportado no contiene lógica visible propia.
- El puente de red utilizado por PIN/bot-check es RequestBypassToServer, no una API visual local.

## 7. Riesgos de modificación

1. No sustituir las preferencias por personaje por claves globales sin confirmar el comportamiento deseado.
2. No borrar el prefijo de nombre de personaje: causaría colisiones entre personajes.
3. No alterar los identificadores 100009/100010 sin rastrear consumidores.
4. No modificar las cadenas bypass de PIN/bot-check como parte de una mejora visual.
5. No asumir que SublimityTimerActor está vacío en el binario original solo porque su fuente exportada no contiene métodos.
6. No tratar las constantes de evento como prueba de que el evento se emite o procesa correctamente.

## 8. Pendientes

- Recuperar bytecode de UISublimityCommonAPI y SublimityTimerActor.
- Encontrar consumidores/productores de 100009 y 100010.
- Localizar todas las ventanas que usan RequestBypassToServer y documentar el contrato de servidor sin ejecutarlo.
- Resolver todos los eventos 25000–25004, 25100–25102 y 100000–100022.
- Comparar la persistencia por personaje con las claves globales del sistema Shortcut estándar.
- Verificar en cliente que cada personaje conserva independientemente sus preferencias.

## 9. Estado

**Confirmado estáticamente:** herencia, constantes, wrappers de preferencias por personaje, creación del actor, payload de SkillID/CastTime y tres rutas de bypass PIN/bot-check.

**Pendiente:** comportamiento nativo del temporizador, consumidores de eventos, respuestas del servidor y pruebas en cliente. No se ejecutaron solicitudes ni se modificaron binarios.
