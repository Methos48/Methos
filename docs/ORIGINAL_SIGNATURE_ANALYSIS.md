# Informe de análisis de los archivos originales U y XDAT

Fecha: 2026-11-11

## Archivos examinados

Los archivos se recuperaron sin modificación desde el artefacto de GitHub Actions `Methos-Lineage2-Original-Files`.

| Archivo | Tamaño | SHA-256 |
|---|---:|---|
| `interface.u` | 1,241,967 bytes | `89fffcb34b0161033f6d2d1b9c650625cd6144fa575ccf3b170b4c0d5f31902d` |
| `interface.xdat` | 705,813 bytes | `7ea4e8830c24b3d803b32429040eb673da87e15c5f83126c4b5c6904825b16bb` |

## Hallazgos binarios

Ambos archivos comienzan con la misma secuencia de seis bytes:

```
FF ED FA CE 01 00
```

El séptimo byte ya es diferente entre los archivos, por lo que no se debe asumir que el resto del encabezado o el flujo cifrado es idéntico.

| Indicador | interface.u | interface.xdat |
|---|---:|---:|
| Entropía global (bits/byte) | 7.986685 | 7.981903 |
| `Lineage2Ver` encontrado | No | No |
| Firma Unreal `C1 83 2A 9E` encontrada | No | No |
| Firma `FF ED FA CE` | Offset 0 | Offset 0 |

La entropía se mantiene elevada a lo largo de bloques de 64 KiB. No aparecen cadenas de texto legibles que permitan reconocer directamente las estructuras habituales de los archivos.

En XDAT, el primer DWORD little-endian es `0xCEFAEDFF` (unsigned: 3,472,551,423), fuera del rango esperado para el contador inicial de ventanas de un XDAT plaintext Interlude.

## Interpretación

1. Los dos archivos tienen un encabezado común de seis bytes y contenido de alta entropía.
2. Esto es compatible con una envoltura de protección/cifrado personalizada compartida, aplicada a dos tipos de archivo distintos.
3. No hay evidencia suficiente para atribuirla específicamente a SmartCrypt, LameCrypt, L2Crypt u otro algoritmo concreto.
4. No se debe tratar `FF ED FA CE` como una versión estándar `Lineage2Ver###`.
5. No se debe intentar decodificar con protocolos estándar 111/120/121/211/212/41x/61x/81x/91x sin primero retirar o identificar la capa externa.
6. Los archivos originales permanecen intactos.

## Próximas pruebas necesarias

- Localizar en el cliente o en las herramientas de protección el código que reconoce la firma `FF ED FA CE`.
- Comparar el encabezado con muestras conocidas de SmartCrypt y otras protecciones, usando muestras completas y hashes verificables.
- Buscar en el ejecutable del cliente las rutinas que leen esta firma y reconstruir el flujo de descifrado.
- Después de retirar la capa externa, volver a identificar el formato interno de cada archivo por separado: Unreal package para U y estructura XDAT para XDAT.
- Validar cada resultado con un parser real y con una recompilación de prueba; nunca sobrescribir los originales.

## Límite de la conclusión

Este informe documenta observaciones reproducibles de los bytes. La entropía alta y la firma compartida no prueban por sí solas el algoritmo, la clave ni siquiera que ambos usen exactamente la misma rutina de cifrado.
