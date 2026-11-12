using System.Text;
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
    IReadOnlyList<string> Errors);

/// <summary>
/// Exports UnrealScript classes from a real UE1/UE2 package using the same
/// UELib reader validated against the Interlude Interface.u reference.
/// </summary>
public static class UnrealPackageDecompiler
{
    public static UnrealPackageDecompileResult DecompileToFolder(
        string packagePath, string outputDirectory)
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
            package.Summary.Version,
            package.Summary.LicenseeVersion,
            checked((int)package.Objects.Count),
            classes.Count,
            exported,
            errors.Count,
            errors);
    }
}
