using System.Text;
using System.Security.Cryptography;
using MethosUEngine;

if (args.Length > 0 && args[0] == "--ucc-decompile")
{
    if (args.Length != 3)
        throw new ArgumentException("Usage: --ucc-decompile <package.u> <outputDirectory>");
    UnrealPackageDecompileResult result = UnrealPackageDecompiler.DecompileToFolder(args[1], args[2]);
    Console.WriteLine($"PASS: UELib exported {result.ExportedCount}/{result.ClassCount} classes from package {result.PackageVersion}/{result.LicenseeVersion}; objects={result.ObjectCount}; failed={result.FailedCount}.");
    foreach (string error in result.Errors.Take(20)) Console.WriteLine("EXPORT_ERROR " + error);
    if (result.FailedCount != 0 || result.ExportedCount != result.ClassCount)
        throw new InvalidDataException("UnrealScript source export did not complete without errors.");
    return;
}

if (args.Length > 0 && args[0] == "--ucc-compile")
{
    if (args.Length != 4)
        throw new ArgumentException("Usage: --ucc-compile <runtimeRoot> <sourceDirectory> <outputPackage>");
    UccCompileResult result = UccCompiler.CompileInterface(args[1], args[2]);
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[3]))!);
    File.WriteAllBytes(args[3], result.PackageBytes);
    Console.WriteLine($"PASS: UCC compiled {result.PackageBytes.Length} bytes; profile={result.ProfileDescription}; names={result.NameCount}; imports={result.ImportCount}; exports={result.ExportCount}.");
    return;
}

static void Check(bool value, string message)
{
    if (!value) throw new Exception(message);
}

byte[] smartSample = new byte[0x400];
Convert.FromHexString("FFEDFACE0100").CopyTo(smartSample, 0);
var smart = ProtectionPipelinePlanner.Inspect(smartSample, "interface.xdat");
Check(smart.Status == "recognized-decode-only" && smart.CanDecode && !smart.CanEncode,
    "SmartCrypt sample with an embedded seed block must be recognized as decode-capable but not yet encode-verified.");

byte[] l2 = Encoding.Unicode.GetBytes("Lineage2Ver413");
var lineage = ProtectionPipelinePlanner.Inspect(l2, "systemmsg-e.dat");
Check(lineage.Status == "recognized-supported-encryption" && lineage.CanDecode && lineage.CanEncode,
    "Lineage2Ver413 must report the integrated codec as available.");

var unknown = ProtectionPipelinePlanner.Inspect(new byte[] { 0x12, 0x34, 0x56 }, "unknown.bin");
Check(unknown.Status == "unknown-or-unsupported" && !unknown.CanDecode,
    "Unknown files must fail closed.");

byte[] unreal = new byte[] { 0xC1, 0x83, 0x2A, 0x9E, 0, 0 };
var package = ProtectionPipelinePlanner.Inspect(unreal, "Interface.u");
Check(package.Status == "plain-format-candidate" && package.Layers[0].Kind == ProtectionLayerKind.UnrealPackage,
    "Unreal package signature was not recognized.");
var wrongExtension = ProtectionPipelinePlanner.Inspect(unreal, "Interface.xdat");
Check(wrongExtension.Layers[0].Kind == ProtectionLayerKind.UnrealPackage &&
      !wrongExtension.Explanation.Contains("XDAT válido", StringComparison.OrdinalIgnoreCase),
    "An Unreal package signature must never be mislabeled as XDAT based only on extension.");
var xdatSmart = ProtectionPipelinePlanner.Inspect(smartSample, "Interface.xdat");
Check(xdatSmart.Layers[0].Kind == ProtectionLayerKind.SmartCrypt1522Candidate &&
      xdatSmart.CanDecode && !xdatSmart.CanEncode,
    "Encrypted XDAT with an embedded seed block must be recognized as decode-capable but not encode-verified.");

Console.WriteLine("PASS: SmartCrypt candidate, Lineage II header, unknown fail-closed, and Unreal signature checks.");

