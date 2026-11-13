using System.Text.RegularExpressions;

namespace MethosUEngine;

/// <summary>
/// Restores native Interlude interface-sound enum identifiers when UELib
/// decompiles a typed PlayConsoleSound enum argument as an integer literal.
/// The mapping is taken from UIEventManager.EInterfaceSoundType in the
/// Interlude UCC reference package.
/// </summary>
public static class UnrealScriptNativeEnumArgumentRepair
{
    private static readonly IReadOnlyDictionary<int, string> InterfaceSoundNames =
        new Dictionary<int, string>
        {
            [0] = "IFST_CLICK1",
            [1] = "IFST_CLICK2",
            [2] = "IFST_CLICK_FAILED",
            [3] = "IFST_PICKUP",
            [4] = "IFST_TRASH_BASKET",
            [5] = "IFST_WINDOW_OPEN",
            [6] = "IFST_WINDOW_CLOSE",
            [7] = "IFST_QUEST_TUTORIAL",
            [8] = "IFST_MINIMAP_OPEN_CLOSE",
            [9] = "IFST_COOLTIME_END",
            [10] = "IFST_PETITION",
            [11] = "IFST_STATUSWND_OPEN",
            [12] = "IFST_STATUSWND_CLOSE",
            [13] = "IFST_INVENWND_OPEN",
            [14] = "IFST_INVENWND_CLOSE",
            [15] = "IFST_MAPWND_OPEN",
            [16] = "IFST_MAPWND_CLOSE",
            [17] = "IFST_SYSTEMWND_OPEN",
            [18] = "IFST_SYSTEMWND_CLOSE"
        };

    public static string RepairKnownInterfaceSoundArguments(
        string source, out IReadOnlyList<string> repairs)
    {
        ArgumentNullException.ThrowIfNull(source);
        var applied = new List<string>();

        string repaired = Regex.Replace(
            source,
            @"(?<![A-Za-z0-9_])PlayConsoleSound\s*\(\s*(?<value>\d+)\s*\)",
            match =>
            {
                if (!int.TryParse(match.Groups["value"].Value, out int value) ||
                    !InterfaceSoundNames.TryGetValue(value, out string? enumName))
                    return match.Value;

                applied.Add($"{value} -> {enumName}");
                return Regex.Replace(
                    match.Value,
                    @"\(\s*\d+\s*\)$",
                    $"({enumName})",
                    RegexOptions.CultureInvariant);
            },
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        repairs = applied;
        return repaired;
    }
}
