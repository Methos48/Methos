# SmartCrypt 1.5.2.2 and standard Lineage II pipeline — corrected findings

Date: 2026-11-12

## Target identity

The supplied interface.u and interface.xdat are from an interface made by **Dunk3l**, not original L2Sudamerica interface assets. The server/client installation is a runtime environment and a source of possible loader components, not proof of the interface author's encryption method.

The public MaxCheaters thread “Smart Crypt Decryptor 1.5.2.2” contains a method that explicitly names interface.u and interface.xdat. It also contains a post by Dunk3l in the discussion. This is a strong research lead, but the exact protection on our two files still requires runtime confirmation.

Source: https://maxcheaters.com/topic/249577-smart-crypt-decryptor-1522/

## What the public SmartCrypt method does

The published extraction procedure:
1. Rename a copy of the target file to SysString-e.dat.
2. Load an auxiliary DLL before npkscrypt.dll through the import order in ogg.dll.
3. The helper loads engine.dll, hooks CreateFileW and ReadFile, and tracks the handle opened for SysString-e.dat.
4. Capture the buffer returned by the chained read hook.
5. The resulting intermediate may still have ordinary Lineage II encryption (the thread names 413/111), so a second decode stage may be necessary.

The sample is not production-safe: it writes requested rather than actual byte counts, does not correctly handle overlapped I/O, and uses an unsynchronized handle map. Methos uses a safer capture implementation with actual returned counts, file offsets, synchronization, and non-destructive output.

## Exact DLL evidence and limits

The uploaded npkscrypt.dll has MD5 5908c3ee24b40f6f08f1500723b1a41d and version metadata matching the SmartCrypt 1.5.2.2 family. The supplied ogg.dll imports npkscrypt.dll. The two Dunk3l interface files begin with FF ED FA CE 01 00.

Static analysis found that npkscrypt.dll contains comparisons for this marker and a ReadFile-compatible in-place transform. A conditional initialization path also references dsetup.dll, ordinal 0x67 and dsetup.u. The user explicitly confirmed that neither dsetup.u nor SmartGuard.ini exists in their game. This branch is not proof of the key provider used by these target files and must not be treated as a required file set.

The earlier headless Core.dll stream test called ReadFile directly from Core.dll at RVA 0x76E5F; it bypassed the candidate NPK wrapper. Identical bytes from that test do not prove the real client transform is inactive.

## Integration defect found and corrected

The first Methos SmartCapture DLL only initialized diagnostic hooks from DllMain. It installed the actual ReadFile/CreateFileW hooks only when an exported initialization function was called manually by the test host. This did not match the published helper's lifecycle.

Commits:
- 7b9f7bbf9c1967fa7da74ac5457729d6ec13aa9c: install target hooks automatically during DLL attach.
- 8a49625a177f31c375ace9b3e8bb67c96f3a3e11: make the headless test check automatic initialization rather than manually invoking it.
- 80617f0af551df4e350968ad876866196980b913 and 5e6714a51c39d77a29ad0cd049fc3db076b5b638: add an isolated actual-client capture workflow using copies of the client files.

The old capture artifact predates these changes. A fresh Windows run is still required; no successful real-client capture is claimed yet.

## Standard Lineage II second-stage codec now integrated

The public-domain open-l2encdec project documents and implements standard Lineage II file formats:
- 111: XOR with key 0xAC.
- 413: RSA blocks (128 bytes, 124-byte payload) followed by zlib decompression, with a 28-byte UTF-16LE Lineage2Ver### header and 20-byte tail/checksum.

Source: https://github.com/ritsuwastaken/open-l2encdec

Methos now includes Lineage2StandardCodec.cs for protocols 111 and 413. Protocol 413 decode attempts both the modern and legacy RSA private-key sets; encode uses the modern public-key set. The codec validates the header, tail CRC32, RSA block structure, decompressed length, and zlib stream. It is registered in ProtectionCodecRuntime.CreateDefault() and exposed by the planner when the corresponding header is present.

A synthetic encode/decode round-trip test has been added for both 111 and 413. Windows CI run 43191919868 passed all protection planner checks, generic pipeline checks, and both integrated standard codec round-trips using .NET 8.0.428. The standard layer is runtime-verified against synthetic vectors; real SmartCrypt intermediate validation is still pending.

## Current end-to-end pipeline

    Dunk3l interface file (original preserved)
             |
             v
    SmartCrypt 1.5.2.2 runtime capture (real-client validation pending)
             |
             v
    Lineage2Ver111 / Lineage2Ver413 detection
             |
             v
    Methos Lineage II standard codec (111 XOR / 413 RSA + zlib)
             |
             v
    Format-specific validation
       +-----+-----+
       |           |
    U package    XDAT layout
       |           |
       +-----+-----+
             |
             v
    Edit / compile / reverse the layers in reverse order

The SmartCrypt runtime capture is not yet a standalone offline codec. The standard second layer is now implemented in the .NET runtime. Do not mark a file decoded until the capture is complete, the standard codec succeeds, and the resulting U/XDAT structure passes independent validation.

## Remaining work

- [ ] Run the revised actual-client capture workflow and recover a complete intermediate file.
- [ ] Confirm whether the intermediate begins with Lineage2Ver111, Lineage2Ver413, another supported header, or an unrecognized format.
- [ ] Run the integrated standard codec against the actual intermediate and validate checksum/decompression.
- [ ] Validate interface.u as a real Unreal package and interface.xdat with the Interlude XDAT parser.
- [ ] Determine how to reproduce/reverse the outer SmartCrypt layer for compiling edited files.
- [ ] Prove round-trip behavior without modifying the original files.

No original interface files have been changed.
