# Methos XDAT and U package encryption support

Status: the protection pipeline is integrated into the Windows application. Support is verified for specific formats and samples; this does not claim compatibility with every Lineage II private-server protector.

## Active application architecture

The Windows application uses the C# protection pipeline before handing plaintext data to the format-specific parser:

1. Program.cs calls ProtectionPipelinePlanner.InspectFullPipeline on the original .u or .xdat bytes.
2. ProtectionCodecRuntime decodes the recognized protection layers in order and retains the original per-file profiles.
3. U packages are processed by the Unreal package engine/UCC path. XDAT plaintext is passed to XdatBridge and tools/xdat/xdat_core.py for structural parsing, decompilation and recompilation.
4. When compiling, Methos reuses the detected original profile. It encodes the compiled payload, decodes the result again, and verifies that the produced payload is the one intended.
5. Unknown formats, unavailable keys, and unverified encoders fail closed. Methos must not guess a key or silently strip an unknown header.

The Python xdat_core.py module is the XDAT schema layer, not the encryption layer. The older tools/xdat/encryption_detection.py and auto_pipeline.py files are research/test utilities; they are not the active GUI protection path.

## Implemented protection layers

| Format/layer | Active C# support | Verification and limits |
|---|---|---|
| SmartCrypt 1.5.2.2 (FF ED FA CE) | Detection and decode; profile reconstruction from the embedded 24-byte seed when the external-key flag is clear; encode when the full round-trip is verified | Real nested SmartCrypt + Lineage2Ver111 samples are tested. If the file requires an external key that is not available, Methos stops safely. |
| Lineage2Ver111 with custom terminal bytes | Decode and encode while retaining the original per-file trailer profile | Real `.xdat` layered samples have byte-identical round-trip coverage. The current protection smoke workflow does not include a real protected `.u` fixture; do not generalize the XDAT result to every U package. Trailer bytes are preserved, not invented. |
| Lineage2Ver413 | Standard codec implemented for decode/encode | Codec-level tests are present. Do not infer that every private-server 413 variant has been verified against a real client file. |
| ActiveAntiCheatCrypt | Engine codec and key-profile support implemented | Synthetic RSA-2048/PKCS#1 v1.5 + RC4 tests pass. The GUI does not guess or automatically supply a matching private-key profile; without it, the file remains unavailable for decoding. |

## Recognized but not implemented in the active pipeline

The planner can identify Lineage2 version headers, but the following variants do not currently have an enabled decoder in the active application:

- Lineage2Ver120 and 121
- Lineage2Ver211 and 212
- Lineage2Ver411, 412 and 414
- LameCrypt wrapper families such as 611/612, 811/821, 820, 911 and 912

Recognition is not decryption support. These files must remain unchanged until the exact algorithm, key material, filename rules, wrapper order, and a real-file round-trip are verified.

## Real-file verification

tests/ProtectionPipelineSmoke and .github/workflows/protection-pipeline-smoke.yml exercise the active C# planner/runtime. The real Dunk3l `interface.xdat` sample validates the nested SmartCrypt 1.5.2.2 + custom Lineage2Ver111 path, including exact byte-for-byte reconstruction of the original protected file. The smoke log explicitly skips the real `interface.u` fixture when it is absent; the separate Silent Dark UCC workflow validates package decompilation/compilation, not universal U encryption support.

tools/xdat/validate_dunk3l_smartcrypt.py is an additional XDAT validation utility. Its results do not replace the C# application-path tests.

## Safety and compatibility rules

- Preserve the original input and its SHA-256 before processing.
- Never infer a key from a header alone.
- Never remove terminal bytes unless the detected profile proves how they belong to the payload.
- Validate decoded U package tables or XDAT structure before accepting the intermediate.
- Re-encode with the original detected profile and verify by decoding the output again.
- Keep detection, decryption, schema parsing, compilation and re-encryption as separate stages.
- A successful round-trip for one client/server sample is evidence for that sample and profile only, not universal compatibility.
