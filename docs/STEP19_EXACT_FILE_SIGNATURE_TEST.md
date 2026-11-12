# Step 19 — Automated signature test on the exact original files

Date: 2026-11-12  
Workflow: https://github.com/Methos48/Methos-XDAT/actions/runs/43153029241  
Commit tested: 777b30f7c3561901d2398602b0da5de99d2aa4ae  
Result: workflow succeeded; both original files were read and preserved as a two-day Actions artifact.

## Exact observations

| File | Size | SHA-256 | Prefix |
|---|---:|---|---|
| interface.u | 1,241,967 | 89fffcb34b0161033f6d2d1b9c650625cd6144fa575ccf3b170b4c0d5f31902d | FF ED FA CE 01 00 |
| interface.xdat | 705,813 | 7ea4e8830c24b3d803b32429040eb673da87e15c5f83126c4b5c6904825b16bb | FF ED FA CE 01 00 |

Both files:
- Match the candidate six-byte prefix FF ED FA CE 01 00.
- Do not start with ASCII ActiveAnticheatCrypt.
- Do not start with Lineage2Ver.
- Do not start with Unreal package magic C1 83 2A 9E.

## Interpretation and limits

This is a successful signature/identity test, not a decryption test. The prefix is not yet proven to be a unique SmartCrypt signature. The test does not identify the cipher, key, or full container layout and does not produce decrypted output.

The SmartCrypt hypothesis remains supported by the matching client protection context and published SmartDecrypt 1.5.2.2 workflow, which captures the client-read stream and may leave a second standard Lineage II encryption layer. However, the exact U and XDAT have not yet been passed through that Windows client-side extraction procedure. No decrypted file should be claimed until the output is captured and structurally validated.

Next required validation:
1. Run the matching SmartDecrypt capture against each original separately in the actual Windows client environment.
2. Capture successful ReadFile byte counts and offsets correctly; reject incomplete, duplicated, or out-of-order output.
3. Inspect each captured output independently for a standard Lineage2Ver version or another recognized format.
4. Apply the matching standard L2 decoder only if the captured output demonstrates that second layer.
5. Validate the resulting U package and XDAT structure with independent parsers and record hashes.

Original files remain unchanged in the repository.


## Independent format check for both outputs (2026-11-12)

| File | Observed bytes | Expected inner format | Current verdict |
|---|---|---|---|
| interface.u | `FF ED FA CE 01 00` | Unreal package after protection layers are removed; expected package tag `C1 83 2A 9E` or a recognized intermediate `Lineage2Ver###` header | Still protected; no valid Unreal package produced |
| interface.xdat | `FF ED FA CE 01 00` | Interlude XDAT serialized window/layout stream; not an Unreal package | Still protected; no valid XDAT payload produced |

The two files share the outer six-byte candidate marker, but their inner formats are different. A successful U validation must parse the Unreal package summary/tables and offsets. A successful XDAT validation must parse the XDAT window/layout records and verify all lengths and references. The Unreal package magic must never be used to classify a file as XDAT merely because its extension is `.xdat`.

The current capture worker did not generate either decoded result. Its U capture hash equals the original U hash exactly, proving that it performed an ordinary file read rather than the required post-protection extraction. No valid output exists yet for either file. The format check therefore fails for both, and the pipeline must keep both originals blocked from editing until real decoded payloads pass their respective parsers.
