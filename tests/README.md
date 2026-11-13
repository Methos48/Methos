# Pruebas de Methos

Esta carpeta contiene las pruebas automatizadas de los módulos y motores de Methos.

- `MethosUEngine.Tests/`: pruebas unitarias del motor Unreal/UCC.
- `ProtectionPipelineSmoke/`: pruebas de integración de codecs, capas de protección y reconstrucción; referencia el único proyecto `src/MethosUEngine/MethosUEngine.csproj`.
- Los archivos `test_*.py` validan el motor XDAT y se apoyan en `tools/xdat/` como ubicación canónica de imports.

No crear un segundo motor para pruebas. Si una prueba necesita una función nueva, incorporarla al proyecto canónico correspondiente y referenciarlo desde aquí. Mantener los paquetes de entrada como fixtures de solo lectura y escribir resultados únicamente en carpetas temporales.
