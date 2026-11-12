# Step 32 — Standard Lineage II layer connected to the protection runtime

Date: 2026-11-12

## Source and format contract

The public-domain open-l2encdec implementation was inspected directly. It supports 111, 120, 121, 211, 212 and 411–414. Its documented format contract for this project is:

- Protocol 111: XOR using 0xAC.
- Protocol 413: RSA transformation of 128-byte blocks, with 124-byte payload framing, followed by zlib decompression.
- Header: UTF-16LE Lineage2Ver plus a three-digit protocol number (28 bytes).
- Tail: 20 bytes; CRC32 is stored at tail offset +12.

The source provides two 413 decryption key sets: the modern default and the legacy per-protocol key. Methos tries both during decode. Encoding uses the modern public key set.

Reference implementation (public domain): https://github.com/ritsuwastaken/open-l2encdec

## Methos integration

New file: methos_crypto/Lineage2StandardCodec.cs

Integrated:
- strict protocol header validation;
- 20-byte tail and CRC32 validation;
- protocol 111 XOR decode/encode;
- protocol 413 RSA block decode/encode;
- modern then legacy 413 private-key attempts;
- zlib decompression and exact expected-output-length validation;
- size cap for decompression;
- cancellation checks between RSA blocks.

Runtime registry:
- ProtectionCodecRuntime.CreateDefault() registers 111 and 413.
- ProtectionPipelinePlanner reports these headers as supported codecs, not merely recognized signatures.
- Unknown protocols still fail closed.

Tests added:
- synthetic 111 encode/decode round-trip;
- synthetic 413 encode/decode round-trip;
- planner detection of generated standard headers;
- existing two-layer execution order and missing-SmartCrypt-codec refusal remain covered.

## Independent algorithm sanity check

A separate Python calculation using the exact modern 413 modulus/exponents from open-l2encdec encrypted a 57-byte test payload into one 128-byte RSA block, reversed the block with the corresponding private exponent, removed the 124-byte framing, and decompressed the zlib payload to the exact original 57 bytes. A separate 111 calculation verified XOR and CRC32 framing.

This validates the mathematical key pairing and block framing independently of the C# implementation. It is not a substitute for running the .NET smoke test or testing a real captured file.

## Remaining validation

Windows CI run 43191919868 completed successfully with .NET SDK 8.0.428. It reported PASS for the protection planner checks, the generic two-layer runtime checks, and the integrated 111 XOR plus 413 RSA/zlib encode/decode round-trips. The codec is now runtime-verified against synthetic vectors. Real captured-file validation is still pending.

The SmartCrypt capture remains a separate first-stage runtime task. The standard codec is now ready to process its intermediate only when that intermediate has a valid Lineage2Ver111 or Lineage2Ver413 header and a valid tail checksum.

No actual Dunk3l interface file has been claimed as decoded by this step.