var twoLayerPlan = new ProtectionPlan(".dat", new[]
{
    new ProtectionLayer(ProtectionLayerKind.Lineage2Standard, "test-a", "test", true, true, "test vector"),
    new ProtectionLayer(ProtectionLayerKind.Lineage2Standard, "test-b", "test", true, true, "test vector")
}, true, true, "test", "Synthetic known-answer pipeline test.");
var runtime = new ProtectionCodecRuntime(new IProtectionCodec[] { new XorTestCodec("test-a", 0x35), new XorTestCodec("test-b", 0xA7) });
byte[] plain = Encoding.UTF8.GetBytes("Methos reversible pipeline vector");
byte[] encrypted = runtime.Encode(twoLayerPlan, plain, "vector.dat");
byte[] decoded = runtime.Decode(twoLayerPlan, encrypted, "vector.dat");
Check(plain.SequenceEqual(decoded), "Two-layer encode/decode round-trip failed.");

var noSmartCodec = new ProtectionCodecRuntime(Array.Empty<IProtectionCodec>());
bool blocked = false;
try { noSmartCodec.Decode(smart, smartSample, "interface.xdat"); }
catch (NotSupportedException) { blocked = true; }
Check(blocked, "Runtime must refuse SmartCrypt when no codec is registered.");

Console.WriteLine("PASS: synthetic two-layer encode/decode round-trip and missing SmartCrypt codec rejection.");

var standard = ProtectionCodecRuntime.CreateDefault();
byte[] standardPlain = Encoding.UTF8.GetBytes("Methos standard Lineage II codec known-answer round-trip.");
foreach (int protocol in new[] { 111, 413 })
{
    var standardPlan = new ProtectionPlan(".dat", new[]
    {
        new ProtectionLayer(ProtectionLayerKind.Lineage2Standard, $"lineage2-{protocol}", "test", true, true, "synthetic round-trip")
    }, true, true, "test", "Synthetic Lineage II standard codec round-trip.");
    byte[] standardEncrypted = standard.Encode(standardPlan, standardPlain, "vector.dat");
    var detected = ProtectionPipelinePlanner.Inspect(standardEncrypted, "vector.dat");
    Check(detected.CanDecode && detected.CanEncode, $"Protocol {protocol} header was not recognized as supported.");
    byte[] standardDecoded = standard.Decode(standardPlan, standardEncrypted, "vector.dat");
    Check(standardPlain.SequenceEqual(standardDecoded), $"Protocol {protocol} round-trip failed.");
}
Console.WriteLine("PASS: integrated Lineage II 111 XOR and 413 RSA/zlib encode/decode round-trips.");

if (File.Exists("sysstring-e.dat"))
{
    byte[] realSysString = File.ReadAllBytes("sysstring-e.dat");
    var realSysPlan = ProtectionPipelinePlanner.Inspect(realSysString, "sysstring-e.dat");
    Check(realSysPlan.Status == "recognized-supported-encryption" &&
          realSysPlan.Layers.Any(x => x.Id == "lineage2-413"),
        "Real SysString-e.dat must be detected as Lineage2Ver413.");

    var realSysCodec = new Lineage2StandardCodec(413);
    byte[] decodedSysString = realSysCodec.Decode(realSysString, "sysstring-e.dat");
    Check(decodedSysString.Length == 29584 &&
          decodedSysString.AsSpan().IndexOf("Equipment"u8) >= 0 &&
          decodedSysString.AsSpan().IndexOf("Lineage II User Agreement"u8) >= 0,
        "Real SysString-e.dat did not decode to the expected English system-text resource.");
    string decodedSysHash = Convert.ToHexString(SHA256.HashData(decodedSysString)).ToLowerInvariant();
    Check(decodedSysHash == "a2c15980a6278c04640fd9163da9f8916d2f0e9349538cc9a6f7d4b74a4065b4",
        "Real SysString-e.dat decoded content hash differs from the independently validated reference.");

    byte[] rebuiltSysString = realSysCodec.Encode(decodedSysString, "sysstring-e.dat");
    byte[] verifiedSysString = realSysCodec.Decode(rebuiltSysString, "sysstring-e.dat");
    Check(decodedSysString.SequenceEqual(verifiedSysString),
        "Real SysString-e.dat protocol 413 decode/re-encode round-trip failed.");
    Console.WriteLine($"PASS: REAL SysString-e.dat Lineage2Ver413 RSA/zlib decode and re-encode round-trip; encrypted={realSysString.Length}, decoded={decodedSysString.Length}, SHA256={decodedSysHash}.");
}
else
{
    Console.WriteLine("SKIP: sysstring-e.dat real protocol 413 fixture is not present.");
}

