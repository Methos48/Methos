# Lineage II interface reference archive

This directory tracks externally sourced Lineage II interface samples for the **Methos** project.
It is a research catalogue, not a claim that every listed archive has been downloaded or validated.

## Rules

- Keep each original archive immutable; never overwrite or patch a source sample.
- Store samples by chronicle/client label and protocol, not by guessed protection version.
- Record the original URL, retrieval date, filename, byte size, SHA-256, and observed contents.
- Separate compiled files (`.u`, `.xdat`) from source trees (`.uc`) and editor/tool bundles.
- Do not infer that a protocol number is an encryption header. Track protocol and file protection independently.
- Mark a sample **acquired** only after the bytes are actually saved; **verified** only after hashes and file contents are inspected.
- Respect source licensing. Public download availability is not a redistribution license.
- This archive belongs only to `Methos`; do not mix it with `Methos-Interface`.

## Interlude-labelled candidates

| Sample | Evidence / known contents | Status | Source |
|---|---|---|---|
| Interlude C6 XDAT 2-row bars | `InterfaceXdatC6.zip`, 52.41 KB; forum post explicitly identifies Interlude `interface.xdat` | Attachment exists but forum currently denies guest download; not acquired | [Zone-Game attachment thread](https://forum.zone-game.info/showthread.php?tid=44315) |
| Interlude pair (unknown client) | MediaFire links to `interface.u` and `interface.xdat`; forum respondent says U appears obfuscated, but author only reports inability to read the files with tools. No SmartCrypt proof | Links located; files not acquired; protection not independently verified | [MaxCheaters thread](https://maxcheaters.com/topic/245142-decrypt-interfaceuxdat-interlude/) |
| Ares Interlude Interface | Author says both files were decrypted/editable; `Interface.u` has obfuscation | Thread located; binary download link not yet extracted/verified | [MaxCheaters Ares Interlude](https://maxcheaters.com/topic/242437-ares-interlude-interface/) |
| Interlude P162+ source-rich U | Forum request seeks `Interface.u` for protocol 162+ with full sources; later reply says a source was found/translated, but no direct archive is attached in indexed content | Lead only; no binary acquired | [Zone-Game P162+ discussion](https://forum.zone-game.info/showthread.php?tid=44082) |
| Interlude P196 source | Reply in the P162+ discussion points to an Interface/NWindow source for protocol 196 | Lead only; locate the linked attachment in the forum thread | [Zone-Game P162+ discussion](https://forum.zone-game.info/showthread.php?tid=44082) |

## Additional Interlude-labelled candidate

| GK Global / Rusacis Interlude | `interfacegk.rar`; post describes retail `Interface.xdat` with GK interface, `Interface.u` UnrealScript and XML; follow-up provides compiled `Interface.u` and Interlude compiler RAR | Interlude-labelled archive link located; download attempt failed; contents and compatibility not inspected | [MaxCheaters thread](https://maxcheaters.com/topic/249411-gk-global-interface-rusacis-l2royale/) · [RAR](https://www.mediafire.com/file/g8sucve4j3gbxit/interfacegk.rar/file) |

## Newly located Interlude binaries and historical leads

| Candidate | Evidence | Status |
|---|---|---|
| Ares Interlude updated package | Later post shares an Interlude package on Mega: `https://mega.nz/file/qrgAhIpS#ZxQBz9NdaK_-f1lfjUyrdPMfriDKVuaeEerXbXVQTSU`. A C4 package is listed separately and is not Interlude. | Link located; contents/download not verified. |
| Wellington V3 Interlude XDAT | Forum user identifies a decrypted Interlude `interface.xdat` from Wellington V3: `https://www.mediafire.com/file/mcp9v8fy8o0vd8u/interface.xdat/file`. | Link located; file identity and download not verified. |
| Euro-PVP Interlude XDAT | 2025 forum request links an `interface.xdat` from EURO-PVP: `https://drive.google.com/file/d/1IBTapu25niYl1vaKM4XxEYO0kgpaVDa8/view?usp=sharing`. | Server identified; bytes/hash not verified. |
| Elfen edited Interlude pair | 2017 post describes paired `Interface.u` + `Interface.xdat` RAR, four shortcut bars and inventory sorting. Old Zippyshare link is historical; password shown in source: `TgC68qs`. | Host likely obsolete; not acquired. |
| Interlude Interface.u (2019) | Forum reply points to `https://www.sendspace.com/file/60jlhv`. | Thread blocked to automated retrieval; file identity/availability unverified. |

## Other client-interface archives (chronicle not yet verified)

| Label | Known contents from source listing | Status | Source |
|---|---|---|---|
| P166 | RAR 4.8 MiB; 996 files, 870 .uc; Interface/NWindow/Core/Engine sources and UCC build setup. Published SHA-256: `359b470ca06cc4f0baa258e4c692ee5b0015bf02f8786c8c146afb88dab79589` | Direct RAR URL verified; retrieval endpoint rejects `application/x-rar-compressed`; not acquired | [Details](https://l2jbaylee.dev/resources/interface-p166) · [RAR](https://files.l2jbaylee.dev/shares/Interface_P166.rar) |
| P196 | RAR 6.9 MiB; 702 files, 608 .uc; Interface/NWindow sources and `_Compile` tools. Published SHA-256: `61298ddef8dad003cc4d556c7278beab3401f2aa2bb00a502d5178c50f7cba23` | Direct RAR URL verified; retrieval endpoint rejects `application/x-rar-compressed`; not acquired | [Details](https://l2jbaylee.dev/resources/interface-p196) · [RAR](https://files.l2jbaylee.dev/shares/Interface_P196.rar) |
| P286 | RAR 19.5 MiB; 829 files, 716 .uc; Interface/NWindow sources, L2Editor and Java XDatEditor. Published SHA-256: `d8b9756a94fa40b235675c3c7ebd78c2d782ab5ef4452aa0aec6940ba6cc7e79` | Direct RAR URL verified; retrieval endpoint rejects `application/x-rar-compressed`; not acquired | [Details](https://l2jbaylee.dev/resources/interface-p286) · [RAR](https://files.l2jbaylee.dev/shares/Interface_P286.rar) |
| P311 | RAR 6.0 MiB; 883 files, 788 .uc; Interface/NWindow sources and L2Editor tools. Published SHA-256: `92228e6a558f21e739d29f4a0b9e47a3042a70e3b72450f26a5337ae9b9fb70b` | Direct RAR URL verified; retrieval endpoint rejects `application/x-rar-compressed`; not acquired | [Details](https://l2jbaylee.dev/resources/interface-p311) · [RAR](https://files.l2jbaylee.dev/shares/Interface_P311.rar) |
| P338 Voices | RAR 3.3 MiB; exactly 2 files: `InterfaceClassic.u` + `InterfaceClassic.xdat`; archive named `Interface_P338_Voices_UNPROTECTED.rar`. Published SHA-256: `7751d729633c3c4676da70365e7b9085448b1ccefbdcd3392e48c26ac94693b5` | Direct RAR URL verified; retrieval endpoint rejects `application/x-rar-compressed`; not acquired. Chronicle compatibility untested | [Details](https://l2jbaylee.dev/resources/interface-p338) · [RAR](https://files.l2jbaylee.dev/shares/Interface_P338_Voices_UNPROTECTED.rar) |
| P542 | 37.5 MiB RAR, 2,049 files; includes `InterfaceClassic/Classes/InventoryWnd.uc`, compiled `InterfaceClassic.u` and `InterfaceClassic.xdat`, NWindow sources and Samurai/Crow XML layouts | Exact SHA-256 published; download attempted but host rejected binary retrieval; not acquired | [P542 archive details](https://l2jbaylee.dev/resources/interface-p542) |

P166/P196/P286/P311/P338/P542 are archive labels, not proof that they are Interlude builds. Keep them as comparative candidates and classify their chronicle only after inspecting the archive.

## Additional technical leads

- [L2Fater: Interface.u / Interface.xdat notes and tools](https://www.l2fater.cn/forum.php?mod=viewthread&tid=9928) lists claimed `.u` support for HF, 448, 64, 166, 196, 245, 286, 311 and 33, and XDAT support through EP4/110. Treat this as a community lead, not a verified compatibility matrix.
- [MaxCheaters: Interface.u editing discussion](https://maxcheaters.com/topic/207983-editing-interfaceu/) contains an older H5 script archive lead; not an Interlude sample confirmation.
- [acmi/xdat_editor format notes](https://deepwiki.com/acmi/xdat_editor/7-data-model-and-file-format) documents versioned XDAT schemas; useful for format comparison.
- [Open-source XDAT_NEW editor](https://github.com/nascimentolh/XDAT_NEW) claims schema coverage from Interlude (CT0) through Salvation; useful for format comparison, not a substitute for original samples.
- [Interlude-only XDAT XML compiler/decompiler discussion](https://maxcheaters.com/topic/213962-xdat-xml-compilerdecompiler/) documents a tool specifically claiming Interlude support; potential parser cross-check reference.
- [L2Crypt interface tools](https://l2crypt.com/l2-tools/interface/) lists UE Explorer, XDAT Editor, WOTgreal and UTPT; tools are references, not sample archives.

## Download limitations encountered

- The Zone-Game attachment page blocks guest access to `InterfaceXdatC6.zip`.
- The L2JBaylee direct RAR URLs for P166/P196/P286/P311/P338 and P542 were extracted from their resource pages. Web retrieval fails because the host responds with `application/x-rar-compressed`, which this retrieval path rejects. These are verified URLs, not acquired archives. Published SHA-256 values are recorded above/in the CSV.
- MaxCheaters pages expose source links in forum posts, but no verified binary was acquired in this pass.

Therefore **no newly sourced binary archive is claimed to be stored in this repository yet**.

## Next acquisition sequence

1. Obtain the Ares Mega package, Wellington V3 XDAT, Euro-PVP XDAT and GK Global/Rusacis package; then resolve older Interlude U/XDAT leads.
2. Obtain the P162+/P196 source-rich interface package if the forum attachment remains available.
3. Obtain the L2JBaylee RAR archives and verify each against its published SHA-256.
4. Extract into separate immutable folders under `reference-data/interfaces/<chronicle-or-label>/original/`.
5. Generate a manifest containing every file path, size, and hash.
6. Identify actual chronicle, package names, protocol metadata, encryption header, and whether the archive includes compiled `.u` / `.xdat` or only sources.
7. Add further samples only when an original download or user-provided archive is available.

- The 2017 Elfen/Zippyshare and 2019 Sendspace Interlude leads have not been confirmed alive; retain them as historical references only.
