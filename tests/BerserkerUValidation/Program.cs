using System.Text;
using System.Text.RegularExpressions;
using Methos.Engine;

if (args.Length != 4)
    throw new ArgumentException("Usage: BerserkerUValidation <uccRuntimeRoot> <inputInterface.u> <outputDirectory> <outputInterface.u>");

string runtimeRoot = Path.GetFullPath(args[0]);
string inputPath = Path.GetFullPath(args[1]);
string outputRoot = Path.GetFullPath(args[2]);
string outputPath = Path.GetFullPath(args[3]);
string sourceDirectory = Path.Combine(outputRoot, "Sources");
string compiledDirectory = Path.Combine(outputRoot, "Compiled");
Directory.CreateDirectory(outputRoot);
Directory.CreateDirectory(compiledDirectory);

byte[] original = File.ReadAllBytes(inputPath);
var inspection = ProtectionPipelinePlanner.InspectFullPipeline(original, inputPath);
if (!inspection.IsPlaintextCandidate || !inspection.Plan.CanDecode)
    throw new InvalidDataException($"Berserker Interface.u cannot be decoded safely: {inspection.Plan.Status}; {inspection.Plan.Explanation}");
if (inspection.Plan.Layers.Any(layer => !layer.EncodeAvailable))
    throw new InvalidDataException("Berserker protection chain has an unverified encoder.");