{
    byte[] seedBlock = Enumerable.Range(0, 24).Select(i => (byte)(0x31 + i * 7)).ToArray();
seedBlock[0] = 1;
seedBlock[1] = 0;
byte[] smartMarker = { 0xFF, 0xED, 0xFA, 0xCE };
byte[] fakeEncryptedHeader = new byte[0x400];
smartMarker.CopyTo(fakeEncryptedHeader, 0);
seedBlock.CopyTo(fakeEncryptedHeader, 4);
var smartProfile = SmartCryptKeyProfile.FromEncryptedFile(fakeEncryptedHeader);
Check(smartProfile.Lineage2Version == 111 && smartProfile.SeedBlock.SequenceEqual(seedBlock),
    "SmartCrypt per-file key derivation from embedded seed block failed.");

var smartCodec = new SmartCrypt1522Codec(smartProfile);
byte[] smartPlain = Enumerable.Range(0, 2048).Select(i => (byte)((i * 37 + 19) & 0xFF)).ToArray();
Encoding.Unicode.GetBytes("Lineage2Ver111").CopyTo(smartPlain, 0);
byte[] smartEncrypted = smartCodec.Encode(smartPlain, "Interface.u");
Check(SmartCrypt1522Codec.HasHeader(smartEncrypted) &&
      smartEncrypted.AsSpan(4, 24).SequenceEqual(seedBlock),
    "SmartCrypt encoder did not restore the original marker and per-file seed block.");

string smartProfileText =
    $"NPK_GLOBAL_KEY=12345678\\nSTATE_SEED=0100\\nSEED_BLOCK={Convert.ToHexString(seedBlock)}\\nMARKER=FFEDFACE\\nLINEAGE2_VERSION=0000006F\\nKEY6={Convert.ToHexString(smartProfile.Key6)}\\nKEY16={Convert.ToHexString(smartProfile.Key16)}\\nFLAG=00\\n";
var parsedSmartProfile = SmartCryptKeyProfile.ParseLog(smartProfileText, "synthetic-log");
Check(parsedSmartProfile.Key6.SequenceEqual(smartProfile.Key6) &&
      parsedSmartProfile.Key16.SequenceEqual(smartProfile.Key16) &&
      parsedSmartProfile.SeedBlock.SequenceEqual(seedBlock) &&
      parsedSmartProfile.Lineage2Version == 111 &&
      parsedSmartProfile.GlobalKey == 0x12345678 &&
      parsedSmartProfile.Origin == "synthetic-log",
    "SmartCrypt captured key profile import failed.");

var smartRuntime = ProtectionCodecRuntime.CreateDefault(smartCryptProfile: parsedSmartProfile);
var smartPlan = ProtectionPipelinePlanner.Inspect(smartEncrypted, "Interface.u",
    smartCryptKeyAvailable: false, smartCryptEncoderVerified: false);
Check(smartPlan.CanDecode && !smartPlan.CanEncode &&
      smartPlan.Layers[0].Kind == ProtectionLayerKind.SmartCrypt1522Candidate,
    "SmartCrypt planner did not derive decode keys from the embedded seed block or kept unverified encoding disabled.");
byte[] smartDecoded = smartRuntime.Decode(smartPlan, smartEncrypted, "Interface.u");
Check(smartPlain.SequenceEqual(smartDecoded), "SmartCrypt 1.5.2.2 header reconstruction and XOR round-trip failed.");

bool smartEncodeBlocked = false;
try { _ = smartRuntime.Encode(smartPlan, smartPlain, "Interface.u"); }
catch (NotSupportedException) { smartEncodeBlocked = true; }
Check(smartEncodeBlocked, "SmartCrypt runtime must block re-encoding until a real-file round trip is verified.");

byte[] externalKeySample = new byte[0x400];
smartMarker.CopyTo(externalKeySample, 0);
byte[] externalSeed = seedBlock.ToArray();
externalSeed[1] = 1;
externalSeed.CopyTo(externalKeySample, 4);
var smartNeedsExternal = ProtectionPipelinePlanner.Inspect(externalKeySample, "Interface.u");
Check(smartNeedsExternal.Status == "recognized-key-required" && !smartNeedsExternal.CanDecode,
    "SmartCrypt must require the external key when the seed block sets its external-key bit.");
Console.WriteLine("PASS: SmartCrypt 1.5.2.2 embedded seed derivation, Lineage2Ver header reconstruction, profile import, and fail-closed encode.");
}

