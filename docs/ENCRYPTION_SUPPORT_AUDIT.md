# Methos XDAT — encryption support audit

Status: research record; this document does not claim that every format is implemented.

## What the current application actually does

The current `xdat_core.py` starts by parsing the supplied bytes as a raw `ct0.XDAT` structure. The GUI passes the file bytes directly to `decompile_to_folder()` and `compile_from_folder()`. There is no encryption/header detection or decrypt/encrypt adapter in the current application source. Its successful byte-identical round-trip test covers the supplied known-good Interlude fixture only.

## Documented Lineage II file encryption families

The open-source `acmi/L2crypt` project documents these file header families:

| Header/version | Family / layer | Notes |
|---|---|---|
| 111 | XOR | Standard L2 file encryption |
| 120 | XOR variant | Separate stream format |
| 121 | XOR variant | Filename-dependent key; correct filename matters |
| 211, 212 | Blowfish | Block/stream handling differs by version |
| 411–414 (41x) | RSA family | Key/profile and exact variant matter |
| 811, 821 | LameCrypt wrapper over XOR variants | Wrapper is applied before the underlying cipher |
| 820 | LameCrypt wrapper over 120 | Wrapper is applied before the underlying cipher |
| 911, 912, 61x | LameCrypt family | Must be matched to the precise documented variant |

Independent tools such as `l2encdec` and `open-l2encdec` also document support for standard headers 111, 120, 121, 211, 212 and 41x (including 413). Their supported lists are not proof that every private server build or custom wrapper is compatible.

## Custom protection

SmartCrypt and other private/custom protectors can use a custom key or an additional header-obfuscation layer. They cannot be made universally decryptable by adding a fixed list of public algorithms: the matching key, wrapper, filename rules, and client-side format must be known. Methos should support explicit user-supplied profiles where technically and legally appropriate, but must not guess keys or silently strip unknown bytes.

## Required architecture before claiming broad support

1. Preserve the original input and calculate its hash.
2. Identify plain files and recognized headers without modifying the source.
3. Route each recognized format through a separate decoder adapter.
4. Validate the decoded payload structurally as the expected XDAT schema before accepting it.
5. Run the existing parse/export/compile pipeline on the decoded payload.
6. Re-encode with the original detected profile (or an explicitly selected output profile).
7. Decode the produced output again and compare the payload; for unmodified files, require byte-identical round-trip where the format permits it.
8. For unknown formats or missing custom keys, stop safely and report diagnostics.

## Current user test file

The repository's `interface.xdat` begins with bytes `FF ED FA CE`, not the documented UTF-16LE `Lineage2Ver###` header. It is therefore not identified as one of the standard public header formats by this signature alone. This is not enough evidence to label it SmartCrypt or to choose a decryption algorithm. Do not strip the first bytes or try arbitrary transformations against the user's only copy.

## Scope separation

This audit concerns offline client-file encryption for files such as XDAT and U packages. Network packet encryption is a different protocol and is not a file decoder. Likewise, decoding an XDAT encryption wrapper does not imply that the internal XDAT schema is supported; the schema parser remains a separate compatibility layer.


## Implemented detection layer (research branch)

The research branch now includes `encryption_detection.py`, which recognizes all 18 documented header values listed above, identifies their cipher family/wrapper, flags the filename-dependent 121/821 variants, distinguishes malformed and unknown numeric headers, and reports short/unrecognized input without changing bytes.

The Windows build overlay connects this detector to the GUI. A recognized encrypted file is reported and the operation stops safely because the decryptor/encoder adapters have not yet been implemented. Compilation against an encrypted original is also blocked rather than silently emitting an incompatible plaintext file.

Local verification performed:
- Python syntax compilation passed for the app and detector.
- 7 detector tests passed, covering every listed version, filename-dependent variants, malformed/unknown headers, truncated data, and the user's `FF ED FA CE` signature.
- 2 existing XDAT fixture round-trip tests passed byte-for-byte.

This is a tested **detection** milestone, not yet a claim that Methos can decrypt or encrypt these 18 formats. The custom `FF ED FA CE` file remains unidentified.
