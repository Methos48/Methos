# Step 26 — npkscrypt stream transform and missing key provider

Date: 2026-11-12

## Exact file signature is recognized by npkscrypt

Static disassembly of the exact uploaded `npkscrypt.dll` found the target marker in its executable code and branches for both little-endian values:
- `0xCEFAEDFF` — file bytes `FF ED FA CE`, matching the supplied `interface.u` and `interface.xdat`.
- `0xCEFAEDFE` — alternate marker `FE ED FA CE`.

This directly ties the uploaded file header to code inside the supplied protection DLL. The earlier SmartCrypt-only label was too narrow; the protection stack includes a dsetup-based loader/key-provider path.

## ReadFile transform reconstructed

The routine at preferred VA `0x10119C6C` is a five-argument ReadFile-compatible wrapper. It calls the original read function, then transforms the returned buffer in place. It tracks absolute file position so partial reads can be transformed consistently.

For the `FF ED FA CE` variant, the byte transform is structurally:

```
for each byte at absolute file position i:
    data[i] ^= key6[i % 6]
    data[i] ^= key16[i % 16]
    data[i] ^= (((i ^ 0x65) * 0x93) & 0xFF)
```

The alternate mode uses `0xC5` instead of `0x65`. The 6-byte and 16-byte key arrays live in a per-file-handle state structure at offsets +2..+7 and +8..+23. The handle state is retrieved through helper `0x1011983C` and initialized through helper `0x1011989C`.

This is a reconstructed transform shape, not yet a working codec: the exact per-file key state and initialization must be obtained before decoding.

## Key initialization gate

The initialization path at preferred VA `0x1011A02C`:
1. Requires the process main image to be mapped at `0x10900000`.
2. Parses the PE header at that address.
3. Looks for an already-loaded module named `dsetup.dll`.
4. Checks an ordinal-0x67 entry and then processes a `dsetup.u` data/resource path.
5. Only after that path succeeds does it populate the global key pointer/activation flag used by the stream transform.

The public SGuard protection description independently identifies `dsetup.dll` as the protection loader and `dsetup.u` as customer-specific encrypted data. This is consistent with the strings and control flow found in the supplied DLL, but the exact build/customer match remains to be verified against the user's two files.

## Headless test result

A custom 32-bit host was linked at `0x10900000`, without running the original `l2.exe`. The PE-base gate passed: the DLL recorded main-image bounds `0x10901000..0x10914BFF`.

However:
- `dsetup.dll` was not present in either uploaded `system.zip` or `engine.zip`, and was not available in the runner's 32-bit/64-bit Windows system directories.
- `dsetup.u` was not present in either uploaded archive.
- The key globals remained zero (`0x51430=0`, `0x51468=0`).
- The real interface read remained byte-identical to the original; no decryption is claimed.

## Required exact inputs

The next real test requires the matching client copies of:
- `dsetup.dll`
- `dsetup.u`

They must come from the same L2Sudamerica client installation as the supplied `npkscrypt.dll`, not from another server or a random DirectX download. Once present, the loader/key path can be traced and the reconstructed byte transform can be validated against both original files.

## Status

- [x] Find the exact `FF ED FA CE` signature check inside npkscrypt.
- [x] Reconstruct the per-byte XOR stream transform shape.
- [x] Identify the image-base initialization gate.
- [x] Identify the dsetup.dll / dsetup.u key-provider dependency.
- [x] Confirm those two files are absent from the uploaded archives.
- [ ] Recover the matching dsetup key state.
- [ ] Decode `interface.u` and validate a real Unreal package.
- [ ] Decode `interface.xdat` and validate a real XDAT layout stream.
- [ ] Verify encode/decode round trips without modifying originals.

No original files have been changed.