using (RSA aacRsa = RSA.Create(2048))
{
    RSAParameters aacParameters = aacRsa.ExportParameters(true);
    byte[] nLe = aacParameters.Modulus!.Reverse().ToArray();
    byte[] dLe = aacParameters.D!.Reverse().ToArray();
    byte[] eLe = aacParameters.Exponent!.Reverse().ToArray();
    var aacProfile = new ActiveAntiCheatRsaProfile(nLe, dLe, eLe);
    var parsedAacProfile = ActiveAntiCheatRsaProfile.ParseLog(
        $"N_LE={Convert.ToHexString(nLe)}\\nD_LE={Convert.ToHexString(dLe)}\\n", eLe, "synthetic-log");
    Check(parsedAacProfile.Modulus == aacProfile.Modulus &&
          parsedAacProfile.PrivateExponent == aacProfile.PrivateExponent &&
          parsedAacProfile.Origin == "synthetic-log",
        "ActiveAntiCheatCrypt captured N_LE/D_LE profile import failed.");
    var aacRuntime = ProtectionCodecRuntime.CreateDefault(aacProfile);
    var aacPlan = new ProtectionPlan(".u", new[]
    {
        new ProtectionLayer(ProtectionLayerKind.ActiveAntiCheatCrypt,
            "active-anticheat-crypt", "synthetic", true, true, "synthetic RSA/RC4 fixture")
    }, true, true, "test", "Synthetic ActiveAntiCheatCrypt round-trip.");
    byte[] aacPlain = Encoding.UTF8.GetBytes("Methos ActiveAntiCheatCrypt RSA-2048 RC4 test vector.");
    byte[] aacEncrypted = aacRuntime.Encode(aacPlan, aacPlain, "Interface.u");
    Check(ActiveAntiCheatCodec.HasHeader(aacEncrypted), "ActiveAntiCheatCrypt header was not emitted.");
    byte[] rsaMessage = aacRsa.Decrypt(
        aacEncrypted.AsSpan(ActiveAntiCheatCodec.RsaBlockOffset, ActiveAntiCheatCodec.RsaBlockLength).ToArray(),
        RSAEncryptionPadding.Pkcs1);
    Check(rsaMessage.Length == 40 &&
          rsaMessage.AsSpan(0, 20).SequenceEqual("ActiveAnticheatCrypt"u8),
        "ActiveAntiCheatCrypt RSA block did not contain the expected magic and RC4 key.");
    var aacDetected = ProtectionPipelinePlanner.Inspect(aacEncrypted, "Interface.u",
        activeAntiCheatPrivateKeyAvailable: true, activeAntiCheatEncoderVerified: false);
    Check(aacDetected.CanDecode && !aacDetected.CanEncode &&
          aacDetected.Layers[0].Kind == ProtectionLayerKind.ActiveAntiCheatCrypt,
        "ActiveAntiCheatCrypt detection did not expose the configured key capabilities.");
    byte[] aacDecoded = aacRuntime.Decode(aacDetected, aacEncrypted, "Interface.u");
    Check(aacPlain.SequenceEqual(aacDecoded), "ActiveAntiCheatCrypt RSA/RC4 round-trip failed.");
    var aacNoKey = ProtectionPipelinePlanner.Inspect(aacEncrypted, "Interface.u");
    Check(aacNoKey.Status == "recognized-key-required" && !aacNoKey.CanDecode,
        "ActiveAntiCheatCrypt must fail closed when its matching private key is unavailable.");
}
Console.WriteLine("PASS: ActiveAntiCheatCrypt RSA-2048/PKCS#1 v1.5 + RC4 encode/decode and key-required detection.");
var literalBody = "return 42;";
byte[] literalBytecode = UnrealScriptFunctionCompiler.CompileReturnOnlyBody(literalBody);
string restoredLiteralBody = UnrealScriptFunctionDecompiler.DecompileReturnOnly(literalBytecode);
Check(restoredLiteralBody == literalBody,
    "UnrealScript literal return compile/decompile round-trip failed.");
