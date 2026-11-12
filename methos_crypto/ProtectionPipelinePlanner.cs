using System.Text;

namespace MethosUEngine;

public enum ProtectionLayerKind
{
    SmartCrypt1522Candidate,
    ActiveAntiCheatCrypt,
    Lineage2Standard,
    Lineage2CustomTrailer,
    UnrealPackage,
    XdatPayload,
    Unknown
}

public sealed record ProtectionLayer(
    ProtectionLayerKind Kind,
    string Id,
    string Status,
    bool DecodeAvailable,
    bool EncodeAvailable,
    string Evidence);

public sealed record ProtectionPlan(
    string FileExtension,
    IReadOnlyList<ProtectionLayer> Layers,
    bool CanDecode,
    bool CanEncode,
    string Status,
    string Explanation);

/// <summary>
/// Fail-closed planner for ordered protection stages. Detection is not decryption.
/// </summary>
public static class ProtectionPipelinePlanner
{
    private const uint UnrealPackageTag = 0x9E2A83C1;

    public static ProtectionPlan Inspect(ReadOnlySpan<byte> data, string fileName,
        bool smartCryptKeyAvailable = false, bool smartCryptEncoderVerified = false,
        bool activeAntiCheatPrivateKeyAvailable = false, bool activeAntiCheatEncoderVerified = false,
        bool lineage2CustomTailProfileAvailable = false, bool lineage2CustomTailEncoderVerified = false)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        string extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (data.Length >= 0x400 && SmartCrypt1522Codec.HasHeader(data))
        {
            bool keyDerivableFromHeader;
            try { _ = SmartCryptKeyProfile.FromEncryptedFile(data); keyDerivableFromHeader = true; }
            catch (InvalidDataException) { keyDerivableFromHeader = false; }
            bool canDecode = smartCryptKeyAvailable || keyDerivableFromHeader;
            bool canEncode = canDecode && smartCryptEncoderVerified;
            var layer = new ProtectionLayer(ProtectionLayerKind.SmartCrypt1522Candidate,
                "smartcrypt-1.5.2.2",
                canEncode ? "codec-ready" : canDecode ? "decode-key-available-encode-unverified" : "recognized-key-required",
                canDecode, canEncode,
                "FF ED FA CE marker recognized. The transform is implemented. For this variant the per-file key6/key16 state can be reconstructed from the embedded 24-byte seed block when its external-key bit is clear; otherwise the matching global key is required. Re-encoding remains disabled until a real-file round trip is verified.");
            return new(extension, new[] { layer }, canDecode, canEncode,
                canEncode ? "recognized-supported-encryption" : canDecode ? "recognized-decode-only" : "recognized-key-required",
                canDecode
                    ? "SmartCrypt 1.5.2.2 recognized and a matching key profile is available. The file remains protected until the decoded intermediate is validated."
                    : "SmartCrypt 1.5.2.2 recognized, but this file's header does not provide enough key material and no matching external profile is available. The file is preserved; no key is guessed.");
        }

        if (ActiveAntiCheatCodec.HasHeader(data))
        {
            bool canDecode = activeAntiCheatPrivateKeyAvailable;
            bool canEncode = canDecode && activeAntiCheatEncoderVerified;
            var layer = new ProtectionLayer(ProtectionLayerKind.ActiveAntiCheatCrypt,
                "active-anticheat-crypt",
                canEncode ? "codec-ready" : canDecode ? "decode-key-available-encode-key-unverified" : "recognized-key-required",
                canDecode, canEncode,
                "ActiveAntiCheatCrypt magic recognized. Decoding requires the matching RSA private key captured from the protected client; encoding additionally requires a verified public exponent.");
            return new(extension, new[] { layer }, canDecode, canEncode,
                canEncode ? "recognized-supported-encryption" : canDecode ? "recognized-decode-only" : "recognized-key-required",
                canDecode
                    ? "ActiveAntiCheatCrypt container recognized and a private key is available. Re-encoding remains disabled until the matching public exponent is verified."
                    : "ActiveAntiCheatCrypt container recognized, but the matching RSA key is not available. The file is preserved; no key is guessed.");
        }

