# Step 29 — Dunk3L connection and SmartCrypt 1.5.2.2 read-hook method

Date: 2026-11-12

## New primary lead

A public MaxCheaters thread titled **“Smart Crypt Decryptor 1.5.2.2”** contains a concrete method for extracting protected Lineage II files. It explicitly names both `interface.u` and `interface.xdat`, instructs the operator to rename the target to `SysString-e.dat`, and describes loading `SmartDecrypt.dll` before `npkscrypt.dll` through the `ogg.dll` import order. The thread also includes the C++ hook source and the expected output file `decrypted_file.txt`.

The thread includes a post by the user **Dunk3L** on 2024-08-01, who says “I got it...” after the author points them to the compiled DLL. This is a strong provenance/research connection to the Dunk3L name, but does not by itself prove that the target files were encrypted with this exact version. Keep the user's stated authorship/provenance of the supplied interface as the working target identity; verify the cryptographic match experimentally.

Reference:
- https://maxcheaters.com/topic/249577-smart-crypt-decryptor-1522/

## Method described in the thread

The published SmartDecrypt shim:
1. Loads `engine.dll`.
2. Hooks `kernel32!ReadFile` and `kernel32!CreateFileW`.
3. Tracks the handle opened for a filename containing `SysString-e.dat`.
4. After calling the next `ReadFile` implementation, writes the resulting buffer to `decrypted_file.txt`.
5. Is inserted before `npkscrypt.dll` in the `ogg.dll` import order.

The thread says the resulting file is still protected by ordinary L2 encryption (examples 413/111), so the output is a two-stage pipeline:
- SmartCrypt 1.5.2.2 layer extraction/decryption;
- then the ordinary L2 file encryption decoder (e.g. 413/111) if that layer is present.

This is consistent with why a first-stage output might not yet parse as an Unreal package.

## Important implementation caveats

The posted sample is a proof-of-concept, not production-quality code:
- It writes `nNumberOfBytesToRead` rather than the actual `*lpNumberOfBytesRead`.
- It appends chunks without preserving file offsets or handling overlapped reads.
- It keys handles through an `int` cast and does not handle handle reuse robustly.
- It writes a single hard-coded filename and has no validation or round-trip test.

Methos-XDAT must implement the same interception point robustly: track the target handle, capture exactly the bytes returned after the chained hook returns, use actual byte counts and file offsets, and preserve the original source file.

## Relevance to our failed headless probe

The current probe loads `SmartCapture.dll` before `npkscrypt.dll` through the `ogg.dll` import-order patch, matching the published ordering in principle. However, our current capture is byte-identical to the source and does not yet prove that the published SmartCrypt hook chain is active at the correct point.

The current capture DLL uses MinHook and installs API hooks during initialization. The public example uses an inline `splice` detour and relies on hook chaining/order. These are not automatically equivalent. Next work must reproduce and instrument the published chain/order exactly, not infer success from merely seeing a `ReadFile` call.

## Next experiments

1. Retrieve and inspect the exact `hook.h` and `hook.cpp` sources linked by the thread; compare their trampoline semantics with our MinHook implementation.
2. Inspect the `ogg.dll` import-table ordering after patching and verify `SmartDecrypt.dll` is initialized before `npkscrypt.dll`.
3. Instrument both hook entry and return, logging caller address, handle, requested count, actual count, file offset, and first 32 bytes before/after each layer.
4. Run the exact file under the published chain and save the full output. Do not use `l2.exe` as a required manual user step; this is only a controlled test harness until a direct host path is validated.
5. Apply the ordinary L2 413/111 decoder only if the captured first-stage output has the corresponding signature.
6. Validate `interface.u` and `interface.xdat` independently.


## Results from the November 12 Windows probes

