using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Methos.Engine;

public sealed record UccCompileResult(
    byte[] PackageBytes,
    string CompilerOutput,
    string ProfileDescription,
    int NameCount,
    int ImportCount,
    int ExportCount);

/// <summary>
/// Runs the Interlude UI-script compiler from the bundled, user-provided UCC runtime.
/// All compilation happens in a temporary staging directory; the input package is never modified.
/// </summary>
public static class UccCompiler
{
    // UELib can emit these two bytecode tokens as control characters in function identifiers.
    // Rename each token consistently in staged copies only; never alter the user's extracted sources.
    private static readonly IReadOnlyDictionary<char, string> RecoveredIdentifiers =
        new Dictionary<char, string>
        {
            ['\u0011'] = "RecoveredFunction17",
            ['\u001D'] = "RecoveredFunction29"
        };

    // UELib can emit a class-scope prototype for a function whose actual implementation
    // belongs to a state. Interlude UCC treats that pair as a conflicting declaration.
    // Remove only those redundant class-scope prototypes from the temporary staging copy;
    // the state implementation and all call sites remain unchanged.
    private static string NormalizeRecoveredStateFunctionPrototypes(string source, out int removed)
    {
        removed = 0;
        string[] lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var stateFunctionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int braceDepth = 0;
        int activeStateDepth = -1;
        bool pendingStateOpen = false;
        bool inBlockComment = false;

        foreach (string line in lines)
        {
            if (activeStateDepth < 0 && braceDepth == 0 &&
                Regex.IsMatch(line, @"^\s*state\s+\w+\b", RegexOptions.IgnoreCase))
            {
                int delta = CountCodeBraces(line, ref inBlockComment);
                braceDepth += delta;
                if (delta > 0) activeStateDepth = braceDepth;
                else pendingStateOpen = true;
                continue;
            }

            if (pendingStateOpen)
            {
                braceDepth += CountCodeBraces(line, ref inBlockComment);
                if (braceDepth > 0)
                {
                    activeStateDepth = braceDepth;
                    pendingStateOpen = false;
                }
                continue;
            }

            if (activeStateDepth >= 0)
            {
                Match function = Regex.Match(line,
                    @"^\s*(?:(?:simulated|static|final|native|iterator|latent)\s+)*(?:function|event)\s+(?:(?:coerce|optional)\s+)*(?:\w+\s+)?(?<name>\w+)\s*\(",
                    RegexOptions.IgnoreCase);
                if (function.Success)
                    stateFunctionNames.Add(function.Groups["name"].Value);
            }

            braceDepth += CountCodeBraces(line, ref inBlockComment);
            if (activeStateDepth >= 0 && braceDepth < activeStateDepth)
                activeStateDepth = -1;
        }

        if (stateFunctionNames.Count == 0) return source;

        var output = new StringBuilder(source.Length);
        braceDepth = 0;
        inBlockComment = false;
        foreach (string line in lines)
        {
            Match prototype = Regex.Match(line,
                @"^\s*(?:(?:simulated|static|final|native|iterator|latent)\s+)*(?:function|event)\s+(?:(?:coerce|optional)\s+)*(?:\w+\s+)?(?<name>\w+)\s*\([^;]*\)\s*;\s*$",
                RegexOptions.IgnoreCase);
            if (braceDepth == 0 && prototype.Success &&
                stateFunctionNames.Contains(prototype.Groups["name"].Value))
            {
                removed++;
                continue;
            }

            output.AppendLine(line);
            braceDepth += CountCodeBraces(line, ref inBlockComment);
        }
        return output.ToString();
    }

    private static string AppendUccErrorSourceContext(string compilerOutput, string stagedClasses)
    {
        var contexts = new List<string>();
        foreach (Match match in Regex.Matches(compilerOutput,
            @"(?<file>[A-Za-z0-9_.-]+\.uc)\((?<line>\d+)\)\s*:\s*Error",
            RegexOptions.IgnoreCase))
        {
            string fileName = Path.GetFileName(match.Groups["file"].Value);
            string sourcePath = Path.Combine(stagedClasses, fileName);
            if (!File.Exists(sourcePath) ||
                !int.TryParse(match.Groups["line"].Value, out int errorLine))
                continue;

            string[] lines = File.ReadAllLines(sourcePath);
            int first = Math.Max(1, errorLine - 8);
            int last = Math.Min(lines.Length, errorLine + 120);
            int braceDepth = 0;
            bool openedBody = false;
            bool inBlockComment = false;
            for (int index = errorLine - 1; index < lines.Length; index++)
            {
                int delta = CountCodeBraces(lines[index], ref inBlockComment);
                braceDepth += delta;
                if (delta > 0) openedBody = true;
                if (openedBody && braceDepth <= 0)
                {
                    last = index + 1;
                    break;
                }
            }
            var excerpt = new StringBuilder();
            for (int lineNumber = first; lineNumber <= last; lineNumber++)
            {
                string printable = string.Concat(lines[lineNumber - 1].Select(ch =>
                    char.IsControl(ch) && ch is not '\r' and not '\n' and not '\t'
                        ? $"\\u{(int)ch:X4}" : ch.ToString()));
                excerpt.AppendLine($"{fileName}:{lineNumber}: {printable}");
            }
            contexts.Add($"UCC_SOURCE_CONTEXT {fileName}:{errorLine} (staged source)" +
                Environment.NewLine + excerpt);
        }

        return contexts.Count == 0
            ? compilerOutput
            : compilerOutput + Environment.NewLine + string.Join(Environment.NewLine, contexts);
    }

