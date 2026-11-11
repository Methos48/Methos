# Methos XDAT

Aplicación Windows para descompilar y compilar archivos XDAT de Lineage II Interlude.

## Carpetas de trabajo

Dentro de la carpeta de instalación se crean automáticamente:

- `Archivos XDAT/Comprimido`: archivos `.xdat` de entrada y resultados compilados.
- `Archivos XDAT/Descomprimido`: ventanas y accesos directos XML extraídos o preparados para compilar.

No es necesario crear las carpetas manualmente.

## Detección de cifrado

Al seleccionar un archivo, Methos analiza su cabecera y deriva el método reconocido al motor disponible. Si el formato o cifrado no se reconoce, muestra primero **«No se reconoce este cifrado»** y después un diagnóstico técnico (tamaño, firma hexadecimal, estado, versión y motivo). El archivo original se conserva.

El reconocimiento de una versión no significa que todas las claves o protecciones privadas sean compatibles. Las variantes sin motor o sin clave válida se informan como no soportadas; no se fuerza una conversión.

## Estado del soporte

La rama de investigación contiene detección de 18 versiones documentadas y un adaptador para el motor público `open-l2encdec`. La cobertura real de cada protocolo debe validarse con muestras cifradas y pruebas de ida y vuelta antes de considerarse estable.
