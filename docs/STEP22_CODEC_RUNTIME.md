# Step 22 — Codec runtime and safe pipeline executor

Date: 2026-11-12

## Implemented

- Added `ProtectionCodecRuntime`, a codec registry and ordered pipeline executor.
- Every protection layer must have an explicitly registered codec. Unknown or missing layers stop processing; the runtime does not guess.
- Decode applies layers in detection order; encode applies the inverse operations in reverse order.
- Cancellation is checked between layers.
- Added safe output writing: the original input path cannot be used as output, existing outputs are not overwritten, and writes use a temporary file followed by an atomic move.
- The runtime accepts codecs through `IProtectionCodec`, allowing a future native SmartCrypt implementation, a verified Lineage II codec, or a controlled background Windows runtime adapter to be integrated without changing the editor workflow.

## What this does not claim

This is a working orchestration/runtime foundation, not a cipher implementation. No SmartCrypt 1.5.2.2 codec or complete Lineage II codec has been registered. The supplied `FF ED FA CE 01 00` files remain identified candidates, not decrypted files. The engine must report missing codecs instead of returning fabricated plaintext.

## Test plan

The existing Windows smoke workflow checks format detection and fail-closed behavior. The next test increment must add a reversible test codec to verify multi-layer decode order, reverse encode order, cancellation, and safe output behavior. Exact supplied U/XDAT files remain mandatory regression tests when the real transform is recovered.
