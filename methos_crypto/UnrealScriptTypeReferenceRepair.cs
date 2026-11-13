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


        // UELib emits numeric switch labels for enum locals. Interlude UCC
        // requires labels to carry the same enum type as the switched value.
        foreach (System.Text.RegularExpressions.Match declaration in
                 System.Text.RegularExpressions.Regex.Matches(
                     source, @"\\blocal\\s+(?:[A-Za-z_][A-Za-z0-9_]*\\.)*(E[A-Za-z0-9_]+)\\s+([A-Za-z_][A-Za-z0-9_]*)\\s*;"))
        {
            string enumType = declaration.Groups[1].Value;
            string variable = declaration.Groups[2].Value;
            string switchPattern = @"\\bswitch\\s*\\(\\s*" +
                System.Text.RegularExpressions.Regex.Escape(variable) + @"\\s*\\)\\s*\\{";
            var switchMatch = System.Text.RegularExpressions.Regex.Match(source, switchPattern);
            if (!switchMatch.Success) continue;
            int openBrace = source.IndexOf('{', switchMatch.Index + switchMatch.Length - 1);
            int depth = 0;
            int closeBrace = -1;
            bool inString = false, inLineComment = false, inBlockComment = false, escaped = false;
            for (int i = openBrace; i < source.Length; i++)
            {
                char ch = source[i], next = i + 1 < source.Length ? source[i + 1] : '\\0';
                if (inLineComment) { if (ch == '\\n') inLineComment = false; continue; }
                if (inBlockComment) { if (ch == '*' && next == '/') { inBlockComment = false; i++; } continue; }
                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (ch == '\\\\') escaped = true;
                    else if (ch == '"') inString = false;
                    continue;
                }
                if (ch == '/' && next == '/') { inLineComment = true; i++; continue; }
                if (ch == '/' && next == '*') { inBlockComment = true; i++; continue; }
                if (ch == '"') { inString = true; continue; }
                if (ch == '{') depth++;
                else if (ch == '}' && --depth == 0) { closeBrace = i; break; }
            }
            if (closeBrace <= openBrace) continue;
            string switchBody = source.Substring(openBrace + 1, closeBrace - openBrace - 1);
            string repairedSwitch = System.Text.RegularExpressions.Regex.Replace(
                switchBody, @"(?m)(\\bcase\\s+)([0-9]+)(\\s*:)",
                "$1" + enumType + "($2)$3");
            if (!string.Equals(switchBody, repairedSwitch, StringComparison.Ordinal))
            {
                source = source.Remove(openBrace + 1, switchBody.Length)
                    .Insert(openBrace + 1, repairedSwitch);
                applied.Add($"{className}.{variable} numeric switch labels -> {enumType} casts");
            }
        }

        repairs = applied;
        return source;
    }
}
