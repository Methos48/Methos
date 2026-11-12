# ActiveAnticheatCrypt layer research

Date: 2026-11-12

## Why this layer is in the pipeline

The public SmartCrypt 1.5.2.2 discussion documents a two-stage case: after the SmartCrypt capture, the resulting `decrypted_file.txt` may still contain `ActiveAnticheatCrypt`. A later post points to a second decoder method. This is a credible candidate for the additional layer reported by the interface author/community, but the actual Dunk3l intermediate has not yet been captured, so this is not yet proven for the two target files.

The original Dunk3l files currently in Methos all have the outer prefix `FF ED FA CE 01 00`:
- `interface.u`: 1,241,967 bytes; SHA256 `89fffcb34b0161033f6d2d1b9c650625cd6144fa575ccf3b170b4c0d5f31902d`
- `interface.xdat`: 705,813 bytes; SHA256 `7ea4e8830c24b3d803b32429040eb673da87e15c5f83126c4b5c6904825b16bb`
- `Dunk3lFX_ByBALENCIAGA.u`: 7,774,288 bytes; SHA256 `cec0aec45040f015c581a9b4f1728d998afff93cba8c98b605849af7b9266454`

All three show the same SmartCrypt outer marker. None exposes the AAC magic in its raw first bytes; the AAC layer, if present, can only be checked after the outer layer is removed.

## ActiveAnticheatCrypt format

Reference implementation: https://github.com/kerogenesis/ActiveAnticheatDecoder (MIT).

The format implementation in that project establishes:
- Exact 20-byte magic: `ActiveAnticheatCrypt` (note the lowercase `c` in `cheat`).
- RSA block begins at offset `0x14` and is 256 bytes (RSA-2048).
- RC4-encrypted payload begins at offset `0x114`.
- RSA uses PKCS#1 v1.5 type-2 padding.
- The RSA plaintext is either a 20-byte RC4 key or 40 bytes consisting of the 20-byte magic followed by the 20-byte RC4 key.
- The matching client private RSA modulus `N` and private exponent `D` are needed to recover the RC4 key.
- The reference key-capture method scans the protected client process for a 0x78-byte RSA context after `clmods.dll` is loaded and exports `N_LE` and `D_LE`.

## Methos integration status

- Added `ActiveAntiCheatCodec.cs` with RSA-2048/PKCS#1 v1.5 key extraction and RC4 payload decoding.
- Added optional key-profile registration to `ProtectionCodecRuntime.CreateDefault(...)`.
- Added planner detection for the exact AAC magic. Without a matching private key, Methos reports `recognized-key-required` and refuses to decode.
- Added a synthetic RSA/RC4 round-trip test. This validates the implementation mechanics, not a real Dunk3l file.
- Encoding remains deliberately unavailable unless a public exponent is explicitly supplied. The reference decoder captures only `N` and `D`; no verified AAC public exponent or real AAC re-encode vector is available yet.

## Next verification steps

1. Obtain the actual intermediate produced by the SmartCrypt layer, without treating raw API reads as decrypted data.
2. Check whether that intermediate begins with `ActiveAnticheatCrypt`.
3. If it does, capture the matching AAC RSA profile from the client that owns the key and validate the real RSA/RC4 decode.
4. Only enable AAC re-encoding after verifying the public exponent and a real file round-trip.
5. Continue investigating SmartCrypt's license-specific key provider separately; AAC is a subsequent layer, not a replacement for it.
