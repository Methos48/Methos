# Step 30 — Headless probe caller trace: Core.dll bypasses the NPK wrapper

Date: 2026-11-12

## New runtime evidence from artifact 13930541611

The trace file `npkscrypt-api-trace.txt` from workflow run `43190522580` does contain the caller addresses. The previous summary overlooked that this trace is a separate artifact file; the workflow console log does not print its contents.

Relevant trace entries:

```
TARGET_CREATE_A path=SysString-e.dat handle=0000022C
NTREADFILE_CALLER=7742BBBC
CAPTURE_NtReadFile offset=0 bytes=1241967
READFILE_CALLER=10176E65
CAPTURE_ReadFile offset=0 bytes=1241967
```

The loaded module map from the same run identifies:
- `Core.dll = 0x10100000`
- `npkscrypt.dll = 0x75130000`
- `SmartCapture.dll = 0x752A0000`

Therefore `READFILE_CALLER=0x10176E65` is inside **Core.dll**, not inside npkscrypt.dll. The headless test's `FFileStream::CreateStream` path is reading the file through Core.dll directly. The NPK wrapper at preferred VA `0x10119C6C` is not the caller in this test path.

The captured data and Core stream output both remain byte-identical to the source (SHA256 `89fffcb34b0161033f6d2d1b9c650625cd6144fa575ccf3b170b4c0d5f31902d`). This is now explained more narrowly: this particular Core.dll FFileStream route does not demonstrate the game's protected interface-loading chain. It does **not** prove the SmartCrypt transform is inactive in the real client.

## Consequence

Stop treating the headless Core.dll FFileStream read as a decryption test. It is a valid file-I/O control test only.

The next test must reproduce the published SmartCrypt hook chain:
- `SmartDecrypt.dll` (our equivalent probe shim) must be initialized before `npkscrypt.dll`;
- the target must be opened as `SysString-e.dat`;
- the capture must occur at the same chained `ReadFile` point used by the published sample;
- caller addresses must be resolved to module + RVA, and the before/after bytes compared.

The current probe's hook order and API patching are not yet proven equivalent to the published `splice`-based detour. The linked `hook.h` and `hook.cpp` sources still need to be retrieved and compared. If the true chain is not reached by this minimal Core host, a controlled actual-client runtime invocation or a purpose-built loader that invokes the exact NPK wrapper is required.

## Status

- [x] Recover caller addresses from the artifact trace.
- [x] Prove the current ReadFile caller is Core.dll, not npkscrypt.dll.
- [x] Explain why the current Core.dll capture is byte-identical without claiming the actual client transform is inactive.
- [ ] Resolve the ReadFile caller to exact Core.dll function/RVA and disassemble its call path.
- [ ] Reproduce the published SmartCrypt 1.5.2.2 hook chain and order.
- [ ] Capture a genuinely transformed output and test the subsequent 413/111 layer if present.
- [ ] Validate U and XDAT independently.

No original interface files have been modified.
