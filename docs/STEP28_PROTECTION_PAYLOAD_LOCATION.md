# Step 28 — Retired SGuard hypothesis; correct target provenance

Date: 2026-11-12

## Correct target identity

The files under investigation, `interface.u` (1,241,967 bytes) and `interface.xdat` (705,813 bytes), belong to an interface made by the Lineage II player/creator **Dunk3l**. They are not original interface files supplied by L2Sudamerica.

This distinction is central. The server/client installation is only a possible runtime environment for testing compatibility and observing file access. It is not evidence that Dunk3l used the server's own protection, encryption, or packaging method.

## Withdrawn hypothesis

The earlier SGuard/`dsetup.u`/`SmartGuard.ini` path is not established as relevant to Dunk3l's files. The user explicitly confirmed those files do not exist in the game installation. Do not request them again, do not treat them as required, and do not substitute unrelated DirectX `dsetup.dll` files.

The observed conditional `dsetup.dll`/ordinal-0x67 branch inside the uploaded `npkscrypt.dll` remains a static reverse-engineering observation only. It does not prove that this DLL encrypted the Dunk3l interface or that the branch participates in its decoding.

## Required investigation direction

1. Identify the original distribution package and any readme/version/author metadata for Dunk3l's interface.
2. Inspect the two file headers, section/layout patterns, entropy, and known Unreal package/XDAT signatures independently.
3. Search for Dunk3l-specific tools, loaders, patchers, release notes, and community documentation; distinguish a wrapper from the underlying Unreal/XDAT serialization.
4. Treat `npkscrypt.dll` as a candidate runtime component only. Confirm its involvement with a call trace from the actual interface-loading path before attributing encryption to it.
5. Recover the actual key/state source only after the responsible transform is demonstrated.
6. Validate decoded U as a structurally valid Unreal package and XDAT as a valid interface data stream; preserve original bytes and prove round-trip behavior.

## Status

- [x] Record correct provenance: Dunk3l-created interface, not L2Sudamerica original assets.
- [x] Withdraw SGuard files as assumed prerequisites.
- [ ] Establish Dunk3l's exact release/toolchain and protection layer.
- [ ] Prove or rule out `npkscrypt.dll` involvement at runtime.
- [ ] Decode and structurally validate U and XDAT independently.
- [ ] Implement tested automatic decode/encode support in Methos-XDAT.