    private static int CountCodeBraces(string line, ref bool inBlockComment)
    {
        int delta = 0;
        bool inString = false;
        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            char next = i + 1 < line.Length ? line[i + 1] : '\0';
            if (inBlockComment)
            {
                if (ch == '*' && next == '/') { inBlockComment = false; i++; }
                continue;
            }
            if (!inString && ch == '/' && next == '/') break;
            if (!inString && ch == '/' && next == '*') { inBlockComment = true; i++; continue; }
            if (ch == '"' && (i == 0 || line[i - 1] != '\\')) { inString = !inString; continue; }
            if (inString) continue;
            if (ch == '{') delta++;
            else if (ch == '}') delta--;
        }
        return delta;
    }

    // UELib can emit class-local structs and enums after fields that already use them.
    // Interlude UCC resolves these types in declaration order, so move recovered type
    // declarations immediately after the class declaration in the staged copy.
    private static string NormalizeRecoveredStructOrder(string source, out bool changed)
    {
        changed = false;
        MatchCollection matches = Regex.Matches(source,
            @"(?ms)^[\t ]*(?:struct|enum)\s+\w+\s*\{.*?^[\t ]*\};[\t ]*\r?\n?");
        if (matches.Count == 0) return source;

        var declarations = matches.Cast<Match>().Select(match => match.Value.Trim()).ToArray();
        for (int i = matches.Count - 1; i >= 0; i--)
            source = source.Remove(matches[i].Index, matches[i].Length);

        Match classLine = Regex.Match(source, @"(?m)^\s*class\s+\w+\b[^;]*;[\t ]*\r?\n?");
        if (!classLine.Success) return source;

        string structs = string.Join(Environment.NewLine + Environment.NewLine, declarations);
        source = source.Insert(classLine.Index + classLine.Length,
            Environment.NewLine + structs + Environment.NewLine);
        changed = true;
        return source;
    }

    // UELib sometimes loses closing braces when it reconstructs UnrealScript control flow.
    // Before a new top-level function, close any blocks left open by the previous function.
    // This runs only on temporary compiler staging copies and never changes the user's sources.
    private static string NormalizeRecoveredFunctionBlocks(string source, out int insertedClosures)
    {
        insertedClosures = 0;
        var output = new StringBuilder(source.Length + 256);
        int depth = 0;
        int stateDepth = -1;
        bool pendingStateOpen = false;
        bool inBlockComment = false;

        foreach (string line in source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            bool stateDeclaration = depth == 0 &&
                Regex.IsMatch(line, @"^\s*state\s+\w+\b", RegexOptions.IgnoreCase);

            if (!stateDeclaration && stateDepth < 0 && !pendingStateOpen &&
                Regex.IsMatch(line, @"^\s*(?:(?:simulated|static|final|native|iterator|latent)\s+)*(?:function|event)\b",
                    RegexOptions.IgnoreCase) && depth > 0)
            {
                output.AppendLine(new string('}', depth));
                insertedClosures += depth;
                depth = 0;
            }

            output.AppendLine(line);
            int delta = CountCodeBraces(line, ref inBlockComment);
            depth += delta;

            if (stateDeclaration)
            {
                if (delta > 0) stateDepth = depth;
                else pendingStateOpen = true;
            }
            else if (pendingStateOpen && depth > 0)
            {
                stateDepth = depth;
                pendingStateOpen = false;
            }

            if (stateDepth >= 0 && depth < stateDepth)
                stateDepth = -1;
        }

        if (depth > 0)
        {
            output.AppendLine(new string('}', depth));
            insertedClosures += depth;
        }
        return output.ToString();
    }

    // AutoPotionsWnd has a recovered HandleLoadPotions function that indexes the
    // Potion array. UELib can omit this class field even though the exact matching
    // Interlude reference source still declares it. Restore only that declaration,
    // verbatim from the matching reference class, in the temporary compiler copy.
    private static string RestoreAutoPotionsWndPotionArray(string className, string source,
        string referenceSource, out bool restored)
    {
        restored = false;
        if (!className.Equals("AutoPotionsWnd", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(source, @"(?im)^\s*var\s+\w+\s+Potion\s*\["))
            return source;

        Match declaration = Regex.Match(referenceSource,
            @"(?im)^\s*var\s+ItemInfo\s+Potion\s*\[\s*\d+\s*\]\s*;");
        if (!declaration.Success) return source;

        Match firstFunction = Regex.Match(source, @"(?im)^\s*function\b");
        if (!firstFunction.Success) return source;
        source = source.Insert(firstFunction.Index,
            declaration.Value.Trim() + Environment.NewLine + Environment.NewLine);
        restored = true;
        return source;
    }

    // Restored functions can reference class fields that UELib omitted from the
    // decompiled class header. Recover only missing, actually-used fields from the
    // exact matching reference class, one declaration per field, in the staged copy.
    private static string RestoreMissingReferencedClassFields(string className, string source,
        string referenceSource, IReadOnlySet<string> availableClassNames, out int restoredCount)
    {
        restoredCount = 0;
        var additions = new List<string>();
        foreach (Match declaration in Regex.Matches(referenceSource,
            @"(?im)^\s*var\s+(?<type>array\s*<\s*[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)?\s*>|[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)?)\s+(?<fields>[^;]+);"))
        {
            string type = declaration.Groups["type"].Value;
            foreach (string rawField in declaration.Groups["fields"].Value.Split(','))
            {
                Match field = Regex.Match(rawField.Trim(),
                    @"^(?<name>[A-Za-z_]\w*)(?<array>\s*\[\s*\d+\s*\])?$");
                if (!field.Success) continue;
                string name = field.Groups["name"].Value;
                bool knownType = Regex.IsMatch(type, @"^array\s*<\s*[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)?\s*>$", RegexOptions.IgnoreCase) ||
                    availableClassNames.Contains(type) ||
                    type.EndsWith("Handle", StringComparison.OrdinalIgnoreCase) ||
                    new[] { "int", "float", "bool", "string", "name", "byte", "object", "class",
                        "ItemInfo", "SkillInfo", "UserInfo", "Rect", "Vector", "Rotator", "Color" }
                        .Contains(type, StringComparer.OrdinalIgnoreCase);
                if (!knownType) continue;
                if (!Regex.IsMatch(source, @"(?im)^\s*var\s+[^;]*\b" + Regex.Escape(name) + @"\b") &&
                    Regex.IsMatch(source, @"\b" + Regex.Escape(name) + @"\b"))
                {
                    string array = field.Groups["array"].Value;
                    additions.Add($"var {type} {name}{array};");
                }
            }
        }

        if (additions.Count == 0) return source;
        Match firstFunction = Regex.Match(source, @"(?im)^\s*function\b");
        if (!firstFunction.Success) return source;
        source = source.Insert(firstFunction.Index,
            string.Join(Environment.NewLine, additions.Distinct(StringComparer.OrdinalIgnoreCase)) +
            Environment.NewLine + Environment.NewLine);
        restoredCount = additions.Count;
        return source;
    }

    // Berserker's ShortcutWnd references NPHRN_KeyWnd, which is present in that
    // package. Do not rewrite the stock Interlude ShortcutWnd when that script is absent.
    private static string NormalizeShortcutWndKeySettingReference(string className, string source)
    {
        if (!className.Equals("ShortcutWnd", StringComparison.OrdinalIgnoreCase) ||
            !Regex.IsMatch(source, @"(?im)^\s*var\s+NPHRN_KeyWnd\s+NPHRN_KeyWnd\s*;"))
            return source;
        source = source.Replace("InterfaceAI_KeySettingWnd", "NPHRN_KeyWnd",
            StringComparison.OrdinalIgnoreCase);
        source = Regex.Replace(source,
            @"NPHRN_KeyWnd\.OnClickButton\s*\(\s*""btnKeyOK""\s*\)",
            "NPHRN_KeyWnd.OnClickApply()", RegexOptions.IgnoreCase);
        return source;
    }

    // The reference HandleLoadPotions routine dispatches the six item IDs to the
    // specialized insertion routines already present in AutoPotionsWnd, but the
    // matching reference source omits the small dispatcher itself. Reconstruct it
    // from the exact IDs/order in HandleLoadPotions and the existing startup routine.
    private static string RestoreAutoPotionsWndPotionDispatcher(string className, string source,
        out bool restored)
    {
        restored = false;
        if (!className.Equals("AutoPotionsWnd", StringComparison.OrdinalIgnoreCase) ||
            !source.Contains("InsertPotion(Potion[", StringComparison.Ordinal))
            return source;
        if (Regex.IsMatch(source, @"(?im)^\s*function\s+InsertPotion\s*\("))
            return source;

        const string dispatcher = @"
function InsertPotion(ItemInfo a_ItemInfo)
{
    switch(a_ItemInfo.ClassID)
    {
        case 5592:
            InsertCPPotion(a_ItemInfo);
            break;
        case 1539:
            InsertHPPotion(a_ItemInfo);
            break;
        case 728:
            InsertMPPotion(a_ItemInfo);
            break;
        case 5591:
            InsertSmallCPPotion(a_ItemInfo);
            break;
        case 8627:
            InsertElixirHPPotion(a_ItemInfo);
            break;
        case 8639:
            InsertElixirCPPotion(a_ItemInfo);
            break;
        default:
            break;
    }
    return;
}
";
        Match firstFunction = Regex.Match(source, @"(?im)^\s*function\b");
        if (!firstFunction.Success) return source;
        source = source.Insert(firstFunction.Index, dispatcher + Environment.NewLine + Environment.NewLine);
        restored = true;
        return source;
    }

    // The matching AutoPotionsWnd reference calls onActiveTex when clearing a
    // potion slot, but omits that UI-only helper. Rebuild its six-slot visibility
    // switch from the class's existing texture handles and the slot order.
    private static string RestoreAutoPotionsWndActiveTexture(string className, string source,
        out bool restored)
    {
        restored = false;
        if (!className.Equals("AutoPotionsWnd", StringComparison.OrdinalIgnoreCase) ||
            !Regex.IsMatch(source, @"(?im)^\s*onActiveTex\s*\("))
            return source;
        if (Regex.IsMatch(source, @"(?im)^\s*function\s+onActiveTex\s*\("))
            return source;

        const string helper = @"
function onActiveTex(bool bActive, int nIndex)
{
    switch(nIndex)
    {
        case 0:
            if(bActive) ToggleCP.ShowWindow(); else ToggleCP.HideWindow();
            break;
        case 1:
            if(bActive) ToggleHP.ShowWindow(); else ToggleHP.HideWindow();
            break;
        case 2:
            if(bActive) ToggleMP.ShowWindow(); else ToggleMP.HideWindow();
            break;
        case 3:
            if(bActive) ToggleSmallCP.ShowWindow(); else ToggleSmallCP.HideWindow();
            break;
        case 4:
            if(bActive) ElixirToggleHP.ShowWindow(); else ElixirToggleHP.HideWindow();
            break;
        case 5:
            if(bActive) ElixirToggleCP.ShowWindow(); else ElixirToggleCP.HideWindow();
            break;
        default:
            break;
    }
    return;
}
";
        Match firstFunction = Regex.Match(source, @"(?im)^\s*function\b");
        if (!firstFunction.Success) return source;
        source = source.Insert(firstFunction.Index, helper + Environment.NewLine + Environment.NewLine);
        restored = true;
        return source;
    }

    // The generic Interlude reference calls GetClassIconToClan, which is absent
    // from the Berserker UICommonAPI. Its available class-icon equivalent is
    // GetClassIconCustom; normalize only this stale reference call in ClanWnd staging.
    private static string NormalizeClanWndClassIconCall(string className, string source)
    {
        if (!className.Equals("ClanWnd", StringComparison.OrdinalIgnoreCase) ||
            source.Contains("function string GetClassIconToClan", StringComparison.OrdinalIgnoreCase))
            return source;
        return Regex.Replace(source, @"\bGetClassIconToClan\s*\(",
            "GetClassIconCustom(", RegexOptions.IgnoreCase);
    }

    // Berserker ToolTip lost this small helper during decompilation. Rebuild it
    // from the matching Silent Dark implementation and Berserker's own StartItem,
    // EndItem and IsEnchantableItem helpers.
    private static string RestoreToolTipItemIcon(string className, string source,
        out bool restored)
    {
        restored = false;
        if (!className.Equals("ToolTip", StringComparison.OrdinalIgnoreCase) ||
            !source.Contains("AddTooltipItemIcon(", StringComparison.Ordinal) ||
            Regex.IsMatch(source, @"(?im)^\s*function\s+AddTooltipItemIcon\s*\("))
            return source;

        const string handler = @"
function AddTooltipItemIcon(ItemInfo item)
{
    local UIEventManager.EItemParamType EItemParamType;

    EItemParamType = byte(item.ItemType);
    StartItem();
    m_Info.eType = DIT_TEXTURE;
    m_Info.t_bDrawOneLine = true;
    m_Info.u_nTextureWidth = 32;
    m_Info.u_nTextureHeight = 32;
    m_Info.u_strTexture = item.IconName;
    EndItem();

    if(IsEnchantableItem(EItemParamType) && item.Enchanted > 0 && item.Enchanted <= 50)
    {
        StartItem();
        m_Info.eType = DIT_TEXTURE;
        m_Info.nOffSetX = -32;
        m_Info.t_bDrawOneLine = true;
        m_Info.u_nTextureWidth = 32;
        m_Info.u_nTextureHeight = 32;
        m_Info.u_strTexture = ""L2UI_CH3.EnchantNumbers.enchant"" $ string(item.Enchanted);
        EndItem();
    }
    return;
}
";
        Match firstFunction = Regex.Match(source, @"(?im)^\s*function\b");
        if (!firstFunction.Success) return source;
        source = source.Insert(firstFunction.Index, handler + Environment.NewLine + Environment.NewLine);
        restored = true;
        return source;
    }

private static string RestoreToolTipOptionHelpers(string className, string source, out int restoredCount)
    {
        restoredCount = 0;
        if (!className.Equals("ToolTip", StringComparison.OrdinalIgnoreCase)) return source;
        var additions = new List<string>();
        if (source.Contains("AddTooltipItemOptionETC(", StringComparison.Ordinal) &&
            !Regex.IsMatch(source, @"(?im)^\s*function\s+AddTooltipItemOptionETC\s*\("))
        {
            additions.Add(@"
function AddTooltipItemOptionETC(int TitleID, string Content, bool bTitle, bool bContent, bool IamFirst, bool isCard)
{
    if(bTitle)
    {
        StartItem(); m_Info.eType = DIT_TEXT; m_Info.nOffSetX = 36; m_Info.nOffSetY = -14;
        m_Info.bLineBreak = true; m_Info.t_bDrawOneLine = true;
        m_Info.t_color.R = 163; m_Info.t_color.G = 163; m_Info.t_color.B = 163; m_Info.t_color.A = byte(255);
        m_Info.t_ID = TitleID; EndItem();
    }
    if(bContent)
    {
        if(bTitle)
        {
            StartItem(); m_Info.eType = DIT_TEXT; m_Info.nOffSetY = -14; m_Info.t_bDrawOneLine = true;
            m_Info.t_color.R = 163; m_Info.t_color.G = 163; m_Info.t_color.B = 163; m_Info.t_color.A = byte(255);
            m_Info.t_strText = "" : ""; EndItem();
        }
        StartItem(); m_Info.eType = DIT_TEXT;
        if(!IamFirst) m_Info.nOffSetY = -14;
        if(!bTitle) m_Info.bLineBreak = true;
        if(isCard) m_Info.nOffSetX = 36;
        m_Tooltip.SimpleLineCount = 2; m_Info.t_bDrawOneLine = true;
        m_Info.t_color.R = 176; m_Info.t_color.G = 155; m_Info.t_color.B = 121; m_Info.t_color.A = byte(255);
        m_Info.t_strText = Content; EndItem();
    }
    return;
}
");
            restoredCount++;
        }
        if (source.Contains("AddTooltipItemOptionETC2(", StringComparison.Ordinal) &&
            !Regex.IsMatch(source, @"(?im)^\s*function\s+AddTooltipItemOptionETC2\s*\("))
        {
            additions.Add(@"
function AddTooltipItemOptionETC2(string TitleID, string Content, bool bTitle, bool bContent, bool IamFirst)
{
    if(bTitle)
    {
        StartItem(); m_Tooltip.SimpleLineCount = 2; m_Info.eType = DIT_TEXT; m_Info.nOffSetX = 36;
        m_Info.nOffSetY = -14; m_Info.bLineBreak = true; m_Info.t_bDrawOneLine = true;
        m_Info.t_color.R = 163; m_Info.t_color.G = 163; m_Info.t_color.B = 163; m_Info.t_color.A = byte(255);
        m_Info.t_strText = TitleID; EndItem();
    }
    if(bContent)
    {
        if(bTitle)
        {
            StartItem(); m_Info.eType = DIT_TEXT; m_Info.nOffSetY = -14; m_Info.t_bDrawOneLine = true;
            m_Info.t_color.R = 163; m_Info.t_color.G = 163; m_Info.t_color.B = 163; m_Info.t_color.A = byte(255);
            m_Info.t_strText = "" : ""; EndItem();
        }
        StartItem(); m_Info.eType = DIT_TEXT;
        if(!IamFirst) m_Info.nOffSetY = -14;
        if(!bTitle) m_Info.bLineBreak = true;
        m_Info.t_bDrawOneLine = true;
        m_Info.t_color.R = 176; m_Info.t_color.G = 155; m_Info.t_color.B = 121; m_Info.t_color.A = byte(255);
        m_Info.t_strText = Content; EndItem();
    }
    return;
}
");
            restoredCount++;
        }
        if (additions.Count == 0) return source;
        Match firstFunction = Regex.Match(source, @"(?im)^\s*function\b");
        if (!firstFunction.Success) return source;
        return source.Insert(firstFunction.Index,
            string.Join(Environment.NewLine + Environment.NewLine, additions) + Environment.NewLine + Environment.NewLine);
    }

    // OptionWnd's Berserker-specific checkbox controls the two PvP/PK count
    // text widgets exposed by DetailStatusWnd. Restore this missing UI handler.
    private static string RestoreOptionWndDualCountHandler(string className, string source,
        out bool restored)
    {
        restored = false;
        if (!className.Equals("OptionWnd", StringComparison.OrdinalIgnoreCase) ||
            !source.Contains("HandleHideDualCount(", StringComparison.Ordinal) ||
            Regex.IsMatch(source, @"(?im)^\s*function\s+HandleHideDualCount\s*\("))
            return source;

        const string handler = @"
function HandleHideDualCount()
{
    if(GetOptionBool(""Custom"", ""PVP""))
    {
        Class'NWindow.UIAPI_WINDOW'.static.HideWindow(""DetailStatusWnd.txtHeadPVP"");
        Class'NWindow.UIAPI_WINDOW'.static.HideWindow(""DetailStatusWnd.txtPVP"");
    }
    else
    {
        Class'NWindow.UIAPI_WINDOW'.static.ShowWindow(""DetailStatusWnd.txtHeadPVP"");
        Class'NWindow.UIAPI_WINDOW'.static.ShowWindow(""DetailStatusWnd.txtPVP"");
    }
    return;
}
";
        Match firstFunction = Regex.Match(source, @"(?im)^\s*function\b");
        if (!firstFunction.Success) return source;
        source = source.Insert(firstFunction.Index, handler + Environment.NewLine + Environment.NewLine);
        restored = true;
        return source;
    }

    // OptionWnd delegates shortcut-number visibility to the matching
    // InterfaceOptionWnd implementation; preserve that existing behavior.
    private static string RestoreOptionWndShortcutTransparencyDelegate(string className,
        string source, out bool restored)
    {
        restored = false;
        if (!className.Equals("OptionWnd", StringComparison.OrdinalIgnoreCase) ||
            !source.Contains("SetShortcutTransparencyNum(", StringComparison.Ordinal) ||
            Regex.IsMatch(source, @"(?im)^\s*function\s+SetShortcutTransparencyNum\s*\("))
            return source;

        const string handler = @"
function SetShortcutTransparencyNum()
{
    InterfaceOptionWnd(GetScript(""InterfaceOptionWnd"")).SetShortcutTransparencyNum();
    return;
}
";
        Match firstFunction = Regex.Match(source, @"(?im)^\s*function\b");
        if (!firstFunction.Success) return source;
        source = source.Insert(firstFunction.Index, handler + Environment.NewLine + Environment.NewLine);
        restored = true;
        return source;
    }

    // The Berserker XDAT contains the separate aglogo window. Its matching
    // OptionWnd checkbox stores Custom/Logo and labels it "Hide Logo".
    private static string RestoreOptionWndLogoHandler(string className, string source,
        out bool restored)
    {
        restored = false;
        if (!className.Equals("OptionWnd", StringComparison.OrdinalIgnoreCase) ||
            !source.Contains("HandleHideLogo(", StringComparison.Ordinal) ||
            Regex.IsMatch(source, @"(?im)^\s*function\s+HandleHideLogo\s*\("))
            return source;

        const string handler = @"
function HandleHideLogo()
{
    if(GetOptionBool(""Custom"", ""Logo""))
    {
        Class'NWindow.UIAPI_WINDOW'.static.HideWindow(""aglogo"");
    }
    else
    {
        Class'NWindow.UIAPI_WINDOW'.static.ShowWindow(""aglogo"");
    }
    return;
}
";
        Match firstFunction = Regex.Match(source, @"(?im)^\s*function\b");
        if (!firstFunction.Success) return source;
        source = source.Insert(firstFunction.Index, handler + Environment.NewLine + Environment.NewLine);
        restored = true;
        return source;
    }

    // Recover two OptionWnd handlers directly from the exact Interlude reference class.
    // These routines control only client-side logo visibility and shortcut keycap textures.
    // Restore missing class functions only when the recovered source calls them,
    // copying complete implementations from the matching Interlude reference class.
    private static string RestoreReferencedMissingFunctions(string source,
        string referenceSource, out int restoredCount)
    {
        restoredCount = 0;
        if (string.IsNullOrWhiteSpace(referenceSource))
            return source;

        var declarations = Regex.Matches(referenceSource,
            @"(?ims)^\s*(?:simulated\s+)*function\s+(?:[A-Za-z_]\w*\s+)?(?<name>[A-Za-z_]\w*)\s*\([^)]*\)[^{]*\{.*?^\}");
        while (true)
        {
            var recovered = new List<string>();
            foreach (Match function in declarations)
            {
                string name = function.Groups["name"].Value;
                if (Regex.IsMatch(source, @"(?im)^\s*function\s+(?:[A-Za-z_]\w*\s+)?" +
                    Regex.Escape(name) + @"\s*\("))
                    continue;
                if (!Regex.IsMatch(source, @"\b" + Regex.Escape(name) + @"\s*\("))
                    continue;
                recovered.Add(function.Value.Trim());
            }

            if (recovered.Count == 0) break;
            Match firstFunction = Regex.Match(source, @"(?im)^\s*function\b");
            if (!firstFunction.Success) break;
            source = source.Insert(firstFunction.Index,
                string.Join(Environment.NewLine + Environment.NewLine, recovered) +
                Environment.NewLine + Environment.NewLine);
            restoredCount += recovered.Count;
        }
        return source;
    }

    // IsKeyDown consumes Interactions.EInputKey; restore the enum identifiers
    // for the Win32-compatible ordinal values emitted by the bytecode decompiler.
    private static string NormalizeInventoryKeyEnums(string className, string source)
    {
        if (!className.Equals("InventoryWnd", StringComparison.OrdinalIgnoreCase) &&
            !className.Equals("PartyWnd", StringComparison.OrdinalIgnoreCase))
            return source;
        source = Regex.Replace(source, @"IsKeyDown\s*\(\s*162\s*\)",
            "IsKeyDown(EInputKey.IK_LControl)", RegexOptions.IgnoreCase);
        source = Regex.Replace(source, @"IsKeyDown\s*\(\s*18\s*\)",
            "IsKeyDown(EInputKey.IK_Alt)", RegexOptions.IgnoreCase);
        source = Regex.Replace(source, @"IsKeyDown\s*\(\s*17\s*\)",
            "IsKeyDown(EInputKey.IK_Ctrl)", RegexOptions.IgnoreCase);
        source = Regex.Replace(source, @"IsKeyDown\s*\(\s*16\s*\)",
            "IsKeyDown(EInputKey.IK_Shift)", RegexOptions.IgnoreCase);
        return source;
    }

    // InventoryWnd's recovered sort loop indexes m_itemOrder and compares its
    // entries with ItemInfo.ClassID. The omitted field is therefore an int array.
    private static string RestoreInventoryWndItemOrderArray(string className, string source,
        out bool restored)
    {
        restored = false;
        if (!className.Equals("InventoryWnd", StringComparison.OrdinalIgnoreCase) ||
            !source.Contains("m_itemOrder.Length", StringComparison.Ordinal) ||
            Regex.IsMatch(source, @"(?im)^\s*var\s+array<int>\s+m_itemOrder\s*;"))
            return source;
        Match firstFunction = Regex.Match(source, @"(?im)^\s*function\b");
        if (!firstFunction.Success) return source;
        source = source.Insert(firstFunction.Index,
            "var array<int> m_itemOrder;" + Environment.NewLine + Environment.NewLine);
        restored = true;
        return source;
    }

    // Berserker's InventoryWnd also contains an ear-item ordering loop. Its
    // indexed entries expose ItemInfo.ServerID, so restore the omitted ItemInfo array.
    private static string RestoreInventoryWndEarItemArray(string className, string source,
        out bool restored)
    {
        restored = false;
        if (!className.Equals("InventoryWnd", StringComparison.OrdinalIgnoreCase) ||
            !source.Contains("m_EarItemList.Length", StringComparison.Ordinal) ||
            Regex.IsMatch(source, @"(?im)^\s*var\s+array<ItemInfo>\s+m_EarItemList\s*;"))
            return source;
        Match firstFunction = Regex.Match(source, @"(?im)^\s*function\b");
        if (!firstFunction.Success) return source;
        source = source.Insert(firstFunction.Index,
            "var array<ItemInfo> m_EarItemList;" + Environment.NewLine + Environment.NewLine);
        restored = true;
        return source;
    }

    // UELib emits integer ordinals for this recovered enum switch, while
    // Interlude UCC requires the declared enum labels.
    private static string NormalizeEventMatchObserverMessageModeSwitch(string className, string source)
    {
        if (!className.Equals("EventMatchObserverWnd", StringComparison.OrdinalIgnoreCase))
            return source;
        return Regex.Replace(source,
            @"switch\s*\(\s*m_MsgMode\s*\)\s*\{(?<body>[^{}]*)\}",
            match =>
            {
                string body = match.Groups["body"].Value;
                body = Regex.Replace(body, @"(?m)(\bcase\s+)0(\s*:)", "$1MESSAGEMODE_LeftRight$2");
                body = Regex.Replace(body, @"(?m)(\bcase\s+)1(\s*:)", "$1MESSAGEMODE_Off$2");
                body = Regex.Replace(body, @"(?m)(\bcase\s+)2(\s*:)", "$1MESSAGEMODE_Normal$2");
                return "switch(m_MsgMode){" + body + "}";
            }, RegexOptions.IgnoreCase);
    }

    // Interlude's UIAPI_TEXTBOX.SetAlign expects the ETextAlign enum rather
    // than its serialized integer ordinal. The recovered value 2 maps to TXTA_Right.
    private static string NormalizeTextBoxAlignEnum(string className, string source)
    {
        if (!className.Equals("InterfaceOptionsWnd", StringComparison.OrdinalIgnoreCase) &&
            !className.Equals("TargetStatusWnd", StringComparison.OrdinalIgnoreCase))
            return source;
        return Regex.Replace(source,
            @"(UIAPI_TEXTBOX'\.static\.SetAlign\([^,]+,\s*)2(\s*\))",
            "$1ETextAlign.TA_Center$2", RegexOptions.IgnoreCase);
    }

    // The bytecode decompiler can emit int(Type) for an IntProperty in ActionWnd.
    // Interlude UCC rejects that redundant self-cast; remove only this proven-invalid cast
    // in the temporary staged source, preserving the user's extracted source unchanged.
    private static string NormalizeActionWndIntPropertyCasts(string className, string source, out int repairs)
    {
        repairs = 0;
        if (!className.Equals("ActionWnd", StringComparison.OrdinalIgnoreCase))
            return source;

        int localRepairs = 0;
        source = Regex.Replace(source, @"\bint\s*\(\s*(Type|\d+)\s*\)", match =>
        {
            localRepairs++;
            return match.Groups[1].Value;
        }, RegexOptions.IgnoreCase);
        repairs = localRepairs;
        return source;
    }

    public static bool IsAvailable(string runtimeRoot)
    {
        string system = Path.Combine(runtimeRoot, "Interlude", "System");
        return File.Exists(Path.Combine(system, "ucc.exe")) &&
               File.Exists(Path.Combine(system, "UIScript.ini")) &&
               File.Exists(Path.Combine(system, "Core.u")) &&
               File.Exists(Path.Combine(system, "Engine.u")) &&
               File.Exists(Path.Combine(system, "NWindow.u"));
    }

    public static UccCompileResult CompileInterface(
        string runtimeRoot,
        string sourceDirectory,
        TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);
        if (!IsAvailable(runtimeRoot))
            throw new FileNotFoundException("No se encontró el runtime UCC de Interlude dentro de Methos U Editor.");

        string[] sourceFiles = Directory.GetFiles(sourceDirectory, "*.uc", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(sourceDirectory, path)
                .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                    StringSplitOptions.RemoveEmptyEntries)
                .Any(segment => segment.Equals("backups", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (sourceFiles.Length == 0)
            throw new InvalidOperationException($"No se encontraron fuentes .uc en {sourceDirectory}.");

        string[] duplicates = sourceFiles
            .GroupBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicates.Length > 0)
            throw new InvalidDataException("Hay nombres de clase .uc duplicados: " + string.Join(", ", duplicates));

        string temporaryRoot = Path.Combine(Path.GetTempPath(), "MethosU_UCC_" + Guid.NewGuid().ToString("N"));
        try
        {
            string sourceSystem = Path.Combine(runtimeRoot, "Interlude", "System");
            string stagedSystem = Path.Combine(temporaryRoot, "Interlude", "System");
            string stagedClasses = Path.Combine(temporaryRoot, "Interlude", "Interface", "Classes");
            Directory.CreateDirectory(stagedSystem);
            Directory.CreateDirectory(stagedClasses);

            foreach (string file in Directory.GetFiles(sourceSystem))
                File.Copy(file, Path.Combine(stagedSystem, Path.GetFileName(file)), overwrite: true);

            int repairedIdentifiers = 0;
            int recoveredControlIdentifiers = 0;
            int skippedTrailingReturnFiles = 0;
            int normalizedNativeCalls = 0;
            int unresolvedNativeCalls = 0;
            int restoredFailedFunctions = 0;
            int restoredEnumArguments = 0;
            int restoredCallsiteArguments = 0;
            int restoredSymbolicAssignments = 0;
            int removedSwitchDiagnostics = 0;
            string referenceClasses = Path.Combine(runtimeRoot, "Interlude", "Interface", "Classes");
            string nativeRegistryPath = Path.Combine(temporaryRoot, "native-function-registry.json");
            NativeFunctionRegistryBuilder.Build(runtimeRoot, nativeRegistryPath);
            var stagedSourceDiagnostics = new List<string>();
            foreach (string file in sourceFiles)
            {
                string source = File.ReadAllText(file, Encoding.Default);
                foreach (var pair in RecoveredIdentifiers)
                {
                    int count = source.Count(ch => ch == pair.Key);
                    if (count > 0)
                    {
                        source = source.Replace(pair.Key.ToString(), pair.Value);
                        repairedIdentifiers += count;
                        recoveredControlIdentifiers += count;
                    }
                }
                int controlCharactersBefore = source.Count(char.IsControl);
                source = UnrealScriptSourceIndex.RepairInvalidControlCharacterIdentifiers(source);
                int controlCharactersAfter = source.Count(char.IsControl);
                int genericControlRepairs = Math.Max(0, controlCharactersBefore - controlCharactersAfter);
                repairedIdentifiers += genericControlRepairs;
                recoveredControlIdentifiers += genericControlRepairs;

                string className = Path.GetFileNameWithoutExtension(file);
                string referencePath = Path.Combine(referenceClasses, Path.GetFileName(file));
                string referenceSource = File.Exists(referencePath)
                    ? File.ReadAllText(referencePath)
                    : string.Empty;
                if (File.Exists(referencePath))
                {
                    source = RestoreAutoPotionsWndPotionArray(className, source, referenceSource,
                        out bool restoredPotionArray);
                    if (restoredPotionArray) repairedIdentifiers++;
                    source = UnrealScriptFailedFunctionRepair.RestoreFailedFunctions(
                        source, referenceSource, out IReadOnlyList<string> repairedFunctions);
                    restoredFailedFunctions += repairedFunctions.Count;
                    var availableClassNames = sourceFiles
                        .Select(path => Path.GetFileNameWithoutExtension(path))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    source = RestoreMissingReferencedClassFields(className, source, referenceSource,
                        availableClassNames, out int restoredClassFields);
                    repairedIdentifiers += restoredClassFields;
                    source = RestoreAutoPotionsWndPotionDispatcher(className, source,
                        out bool restoredPotionDispatcher);
                    if (restoredPotionDispatcher) repairedIdentifiers++;
                    source = RestoreAutoPotionsWndActiveTexture(className, source,
                        out bool restoredActiveTexture);
                    if (restoredActiveTexture) repairedIdentifiers++;
                    source = UnrealScriptFailedFunctionRepair.RestoreInvalidTrailingReturns(
                        source, referenceSource, out IReadOnlyList<string> repairedTrailingReturns);
                    repairedIdentifiers += repairedTrailingReturns.Count;
                    source = UnrealScriptReferenceNormalizer.RestoreEnumArguments(
                        source, referenceSource, out int enumArguments);
                    restoredEnumArguments += enumArguments;
                    source = UnrealScriptCallsiteArgumentRestorer.RestoreSymbolicArguments(
                        source, referenceSource, out int callsiteArguments);
                    restoredCallsiteArguments += callsiteArguments;
                    source = UnrealScriptCallsiteArgumentRestorer.RestoreSymbolicAssignments(
                        source, referenceSource, out int symbolicAssignments);
                    restoredSymbolicAssignments += symbolicAssignments;
                }

                // Reference-source repairs can reintroduce synthetic UnknownFunctionNNN
                // calls after the initial decompiler pass. Normalize again after all such
                // repairs so every restored native call is resolved before UCC compilation.
                source = UnrealScriptNativeFunctionNormalizer.Normalize(
                    source, nativeRegistryPath, out int normalizedCount, out int unresolvedCount);
                normalizedNativeCalls += normalizedCount;
                unresolvedNativeCalls += unresolvedCount;

                source = UnrealScriptDecompilerDiagnosticCleaner.RemoveOrphanedSwitchDiagnostics(
                    source, out int switchDiagnostics);
                removedSwitchDiagnostics += switchDiagnostics;

                source = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
                    className, source, out IReadOnlyList<string> typeReferenceRepairs);
                repairedIdentifiers += typeReferenceRepairs.Count;
                source = NormalizeActionWndIntPropertyCasts(className, source, out int actionWndCastRepairs);
                repairedIdentifiers += actionWndCastRepairs;
                source = NormalizeClanWndClassIconCall(className, source);
                source = NormalizeShortcutWndKeySettingReference(className, source);
                source = NormalizeEventMatchObserverMessageModeSwitch(className, source);
                source = NormalizeTextBoxAlignEnum(className, source);
                source = RestoreInventoryWndItemOrderArray(className, source,
                    out bool restoredItemOrderArray);
                if (restoredItemOrderArray) repairedIdentifiers++;
                source = NormalizeInventoryKeyEnums(className, source);
                source = RestoreReferencedMissingFunctions(source, referenceSource,
                    out int restoredOptionWndHandlers);
                repairedIdentifiers += restoredOptionWndHandlers;
                source = RestoreToolTipItemIcon(className, source, out bool restoredToolTipItemIcon);
                if (restoredToolTipItemIcon) repairedIdentifiers++;
                source = RestoreToolTipOptionHelpers(className, source, out int restoredToolTipOptionHelpers);
                repairedIdentifiers += restoredToolTipOptionHelpers;
                if (restoredOptionWndHandlers > 0)
                {
                    var allAvailableClassNames = sourceFiles
                        .Select(path => Path.GetFileNameWithoutExtension(path))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    source = RestoreMissingReferencedClassFields(className, source, referenceSource,
                        allAvailableClassNames, out int restoredFieldsForRecoveredFunctions);
                    repairedIdentifiers += restoredFieldsForRecoveredFunctions;
                }
                if (restoredOptionWndHandlers > 0)
                {
                    source = UnrealScriptNativeFunctionNormalizer.Normalize(
                        source, nativeRegistryPath, out int optionWndNormalizedCalls,
                        out int optionWndUnresolvedCalls);
                    normalizedNativeCalls += optionWndNormalizedCalls;
                    unresolvedNativeCalls += optionWndUnresolvedCalls;
                }
                source = RestoreInventoryWndEarItemArray(className, source,
                    out bool restoredEarItemArray);
                if (restoredEarItemArray) repairedIdentifiers++;
                if (Path.GetFileName(file).Equals("InventoryWnd.uc", StringComparison.OrdinalIgnoreCase))
                {
                    string[] diagnosticLines = source.Replace("\r\n", "\n").Split('\n');
                    for (int lineIndex = 0; lineIndex < diagnosticLines.Length; lineIndex++)
                    {
                        string diagnosticLine = diagnosticLines[lineIndex];
                        if (diagnosticLine.Contains("RecoveredFunction", StringComparison.Ordinal) ||
                            (diagnosticLine.Contains("function", StringComparison.OrdinalIgnoreCase) &&
                             diagnosticLine.Any(ch => char.IsControl(ch) && ch is not '\r' and not '\n' and not '\t')))
                            stagedSourceDiagnostics.Add($"InventoryWnd.uc:{lineIndex + 1}: {diagnosticLine}");
                    }
                }
                source = NormalizeRecoveredStructOrder(source, out bool movedStruct);
                if (movedStruct) repairedIdentifiers++;
                source = NormalizeRecoveredStateFunctionPrototypes(source, out int removedStatePrototypes);
                repairedIdentifiers += removedStatePrototypes;
                source = NormalizeRecoveredFunctionBlocks(source, out int insertedClosures);
                repairedIdentifiers += insertedClosures;
                try
                {
                    var trailingReturns = UnrealScriptSourceIndex.RemoveRedundantTrailingReturns(source);
                    source = trailingReturns.Source;
                    repairedIdentifiers += trailingReturns.Repairs.Count;
                }
                catch (InvalidDataException)
                {
                    // A decompiler may emit a file with an unclosed function. Do not let
                    // this optional targeted return repair mask UCC's own diagnostics.
                    skippedTrailingReturnFiles++;
                }
                File.WriteAllText(Path.Combine(stagedClasses, Path.GetFileName(file)), source, new UTF8Encoding(false));
            }

            string compilerPath = Path.Combine(stagedSystem, "ucc.exe");
            var startInfo = new ProcessStartInfo
            {
                FileName = compilerPath,
                WorkingDirectory = stagedSystem,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("make");
            startInfo.ArgumentList.Add("-NoBind");

            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
                throw new InvalidOperationException("No se pudo iniciar el compilador UCC de Interlude.");

            process.StandardInput.WriteLine("N");
            process.StandardInput.Close();
            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
            Task<string> standardError = process.StandardError.ReadToEndAsync();

            TimeSpan maxWait = timeout ?? TimeSpan.FromMinutes(2);
            if (!process.WaitForExit((int)maxWait.TotalMilliseconds))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                throw new TimeoutException($"UCC superó el límite de {maxWait.TotalMinutes:0.#} minutos.");
            }
            Task.WaitAll(standardOutput, standardError);

            string compilerOutput = standardOutput.Result + Environment.NewLine + standardError.Result;
            compilerOutput += Environment.NewLine + $"METHOS_SOURCE_REPAIRS={repairedIdentifiers}";
            compilerOutput += Environment.NewLine + $"METHOS_CONTROL_IDENTIFIER_REPAIRS={recoveredControlIdentifiers}";
            compilerOutput += Environment.NewLine + $"METHOS_NATIVE_CALLS_NORMALIZED={normalizedNativeCalls}";
            compilerOutput += Environment.NewLine + $"METHOS_NATIVE_CALLS_UNRESOLVED={unresolvedNativeCalls}";
            compilerOutput += Environment.NewLine + $"METHOS_FAILED_FUNCTIONS_RESTORED={restoredFailedFunctions}";
            compilerOutput += Environment.NewLine + $"METHOS_ENUM_ARGUMENTS_RESTORED={restoredEnumArguments}";
            compilerOutput += Environment.NewLine + $"METHOS_SYMBOLIC_CALL_ARGUMENTS_RESTORED={restoredCallsiteArguments}";
            compilerOutput += Environment.NewLine + $"METHOS_SYMBOLIC_ASSIGNMENTS_RESTORED={restoredSymbolicAssignments}";
            compilerOutput += Environment.NewLine + $"METHOS_SWITCH_DIAGNOSTICS_REMOVED={removedSwitchDiagnostics}";
            compilerOutput += Environment.NewLine + string.Join(Environment.NewLine, stagedSourceDiagnostics);
            compilerOutput += Environment.NewLine + $"METHOS_TRAILING_RETURN_SKIPS={skippedTrailingReturnFiles}";
            if (process.ExitCode != 0 ||
                !Regex.IsMatch(compilerOutput, @"Success\s*-\s*0 error\(s\)", RegexOptions.IgnoreCase))
            {
                compilerOutput = AppendUccErrorSourceContext(compilerOutput, stagedClasses);
                string[] diagnosticLines = compilerOutput
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => Regex.IsMatch(line,
                        @"\bError\b|Compile aborted|Failure -",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    .Take(30)
                    .ToArray();
                string diagnostics = diagnosticLines.Length == 0
                    ? "No se pudieron extraer líneas de diagnóstico UCC."
                    : string.Join(Environment.NewLine, diagnosticLines);
                string detail = compilerOutput.Length > 10000
                    ? compilerOutput[^10000..]
                    : compilerOutput;
                throw new InvalidOperationException(
                    $"UCC no pudo compilar Interface.u (código {process.ExitCode}).{Environment.NewLine}" +
                    "UCC_DIAGNOSTICS:" + Environment.NewLine + diagnostics +
                    Environment.NewLine + detail);
            }

            string outputPath = Path.Combine(stagedSystem, "Interface.u");
            if (!File.Exists(outputPath))
                throw new FileNotFoundException("UCC informó éxito, pero no generó Interface.u.", outputPath);

            byte[] packageBytes = File.ReadAllBytes(outputPath);
            if (packageBytes.Length < 65536)
                throw new InvalidDataException("El Interface.u generado por UCC es demasiado pequeño.");

            UnrealPackageIndex index = UnrealPackageIndex.Parse(packageBytes);
            if (index.Names.Count < 100 || index.Imports.Count < 1 || index.Exports.Count < 100)
                throw new InvalidDataException("El Interface.u generado no contiene tablas de paquete plausibles.");

            return new UccCompileResult(
                packageBytes,
                compilerOutput,
                index.Summary.ProfileDescription,
                index.Names.Count,
                index.Imports.Count,
                index.Exports.Count);
        }
        finally
        {
            try
            {
                if (Directory.Exists(temporaryRoot))
                    Directory.Delete(temporaryRoot, recursive: true);
            }
            catch
            {
                // Temporary compiler logs are disposable; cleanup must not hide the actual result.
            }
        }
    }
}