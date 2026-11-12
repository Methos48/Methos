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

byte[] smartSample = Convert.FromHexString("FFEDFACE0100").Concat(new byte[32]).ToArray();
var smart = ProtectionPipelinePlanner.Inspect(smartSample, "interface.xdat");
Check(smart.Status == "recognized-candidate" && !smart.CanDecode && !smart.CanEncode,
    "SmartCrypt sample must be identified as a candidate, never falsely reported as decrypted.");

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
      !xdatSmart.CanDecode && !xdatSmart.CanEncode,
    "Encrypted XDAT must remain a SmartCrypt candidate until decoded and structurally validated.");

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
Check(blocked, "Runtime must refuse the real SmartCrypt candidate until a verified codec is registered.");

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

using (RSA aacRsa = RSA.Create(2048))
{
    RSAParameters aacParameters = aacRsa.ExportParameters(true);
    byte[] nLe = aacParameters.Modulus!.Reverse().ToArray();
    byte[] dLe = aacParameters.D!.Reverse().ToArray();
    byte[] eLe = aacParameters.Exponent!.Reverse().ToArray();
    var aacProfile = new ActiveAntiCheatRsaProfile(nLe, dLe, eLe);
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
          rsaMessage.AsSpan(0, 20).SequenceEqual("ActiveAntiCheatCrypt"u8),
        "ActiveAntiCheatCrypt RSA block did not contain the expected magic and RC4 key.");
    var aacDetected = ProtectionPipelinePlanner.Inspect(aacEncrypted, "Interface.u",
        activeAntiCheatPrivateKeyAvailable: true, activeAntiCheatPublicExponentAvailable: true);
    Check(aacDetected.CanDecode && aacDetected.CanEncode &&
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
