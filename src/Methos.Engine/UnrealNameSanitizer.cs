using System.Text;
using System.Text.Json;
using UELib;

namespace Methos.Engine;

/// <summary>
/// Replaces invalid Unreal FNames with temporary source-safe identifiers and
/// restores the original FNames in the compiled package before it is published.
/// </summary>
public static class UnrealNameSanitizer
{
    public const string MapFileName = "methos_name_map.json";

    public static IReadOnlyList<UnrealNameReplacement> SanitizePackageNames(UnrealPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        var replacements = new List<UnrealNameReplacement>();
        var usedNames = new HashSet<string>(
            package.Names.Select(item => item.Name ?? string.Empty),
            StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < package.Names.Count; index++)
        {
            string originalName = package.Names[index].Name ?? string.Empty;
            if (IsValidIdentifier(originalName))
                continue;

            string safeName = $"MethosRecoveredName_{index}";
            while (!usedNames.Add(safeName))
                safeName += "_";

            replacements.Add(new UnrealNameReplacement(index, safeName, originalName));
            package.Names[index].Name = safeName;
        }

        return replacements;
    }

    public static void SaveNameMap(
        string outputDirectory,
        IReadOnlyList<UnrealNameReplacement> replacements)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentNullException.ThrowIfNull(replacements);
        Directory.CreateDirectory(outputDirectory);
        string path = Path.Combine(outputDirectory, MapFileName);
        string json = JsonSerializer.Serialize(replacements, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json, new UTF8Encoding(false));
    }

    public static byte[] RestoreOriginalNames(byte[] compiledPackage, string sourceDirectory)
    {
        ArgumentNullException.ThrowIfNull(compiledPackage);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);

        string mapPath = Path.Combine(sourceDirectory, MapFileName);
        if (!File.Exists(mapPath))
            return compiledPackage;

        List<UnrealNameReplacement> replacements;
        try
        {
            replacements = JsonSerializer.Deserialize<List<UnrealNameReplacement>>(
                File.ReadAllText(mapPath)) ?? throw new InvalidDataException("El mapa de nombres está vacío o no es válido.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("No se pudo leer el mapa reversible de nombres de Unreal.", exception);
        }

        if (replacements.Count == 0)
            return compiledPackage;

        if (replacements.Any(item =>
                string.IsNullOrWhiteSpace(item.SafeName) || item.OriginalName is null) ||
            replacements.Select(item => item.SafeName)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() != replacements.Count)
            throw new InvalidDataException("El mapa reversible de nombres contiene entradas inválidas o duplicadas.");

        var map = replacements.ToDictionary(
            item => item.SafeName,
            item => item.OriginalName,
            StringComparer.OrdinalIgnoreCase);

        byte[] restored = UnrealPackageIndex.Parse(compiledPackage).CreateWithReplacedNames(map);
        _ = UnrealPackageIndex.Parse(restored);
        return restored;
    }

    private static bool IsValidIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value) || !IsAsciiLetter(value[0]) && value[0] != '_')
            return false;

        for (int index = 1; index < value.Length; index++)
        {
            char character = value[index];
            if (!IsAsciiLetter(character) && (character < '0' || character > '9') && character != '_')
                return false;
        }

        return true;
    }

    private static bool IsAsciiLetter(char character) =>
        (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z');
}
