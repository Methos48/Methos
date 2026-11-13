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


        if (className.Equals("ChatWnd", StringComparison.OrdinalIgnoreCase) &&
            source.Contains("function UIEventManager.EChatType GetChatTypeByTabIndex", StringComparison.Ordinal) &&
            source.Contains("local UIEventManager.EChatType Type;", StringComparison.Ordinal))
        {
            var function = UnrealScriptSourceIndex.ParseFunctions(source).FirstOrDefault(item =>
                item.Name.Equals("GetChatTypeByTabIndex", StringComparison.OrdinalIgnoreCase));
            if (function is not null)
            {
                string repairedBody = System.Text.RegularExpressions.Regex.Replace(
                    function.Body,
                    @"(?m)(\bType\s*=\s*)([0-9]+)(\s*;)",
                    "$1EChatType($2)$3");
                if (!string.Equals(repairedBody, function.Body, StringComparison.Ordinal))
                {
                    source = source.Remove(function.BodyOffset, function.BodyLength)
                        .Insert(function.BodyOffset, repairedBody);
                    applied.Add("ChatWnd.GetChatTypeByTabIndex numeric assignments -> EChatType casts");
                }
            }
        }

        repairs = applied;
        return source;
    }
}
