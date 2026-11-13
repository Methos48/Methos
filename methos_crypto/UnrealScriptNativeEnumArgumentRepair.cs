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

    private static readonly IReadOnlyDictionary<int, string> DialogTypeNames =
        new Dictionary<int, string>
        {
            [0] = "DIALOG_OKCancel",
            [1] = "DIALOG_OK",
            [2] = "DIALOG_OKCancelInput",
            [3] = "DIALOG_OKInput",
            [4] = "DIALOG_Warning",
            [5] = "DIALOG_Notice",
            [6] = "DIALOG_NumberPad",
            [7] = "DIALOG_Progress",
            [8] = "DIALOG_Block",
            [9] = "DIALOG_Silent"
        };

    /// <summary>
    /// Restores the EDialogType identifier for numeric DialogShow calls made
    /// inside UICommonAPI itself. UCC requires the enum identifier for this
    /// same-class call even though the decompiler emits the underlying byte.
    /// </summary>
    public static string RepairKnownDialogTypeArguments(
        string className, string source, out IReadOnlyList<string> repairs)
    {
        ArgumentNullException.ThrowIfNull(className);
        ArgumentNullException.ThrowIfNull(source);
        var applied = new List<string>();
        if (!className.Equals("UICommonAPI", StringComparison.OrdinalIgnoreCase))
        {
            repairs = applied;
            return source;
        }

        string repaired = Regex.Replace(
            source,
            @"(?<![A-Za-z0-9_])DialogShow\s*\(\s*(?<value>\d+)\s*,",
            match =>
            {
                if (!int.TryParse(match.Groups["value"].Value, out int value) ||
                    !DialogTypeNames.TryGetValue(value, out string? enumName))
                    return match.Value;
                applied.Add($"{value} -> {enumName}");
                return Regex.Replace(match.Value, @"(?<=\()\s*\d+", enumName);
            },
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        repairs = applied;
        return repaired;
    }

    private static readonly IReadOnlyDictionary<int, string> DrawItemTypeNames =
        new Dictionary<int, string>
        {
            [0] = "DIT_BLANK",
            [1] = "DIT_TEXT",
            [2] = "DIT_TEXTURE"
        };

    /// <summary>
    /// Restores typed DrawItemInfo.eType enum assignments in UICommonAPI.
    /// The values are defined by UIEventManager.EDrawItemType in the
    /// verified Interlude native API source.
    /// </summary>
    public static string RepairKnownDrawItemTypeAssignments(
        string className, string source, out IReadOnlyList<string> repairs)
    {
        ArgumentNullException.ThrowIfNull(className);
        ArgumentNullException.ThrowIfNull(source);
        var applied = new List<string>();
        if (!className.Equals("UICommonAPI", StringComparison.OrdinalIgnoreCase))
        {
            repairs = applied;
            return source;
        }

        string repaired = Regex.Replace(
            source,
            @"(?<target>[A-Za-z_][A-Za-z0-9_]*(?:\s*\[[^\]]+\])?(?:\.[A-Za-z_][A-Za-z0-9_]*(?:\s*\[[^\]]+\])?)*)\.eType\s*=\s*(?<value>[0-2])\b",
            match =>
            {
                if (!int.TryParse(match.Groups["value"].Value, out int value) ||
                    !DrawItemTypeNames.TryGetValue(value, out string? enumName))
                    return match.Value;
                applied.Add($"{match.Groups["target"].Value}: {value} -> {enumName}");
                return $"{match.Groups["target"].Value} = {enumName}";
            },
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        repairs = applied;
        return repaired;
    }

    private static readonly IReadOnlyDictionary<int, string> DialogDefaultActionNames =
        new Dictionary<int, string>
        {
            [0] = "EDefaultNone",
            [1] = "EDefaultOK",
            [2] = "EDefaultCancel"
        };

    /// <summary>
    /// Restores UICommonAPI.DialogDefaultAction identifiers on calls into
    /// DialogBox, whose typed parameter cannot accept the decompiler's byte.
    /// </summary>
    public static string RepairKnownDialogDefaultActionArguments(
        string className, string source, out IReadOnlyList<string> repairs)
    {
        ArgumentNullException.ThrowIfNull(className);
        ArgumentNullException.ThrowIfNull(source);
        var applied = new List<string>();
        if (!className.Equals("UICommonAPI", StringComparison.OrdinalIgnoreCase))
        {
            repairs = applied;
            return source;
        }

        string repaired = Regex.Replace(
            source,
            @"(?<![A-Za-z0-9_])SetDefaultAction\\s*\\(\\s*(?<value>[0-2])\\s*\\)",
            match =>
            {
                if (!int.TryParse(match.Groups["value"].Value, out int value) ||
                    !DialogDefaultActionNames.TryGetValue(value, out string? enumName))
                    return match.Value;
                applied.Add($"{value} -> {enumName}");
                return Regex.Replace(match.Value, @"(?<=\\()\\s*\\d+", enumName);
            },
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        repairs = applied;
        return repaired;
    }

}
