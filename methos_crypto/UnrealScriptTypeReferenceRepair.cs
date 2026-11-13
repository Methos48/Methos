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


        if (className.Equals("DamageLiteAni", StringComparison.OrdinalIgnoreCase) &&
            source.Contains("function OnLoad()", StringComparison.Ordinal) &&
            source.Contains("function OnTimer(int RecoveredName_2125)", StringComparison.Ordinal))
        {
            var missingFields = new (string Type, string Name)[]
            {
                ("WindowHandle", "RecoveredName_2057"),
                ("TextBoxHandle", "RecoveredName_2070[29]"),
                ("string", "RecoveredName_2105"),
                ("string", "RecoveredName_2106"),
                ("string", "RecoveredName_2107"),
                ("string", "RecoveredName_2108"),
                ("int", "RecoveredName_2073"),
                ("int", "RecoveredName_2074"),
                ("int", "RecoveredName_2075"),
                ("int", "RecoveredName_2076"),
                ("int", "RecoveredName_2077"),
                ("int", "RecoveredName_2078"),
                ("int", "RecoveredName_2079"),
                ("int", "RecoveredName_2080"),
                ("int", "RecoveredName_2081"),
                ("int", "RecoveredName_2082"),
                ("int", "RecoveredName_2083"),
                ("int", "RecoveredName_2084"),
                ("int", "RecoveredName_2085"),
                ("int", "RecoveredName_2086"),
                ("int", "RecoveredName_2087"),
                ("int", "RecoveredName_2088"),
                ("int", "RecoveredName_2089"),
                ("int", "RecoveredName_2090"),
                ("int", "RecoveredName_2091"),
                ("int", "RecoveredName_2092"),
                ("int", "RecoveredName_2093"),
                ("int", "RecoveredName_2094"),
                ("int", "RecoveredName_2095"),
                ("int", "RecoveredName_2096"),
                ("int", "RecoveredName_2097"),
                ("int", "RecoveredName_2098"),
                ("int", "RecoveredName_2099"),
                ("int", "RecoveredName_2100"),
                ("int", "RecoveredName_2101"),
                ("int", "RecoveredName_2102"),
                ("int", "RecoveredName_2103"),
                ("int", "RecoveredName_2104")
            };
            var declarations = new List<string>();
            foreach (var field in missingFields)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(
                    source, @"(?m)^\s*var\s+(?:private\s+)?" +
                    System.Text.RegularExpressions.Regex.Escape(field.Type) + @"\s+" +
                    System.Text.RegularExpressions.Regex.Escape(field.Name) + @"\s*;"))
                    declarations.Add("var private " + field.Type + " " + field.Name + ";");
            }
            if (declarations.Count > 0)
            {
                const string classAnchor = "class DamageLiteAni extends UICommonAPI;";
                source = source.Replace(classAnchor,
                    classAnchor + Environment.NewLine + string.Join(Environment.NewLine, declarations),
                    StringComparison.Ordinal);
                applied.Add("DamageLiteAni restored " + declarations.Count + " missing typed state fields");
            }
        }

        if (className.Equals("DamageLite", StringComparison.OrdinalIgnoreCase) &&
            source.Contains("ManaBurnPending", StringComparison.Ordinal) &&
            source.Contains("ManaBurnTargetName", StringComparison.Ordinal))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(source, @"(?m)^\s*var\s+bool\s+ManaBurnPending\s*;"))
            {
                source = source.Replace("var bool Critical;",
                    "var bool Critical;" + Environment.NewLine + "var bool ManaBurnPending;",
                    StringComparison.Ordinal);
                applied.Add("DamageLite.ManaBurnPending bool field restored");
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(source, @"(?m)^\s*var\s+string\s+ManaBurnTargetName\s*;"))
            {
                source = source.Replace("var WindowHandle OnMsgWnd9;",
                    "var WindowHandle OnMsgWnd9;" + Environment.NewLine + "var string ManaBurnTargetName;",
                    StringComparison.Ordinal);
                applied.Add("DamageLite.ManaBurnTargetName string field restored");
            }
        }

        if (className.Equals("ChatWnd", StringComparison.OrdinalIgnoreCase) &&
            source.Contains("ChatFilterBtn.SetTooltipCustomType(SetTooltip(\"Chat Filter\\\\nMouse R-Click: Hide Chat\"));", StringComparison.Ordinal))
        {
            source = source.Replace(
                "ChatFilterBtn.SetTooltipCustomType(SetTooltip(\"Chat Filter\\\\nMouse R-Click: Hide Chat\"));",
                "ChatFilterBtn.SetTooltipCustomType(MakeTooltipSimpleText(\"Chat Filter\\\\nMouse R-Click: Hide Chat\"));",
                StringComparison.Ordinal);
            applied.Add("ChatWnd tooltip uses inherited MakeTooltipSimpleText helper");
        }

        if (className.Equals("ChatWnd", StringComparison.OrdinalIgnoreCase) &&

            source.Contains("ChatFilterBtn = ButtonHandle(", StringComparison.Ordinal) &&
            !System.Text.RegularExpressions.Regex.IsMatch(source, @"(?m)^\s*var\s+ButtonHandle\s+ChatFilterBtn\s*;"))
        {
            const string handleAnchor = "var EditBoxHandle ChatEditBox;";
            if (source.Contains(handleAnchor, StringComparison.Ordinal))
            {
                source = source.Replace(handleAnchor,
                    handleAnchor + Environment.NewLine + "var ButtonHandle ChatFilterBtn;",
                    StringComparison.Ordinal);
                applied.Add("ChatWnd.ChatFilterBtn missing ButtonHandle field restored");
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


        if (className.Equals("DialogBox", StringComparison.OrdinalIgnoreCase) &&
            source.Contains("function SetDefaultAction(UICommonAPI.DialogDefaultAction defaultAction)", StringComparison.Ordinal))
        {
            string repairedCalls = System.Text.RegularExpressions.Regex.Replace(
                source, @"\bSetDefaultAction\s*\(\s*([0-9]+)\s*\)",
                "SetDefaultAction(DialogDefaultAction($1))");
            if (!string.Equals(source, repairedCalls, StringComparison.Ordinal))
            {
                source = repairedCalls;
                applied.Add("DialogBox.SetDefaultAction numeric arguments -> DialogDefaultAction casts");
            }
        }

        if (className.Equals("DialogBox", StringComparison.OrdinalIgnoreCase) &&
            source.Contains("var UICommonAPI.DialogDefaultAction m_defaultAction;", StringComparison.Ordinal) &&
            source.Contains("switch(m_defaultAction)", StringComparison.Ordinal))
        {
            var switchMatch = System.Text.RegularExpressions.Regex.Match(source, @"\bswitch\s*\(\s*m_defaultAction\s*\)\s*\{");
            if (switchMatch.Success)
            {
                int openBrace = source.IndexOf('{', switchMatch.Index + switchMatch.Length - 1);
                int depth = 0, closeBrace = -1;
                for (int i = openBrace; i < source.Length; i++)
                {
                    if (source[i] == '{') depth++;
                    else if (source[i] == '}' && --depth == 0) { closeBrace = i; break; }
                }
                if (closeBrace > openBrace)
                {
                    string body = source.Substring(openBrace + 1, closeBrace - openBrace - 1);
                    string repaired = System.Text.RegularExpressions.Regex.Replace(body, @"(?m)(^[ \t]*case\s+)([0-9]+)(\s*:)", "$1DialogDefaultAction($2)$3");
                    if (!string.Equals(body, repaired, StringComparison.Ordinal))
                    {
                        source = source.Remove(openBrace + 1, body.Length).Insert(openBrace + 1, repaired);
                        applied.Add("DialogBox.m_defaultAction numeric switch labels -> DialogDefaultAction casts");
                    }
                }
            }
        }
        // UELib emits numeric switch labels for enum locals. Interlude UCC
        // requires labels to carry the same enum type as the switched value.
        foreach (System.Text.RegularExpressions.Match declaration in
                 System.Text.RegularExpressions.Regex.Matches(
                     source, @"\blocal\s+(?:[A-Za-z_][A-Za-z0-9_]*\.)*(E[A-Za-z0-9_]+)\s+([A-Za-z_][A-Za-z0-9_]*)\s*;"))
        {
            string enumType = declaration.Groups[1].Value;
            string variable = declaration.Groups[2].Value;
            string switchPattern = @"\bswitch\s*\(\s*" +
                System.Text.RegularExpressions.Regex.Escape(variable) + @"\s*\)\s*\{";
            var switchMatch = System.Text.RegularExpressions.Regex.Match(source, switchPattern);
            if (!switchMatch.Success) continue;
            int openBrace = source.IndexOf('{', switchMatch.Index + switchMatch.Length - 1);
            int depth = 0;
            int closeBrace = -1;
            bool inString = false, inLineComment = false, inBlockComment = false, escaped = false;
            for (int i = openBrace; i < source.Length; i++)
            {
                char ch = source[i], next = i + 1 < source.Length ? source[i + 1] : '\0';
                if (inLineComment) { if (ch == '\n') inLineComment = false; continue; }
                if (inBlockComment) { if (ch == '*' && next == '/') { inBlockComment = false; i++; } continue; }
                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (ch == '\\') escaped = true;
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
                switchBody, @"(?m)(^[ \t]*case\s+)([0-9]+)(\s*:)",
                "$1" + enumType + "($2)$3");
            if (!string.Equals(switchBody, repairedSwitch, StringComparison.Ordinal))
            {
                source = source.Remove(openBrace + 1, switchBody.Length)
                    .Insert(openBrace + 1, repairedSwitch);
                applied.Add($"{className}.{variable} numeric switch labels -> {enumType} casts");
            }
        }

        // UELib also emits numeric labels when the switched enum is a function parameter.
        foreach (System.Text.RegularExpressions.Match functionMatch in
                 System.Text.RegularExpressions.Regex.Matches(
                     source, @"\bfunction\b[^{;\r\n]*\((?<parameters>[^)]*)\)"))
        {
            string parameters = functionMatch.Groups["parameters"].Value;
            foreach (System.Text.RegularExpressions.Match parameter in
                     System.Text.RegularExpressions.Regex.Matches(
                         parameters, @"(?<enumType>(?:[A-Za-z_][A-Za-z0-9_]*\.)*E[A-Z][A-Za-z0-9_]*)\s+(?<variable>[A-Za-z_][A-Za-z0-9_]*)"))
            {
                string enumType = parameter.Groups["enumType"].Value;
                int enumSeparator = enumType.LastIndexOf('.');
                if (enumSeparator >= 0) enumType = enumType[(enumSeparator + 1)..];
                string variable = parameter.Groups["variable"].Value;
                var switchMatch = System.Text.RegularExpressions.Regex.Match(
                    source, @"\bswitch\s*\(\s*" +
                    System.Text.RegularExpressions.Regex.Escape(variable) + @"\s*\)\s*\{");
                if (!switchMatch.Success) continue;
                int openBrace = source.IndexOf('{', switchMatch.Index + switchMatch.Length - 1);
                int depth = 0, closeBrace = -1;
                bool inString = false, inLineComment = false, inBlockComment = false, escaped = false;
                for (int i = openBrace; i < source.Length; i++)
                {
                    char ch = source[i], next = i + 1 < source.Length ? source[i + 1] : '\0';
                    if (inLineComment) { if (ch == '\n') inLineComment = false; continue; }
                    if (inBlockComment) { if (ch == '*' && next == '/') { inBlockComment = false; i++; } continue; }
                    if (inString)
                    {
                        if (escaped) escaped = false;
                        else if (ch == '\\') escaped = true;
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
                string body = source.Substring(openBrace + 1, closeBrace - openBrace - 1);
                string repaired = System.Text.RegularExpressions.Regex.Replace(
                    body, @"(?m)(^[ \t]*case\s+)([0-9]+)(\s*:)",
                    "$1" + enumType + "($2)$3");
                if (!string.Equals(body, repaired, StringComparison.Ordinal))
                {
                    source = source.Remove(openBrace + 1, body.Length).Insert(openBrace + 1, repaired);
                    applied.Add($"{className}.{variable} numeric parameter-enum switch labels -> {enumType} casts");
                }
            }
        }

        if (className.Equals("Interface_by_SILENT", StringComparison.OrdinalIgnoreCase) &&
            source.Contains("SetTextColor(\"OKTUBRE INTERFACE\", RecoveredName_98(\"white\"))", StringComparison.Ordinal))
        {
            source = source.Replace(
                "local OptionWnd script;",
                "local OptionWnd script;" + Environment.NewLine + "local Color whiteColor;",
                StringComparison.Ordinal);
            source = source.Replace(
                "Class'NWindow.UIAPI_TEXTBOX'.static.SetTextColor(\"OKTUBRE INTERFACE\", RecoveredName_98(\"white\"));",
                "whiteColor.R = 255;" + Environment.NewLine +
                "whiteColor.G = 255;" + Environment.NewLine +
                "whiteColor.B = 255;" + Environment.NewLine +
                "whiteColor.A = 255;" + Environment.NewLine +
                "Class'NWindow.UIAPI_TEXTBOX'.static.SetTextColor(\"OKTUBRE INTERFACE\", whiteColor);",
                StringComparison.Ordinal);
            applied.Add("Interface_by_SILENT white text color restored as explicit RGBA Color");
        }

        if (className.Equals("OptionWnd", StringComparison.OrdinalIgnoreCase) &&
            source.Contains("bLoginInitialized", StringComparison.Ordinal) &&
            !System.Text.RegularExpressions.Regex.IsMatch(source, @"(?m)^\\s*var\\s+bool\\s+bLoginInitialized\\s*;"))
        {
            const string fieldAnchor = "var bool Log;";
            if (source.Contains(fieldAnchor, StringComparison.Ordinal))
            {
                source = source.Replace(fieldAnchor,
                    fieldAnchor + Environment.NewLine + "var bool bLoginInitialized;",
                    StringComparison.Ordinal);
                applied.Add("OptionWnd.bLoginInitialized bool state field restored");
            }
        }

        if (className.Equals("InventoryWnd", StringComparison.OrdinalIgnoreCase) &&
            source.Contains("PetInvenWnd", StringComparison.Ordinal) &&
            source.Contains("Info.DragSrcName", StringComparison.Ordinal))
        {
            // UELib can emit unbalanced parentheses for this recovered drag-source predicate.
            const string correctedPredicate = "if (Info.DragSrcName != \"InventoryItem\" && Info.DragSrcName != \"QuestItem\" && Info.DragSrcName != InventoryItem_1 && InStr(Info.DragSrcName, \"EquipItem\") == -1 && Info.DragSrcName != \"PetInvenWnd\")";
            string normalizedPredicate = System.Text.RegularExpressions.Regex.Replace(
                source,
                @"(?m)^(?<indent>[ \t]*)if\s*\([^;\r\n]*Info\.DragSrcName[^;\r\n]*PetInvenWnd[^;\r\n]*\)\s*$",
                "${indent}" + correctedPredicate,
                System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            if (!string.Equals(source, normalizedPredicate, StringComparison.Ordinal))
            {
                source = normalizedPredicate;
                applied.Add("InventoryWnd.OnDropItemSource malformed drag-source predicate normalized");
            }

            const string unresolvedDragHelper = "if(!isDragSrcInventory(Info.DragSrcName))";
            if (source.Contains(unresolvedDragHelper, StringComparison.Ordinal))
            {
                source = source.Replace(unresolvedDragHelper, correctedPredicate, StringComparison.Ordinal);
                applied.Add("InventoryWnd missing isDragSrcInventory helper inlined with equivalent source-name filter");
            }

            const string unresolvedWindowHandleHelper = "getItemWindowHandleBystrTarget(strTarget)";
            if (source.Contains(unresolvedWindowHandleHelper, StringComparison.Ordinal))
            {
                source = source.Replace(unresolvedWindowHandleHelper,
                    "ItemWindowHandle(GetHandle(strTarget))", StringComparison.Ordinal);
                applied.Add("InventoryWnd missing target-handle helper replaced with typed GetHandle lookup");
            }

            // This branch belongs to High Five bracelet/talisman UI, not the Interlude client.
            // Remove the recovered, unresolved callback as a unit; the normal Interlude equipment
            // slots and all supported item-update logic below remain intact.
            int braceletCondition = source.IndexOf(
                "if(!Class'NWindow.UIAPI_CHECKBOX'.static.IsChecked(", StringComparison.Ordinal);
            if (braceletCondition >= 0 &&
                source.Contains("RecoveredName_1266(RecoveredName_2883);", StringComparison.Ordinal))
            {
                int openBrace = source.IndexOf('{', braceletCondition);
                int depth = 0;
                int closeBrace = -1;
                for (int i = openBrace; i >= 0 && i < source.Length; i++)
                {
                    if (source[i] == '{') depth++;
                    else if (source[i] == '}' && --depth == 0)
                    {
                        closeBrace = i;
                        break;
                    }
                }
                if (closeBrace > openBrace)
                {
                    int lineStart = source.LastIndexOf('\n', braceletCondition);
                    lineStart = lineStart < 0 ? 0 : lineStart + 1;
                    source = source.Remove(lineStart, closeBrace + 1 - lineStart);
                    applied.Add("InventoryWnd removed unsupported High Five bracelet/talisman callback for Interlude");
                }
            }

            var missingFields = new (string Type, string Name)[]
            {
                ("ItemWindowHandle", "RecoveredName_2802"),
                ("ItemWindowHandle", "RecoveredName_2803"),
                ("ItemWindowHandle", "RecoveredName_2804"),
                ("string", "InventoryItem_1"),
                ("ButtonHandle", "TrashButton"),
                ("EditBoxHandle", "m_SearchBar")
            };
            var declarations = new List<string>();
            foreach (var field in missingFields)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(
                    source, @"(?m)^\s*var\s+(?:private\s+)?" +
                    System.Text.RegularExpressions.Regex.Escape(field.Type) + @"\s+" +
                    System.Text.RegularExpressions.Regex.Escape(field.Name) + @"\s*;"))
                    declarations.Add("var " + field.Type + " " + field.Name + ";");
            }
            if (declarations.Count > 0)
            {
                const string classAnchor = "class InventoryWnd extends UICommonAPI;";
                source = source.Replace(classAnchor,
                    classAnchor + Environment.NewLine + string.Join(Environment.NewLine, declarations),
                    StringComparison.Ordinal);
                applied.Add("InventoryWnd restored " + declarations.Count + " missing typed handle/state fields");
            }
        }

        if (className.Equals("InventoryWnd", StringComparison.OrdinalIgnoreCase) &&
            source.Contains("function int EquipItemGetItemNum()", StringComparison.Ordinal) &&
            source.Contains("RecoveredName_2802.GetItemNum()", StringComparison.Ordinal) &&
            source.Contains("RecoveredName_2803.GetItemNum()", StringComparison.Ordinal))
        {
            const string malformed = "Count = (RecoveredName_2802.GetItemNum() + RecoveredName_2803.GetItemNum()) + (EquipItemGetItemNum());";
            const string partiallyRepaired = "Count = (RecoveredName_2802.GetItemNum() + RecoveredName_2803.GetItemNum()) + EquipItemGetItemNum();";
            string corrected = "Count = RecoveredName_2802.GetItemNum();" + Environment.NewLine +
                "\tCount = Count + RecoveredName_2803.GetItemNum();" + Environment.NewLine +
                "\tCount = Count + EquipItemGetItemNum();";
            if (source.Contains(malformed, StringComparison.Ordinal) ||
                source.Contains(partiallyRepaired, StringComparison.Ordinal))
            {
                source = source.Replace(malformed, corrected, StringComparison.Ordinal)
                    .Replace(partiallyRepaired, corrected, StringComparison.Ordinal);
                applied.Add("InventoryWnd.SetItemCount split nested handle calls into sequential assignments");
            }
        }

        repairs = applied;
        return source;
    }
}
