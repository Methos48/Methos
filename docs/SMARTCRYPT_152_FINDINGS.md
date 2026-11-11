# SmartCrypt 1.5.2.2 — hallazgos y plan de extracción

Fecha: 2026-11-11

## Conclusión actual

La protección exterior de los archivos originales system/interface.u y system/interface.xdat queda identificada con alta confianza como SmartCrypt 1.5.2.2. La rutina exacta de cifrado todavía no está reconstruida; el método documentado disponible recupera los bytes después de que el cliente los procesa en tiempo de ejecución.

## Evidencia reproducible del cliente L2Sudamérica

Del ZIP system.zip del usuario se extrajeron los siguientes archivos sin modificar los originales:

| Archivo | Evidencia |
|---|---|
| system/npkscrypt.dll | Versión PE 1.5.2.2; MD5 5908c3ee24b40f6f08f1500723b1a41d |
| system/ogg.dll | Su tabla de importación incluye npkscrypt.dll!Akumu_ICQ_10050005 |
| system/interface.u | SHA-256 89fffcb34b0161033f6d2d1b9c650625cd6144fa575ccf3b170b4c0d5f31902d; comienza FF ED FA CE 01 00 |
| system/interface.xdat | SHA-256 7ea4e8830c24b3d803b32429040eb673da87e15c5f83126c4b5c6904825b16bb; comienza FF ED FA CE 01 00 |
| system/L2Patch.u | Comienza C1 83 2A 9E, firma de paquete Unreal normal; demuestra que no todos los .u del cliente llevan la misma envoltura |

El MD5 de npkscrypt.dll coincide exactamente con una muestra publicada que identifica la DLL como SmartCrypt 1.5.2.2. La coincidencia de versión/hash y la dependencia importada desde ogg.dll son evidencias mucho más fuertes que la entropía o la firma de seis bytes consideradas aisladamente.

## Método documentado encontrado

El tema público “Smart Crypt Decryptor 1.5.2.2” describe una técnica de captura en tiempo de ejecución:

1. Se carga una DLL auxiliar antes de npkscrypt.dll a través de la tabla de importaciones de ogg.dll.
2. La DLL auxiliar intercepta CreateFileW para reconocer el archivo cuyo nombre contiene SysString-e.dat.
3. Registra el identificador de archivo correspondiente.
4. Intercepta ReadFile y copia los bytes que el cliente ya ha leído y procesado.
5. Para probar otro archivo, se utiliza una copia con el nombre SysString-e.dat y se ejecuta L2.exe; el cliente puede mostrar un error después de leerlo. La técnica documentada indica que el resultado intermedio queda todavía cifrado con el cifrado estándar de Lineage II (por ejemplo 413/111), por lo que hace falta una segunda etapa.

Fuente pública: https://maxcheaters.com/topic/249577-smart-crypt-decryptor-1522/

## Correcciones necesarias antes de integrar el método

El ejemplo de código publicado no debe copiarse tal cual:

- Escribe nNumberOfBytesToRead, que es el tamaño solicitado, en vez de *lpNumberOfBytesRead, que es el tamaño realmente leído. Esto puede añadir bytes no válidos al archivo capturado.
- Usa un std::map global sin sincronización; varios hilos pueden leer y modificarlo simultáneamente.
- No gestiona de forma correcta lecturas superpuestas (OVERLAPPED) ni el caso de operaciones asíncronas.
- No valida rigurosamente INVALID_HANDLE_VALUE, lecturas fallidas, lecturas de cero bytes ni cierre/reutilización de identificadores.
- El filtro de nombre debe ser robusto frente a mayúsculas/minúsculas y rutas completas.
- La captura debe ir a un archivo temporal, conservar el original y registrar tamaños/errores para validar que no se truncó ni duplicó información.

## Arquitectura prevista para Methos

El proceso debe ser independiente del tipo de archivo:

1. Capa SmartCrypt: recuperar el flujo posterior a SmartCrypt mediante el cliente compatible.
2. Capa Lineage II estándar: identificar y retirar la versión interna real (por ejemplo 413/111); no asumirla sin inspeccionar el encabezado intermedio.
3. Parser específico: validar como paquete Unreal en el módulo U o como estructura XDAT en el módulo XDAT.
4. Validación: comprobar estructura, recuentos, límites y posibilidad de volver a compilar; nunca reemplazar el archivo original automáticamente.

## Material que falta para ejecutar la primera prueba real

El ZIP recibido contiene ogg.dll, npkscrypt.dll, l2ui.dll, L2Patch.u y los archivos interface.u/interface.xdat, pero no incluye L2.exe ni engine.dll. Para ejecutar la captura con el cliente real hacen falta esos binarios (y Core.dll si el cliente lo utiliza).

## Estado

- [x] Identificar versión/hash de SmartCrypt en el cliente.
- [x] Confirmar que ogg.dll importa npkscrypt.dll.
- [x] Confirmar que U y XDAT originales comparten la cabecera exterior.
- [x] Encontrar un procedimiento público específico para SmartCrypt 1.5.2.2.
- [ ] Implementar una captura robusta y no destructiva.
- [ ] Ejecutar una prueba con el L2.exe/engine.dll exactos del cliente.
- [ ] Retirar la capa Lineage II estándar del resultado intermedio.
- [ ] Validar U y XDAT con sus parsers independientes.

Límite: todavía no se ha producido ni validado una decodificación completa de los dos archivos originales. La identificación de la protección está sustentada; la extracción real sigue pendiente de la prueba de ejecución.
