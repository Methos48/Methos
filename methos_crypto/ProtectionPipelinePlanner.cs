using System.Text;

namespace MethosUEngine;

public enum ProtectionLayerKind
{
    SmartCrypt1522Candidate,
    ActiveAntiCheatCrypt,
    Lineage2Standard,
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
    private static readonly byte[] SmartCrypt1522SamplePrefix = Convert.FromHexString("FFEDFACE0100");
    private const uint UnrealPackageTag = 0x9E2A83C1;

    public static ProtectionPlan Inspect(ReadOnlySpan<byte> data, string fileName,
        bool activeAntiCheatPrivateKeyAvailable = false, bool activeAntiCheatPublicExponentAvailable = false)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        string extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (data.Length >= SmartCrypt1522SamplePrefix.Length &&
            data[..SmartCrypt1522SamplePrefix.Length].SequenceEqual(SmartCrypt1522SamplePrefix))
        {
            var layer = new ProtectionLayer(ProtectionLayerKind.SmartCrypt1522Candidate,
                "smartcrypt-1.5.2.2", "candidate-unverified", false, false,
                "Coincide con el prefijo de las muestras aportadas. No demuestra por sí solo que cualquier archivo con este prefijo use SmartCrypt.");
            return new(extension, new[] { layer }, false, false, "recognized-candidate",
                "Firma coincidente con las muestras SmartCrypt 1.5.2.2. Falta recuperar y probar el transformador y su inversa; no se intenta descifrar por suposición.");
        }

        if (ActiveAntiCheatCodec.HasHeader(data))
        {
            bool canDecode = activeAntiCheatPrivateKeyAvailable;
            bool canEncode = canDecode && activeAntiCheatPublicExponentAvailable;
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
            var layer = new ProtectionLayer(ProtectionLayerKind.Lineage2Standard,
                $"lineage2-{version}", supported ? "codec-implemented-roundtrip-pending-real-sample" : "identified-codec-unavailable",
                supported, supported,
                supported
                    ? $"Cabecera Lineage2Ver{version:D3} reconocida; hay codec integrado con pruebas sintéticas de ida y vuelta. Falta validarlo con la captura real de SmartCrypt."
                    : $"Cabecera Lineage2Ver{version:D3} reconocida, pero este protocolo aún no tiene codec integrado.");
            return new(extension, new[] { layer }, supported, supported,
                supported ? "recognized-supported-encryption" : "recognized-encryption",
                supported
                    ? $"Se identificó Lineage II {version:D3}; el codec está integrado. La captura real debe superar la validación de checksum, descifrado y formato."
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
