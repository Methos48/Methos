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

The initialization path at preferred VA `0x1011A02C` in the supplied `npkscrypt.dll` contains a branch that looks for an already-loaded module named `dsetup.dll`, resolves ordinal `0x67`, and references the string `dsetup.u`. This is a real code path in this DLL, but it is not proof that the user's installed client ships or uses those files. The user has explicitly confirmed that neither `dsetup.u` nor `SmartGuard.ini` exists in their game installation. Therefore the SGuard identification and any assumption that those files are required for this specific client are withdrawn. Treat this as an observed optional/conditional initialization branch until its caller and activation conditions are reverse-engineered.

## Headless test result

A custom 32-bit host was linked at `0x10900000`, without running the original `l2.exe`. The PE-base gate passed: the DLL recorded main-image bounds `0x10901000..0x10914BFF`.

However:
- `dsetup.dll` was not present in either uploaded `system.zip` or `engine.zip`, and was not available in the runner's 32-bit/64-bit Windows system directories.
- `dsetup.u` was not present in either uploaded archive.
- The key globals remained zero (`0x51430=0`, `0x51468=0`).
- The real interface read remained byte-identical to the original; no decryption is claimed.

## Next investigation

Do not ask the user for `dsetup.u` or `SmartGuard.ini`; they do not exist in this installation. Investigate alternate callers/initializers and determine whether this branch is optional, legacy, or activated only for a different protection mode. The last read-capture experiment observes the raw API buffer; because the hook runs inside the underlying ReadFile/NtReadFile call, that observation alone cannot prove whether a caller-side wrapper transforms the buffer after the API returns. Trace the read caller address and capture the buffer after the candidate wrapper returns before concluding the transform is inactive.

## Status

- [x] Find the exact `FF ED FA CE` signature check inside npkscrypt.
- [x] Reconstruct the per-byte XOR stream transform shape.
- [x] Identify the image-base initialization gate.
- [x] Identify the dsetup.dll / dsetup.u key-provider dependency.
- [x] Confirm those two files are absent from the uploaded archives.
- [ ] Identify the actual key-state provider/initializer for this client build.
- [ ] Decode `interface.u` and validate a real Unreal package.
- [ ] Decode `interface.xdat` and validate a real XDAT layout stream.
- [ ] Verify encode/decode round trips without modifying originals.

No original files have been changed.