Console.WriteLine("PASS: native UnrealScript literal return compile/decompile round-trip.");


foreach (string targetName in new[] { "interface.u", "interface.xdat" })
{
    if (!File.Exists(targetName)) continue;
    byte[] original = File.ReadAllBytes(targetName);
    var smartProfile = SmartCryptKeyProfile.FromEncryptedFile(original);
    var smartCodec = new SmartCrypt1522Codec(smartProfile);
    byte[] lineageEnvelope = smartCodec.Decode(original, targetName);
    Check(lineageEnvelope.AsSpan(0, 28).SequenceEqual(Encoding.Unicode.GetBytes($"Lineage2Ver{smartProfile.Lineage2Version:D3}")),
        $"{targetName}: SmartCrypt did not reconstruct the Lineage2Ver header.");

    var standardCodec = new Lineage2StandardCodec(smartProfile.Lineage2Version);
    byte[] rawWithoutTail = standardCodec.Decode111Unchecked(lineageEnvelope, includeTail: false);
    byte[] rawWithTail = standardCodec.Decode111Unchecked(lineageEnvelope, includeTail: true);
    byte[] selectedRaw;
    bool includeTail;

    if (targetName.EndsWith(".u", StringComparison.OrdinalIgnoreCase))
    {
        bool parsed = false;
        try
        {
            var parsedPackageWithoutTail = UnrealPackageIndex.Parse(rawWithoutTail);
            Check(parsedPackageWithoutTail.Summary.Version >= 68 && package.Summary.Version <= 194,
                "Dunk3l interface.u produced an invalid Unreal package version.");
            selectedRaw = rawWithoutTail;
            includeTail = false;
            parsed = true;
        }
        catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or ArgumentException)
        {
            var parsedPackageWithTail = UnrealPackageIndex.Parse(rawWithTail);
            Check(parsedPackageWithTail.Summary.Version >= 68 && package.Summary.Version <= 194,
                "Dunk3l interface.u produced an invalid Unreal package version.");
            selectedRaw = rawWithTail;
            includeTail = true;
            parsed = true;
        }
        Check(parsed, "Dunk3l interface.u could not be parsed as a real Unreal package.");
    }
    else
    {
        string withoutTailText = Encoding.ASCII.GetString(rawWithoutTail);
        string withTailText = Encoding.ASCII.GetString(rawWithTail);
        includeTail = !withoutTailText.Contains("AbnormalDebuffsStatusWnd", StringComparison.Ordinal)
            && withTailText.Contains("AbnormalDebuffsStatusWnd", StringComparison.Ordinal);
        selectedRaw = includeTail ? rawWithTail : rawWithoutTail;
        Check(Encoding.ASCII.GetString(selectedRaw).Contains("AbnormalDebuffsStatusWnd", StringComparison.Ordinal),
            "Dunk3l interface.xdat did not expose expected XDAT window names after both layers.");
    }

    byte[] standardRebuilt = standardCodec.Encode111PreservingTail(selectedRaw, lineageEnvelope, includeTail);
    Check(standardRebuilt.SequenceEqual(lineageEnvelope),
        $"{targetName}: protocol-111 payload transform plus original trailer did not round-trip exactly.");
    byte[] smartRebuilt = smartCodec.Encode(standardRebuilt, targetName);
    Check(smartRebuilt.SequenceEqual(original),
        $"{targetName}: SmartCrypt failed byte-for-byte round-trip.");
    Console.WriteLine($"PASS: REAL Dunk3l {targetName} SmartCrypt + Lineage2Ver{smartProfile.Lineage2Version} decode/re-encode exact round-trip; custom trailer preserved ({original.Length} bytes).");
}

