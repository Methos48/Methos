using System.Text;
using System.Text.Json;
using UELib;
using UELib.Core;

namespace MethosUEngine;

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
        string packagePath, string outputDirectory, string? referenceSourceDirectory = null)
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

        // Some protected UE2 interfaces deliberately use control characters,
        // punctuation and empty FNames as UnrealScript identifiers. UELib can
        // read them, but UCC cannot compile those identifiers from source.
        // Replace only the in-memory names used for source generation; retain a
        // reversible map so the original FNames can be restored after UCC.
        var replacements = new List<UnrealNameReplacement>();
        var usedNames = new HashSet<string>(package.Names.Select(item => item.Name), StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < package.Names.Count; i++)
        {
            string originalName = package.Names[i].Name ?? string.Empty;
            if (IsValidIdentifier(originalName))
                continue;

            string safeName = $"MethosRecoveredName_{i}";
            while (!usedNames.Add(safeName))
                safeName += "_";
            replacements.Add(new UnrealNameReplacement(i, safeName, originalName));
            package.Names[i].Name = safeName;
        }

        File.WriteAllText(
            Path.Combine(output, "methos_name_map.json"),
            JsonSerializer.Serialize(replacements, new JsonSerializerOptions { WriteIndented = true }),
            new UTF8Encoding(false));

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
                        source = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
                            safeName, source, out _);
                    }
                }

                // Remove only an unreachable bare return appended after a value return
                // in non-void functions. This repair is safe without a reference file.
                source = UnrealScriptFailedFunctionRepair.RestoreInvalidTrailingReturns(
                    source, string.Empty, out _);
                source = UnrealScriptNativeEnumArgumentRepair.RepairKnownInterfaceSoundArguments(source, out _);
                        source = UnrealScriptNativeEnumArgumentRepair.RepairKnownDialogTypeArguments(safeName, source, out _);
                source = UnrealScriptNativeEnumArgumentRepair.RepairKnownDialogDefaultActionArguments(safeName, source, out _);
                source = UnrealScriptNativeEnumArgumentRepair.RepairKnownDrawItemTypeAssignments(safeName, source, out _);
                source = UnrealScriptKnownNativeOperatorRepair.RepairKnownSilentInterfaceAddressCheck(source, out _);

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

    private static bool IsValidIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value) || !IsAsciiLetter(value[0]) && value[0] != '_')
            return false;
        for (int i = 1; i < value.Length; i++)
        {
            char c = value[i];
            if (!IsAsciiLetter(c) && (c < '0' || c > '9') && c != '_')
                return false;
        }
        return true;
    }

    private static bool IsAsciiLetter(char c) =>
        (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
}
