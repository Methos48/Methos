using System.Text;
using MethosUEngine;

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
Check(lineage.Status == "recognized-encryption" && !lineage.CanDecode && !lineage.CanEncode,
    "Lineage2Ver413 must be identified without claiming a codec is available.");

var unknown = ProtectionPipelinePlanner.Inspect(new byte[] { 0x12, 0x34, 0x56 }, "unknown.bin");
Check(unknown.Status == "unknown-or-unsupported" && !unknown.CanDecode,
    "Unknown files must fail closed.");

byte[] unreal = new byte[] { 0xC1, 0x83, 0x2A, 0x9E, 0, 0 };
var package = ProtectionPipelinePlanner.Inspect(unreal, "Interface.u");
Check(package.Status == "plain-format-candidate" && package.Layers[0].Kind == ProtectionLayerKind.UnrealPackage,
    "Unreal package signature was not recognized.");

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
