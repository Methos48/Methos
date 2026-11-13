# Datos de referencia de Methos

Esta carpeta guarda los kits de compilación, archivos del cliente y paquetes de prueba que usan las validaciones de Methos.

**No es parte de la aplicación distribuida.** Los archivos aquí almacenados no se copian al instalador salvo que un paso de lanzamiento los use expresamente como insumo temporal.

Subcarpetas:

- `kits/`: ZIP históricos de UCC y XDAT.
- `client/lineage2/`: archivos y ZIP de referencia del cliente Lineage II.
- `ucc-runtime/System/`: runtime UCC usado por pruebas.
- `packages/`: paquetes de interfaz y fuentes de prueba.
- `visuals/`: capturas de referencia.

Mantener los originales sin modificaciones. Antes de reemplazar un archivo, confirmar su procedencia y comparar SHA-256. No retirar un kit histórico hasta que la integración equivalente dentro del código mantenido pase las pruebas de compilación, instalación y round-trip.
