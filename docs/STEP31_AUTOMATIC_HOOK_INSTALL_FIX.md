# Step 31 — Fix automatic SmartCapture hook activation

Date: 2026-11-12

## Defect found in our implementation

The public Smart Crypt 1.5.2.2 sample installs its `ReadFile` and `CreateFileW` detours from `DllMain(DLL_PROCESS_ATTACH)`, after loading `engine.dll`.

Our `SmartCapture.dll` previously did something materially different:
- `DllMain` loaded `engine.dll` and called only `SmartCapture_TraceInitialize()`;
- that function installed diagnostic hooks (GetProcAddress, LoadLibraryA, VirtualProtect, etc.);
- the actual target hooks (`ReadFile`, `CreateFileW`) were installed only by the separate exported `SmartCapture_Initialize()` function;
- our headless Core test explicitly called that export, masking the fact that the import-loaded DLL would not automatically install the target hooks in the normal client path.

This was a real integration defect. It explains why our earlier tests did not faithfully reproduce the published DLL's initialization sequence.

## Fix committed

Commit `7b9f7bbf9c1967fa7da74ac5457729d6ec13aa9c` changes DLL attach to:
1. load `engine.dll`;
2. call `SmartCapture_Initialize()` directly;
3. fail DLL initialization if either step fails.

Commit `8a49625a177f31c375ace9b3e8bb67c96f3a3e11` changes the headless host so it no longer manually invokes `SmartCapture_Initialize`. It checks `SmartCapture_Status()` instead. The test will now fail if automatic DLL-attach initialization does not install the hooks.

## What is and is not proven

- [x] The code path now requests automatic installation of ReadFile/CreateFileW hooks during DLL load, matching the published sample's lifecycle.
- [x] The test harness no longer masks missing automatic initialization by manually calling the export.
- [ ] A fresh Windows CI run must compile and execute this revised code.
- [ ] The resulting trace must prove the target handle is intercepted.
- [ ] The captured bytes must be compared with source and with the post-NPK transform boundary.
- [ ] A valid second-stage 413/111 decode must be tested if the captured output still carries that layer.

The old artifact (run `43190522580`) predates these commits and is not evidence that this fix works. GitHub's current Actions integration has not yet returned a run for the new commit, so no new runtime success is claimed.

## Important chain-order caveat

The published method uses a `splice` detour and requires the SmartDecrypt DLL before `npkscrypt.dll` in the `ogg.dll` import order. Our current code uses MinHook. Matching the DLL load order does not automatically prove identical trampoline/chaining semantics. The next validation must check actual module-relative callers and whether the capture callback sees the buffer before or after the NPK transform. Do not mark decryption complete until that test and file-format validation pass.

No original `interface.u` or `interface.xdat` files have been modified.
