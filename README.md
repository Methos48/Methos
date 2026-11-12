# Methos XDAT

Herramienta Windows para investigar, descifrar, editar y compilar archivos de interfaz de Lineage II, con soporte separado para paquetes Unreal (.u) y datos XDAT (.xdat).

## Estado del trabajo

- **Origen de los archivos objetivo:** interfaz creada por Dunk3l; no son archivos originales de L2Sudamerica.
- **Protección exterior:** hay un método público SmartCrypt 1.5.2.2 que captura el flujo de lectura del cliente. Methos ya tiene un capturador experimental y un flujo automatizado de prueba con el cliente exacto, pero falta validar una captura real completa.
- **Capa Lineage II estándar:** implementados codecs para 111 (XOR) y 413 (RSA + zlib), con checksum y validación estructural de la envoltura. Las pruebas sintéticas de ida/vuelta para 111 y 413 ya pasaron en Windows CI (.NET 8.0.428); falta validarlo con una captura real.
- **Archivos .u y .xdat:** se validarán por separado después de retirar las capas de protección. La extensión por sí sola nunca determina el formato.
- **Seguridad:** los originales no se sobrescriben. Las capas desconocidas bloquean la decodificación en vez de adivinar claves o algoritmos.

## Próximo hito

Ejecutar el flujo de captura con una copia del cliente, recuperar el intermedio, detectar si contiene Lineage2Ver111/413 y pasarlo por el codec estándar integrado. Después validar por separado el paquete Unreal y el flujo XDAT y cerrar el ciclo de recompilación.