string tempPackage = Path.Combine(Path.GetTempPath(), "Berserker-" + Guid.NewGuid().ToString("N") + ".u");
string nativeRegistry = Path.Combine(outputRoot, "native-functions.json");
try
{
    File.WriteAllBytes(tempPackage, inspection.DecodedPayload);
    string referenceSources = Path.Combine(runtimeRoot, "Interlude", "Interface", "Classes");
    IReadOnlyList<NativeFunctionDescriptor> natives = NativeFunctionRegistryBuilder.Build(runtimeRoot, nativeRegistry);
    Console.WriteLine($"NATIVE_REGISTRY_ENTRIES={natives.Count}");

    UnrealPackageDecompileResult decompiled = UnrealPackageDecompiler.DecompileToFolder(
        tempPackage, sourceDirectory, referenceSources, nativeRegistry);
    if (decompiled.FailedCount != 0 || decompiled.ExportedCount != decompiled.ClassCount)
        throw new InvalidDataException($"Berserker decompile incomplete: {decompiled.ExportedCount}/{decompiled.ClassCount}; errors={decompiled.FailedCount}.");

    string[] sources = Directory.GetFiles(sourceDirectory, "*.uc", SearchOption.TopDirectoryOnly);
    int unresolved = 0;
    var unresolvedFiles = new List<string>();
    foreach (string file in sources)
    {
        string text = File.ReadAllText(file);
        int count = Regex.Matches(text, @"__NFUN_\d+__|\bUnknownFunction\d+\b").Count;
        if (count > 0) { unresolved += count; unresolvedFiles.Add($"{Path.GetFileName(file)}={count}"); }
    }
    File.WriteAllLines(Path.Combine(outputRoot, "native-resolution-audit.txt"), new[]
    {
        $"CLASSES={decompiled.ExportedCount}",
        $"NATIVE_REGISTRY_ENTRIES={natives.Count}",
        $"UNRESOLVED_NATIVE_CALLS={unresolved}",
        "UNRESOLVED_FILES=" + string.Join(", ", unresolvedFiles)
    });
    if (unresolved != 0)
        throw new InvalidDataException($"Berserker source export still contains {unresolved} unresolved native calls: {string.Join(", ", unresolvedFiles.Take(20))}");

    string marker = "MethosBerserkerFinalProbe_20261113";
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

    // First produce a clean, installable rebuild from the untouched recovered sources.
    UccCompileResult cleanCompile = UccCompiler.CompileInterface(runtimeRoot, sourceDirectory);
    byte[] cleanPackage = UnrealNameSanitizer.RestoreOriginalNames(cleanCompile.PackageBytes, sourceDirectory);
    var cleanIndex = UnrealPackageIndex.Parse(cleanPackage);
    if (cleanIndex.Names.Contains(marker, StringComparer.Ordinal))
        throw new InvalidDataException("Clean Berserker package unexpectedly contains the test marker.");
    var cleanReopened = ProtectionPipelinePlanner.InspectFullPipeline(cleanPackage, inputPath);
    if (!cleanReopened.IsPlaintextCandidate || !cleanReopened.DecodedPayload.AsSpan().SequenceEqual(cleanPackage))
        throw new InvalidDataException("Clean rebuilt Interface.u did not reopen correctly.");
    string cleanOutputPath = Path.Combine(Path.GetDirectoryName(outputPath)!, "Interface-clean.u");
    File.WriteAllBytes(cleanOutputPath, cleanPackage);

    // Then compile a separate copy with a deliberate edit to prove changes survive rebuilding.
    string editedSourceDirectory = Path.Combine(outputRoot, "EditedSources");
    Directory.CreateDirectory(editedSourceDirectory);
    foreach (string file in Directory.GetFiles(sourceDirectory, "*.uc", SearchOption.TopDirectoryOnly))
        File.Copy(file, Path.Combine(editedSourceDirectory, Path.GetFileName(file)), true);
    string tooltip = Path.Combine(editedSourceDirectory, "ToolTip.uc");
    if (!File.Exists(tooltip)) throw new FileNotFoundException("ToolTip.uc missing after export.", tooltip);
    string tooltipText = File.ReadAllText(tooltip);
    int declarationEnd = tooltipText.IndexOf(';');
    if (declarationEnd < 0 || !tooltipText.TrimStart().StartsWith("class ", StringComparison.OrdinalIgnoreCase))
        throw new InvalidDataException("ToolTip.uc has no valid class declaration.");
    tooltipText = tooltipText.Insert(declarationEnd + 1, Environment.NewLine + "var string " + marker + ";" + Environment.NewLine);
    File.WriteAllText(tooltip, tooltipText, new UTF8Encoding(false));

    UccCompileResult editedCompile = UccCompiler.CompileInterface(runtimeRoot, editedSourceDirectory);
    byte[] editedPackage = UnrealNameSanitizer.RestoreOriginalNames(editedCompile.PackageBytes, editedSourceDirectory);
    if (!UnrealPackageIndex.Parse(editedPackage).Names.Contains(marker, StringComparer.Ordinal))
        throw new InvalidDataException("The edit marker was not found in the compiled test U package.");
    var editedReopened = ProtectionPipelinePlanner.InspectFullPipeline(editedPackage, inputPath);
    if (!editedReopened.IsPlaintextCandidate || !editedReopened.DecodedPayload.AsSpan().SequenceEqual(editedPackage) ||
        !UnrealPackageIndex.Parse(editedReopened.DecodedPayload).Names.Contains(marker, StringComparer.Ordinal))
        throw new InvalidDataException("The modified test U package did not reopen with its edit intact.");
    File.WriteAllBytes(outputPath, editedPackage);
    File.WriteAllText(Path.Combine(outputRoot, "final-validation.txt"),
        $"PASS: Berserker clean build and separate edit round-trip; classes={decompiled.ExportedCount}; nativeEntries={natives.Count}; unresolvedNativeCalls={unresolved}; cleanBytes={cleanPackage.Length}; editedBytes={editedPackage.Length}; markerPersisted=true{Environment.NewLine}");
    Console.WriteLine($"PASS: Berserker final U validation; classes={decompiled.ExportedCount}; nativeEntries={natives.Count}; unresolved={unresolved}; cleanBuild=true; editPersisted=true; cleanBytes={cleanPackage.Length}; editedBytes={editedPackage.Length}");
}
finally
{
    try { if (File.Exists(tempPackage)) File.Delete(tempPackage); } catch { }
}
