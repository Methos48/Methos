# Runtime codec findings — L2 Sudamérica client

Analysis date: 2026-11-11  
Scope: static inspection only. No game executable or DLL was run or modified.

## Input files verified

All three PE files are 32-bit x86 (PE32, machine 0x014C).

| File | Size | SHA-256 |
|---|---:|---|
| l2.exe | 491,540 | 5967D5F6221F091D5B557D27F9A40BFB31BA2BA965B57D9B42F94F9B4FEB5707 |
| Core.dll | 1,372,180 | D83449B1CDF0AC717A98BE9289CAAB03CB507324A696EF449E31369E28416639 |
| engine.dll | 30,741,504 | FE4135724D552FB8B0117FB6C9A661252CA86F1822E275632A571BD661F1F98E |

## Confirmed module relationship

- `l2.exe` imports `Core.dll` and `Engine.dll`.
- `l2.exe` imports codec and archive functions from `Core.dll`, including `FCodec::Decode`, `FCodec::Encode`, `FArchive::GetCurrentEncryptVersion`, and the global `GL2DESCodec`.
- `engine.dll` imports `Core.dll`, including `FCodec::Decode`, `FCodec::Encode`, and `FArchive::GetCurrentEncryptVersion`.
- `engine.dll` also imports `ReadFile`, `CreateFileA`, and `CreateFileW` from Kernel32. This makes file-I/O observation a possible later diagnostic route, but does not by itself prove which exact files pass through those calls.

## Crypto / archive symbols found in Core.dll and l2.exe

The Core.dll export table contains these named symbols:
- `FBlowFish::BlowfishDecrypt`
- `FBlowFish::BlowfishEncrypt`
- `FBlowFish::Blowfish_decipher`
- `FBlowFish::Blowfish_encipher`
- `FCodec::Decode` (multiple overloads)
- `FCodec::Encode` (multiple overloads)
- `GL2CurrentTxtCodec`
- `GL2DESCodec`
- `GL2UseKeyCrypt`
- `FArchive::GetCurrentEncryptVersion`

The l2.exe RTTI/string data also contains:
- `FL2RSACodec`
- `FL2DESCodec`
- `FL2FileDecryptArchive`
- `FL2FileEncryptArchive`

These are strong evidence that the client contains native file archive encryption/decryption classes and several codec implementations (DES, RSA, Blowfish-related routines). They are not yet proof of which codec is selected for a particular `.u` or `.xdat` file.

## Implications for Methos

There are at least two distinct protection/decoding layers to keep separate:

1. The SmartCrypt/NProtect wrapper previously identified around the supplied files. The known SmartCrypt 1.5.2.2 approach is to capture the bytes after the client-side wrapper has read/decrypted them.
2. The Lineage II client-native codec/archive layer exposed by `Core.dll` and referenced by `l2.exe` / `engine.dll`. The exact selection and order for `.u` versus `.xdat` still needs to be mapped.

Do not assume that a successful SmartCrypt capture is already a fully decoded Unreal package or XDAT file. Validate each stage using the expected file signatures and structural parsers.

## Next investigation

1. Map references/call sites for `FL2FileDecryptArchive`, `FL2DESCodec`, `FL2RSACodec`, and `GL2UseKeyCrypt` in the exact client binaries.
2. Determine whether the target `.u` and `.xdat` files pass through the same native archive path or separate readers.
3. Only then implement a staged decode pipeline with immutable input copies, output hashes, and validation. Never overwrite the user's original files.

## Limits

This is static import/export/RTTI/string analysis. No algorithm has been fully reverse-engineered, no file has yet been successfully decoded by these newly identified routines, and no runtime hook has been validated against this client build.
