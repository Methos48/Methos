using System.Security.Cryptography;
using Methos.Engine;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: L2SudamericaProbe <input.u|input.xdat> <output-directory> <u|xdat>");
    return 2;
}

string inputPath = Path.GetFullPath(args[0]);
string outputRoot = Path.GetFullPath(args[1]);
string kind = args[2].ToLowerInvariant();
if (kind is not ("u" or "xdat")) throw new ArgumentException("kind must be u or xdat");
Directory.CreateDirectory(outputRoot);

byte[] original = File.ReadAllBytes(inputPath);
Console.WriteLine($"ORIGINAL bytes={original.Length} sha256={Convert.ToHexString(SHA256.HashData(original))}");
SmartCryptKeyProfile smartProfile = SmartCryptKeyProfile.FromEncryptedFile(original);
Console.WriteLine($"SMARTCRYPT marker={Convert.ToHexString(smartProfile.Marker)} state0=0x{smartProfile.Seed0:X2} state1=0x{smartProfile.Seed1:X2} lineageVersion={smartProfile.Lineage2Version} externalKeyFlag={smartProfile.UsesC5Variant}");
var smartCodec = new SmartCrypt1522Codec(smartProfile);
byte[] lineageEnvelope = smartCodec.Decode(original, Path.GetFileName(inputPath));
Console.WriteLine($"SMARTCRYPT_DECODE_OK bytes={lineageEnvelope.Length} header={Convert.ToHexString(lineageEnvelope.AsSpan(0, Math.Min(28, lineageEnvelope.Length)))}");

bool anyValid = false;
foreach (bool includeTail in new[] { kind == "u", kind == "xdat" }.Distinct().Concat(new[] { ! (kind == "u"), !(kind == "xdat") }).Distinct())
{
    string mode = includeTail ? "tail-in-payload" : "separate-20-byte-tail";
    string candidatePath = Path.Combine(outputRoot, $"{Path.GetFileName(inputPath)}.{(includeTail ? "include-tail" : "strip-tail")}.decoded");
    try
    {
        var tailProfile = Lineage2CustomTailProfile.FromEnvelope(lineageEnvelope,
            includeTailInPayload: includeTail, origin: inputPath);
        var lineageCodec = new Lineage2Ver111CustomTailCodec(tailProfile);
        byte[] plaintext = lineageCodec.Decode(lineageEnvelope, Path.GetFileName(inputPath));
        byte[] lineageRebuilt = lineageCodec.Encode(plaintext, Path.GetFileName(inputPath));
        byte[] protectedRebuilt = smartCodec.Encode(lineageRebuilt, Path.GetFileName(inputPath));
        bool exact = protectedRebuilt.AsSpan().SequenceEqual(original);
        if (!exact) throw new InvalidDataException("Two-layer protection round-trip did not reproduce the original bytes.");
        File.WriteAllBytes(candidatePath, plaintext);
        Console.WriteLine($"CANDIDATE mode={mode} bytes={plaintext.Length} sha256={Convert.ToHexString(SHA256.HashData(plaintext))} exactProtectedRoundTrip={exact} path={candidatePath}");

        if (kind == "u")
        {
            var index = UnrealPackageIndex.Parse(plaintext);
            Console.WriteLine($"UNREAL_PACKAGE_OK mode={mode} version={index.Summary.Version}/{index.Summary.LicenseeVersion} names={index.Names.Count} imports={index.Imports.Count} exports={index.Exports.Count}");
            string sourceDir = Path.Combine(outputRoot, includeTail ? "sources-include-tail" : "sources-strip-tail");
            var result = UnrealPackageDecompiler.DecompileToFolder(candidatePath, sourceDir);
            Console.WriteLine($"DECOMPILE_RESULT mode={mode} classes={result.ClassCount} exported={result.ExportedCount} failed={result.FailedCount} objects={result.ObjectCount}");
            anyValid = result.FailedCount == 0 && result.ExportedCount > 0;
        }
        else
        {
            Console.WriteLine($"XDAT_CANDIDATE_READY mode={mode} path={candidatePath}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"CANDIDATE_REJECTED mode={mode} error={ex.GetType().Name}: {ex.Message}");
    }
}
if (kind == "u" && !anyValid)
{
    Console.Error.WriteLine("No candidate U package completed decompilation.");
    return 3;
}
return 0;
