# Methos XDAT

Herramienta Windows para investigar, descifrar, editar y compilar archivos de interfaz de Lineage II, con soporte separado para paquetes Unreal (.u) y datos XDAT (.xdat).

## Estado del trabajo

- **Archivos objetivo:** `interface.u` y `interface.xdat` de la interfaz de Dunk3l. Ambos siguen conservados sin modificar; su envoltura exterior `FF ED FA CE 01 00` todavía no está descifrada.
- **SmartCrypt 1.5.2.2:** el capturador automático ya carga con el cliente y obtiene lecturas completas. Las capturas actuales de `interface.u` siguen siendo idénticas al original, así que todavía no son una salida descifrada. Falta activar/alcanzar la transformación real de NPK y recuperar su inversa para recompilar.
- **Capa Lineage II estándar:** codecs 111 (XOR) y 413 (RSA + zlib) integrados. Ambos pasan pruebas de ida/vuelta sintéticas. Además, el `Engine.u` real del cliente se descifró con 111, validó su CRC32 y reveló un paquete Unreal 123/30.
- **Motor U integrado:** lector y constructor de tablas Unreal, exportador de clases con UELib, compilador de bytecode limitado y backend UCC de Interlude. El paquete real `Engine.u` se analizó, reconstruyó y volvió a cifrar; sus 7058 nombres, 91 imports y 10647 exports se conservaron.
- **Motor XDAT integrado:** codec Interlude ct0 que convierte el paquete en XML por ventanas y accesos directos y lo recompila. El ejemplo de referencia contiene 176 XML (168 ventanas y 8 accesos directos); las pruebas confirman recompilación idéntica byte por byte y descompilar→compilar idéntico.
- **Ejemplos de referencia:** el `Interface.u` de Methos-U-Editor y el `interface.xdat`/XML del kit XDAT se usan como fixtures separados. No se confunden con los archivos Dunk3l.
- **Seguridad:** los originales no se sobrescriben. Las capas desconocidas bloquean la decodificación en vez de adivinar claves o algoritmos.

## Próximo hito

Cerrar el descifrado exterior SmartCrypt para `interface.u` y `interface.xdat`, probar la detección y el ciclo de ambos formatos sobre los archivos Dunk3l y completar la integración de los motores U/XDAT en una sola aplicación Windows.
