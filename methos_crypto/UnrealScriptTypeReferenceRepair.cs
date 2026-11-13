namespace MethosUEngine;

/// <summary>
/// Corrects type references that UELib resolves to stale interface enum names.
/// Each correction is guarded by the matching field declaration in the same class,
/// so unrelated identifiers and user code remain untouched.
/// </summary>
public static class UnrealScriptTypeReferenceRepair
{
    public static string RepairKnownMismatchedEnumReferences(
        string className, string source, out IReadOnlyList<string> repairs)
    {
        ArgumentNullException.ThrowIfNull(className);
        ArgumentNullException.ThrowIfNull(source);
        var applied = new List<string>();

        if (className.Equals("DialogBox", StringComparison.OrdinalIgnoreCase))
        {
            if (source.Contains("var UICommonAPI.EDialogType m_type;", StringComparison.Ordinal) &&
                source.Contains("UICommonAPI.ESilentDialogType", StringComparison.Ordinal))
            {
                source = source.Replace(
                    "UICommonAPI.ESilentDialogType",
                    "UICommonAPI.EDialogType",
                    StringComparison.Ordinal);
                applied.Add("UICommonAPI.ESilentDialogType -> UICommonAPI.EDialogType");
            }

            if (source.Contains("var UICommonAPI.DialogDefaultAction m_defaultAction;", StringComparison.Ordinal) &&
                source.Contains("UICommonAPI.ESilentDialogDefaultAction", StringComparison.Ordinal))
            {
                source = source.Replace(
                    "UICommonAPI.ESilentDialogDefaultAction",
                    "UICommonAPI.DialogDefaultAction",
                    StringComparison.Ordinal);
                applied.Add("UICommonAPI.ESilentDialogDefaultAction -> UICommonAPI.DialogDefaultAction");
            }
        }

        repairs = applied;
        return source;
    }
}
