# Mantenimiento del kit XDAT

Scripts usados por el workflow de lanzamiento para preparar una copia temporal del kit XDAT:

- `apply_detection_layer.py`: aplica la detección de cifrado a la aplicación fuente extraída.
- `auto_pipeline_overlay.py`: incorpora el pipeline, los manifiestos de origen, las pruebas y las rutas de trabajo Methos.

Ambos scripts localizan el repositorio desde su propia ruta y trabajan sobre la carpeta del kit que reciben como argumento. No deben modificar los ZIP ni los paquetes originales del repositorio.

El flujo oficial está en `.github/workflows/windows-release.yml`. Si se cambia una ruta aquí, actualizar también los filtros de ejecución y comprobar la instalación completa.
