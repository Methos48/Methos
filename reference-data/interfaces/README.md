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

## Current acquisition candidates

| Label | Known contents from source listing | Status | Source |
|---|---|---|---|
| P166 | Interface/NWindow/Core/Engine UnrealScript sources, UCC build setup; 996 files, 870 .uc reported | Download link located; archive not yet acquired here | [L2JBaylee P166](https://l2jbaylee.dev/resources/interface-p166) |
| P196 | Interface/NWindow source trees and compiler environment; 702 files, 608 .uc reported | Download link located; archive not yet acquired here | [L2JBaylee P196](https://l2jbaylee.dev/resources/interface-p196) |
| P286 | Interface/NWindow sources, L2Editor, Java XDatEditor; 829 files, 716 .uc reported | Download link located; archive not yet acquired here | [L2JBaylee P286](https://l2jbaylee.dev/resources/interface-p286) |
| P311 | Interface/NWindow sources, L2Editor build/package tools; 883 files, 788 .uc reported | Download link located; archive not yet acquired here | [L2JBaylee P311](https://l2jbaylee.dev/resources/interface-p311) |
| P338 Voices | Compiled `InterfaceClassic.u` and `InterfaceClassic.xdat` | Download link located; archive not yet acquired here | [L2JBaylee P338](https://l2jbaylee.dev/resources/interface-p338) |
| P542 | Interface sources, XML layouts, XDatEditor and InterfaceClassic resources | Listed in collection; detail/download page still to inspect | [L2JBaylee collection](https://l2jbaylee.dev/resources) |

The P166/P196/P286/P311/P338 labels are source archive labels, not proof that they are Interlude builds. They are retained as potentially useful comparative samples and must be chronicle-classified from their contents.

## Additional leads

- [L2Fater: Interface.u / Interface.xdat notes and tools](https://www.l2fater.cn/forum.php?mod=viewthread&tid=9928) lists claimed `.u` support for HF, 448, 64, 166, 196, 245, 286, 311 and 33, and XDAT support through EP4/110. Treat this as a community lead, not a verified compatibility matrix.
- [MaxCheaters: Interface.u editing discussion](https://maxcheaters.com/topic/207983-editing-interfaceu/) contains an older H5 script archive lead; not an Interlude sample confirmation.
- [acmi/xdat_editor format notes](https://deepwiki.com/acmi/xdat_editor/7-data-model-and-file-format) documents versioned XDAT schemas; useful for identifying additional sample families.

## Download limitations encountered

The source pages expose direct RAR links and publish SHA-256 values for P166/P196/P286/P311/P338. The binary host rejected the web fetch as an unsupported RAR content type, and this execution environment could not resolve the host for direct download. Therefore **no binary archive is claimed to be stored in this repository yet**. Keep the external links and checksums as acquisition records until the archives can be downloaded through an accessible route.

## Next acquisition sequence

1. Obtain the five RAR archives from the source pages above.
2. Verify each downloaded archive against its published SHA-256.
3. Extract into separate immutable folders under `reference-data/interfaces/<label>/original/`.
4. Generate a manifest containing every file path, size, and hash.
5. Identify actual chronicle, package names, protocol metadata, encryption header, and whether the archive includes compiled `.u` / `.xdat` or only sources.
6. Add further samples only when an original download or user-provided archive is available.