        if (TryReadLineage2Version(data, out int version))
        {
            bool supported = version is 111 or 413;
            if (version == 111)
            {
                bool stockChecksumValid = new Lineage2StandardCodec(111, verifyChecksumOnDecode: true).VerifyTailChecksum(data);
                bool canEncodeCustom = lineage2CustomTailProfileAvailable && lineage2CustomTailEncoderVerified;
                var customLayer = new ProtectionLayer(ProtectionLayerKind.Lineage2CustomTrailer,
                    "lineage2-111-custom-tail",
                    canEncodeCustom ? "original-trailer-preserved-and-roundtrip-verified" :
                        lineage2CustomTailProfileAvailable ? "original-trailer-profile-loaded-encode-unverified" : "original-trailer-profile-required",
                    true, canEncodeCustom,
                    stockChecksumValid
                        ? "Lineage2Ver111 XOR recognized. Original 20-byte trailer is preserved verbatim to avoid changing client-specific metadata."
                        : "Lineage2Ver111 XOR recognized with a non-stock CRC trailer. Original 20-byte trailer is preserved verbatim; the game client does not require checksum verification.");
                return new(extension, new[] { customLayer }, true, canEncodeCustom,
                    canEncodeCustom ? "recognized-supported-encryption" : "recognized-decode-only",
                    canEncodeCustom
                        ? "Protocol 111 and its original trailer profile are loaded; the exact round-trip has been verified."
                        : "Protocol 111 can be decoded. Compilation remains disabled until the original trailer is retained and the complete round-trip is verified.");
            }

            var layer = new ProtectionLayer(ProtectionLayerKind.Lineage2Standard,
                $"lineage2-{version}", supported ? "codec-implemented" : "identified-codec-unavailable",
                supported, supported,
                supported
                    ? $"Cabecera Lineage2Ver{version:D3} reconocida; codec integrado. La verificación del checksum de la cola es opcional."
                    : $"Cabecera Lineage2Ver{version:D3} reconocida, pero este protocolo aún no tiene codec integrado.");
            return new(extension, new[] { layer }, supported, supported,
                supported ? "recognized-supported-encryption" : "recognized-encryption",
                supported
                    ? $"Se identificó Lineage II {version:D3}; el codec está integrado."
                    : $"Se identificó Lineage II {version:D3}, pero todavía no hay codec disponible para esta versión.");
        }

        if (data.Length >= 4 && BitConverter.ToUInt32(data[..4]) == UnrealPackageTag)
        {
            var layer = new ProtectionLayer(ProtectionLayerKind.UnrealPackage, "unreal-package",
                "signature-only", false, false,
                "Firma Unreal reconocida. Esta firma identifica un paquete Unreal, no un XDAT.");
            return new(extension, new[] { layer }, false, false, "plain-format-candidate",
                "Firma de paquete Unreal reconocida; falta validar tablas, índices y offsets. No clasificar como XDAT aunque la extensión sea .xdat.");
        }

        var unknown = new ProtectionLayer(ProtectionLayerKind.Unknown, "unknown", "unsupported", false, false,
            "No hay una firma ni un codec verificado para este archivo.");
        return new(extension, new[] { unknown }, false, false, "unknown-or-unsupported",
            "Formato o protección desconocidos. El archivo se conserva intacto; no se adivinan algoritmo, clave ni versión.");
    }

    private static bool TryReadLineage2Version(ReadOnlySpan<byte> data, out int version)
    {
        version = 0;
        const int headerChars = 14;
        if (data.Length < headerChars * 2) return false;
        Span<char> chars = stackalloc char[headerChars];
        for (int i = 0; i < headerChars; i++)
        {
            if (data[i * 2 + 1] != 0) return false;
            chars[i] = (char)data[i * 2];
        }
        return chars[..11].SequenceEqual("Lineage2Ver") &&
            int.TryParse(chars[11..], out version);
    }
}
