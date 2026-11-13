# Herramientas de desarrollo

Esta carpeta contiene herramientas auxiliares para inspección y diagnóstico. No son ejecutables de producto y no deben confundirse con Methos.exe.

- `PackageProbe/`: utilidad de diagnóstico para paquetes Unreal; la usa el workflow manual de análisis.
- `analyze_dunk3l_interface.py`: análisis de paquetes de referencia de Dunk3l.
- `analyze_npkscrypt.py`: inspección de cabeceras y datos de protección del cliente.
- `npkscrypt-headless-probe.cs`, `smartcrypt-capture-probe.cs`, `smartcrypt-capture.cpp`, `smartcrypt-capture.def`, `smartcrypt-probe-host.cpp` y `core-filestream-probe.cpp`: fuentes de sondas de investigación; no forman parte de la distribución.
- `make_probe_systextures.py` y `patch_ogg_imports.py`: preparación de entornos de prueba.
- `xdat/`: código mantenido del módulo XDAT; ver su [README](xdat/README.md).

Las pruebas ejecutables están bajo `tests/`. No crear proyectos de prueba duplicados dentro de `tools/`.
