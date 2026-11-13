using System.Text;
using System.Text.RegularExpressions;
using UELib;
using UELib.Core;

namespace Methos.Engine;

public sealed record UnrealPackageDecompileResult(
    int PackageVersion,
    int LicenseeVersion,
    int ObjectCount,
    int ClassCount,
    int ExportedCount,
    int FailedCount,
    IReadOnlyList<string> Errors)
{
    public int SanitizedNameCount { get; init; }
}

public sealed record UnrealNameReplacement(int Index, string SafeName, string OriginalName);

/// <summary>
/// Exports UnrealScript classes from a real UE1/UE2 package using the same
/// UELib reader validated against the Interlude Interface.u reference.
/// </summary>
public static class UnrealPackageDecompiler
{
    public static UnrealPackageDecompileResult DecompileToFolder(
        string packagePath, string outputDirectory, string? referenceSourceDirectory = null, string? nativeRegistryPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        string input = Path.GetFullPath(packagePath);
        string output = Path.GetFullPath(outputDirectory);
        if (!File.Exists(input))
            throw new FileNotFoundException("No se encontró el paquete Unreal de entrada.", input);
        if (File.Exists(output))
            throw new IOException("La ruta de salida apunta a un archivo.");
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
            throw new IOException("La carpeta de salida no está vacía; elige una carpeta nueva para conservar los fuentes existentes.");

        Directory.CreateDirectory(output);
        using var package = UnrealLoader.LoadPackage(input, FileAccess.Read);

        // Keep invalid FNames reversible while giving UCC valid source identifiers.
        var replacements = UnrealNameSanitizer.SanitizePackageNames(package);
        UnrealNameSanitizer.SaveNameMap(output, replacements);

        package.InitializePackage();

        var classes = package.Objects.OfType<UClass>()
            .Where(item => (int)item > 0)
            .ToList();
        var errors = new List<string>();
        int exported = 0;

        foreach (var item in classes)
        {
            try
            {
                string source = item.Decompile();
                source = UnrealScriptSourceIndex.CommentDecompilerStackTraceLines(source);
                string objectName = item.Name?.ToString() ?? $"Object_{exported}";
                string safeName = string.Concat(objectName.Select(character =>
                    Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
                if (string.IsNullOrWhiteSpace(safeName))
                    safeName = $"Object_{exported}";
                // UELib can emit a redundant bare "return;" after a valid value return.
                // UCC rejects that statement in non-void functions. When the exact
                // Interlude reference source is available, remove only those invalid
                // trailing returns whose reference function confirms the real body.
                if (!string.IsNullOrWhiteSpace(referenceSourceDirectory))
                {
                    string referencePath = Path.Combine(
                        Path.GetFullPath(referenceSourceDirectory), safeName + ".uc");
                    if (File.Exists(referencePath))
                    {
                        string referenceSource = File.ReadAllText(referencePath);
                        // Restore only methods whose bytecode UELib represented with
                        // synthetic __NFUN_ calls or generated goto labels. Keep every
                        // successfully decompiled function (and its edits) untouched.
                        source = UnrealScriptFailedFunctionRepair.RestoreFailedFunctions(
                            source, referenceSource, out _);
                    }
                }

                // UELib may emit control-character names for protected identifiers and
                // leave nested scopes open after a switch formatting failure. Repair the
                // source copy before parsing or handing it to UCC.
                source = UnrealScriptSourceIndex.RepairInvalidControlCharacterIdentifiers(source);
                source = UnrealScriptSourceIndex.RepairMissingFunctionClosures(source).Source;

                // Normalize unreachable bare returns using tokenized function scope,
                // including returns following switch/default branches.
                source = UnrealScriptSourceIndex.RemoveRedundantTrailingReturns(source).Source;
                if (!string.IsNullOrWhiteSpace(nativeRegistryPath))
                    source = UnrealScriptNativeFunctionNormalizer.Normalize(source, nativeRegistryPath, out _, out _);
                // Apply narrowly fingerprinted recovered-source repairs in every decompile mode,
                // including package-only round-trip validation without a reference source folder.
                source = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
                    safeName, source, out _);
                source = UnrealScriptNativeEnumArgumentRepair.RepairKnownInterfaceSoundArguments(source, out _);
                        source = UnrealScriptNativeEnumArgumentRepair.RepairKnownDialogTypeArguments(safeName, source, out _);
                source = UnrealScriptNativeEnumArgumentRepair.RepairKnownDialogDefaultActionArguments(safeName, source, out _);
                source = UnrealScriptNativeEnumArgumentRepair.RepairKnownDrawItemTypeAssignments(safeName, source, out _);
                source = UnrealScriptKnownNativeOperatorRepair.RepairKnownSilentInterfaceAddressCheck(source, out _);
                source = UnrealScriptKnownNativeOperatorRepair.RepairKnownNativeEqualityOperators(source, out _);
                source = UnrealScriptKnownNativeOperatorRepair.RepairKnownNativeUnaryOperators(source, out _);

                string destination = Path.Combine(output, safeName + ".uc");
                File.WriteAllText(destination, source, new UTF8Encoding(false));
                exported++;
            }
            catch (Exception exception)
            {
                string objectName = item.Name?.ToString() ?? $"Object_{exported}";
                errors.Add($"{objectName}: {exception.GetType().Name}: {exception.Message}");
            }
        }

        RepairKnownDunk3lInheritedCalls(output, referenceSourceDirectory);

        return new UnrealPackageDecompileResult(
            checked((int)package.Summary.Version),
            checked((int)package.Summary.LicenseeVersion),
            checked((int)package.Objects.Count),
            classes.Count,
            exported,
            errors.Count,
            errors)
        {
            SanitizedNameCount = replacements.Count
        };
    }

    // Dunk3l's StatusWnd calls the inherited two-argument status-message helper
    // using its reference-kit name. In the protected package, the same helper
    // has an invalid control-character FName and is exported under its reversible
    // MethosRecoveredName identifier. Reconcile only when both exact signatures
    // are present; the name map restores the original FName after UCC compilation.
    private static void RepairKnownDunk3lInheritedCalls(string outputDirectory, string? referenceSourceDirectory)
    {
        if (string.IsNullOrWhiteSpace(referenceSourceDirectory))
            return;

        string childPath = Path.Combine(outputDirectory, "StatusWnd.uc");
        string parentPath = Path.Combine(outputDirectory, "UICommonAPI.uc");
        string referenceParentPath = Path.Combine(
            Path.GetFullPath(referenceSourceDirectory), "UICommonAPI.uc");
        if (!File.Exists(childPath) || !File.Exists(parentPath) || !File.Exists(referenceParentPath))
            return;

        string child = File.ReadAllText(childPath);
        string parent = File.ReadAllText(parentPath);
        string referenceParent = File.ReadAllText(referenceParentPath);

        const string referenceMethod = "RecoveredName_389";
        const string sanitizedMethod = "MethosRecoveredName_408";
        const string parameters = @"\s*\(\s*string\s+[A-Za-z_]\w*\s*,\s*Color\s+[A-Za-z_]\w*\s*\)";
        if (!Regex.IsMatch(referenceParent,
                @"\bfunction\s+" + referenceMethod + parameters,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) ||
            !Regex.IsMatch(parent,
                @"\bfunction\s+" + sanitizedMethod + parameters,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) ||
            !Regex.IsMatch(child,
                @"\b" + referenceMethod + @"\s*\(",
                RegexOptions.CultureInvariant))
            return;

        string repaired = Regex.Replace(child,
            @"\b" + referenceMethod + @"(?=\s*\()",
            sanitizedMethod,
            RegexOptions.CultureInvariant);
        if (!string.Equals(repaired, child, StringComparison.Ordinal))
            File.WriteAllText(childPath, repaired, new UTF8Encoding(false));
    }

}
