# SmartCrypt 1.5.2.2: offline layer reconstruction

## Scope

The Dunk3l samples in this repository begin with:

```
FF ED FA CE 01 00 ...
```

The additional protection is not dependent on a separate executable being present beside the interface. The inspected `npkscrypt.dll` contains the ReadFile transform and the first-block header reconstruction. The per-file seed needed for this sample variant is embedded in the first 24 bytes after the 4-byte marker.

## Embedded state

For the current Dunk3l `.u` and `.xdat` samples:

- Marker: `FF ED FA CE`
- Seed block: bytes `[4..28)`
- Selector `state[0]`: `01`, which reconstructs `Lineage2Ver111`
- External-key bit `state[1] & 1`: clear (`00`), so the dsetup.u global key is not needed for these samples
- The first 28 bytes are replaced in memory with UTF-16LE `Lineage2Ver111`

Key derivation for this marker:

```text
state = encrypted_file[4..28]
key6[i] = state[2+i] XOR (state[0] + 1), i=0..5
key16[i] = state[8+i] XOR key6[i % 6], i=0..15
```

The NPK transform starts at absolute file offset `0x1C`. For each absolute byte position `p`:

```text
byte ^= key6[p % 6]
byte ^= key16[p & 0x0F]
byte ^= (((p XOR 0x65) * 0x93) AND 0xFF)
```

The `0xC5` variant is selected by a separate per-handle state flag; the current FF ED FA CE samples use the `0x65` variant.

## Evidence recovered from interface.u

After the SmartCrypt layer and the inner `0xAC` Lineage2 XOR are removed, the U package begins with Unreal tag `C1 83 2A 9E` and reports:

- Unreal package version/licensee: `126/0`
- Names: `5482`
- Exports: `10308`
- Imports: `740`

These values match the earlier UEExplorer inspection of the Dunk3l interface. This confirms recovery of the actual package tables, not just a guessed signature.

## Inner Lineage2Ver111 trailer caveat

The Dunk3l samples have a 20-byte trailer that does not pass the stock protocol-111 CRC32 check used by the strict verifier. The public `open-l2encdec` CLI exposes checksum verification as an optional `-v` operation and documents that the game client does not perform this check. Therefore a checksum mismatch alone does not invalidate the decoded payload.

Methos uses a dedicated protocol-111 compatibility codec that preserves the original 20-byte trailer byte-for-byte. It never silently replaces a custom trailer with a stock one. The Windows smoke tests separately check:
1. SmartCrypt decode and encode round-trip,
2. protocol-111 XOR decode and re-encode while preserving the original trailer,
3. Unreal package parsing and the known 126/0 tables for `interface.u`,
4. XDAT parse/decompile/compile XML round-trip for `interface.xdat`.

Do not label the Dunk3l trailer as stock-CRC-verified. The supported save path requires exact original-file round-trip and successful U/XDAT structural validation; an in-game load test remains useful as final compatibility confirmation.

## Runtime capture notes

A headless Core.dll FFileStream probe reads through the Win32 API and bypasses the NPK wrapper. A later attempt to call the NPK Engine reader wrapper directly from the probe reached an invalid resolved function pointer and was stopped under SEH. This does not block the offline route above; do not treat that failed probe as evidence that the embedded-seed transform is invalid.
