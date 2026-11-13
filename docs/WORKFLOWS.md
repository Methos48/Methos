# Workflows de Methos

Los workflows son automatizaciones de mantenimiento, investigación y validación. No son aplicaciones independientes ni instaladores separados.

## Distribución del producto

- `.github/workflows/windows-release.yml`: único flujo oficial de distribución. Produce `Methos.exe` y el instalador único `Methos-Setup.exe`, con los módulos U y XDAT.

## Compilación y validación U

- `windows-build.yml`: análisis manual de paquetes U, inspección de referencias y prueba exploratoria con UELib; genera artefactos de diagnóstico, no distribuye Methos.
- `validate-ucc-compiler.yml`: verifica el runtime UCC, fuentes y compilación.
- `validate-unreal-engine.yml`: valida el motor de paquetes Unreal y el round-trip U.
- `inspect-dunk3l-ucc-reference.yml`, `inspect-dunk3l-interface-api.yml`, `inspect-dunk3l-native-ui-api.yml`: inspecciones técnicas de paquetes y API de referencia.

## XDAT y cliente

- `inspect-xdat.yml`, `silent-dark-roundtrip.yml`, `protection-pipeline-smoke.yml`: inspección y pruebas de ida/vuelta U/XDAT.
- `inspect-client.yml`, `inspect-runtime.yml`, `analyze-npkscrypt.yml`, `npkscrypt-*.yml`, `smartcrypt-*.yml`: investigación y captura de evidencia del cliente y sus protecciones.

## Archivo de fuentes

- `source-archive.yml`: genera el archivo de fuentes del único repositorio oficial.

## Reglas

1. Solo `windows-release.yml` distribuye Methos como producto.
2. Los workflows de investigación pueden generar artefactos diagnósticos; no deben publicar un segundo ejecutable o instalador para XDAT.
3. Todos los workflows deben trabajar desde `Methos48/Methos`; no incorporar checkouts de repositorios históricos.
4. Los insumos binarios se mueven solo tras actualizar rutas y verificar hashes.
5. Una prueba fallida debe quedar identificada como tal; no ocultarla ni declarar el producto validado por una compilación exitosa aislada.
