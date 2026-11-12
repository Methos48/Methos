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

## Status

- [x] Find a public SmartCrypt 1.5.2.2 extraction method that explicitly names interface.u and interface.xdat.
- [x] Find the required hook ordering: SmartDecrypt before npkscrypt in ogg.dll.
- [x] Identify the two-stage possibility: SmartCrypt layer followed by ordinary L2 413/111 encryption.
- [ ] Retrieve and validate the linked hook implementation.
- [ ] Reproduce the exact chained hook behavior with our files.
- [ ] Produce a real decoded U and XDAT output and validate their structures.
- [ ] Integrate verified decode/encode methods into the standalone Methos-XDAT program.

No original interface files have been modified.
