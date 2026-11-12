# Step 28 — Protection payload location cross-check

Date: 2026-11-12

## Finding

The absence of `dsetup.u` from `system.zip` and `engine.zip` does not establish that it is absent from the client installation. Public SGuard installation discussion lists the client-side protection set alongside the client launcher files: `L2.exe`, `L2.bin`, `dsetup.u`, `dsetup.dll`, and `SmartGuard.ini`. This places the relevant payload in the client root/installation set, not necessarily in `system/`.

Evidence:
- SGuard forum discussion, post #3, 2022-09-30: https://forum.lucera2.com/threads/smartguard.3025/
- L2-Scripts SGuard presentation: https://l2-scripts.com/demo/sguard.pdf

This is corroborating ecosystem documentation, not proof that the exact L2Sudamerica build uses the same SGuard package. The exact DLL must be matched by architecture, exports, hash/build compatibility, and ordinal 0x67 behavior.

## Repository inventory cross-check

Current Methos-XDAT tree contains `dsetup.dll` and `l2.exe`, but no `dsetup.u`, `l2.bin`, or `SmartGuard.ini`. The uploaded `dsetup.dll` has already been identified as the unrelated DirectX setup library and must not be substituted for the protection module.

## Consequence

Do not keep searching only inside `system.zip`. The remaining required evidence is the client-root protection payload (if supplied by this server build), especially the matching `dsetup.dll`, `dsetup.u`, and `SmartGuard.ini`, plus `l2.bin` if present. Do not download a random SGuard DLL or mix files from another server: that could execute incompatible or malicious code and cannot establish the correct key state.

## Status

- [x] Establish that the system/engine archive search was too narrow to rule out client-root payloads.
- [x] Identify the exact filenames and expected client-root placement reported by SGuard installation documentation.
- [ ] Locate the matching L2Sudamerica protection payload.
- [ ] Trace ordinal 0x67 and recover the per-handle key state.
- [ ] Decode and validate both U and XDAT samples.
