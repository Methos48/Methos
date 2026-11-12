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


## Actual-client probe dependency correction

The first actual-client workflow attempt did not capture anything. Inspection of its prepared-file inventory showed that it had copied only engine.dll, l2ui.dll, npkscrypt.dll, ogg.dll and SmartCapture.dll. The exact root-level Core.dll, ifc23.dll, vorbisfile.dll, vorbis.dll and the supplied DirectX dsetup.dll were missing, even though l2.exe imports Core.dll and depends on the client runtime.

The workflow was corrected in commit 513f3a8ee8d2c6055cbe8ff0154702c2eddb3f5e to copy these exact dependencies into the isolated probe folder. The dsetup.dll copied here is the supplied DirectX runtime DLL; this does not imply that dsetup.u or SmartGuard.ini exists or is required.

A new actual-client run is pending. The earlier no-capture attempt is classified as an invalid/incomplete runtime setup, not evidence that the capture technique failed.


## Exact-client launch blocker: missing Window.dll

The actual l2.exe PE import table contains Window.dll, Core.dll and Engine.dll. The prepared runtime now includes Core.dll, Engine.dll, ifc23.dll, vorbisfile.dll, vorbis.dll and the supplied DirectX dsetup.dll, but the artifact inventory confirms Window.dll is still absent from the uploaded system.zip and the repository root.

Window.dll is a Lineage II client module, not a Windows system DLL. Public Lineage II client inventories also list it in the system folder. Without the exact Window.dll matching this l2.exe, Windows cannot complete the executable's import loading, so SmartCapture.dll never reaches DllMain; the absence of smartcapture-debug.log and capture output is therefore expected.

The workflow now stops with an explicit missing-Window.dll error rather than launching an incomplete client. The exact Window.dll from the same client system folder is required to continue the real-client capture. Do not substitute a random Window.dll from another chronicle/build.
