# Investigación histórica del motor nativo UE2

> **Estado:** referencia histórica; no define la arquitectura activa ni el flujo de distribución de Methos.

Este documento conserva observaciones técnicas obtenidas durante una exploración temprana de lectura y escritura de paquetes Unreal Engine 2. No debe interpretarse como un producto, proyecto, motor independiente ni como una promesa de compatibilidad funcional.

## Arquitectura vigente

Methos es una sola aplicación Windows. La aplicación principal está en `src/Methos.App/` y su biblioteca interna está en `src/Methos.Engine/`. La compilación y validación UCC usa el runtime de referencia descrito en `docs/ARQUITECTURA.md`. No existe un producto independiente llamado Methos U Editor ni un proyecto activo `MethosUEngine`.

## Observaciones técnicas conservadas

- En UELib, `UStruct.ScriptOffset` puede ser relativo al búfer serializado del objeto; para obtener un desplazamiento absoluto hay que considerar `Export.SerialOffset`.
- Para ciertos paquetes anteriores a la versión 639, `DataScriptSize` y `ByteScriptSize` pueden coincidir y el bytecode puede comenzar después del campo de tamaño de 32 bits. Validar siempre contra el perfil real del paquete.
- Los mapas de tokens de bytecode varían entre ramas de Unreal Engine. No asumir valores de tokens sin verificar versión y licencia del paquete.
- La aceptación por un parser UE2 genérico no demuestra que un archivo pertenezca a Lineage II ni que sea compatible con Interlude.
- Los archivos originales deben conservarse; cualquier modificación debe realizarse sobre copias y validarse con pruebas de ida y vuelta.

## Límites

Estas notas proceden de investigación previa y no sustituyen las pruebas actuales de `Methos.Engine`, UCC ni las validaciones de `windows-release.yml`. No afirmar que un paquete se puede compilar o reconstruir correctamente basándose solo en estas observaciones.