if (File.Exists("Engine.u"))
{
    byte[] realEngine = File.ReadAllBytes("Engine.u");
    var realDetection = ProtectionPipelinePlanner.Inspect(realEngine, "Engine.u");
    Check(realDetection.Status == "recognized-supported-encryption" &&
          realDetection.Layers.Any(x => x.Id == "lineage2-111"),
        "Real Engine.u must be identified as Lineage2Ver111.");

    var realPlan = new ProtectionPlan(".u", new[]
    {
        new ProtectionLayer(ProtectionLayerKind.Lineage2Standard, "lineage2-111", "verified", true, true,
            "Real client Engine.u sample")
    }, true, true, "real-sample", "Real Lineage2Ver111 Engine.u decode.");

    byte[] realPlain = standard.Decode(realPlan, realEngine, "Engine.u");
    Check(realPlain.Length > 65536 &&
          BitConverter.ToUInt32(realPlain, 0) == UnrealPackageIndex.PackageTag,
        "Decoded Engine.u must begin with a classic Unreal package signature.");

    var realPackage = UnrealPackageIndex.Parse(realPlain);
    Console.WriteLine($"ENGINE_U_TABLES names={realPackage.Names.Count} imports={realPackage.Imports.Count} exports={realPackage.Exports.Count} header={realPackage.Summary.NameCount}/{realPackage.Summary.ImportCount}/{realPackage.Summary.ExportCount}");
    Check(realPackage.Summary.Version == 123 && realPackage.Summary.LicenseeVersion == 30,
        "Decoded Engine.u package profile must be 123/30.");
    Check(realPackage.Names.Count > 1000 && realPackage.Imports.Count > 50 &&
          realPackage.Exports.Count > 1000,
        "Decoded Engine.u must contain plausible name/import/export tables.");

    byte[] rebuiltPlain = new UnrealPackageBuilder(realPackage).Build();
    var rebuiltPackage = UnrealPackageIndex.Parse(rebuiltPlain);
    Check(rebuiltPackage.Names.SequenceEqual(realPackage.Names) &&
          rebuiltPackage.Imports.SequenceEqual(realPackage.Imports) &&
          rebuiltPackage.Exports.SequenceEqual(realPackage.Exports),
        "Engine.u package rebuild must preserve all name/import/export tables.");

    byte[] rebuiltEncrypted = standard.Encode(realPlan, rebuiltPlain, "Engine.u");
    byte[] finalPlain = standard.Decode(realPlan, rebuiltEncrypted, "Engine.u");
    var finalPackage = UnrealPackageIndex.Parse(finalPlain);
    Check(finalPackage.Names.SequenceEqual(realPackage.Names) &&
          finalPackage.Imports.SequenceEqual(realPackage.Imports) &&
          finalPackage.Exports.SequenceEqual(realPackage.Exports),
        "Engine.u 111 encode/decode after package rebuild must preserve all tables.");

    Console.WriteLine($"PASS: REAL Engine.u Lineage2Ver111 decode, Unreal parse, rebuild, and re-encrypt; names={realPackage.Names.Count}, imports={realPackage.Imports.Count}, exports={realPackage.Exports.Count}.");
}
else
{
    Console.WriteLine("SKIP: Engine.u real-client fixture is not present in this checkout.");
}

sealed class XorTestCodec(string layerId, byte key) : IProtectionCodec
{
    public string LayerId => layerId;
    public byte[] Decode(ReadOnlyMemory<byte> input, string originalFileName, CancellationToken cancellationToken = default)
        => Transform(input, cancellationToken);
    public byte[] Encode(ReadOnlyMemory<byte> input, string originalFileName, CancellationToken cancellationToken = default)
        => Transform(input, cancellationToken);
    private byte[] Transform(ReadOnlyMemory<byte> input, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        byte[] result = input.ToArray();
        for (int i = 0; i < result.Length; i++) result[i] ^= key;
        return result;
    }
}
