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
| Interlude pair (unknown client) | MediaFire links to `interface.u` and `interface.xdat`; discussion says U is obfuscated, but does not establish SmartCrypt conclusively | Links located; files not acquired; protection not independently verified | [MaxCheaters thread](https://maxcheaters.com/topic/245142-decrypt-interfaceuxdat-interlude/) |
| Ares Interlude Interface | Author says both files were decrypted/editable; `Interface.u` has obfuscation | Thread located; binary download link not yet extracted/verified | [MaxCheaters Ares Interlude](https://maxcheaters.com/topic/242437-ares-interlude-interface/) |
| Interlude P162+ source-rich U | Forum request seeks `Interface.u` for protocol 162+ with full sources; later reply says a source was found/translated, but no direct archive is attached in indexed content | Lead only; no binary acquired | [Zone-Game P162+ discussion](https://forum.zone-game.info/showthread.php?tid=44082) |
| Interlude P196 source | Reply in the P162+ discussion points to an Interface/NWindow source for protocol 196 | Lead only; locate the linked attachment in the forum thread | [Zone-Game P162+ discussion](https://forum.zone-game.info/showthread.php?tid=44082) |

## Other client-interface archives (chronicle not yet verified)

| Label | Known contents from source listing | Status | Source |
|---|---|---|---|
| P166 | Interface/NWindow/Core/Engine UnrealScript sources, UCC build setup; 996 files, 870 .uc reported | Download link located; archive not yet acquired | [L2JBaylee P166](https://l2jbaylee.dev/resources/interface-p166) |
| P196 | Interface/NWindow source trees and compiler environment; 702 files, 608 .uc reported | Download link located; archive not yet acquired | [L2JBaylee P196](https://l2jbaylee.dev/resources/interface-p196) |
| P286 | Interface/NWindow sources, L2Editor, Java XDatEditor; 829 files, 716 .uc reported | Download link located; archive not yet acquired | [L2JBaylee P286](https://l2jbaylee.dev/resources/interface-p286) |
| P311 | Interface/NWindow sources, L2Editor build/package tools; 883 files, 788 .uc reported | Download link located; archive not yet acquired | [L2JBaylee P311](https://l2jbaylee.dev/resources/interface-p311) |
| P338 Voices | Compiled `InterfaceClassic.u` and `InterfaceClassic.xdat` | Download link located; archive not yet acquired | [L2JBaylee P338](https://l2jbaylee.dev/resources/interface-p338) |
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
- The P542 RAR direct URL is `https://files.l2jbaylee.dev/shares/Interface_P542.rar`; both web retrieval and direct download attempts failed in this environment. Its published SHA-256 is `7251f9dc8ac0896ab4d3d163644192255555d0d2f9e5f4c81718ce63ed353585`.
- MaxCheaters pages expose source links in forum posts, but no verified binary was acquired in this pass.

Therefore **no newly sourced binary archive is claimed to be stored in this repository yet**.

## Next acquisition sequence

1. Obtain the Interlude C6 XDAT attachment and the candidate Interlude U/XDAT pairs.
2. Obtain the P162+/P196 source-rich interface package if the forum attachment remains available.
3. Obtain the L2JBaylee RAR archives and verify each against its published SHA-256.
4. Extract into separate immutable folders under `reference-data/interfaces/<chronicle-or-label>/original/`.
5. Generate a manifest containing every file path, size, and hash.
6. Identify actual chronicle, package names, protocol metadata, encryption header, and whether the archive includes compiled `.u` / `.xdat` or only sources.
7. Add further samples only when an original download or user-provided archive is available.
