# Pruebas de Methos

Esta carpeta contiene las pruebas automatizadas de los módulos y motores de Methos.

- `MethosUEngine.Tests/`: pruebas unitarias del motor Unreal/UCC.
- `ProtectionPipelineSmoke/`: pruebas de integración del motor Unreal/UCC y reconstrucción de paquetes; referencia `src/MethosUEngine/MethosUEngine.csproj`.
- `ProtectionPlannerSmoke/`: pruebas rápidas del planificador de protección, codecs conocidos y rechazo seguro de formatos desconocidos.
- Los archivos `test_*.py` validan el motor XDAT y se apoyan en `tools/xdat/` como ubicación canónica de imports.

No crear un segundo motor para pruebas. Si una prueba necesita una función nueva, incorporarla al proyecto canónico correspondiente y referenciarlo desde aquí. Mantener los paquetes de entrada como fixtures de solo lectura y escribir resultados únicamente en carpetas temporales.