- Run 43207341894 confirmed that `Engine.int` is staged (6,700 bytes), but the actual L2 process never opened `SysString-e.dat`; it loaded `Engine.u` and `Core.u`, then exited with a `Window.Errors.Critical` dialog. The captured bytes from the separate headless probes were byte-identical to the original and are not a decrypted result.
- Run 43211768051 copied the targets under their real names `Interface.u` and `Interface.xdat` and extended the capture shim to keep separate output files for each. The client still exited before opening any of the three target names. Therefore the real-client test is currently blocked by startup/loader conditions, not by a failed file decode.
- The client log repeatedly attempts to open `Window.int` and other missing package/localization files. The supplied test payload has `Window.dll`, but no `Window.u` or `Window.int`. The blank critical dialog is not evidence that the interface was processed.
- The headless probe reports `DSETUP_MODULE` loaded but `GetProcAddress(ordinal 0x67)` returns null. The supplied `dsetup.dll` is an 8,704-byte DirectX-setup-style DLL whose export table contains only ordinals 1–18. In the NPK static initialization routine, a successful lookup of ordinal 0x67 is a prerequisite for populating global key pointer `0x10051430` and activation flag `0x10051468`; in the probe both remain zero. This is a concrete missing-provider lead, not proof that the file itself is the only missing component.
- The user's observation that no `dsetup.u` exists in the supplied client is consistent with a possible external or separately supplied key-provider component. Do not assume the entire protection layer is self-contained in the game's normal system folder.

## Revised next route

1. Reproduce the published `SmartDecrypt.dll` detour semantics exactly and verify hook initialization order relative to NPK's own ReadFile wrapper. Do not treat MinHook's successful installation as proof that it is observing the post-transform buffer.
2. Identify the expected provider behind `dsetup.dll` ordinal 0x67 and the `dsetup.u` resource/file reference. Search both in the game payload and in external tooling; do not require the user to produce a file already confirmed absent.
3. Repair the controlled client harness so it reaches the same file-load stage as the published method. The current client exits before opening `SysString-e.dat`, `Interface.u`, or `Interface.xdat`.
4. In parallel, reverse the per-handle key-state creation and the NPK transform offline. A successful key-state reconstruction would remove dependence on launching the game or an external decryptor.
5. Once any candidate intermediate is recovered, validate its standard 111/413 layer and then independently validate U and XDAT structure. Preserve both original files throughout.

## Verified file-based result (2026-11-13)

The supplied Dunk3l files have now been validated independently of the still-pending runtime hook investigation:

- The real `Interface.u` is 1,241,967 bytes. Its `FF ED FA CE` SmartCrypt marker and embedded 24-byte seed allow the per-file key state to be derived with the external-key flag clear; the next layer is `Lineage2Ver111`.
- The original U was decoded and both layers were re-encoded byte for byte. The rebuilt U was then re-protected, decoded again, parsed as Unreal package version 126/0, and re-decompiled: 167/167 exported classes, zero failed classes, identical class-name set, 208 sanitized names restored.
- The real `Interface.xdat` is 705,813 bytes. Its SmartCrypt and Lineage2Ver111 layers were rebuilt byte for byte; the exact custom 20-byte trailer was retained. XML round-trip: 169 windows and 8 shortcuts. The stored trailer checksum differs from the stock CRC, so the original trailer is preserved rather than replaced.
- The UCC reference kit contains 169 `.uc` files and its full reference compilation passed. The actual Dunk3l package exports 167 classes; the rebuild intentionally preserves those 167 exports rather than adding unrelated stock reference classes.
- GitHub Actions: [Dunk3l full round-trip #189](https://github.com/Methos48/Methos/actions/runs/43485104819) and [UCC validation #293](https://github.com/Methos48/Methos/actions/runs/43485104806), both SUCCESS.

For the U file, the terminal 20 bytes are part of the final Unreal export's serial range and remain inside the payload. XDAT uses a separate 20-byte custom trailer. This distinction is deliberate and is tested.

The published `SmartDecrypt.dll`/`npkscrypt.dll` runtime hook chain remains a separate investigation: the successful result above proves the file-based seed-derived codec and round-trip, not that the client runtime hook path was reproduced.
## Status

- [x] Find a public SmartCrypt 1.5.2.2 extraction method that explicitly names interface.u and interface.xdat.
- [x] Find the required hook ordering: SmartDecrypt before npkscrypt in ogg.dll.
- [x] Identify the two-stage possibility: SmartCrypt layer followed by ordinary L2 413/111 encryption.
- [ ] Retrieve and validate the linked hook implementation.
- [ ] Reproduce the exact chained hook behavior with our files.
- [x] Produce real decoded U and XDAT outputs and validate their structures with full round-trip checks.
- [x] Integrate and verify file-based decode/encode methods in the unified Methos engine for the supplied Dunk3l samples; this is not a claim of universal protocol support.

No original interface files have been modified.
