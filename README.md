# Methos XDAT

Aplicación para Windows destinada a detectar, descifrar, descompilar, editar y volver a compilar archivos de interfaz de Lineage II: Unreal `.u` y XDAT `.xdat`.

## Estado del trabajo

### Archivos Dunk3l originales

Los originales `interface.u` y `interface.xdat` se conservan intactos. Se identificaron y probaron las dos capas presentes en ambos archivos:

1. SmartCrypt 1.5.2.2 con semilla embebida.
2. Envoltura Lineage2Ver111 con el tratamiento personalizado de los datos finales.

**Prueba real aprobada:** ambos archivos se descifran y vuelven a cifrar byte por byte, reproduciendo exactamente los originales.

### Motor Unreal (.u)

- El archivo Dunk3l es un paquete Unreal **126/0**.
- Tablas verificadas: **5.482 nombres, 740 imports y 10.308 exports**.
- La reconstrucción sin modificaciones conserva las tablas originales.
- UELib pudo descompilar **167 de 167 clases**, sin errores de exportación.
- El motor UCC de Interlude ya compila y vuelve a descompilar el paquete de referencia de 169 clases.
- Está en curso la prueba de compilar las 167 clases reales de Dunk3l y reconstruir su `.u` protegido.

### Motor XDAT (.xdat)

- El archivo Dunk3l se descompila en **169 ventanas y 8 accesos directos**.
- Se corrigió la conservación de listas internas, cadenas ANSI/Latin-1 y metadatos originales.
- **Prueba real aprobada:** descompilar a XML, recompilar y obtener el mismo contenido binario, byte por byte.
- La reconstrucción conserva la capa Lineage2Ver111 y vuelve a aplicar SmartCrypt con los mismos datos originales.

### Otros formatos y muestras reales

- Lineage2Ver111: XOR.
- Lineage2Ver413: RSA por bloques y zlib.
- SmartCrypt 1.5.2.2: variante con semilla embebida comprobada en los dos archivos Dunk3l.
- ActiveAntiCheatCrypt: motor RSA-2048/PKCS#1 v1.5 + RC4 integrado; requiere el perfil de clave privada correspondiente.
- `SysString-e.dat` aportado como muestra real: descifrado y recompilado con protocolo 413; 14.896 bytes cifrados y 29.584 bytes recuperados.

## Reglas de seguridad del programa

- El archivo original fuera de Methos nunca se modifica.
- La copia de trabajo se guarda separada de los archivos descompilados.
- Los perfiles de cifrado y los metadatos originales se conservan al importar modificaciones.
- Si falta una clave o no se puede verificar una capa, Methos se detiene y no genera un archivo que parezca válido sin serlo.
- Antes de guardar un archivo compilado, se vuelve a descifrar y se compara con el contenido compilado para comprobar la reconstrucción.

## Próximo objetivo

Completar la compilación real de las 167 clases Dunk3l con UCC, validar el archivo protegido resultante y terminar la aplicación Windows con el flujo común para `.u` y `.xdat`.
