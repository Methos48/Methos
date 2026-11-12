# Step 23 — Exact npkscrypt.dll static analysis

Date: 2026-11-12

## Exact binary examined

Extracted `npkscrypt.dll` from the user's committed `system.zip` in an isolated GitHub Actions Linux runner. No game process was launched and no input file was modified.

- Size: 859,872 bytes
- MD5: `5908c3ee24b40f6f08f1500723b1a41d` (matches the previously recorded client DLL)
- SHA-256: `6587e052f606fa3b7200ea5f9aebcd800dccdf483ada9d8f4845603f7ae23169`
- PE: x86 (machine `0x014C`)
- Entry-point RVA: `0xA6520`
- Nine sections; the large section `ogojilna` has raw size 841,216 bytes and entropy 7.607, consistent with packed/encrypted content but not proof of a particular packer.

## Confirmed linkage

- `ogg.dll` imports `npkscrypt.dll!Akumu_ICQ_10050005`.
- `npkscrypt.dll` lists exports `Akumu_ICQ_10050005` and `Email_me_at_akumu_dot_ru`, both pointing to RVA `0x28C0`.
- A naive disassembly at that RVA produces invalid-looking x86 instructions. This is not a usable decryption routine. The export table/entry point requires mapping validation and runtime unpacking before interpreting code.
- The DLL imports only a small set of kernel32 functions in its visible import table; its strings include `VirtualProtect`, suggesting further dynamic resolution may occur, but this alone does not prove which APIs are used.

## Consequence for implementation

Do not call the export as if it were a normal C function and do not register it as a codec. The static export is not yet a recovered algorithm. Next steps are to map the entry point and export RVAs against PE sections, inspect the loader/unpacking path, then instrument a disposable Windows process to observe the unpacked module and the actual function contract. Only after the calling convention, arguments, buffers, return values, and transform are known can a safe background runtime adapter be implemented.

## Validation status

The planner smoke workflow passed, including synthetic multi-layer round-trip and refusal to decode the real SmartCrypt candidate without a codec. That validates orchestration only; it does not decrypt the supplied U/XDAT samples.


## Step 23B — loaded-image snapshot (2026-11-12)

The exact DLL was loaded in a disposable x86 process without starting `l2.exe`. The process captured the mapped image after Windows loader initialization:

- LoadLibrary succeeded; mapped base `0x737C0000`; `SizeOfImage=0x16B000`.
- Snapshot: 363 readable pages, 1,486,848 bytes.
- Both exported names resolve to RVA `0x28C0`; the mapped code at that RVA is `mov eax,1; ret`. These exports are success stubs, not file decoders.
- The PE entry point remains at RVA `0xA6520`, inside the high-entropy `ogojilna` section. A mapped-image disassembly still produces heavily obfuscated/virtualized-looking instructions there; ordinary static x86 decoding does not recover a readable file transform.
- The mapped image contains dynamically resolved API names including `ReadFile`, `WriteFile`, `CreateFileW`, `GetProcAddress`, and `VirtualProtect`. The import table alone therefore understates its runtime API use.

This confirms that the earlier static disassembly was incomplete because most executable sections have no raw-file data. The loader snapshot improves the analysis, but it does not expose a usable cipher or key. No decode is claimed.

## Next technical target

Trace the exact client file-read chain after the engine's own file subsystem is initialized. Core.dll exports `FFileStream::Init`, `FFileStream::CreateStream`, and `FFileStream::Read`; their implementations call the Win32 file APIs. The existing worker merely loads Core/engine and performs a .NET file read, so it has not exercised this engine stream path. Build a controlled x86 probe for that path and verify whether it produces a post-protection stream before attempting the standard Lineage II second layer.
