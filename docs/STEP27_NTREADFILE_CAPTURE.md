# Step 27 — Native read capture and key-state activation

Date: 2026-11-12

## Code change

Commit `a8ef019941b61100504ea2226329f1a5de037d31` extends `tools/smartcrypt-capture.cpp` with a MinHook detour for `ntdll!NtReadFile`. The capture shim now observes both:
- `kernel32!ReadFile`
- `ntdll!NtReadFile`

The capture records absolute offsets and byte counts. This is diagnostic instrumentation only; it is not a decryption codec.

## Exact runtime result

Workflow run: [43187897191](https://github.com/Methos48/Methos-XDAT/actions/runs/43187897191)

The x86 headless host:
- loaded the exact client `ogg.dll`, `Core.dll`, and `npkscrypt.dll`;
- initialized SmartCapture hooks successfully;
- called `FFileStream::CreateStream` on the exact supplied `interface.u` renamed `SysString-e.dat`;
- opened the file through `CreateFileA`;
- generated both `CAPTURE_NtReadFile offset=0 bytes=1241967` and `CAPTURE_ReadFile offset=0 bytes=1241967` events.

All three output files were byte-identical:
- source `SysString-e.dat`
- Core stream buffer `filestream-read.bin`
- hook capture `smartcrypt-capture.bin`

SHA-256 for all: `89fffcb34b0161033f6d2d1b9c650625cd6144fa575ccf3b170b4c0d5f31902d`.

The capture hook is now proven to observe the actual Core file read. The absence of transformed bytes is not a capture blind spot.

## Key activation state

At the time of the read:
- `npkscrypt.dll` was loaded;
- `dsetup.dll` was not loaded;
- ordinal `0x67` was unavailable;
- global key pointer at image offset `0x51430` was zero;
- activation flag at image offset `0x51468` was zero.

Therefore the stream transform is inactive in this headless host. The earlier reconstructed XOR transform shape cannot be used to decode the files until the per-handle key state is obtained from the correct initialization path.

## Important correction

The previous read-capture implementation already called `SmartCapture_Initialize` from the host and installed the `CreateFileA/W` and `ReadFile` hooks. The latest trace confirms this. The missing observation was the native read boundary, now covered by `NtReadFile`.

## Next reverse-engineering target

Do not guess keys or register a codec. Trace the protection initialization and identify the real provider for the missing module/ordinal path. Inspect the client loader chain and any separately distributed protection/bootstrap files, then instrument the initialization routine and the per-handle state constructor. A candidate codec must pass exact known-answer decoding and round-trip tests on both `interface.u` and `interface.xdat`.

## Status

- [x] Confirm Core opens the supplied file.
- [x] Capture native and Win32 read buffers.
- [x] Prove the current headless stream is byte-identical to the source.
- [x] Confirm the protection key pointer and activation flag remain zero.
- [ ] Recover the matching key provider/initialization.
- [ ] Decode and structurally validate `interface.u`.
- [ ] Decode and structurally validate `interface.xdat`.
- [ ] Implement and verify reversible codecs.
