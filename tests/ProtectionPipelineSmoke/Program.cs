using System.Text;
using System.Security.Cryptography;
using Methos.Engine;

if (args.Length > 0 && args[0] == "--ucc-decompile")
{
    if (args.Length is < 3 or > 4)
        throw new ArgumentException("Usage: --ucc-decompile <package.u> <outputDirectory> [referenceSourceDirectory]");
    UnrealPackageDecompileResult result = UnrealPackageDecompiler.DecompileToFolder(args[1], args[2], args.Length == 4 ? args[3] : null);
    Console.WriteLine($"PASS: UELib exported {result.ExportedCount}/{result.ClassCount} classes from package {result.PackageVersion}/{result.LicenseeVersion}; objects={result.ObjectCount}; failed={result.FailedCount}.");
    foreach (string error in result.Errors.Take(20)) Console.WriteLine("EXPORT_ERROR " + error);
    if (result.FailedCount != 0 || result.ExportedCount != result.ClassCount)
        throw new InvalidDataException("UnrealScript source export did not complete without errors.");
    return;
}


if (args.Length > 0 && args[0] == "--repair-diagnostic")
{
    if (args.Length != 4)
        throw new ArgumentException("Usage: --repair-diagnostic <decompiled.uc> <reference.uc> <output.uc>");
    string source = File.ReadAllText(args[1]);
    string reference = File.ReadAllText(args[2]);
    try
    {
        var parsed = UnrealScriptSourceIndex.ParseFunctions(source);
        var targetFunction = parsed.FirstOrDefault(function =>
            function.Name.Equals("StrToLower", StringComparison.OrdinalIgnoreCase));
        Console.WriteLine($"TARGET_PARSE_FUNCTIONS={parsed.Count}; STRTOLOWER_FOUND={targetFunction is not null}; STRTOLOWER_HAS_NFUN={targetFunction?.Body.Contains("__NFUN_", StringComparison.Ordinal) ?? false}");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"TARGET_PARSE_ERROR={exception.GetType().Name}: {exception.Message}");
    }
    string repaired = UnrealScriptFailedFunctionRepair.RestoreFailedFunctions(
        source, reference, out IReadOnlyList<string> restored);
    Console.WriteLine($"RESTORED_FUNCTION_COUNT={restored.Count}");
    Console.WriteLine("RESTORED_FUNCTIONS=" + string.Join(",", restored));
    var after = UnrealScriptSourceIndex.ParseFunctions(repaired).FirstOrDefault(function =>
        function.Name.Equals("StrToLower", StringComparison.OrdinalIgnoreCase));
    Console.WriteLine($"STRTOLOWER_AFTER_HAS_NFUN={after?.Body.Contains("__NFUN_", StringComparison.Ordinal) ?? false}");
    File.WriteAllText(args[3], repaired, new UTF8Encoding(false));
    return;
}

if (args.Length > 0 && args[0] == "--ucc-compile")
{
    if (args.Length != 4)
        throw new ArgumentException("Usage: --ucc-compile <runtimeRoot> <sourceDirectory> <outputPackage>");
    UccCompileResult result = UccCompiler.CompileInterface(args[1], args[2]);
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[3]))!);
    File.WriteAllBytes(args[3], result.PackageBytes);
    Console.WriteLine($"PASS: UCC compiled {result.PackageBytes.Length} bytes; profile={result.ProfileDescription}; names={result.NameCount}; imports={result.ImportCount}; exports={result.ExportCount}.");
    return;
}

if (args.Length > 0 && args[0] == "--dunk3l-u-rebuild")
{
    if (args.Length != 5)
        throw new ArgumentException("Usage: --dunk3l-u-rebuild <uccRuntimeRoot> <encryptedInterface.u> <outputProtected.u> <sourceOutputDirectory>");

    string runtimeRoot = args[1];
    string inputPath = Path.GetFullPath(args[2]);
    string outputPath = Path.GetFullPath(args[3]);
    string sourceDirectory = Path.GetFullPath(args[4]);
    byte[] original = File.ReadAllBytes(inputPath);

    SmartCryptKeyProfile smartProfile = SmartCryptKeyProfile.FromEncryptedFile(original);
    var smartCodec = new SmartCrypt1522Codec(smartProfile);
    byte[] lineageEnvelope = smartCodec.Decode(original, Path.GetFileName(inputPath));
    var tailProfile = Lineage2CustomTailProfile.FromEnvelope(
        lineageEnvelope, includeTailInPayload: true, origin: inputPath);
    var lineageCodec = new Lineage2Ver111CustomTailCodec(tailProfile);
    byte[] originalPackage = lineageCodec.Decode(lineageEnvelope, Path.GetFileName(inputPath));
    var originalIndex = UnrealPackageIndex.Parse(originalPackage);

    string temporaryPackage = Path.Combine(Path.GetTempPath(), "Methos-Dunk3l-" + Guid.NewGuid().ToString("N") + ".u");
    try
    {
        File.WriteAllBytes(temporaryPackage, originalPackage);
        string referenceSourceDirectory = Path.Combine(
            runtimeRoot, "Interlude", "Interface", "Classes");
        string nativeRegistryPath = Path.Combine(Path.GetTempPath(), "Methos-Interlude-Natives-" + Guid.NewGuid().ToString("N") + ".json");
        NativeFunctionRegistryBuilder.Build(runtimeRoot, nativeRegistryPath);
        UnrealPackageDecompileResult decompiled;
        try
        {
            decompiled = UnrealPackageDecompiler.DecompileToFolder(
                temporaryPackage, sourceDirectory, referenceSourceDirectory, nativeRegistryPath);
        }
        finally
        {
            try { if (File.Exists(nativeRegistryPath)) File.Delete(nativeRegistryPath); } catch { }
        }
        if (decompiled.FailedCount != 0 || decompiled.ExportedCount != decompiled.ClassCount)
            throw new InvalidDataException($"Dunk3l U decompilation incomplete: {decompiled.ExportedCount}/{decompiled.ClassCount}, errors={decompiled.FailedCount}.");

        UccCompileResult compiled = UccCompiler.CompileInterface(runtimeRoot, sourceDirectory);
        string nameMapPath = Path.Combine(sourceDirectory, "methos_name_map.json");
        List<UnrealNameReplacement> nameMap = File.Exists(nameMapPath)
            ? System.Text.Json.JsonSerializer.Deserialize<List<UnrealNameReplacement>>(File.ReadAllText(nameMapPath)) ?? new()
            : new();
        var replacements = nameMap.ToDictionary(item => item.SafeName, item => item.OriginalName, StringComparer.OrdinalIgnoreCase);
        byte[] compiledPackage = replacements.Count == 0
            ? compiled.PackageBytes
            : UnrealPackageIndex.Parse(compiled.PackageBytes).CreateWithReplacedNames(replacements);
        var compiledIndex = UnrealPackageIndex.Parse(compiledPackage);

        byte[] rebuiltLineage = lineageCodec.Encode(compiledPackage, Path.GetFileName(inputPath));
        byte[] rebuiltProtected = smartCodec.Encode(rebuiltLineage, Path.GetFileName(inputPath));
        byte[] verifiedLineage = smartCodec.Decode(rebuiltProtected, Path.GetFileName(inputPath));
        if (!verifiedLineage.AsSpan().SequenceEqual(rebuiltLineage))
            throw new InvalidDataException("SmartCrypt verification failed for the newly compiled Dunk3l U package.");
        byte[] verifiedPackage = lineageCodec.Decode(verifiedLineage, Path.GetFileName(inputPath));
        if (!verifiedPackage.AsSpan().SequenceEqual(compiledPackage))
            throw new InvalidDataException("Lineage2Ver111 verification failed for the newly compiled Dunk3l U package.");
        _ = UnrealPackageIndex.Parse(verifiedPackage);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllBytes(outputPath, rebuiltProtected);
        Console.WriteLine($"PASS: DUNK3L U full decompile/compile/re-encrypt cycle; classes={decompiled.ExportedCount}; sanitizedNames={decompiled.SanitizedNameCount}; restoredNames={nameMap.Count}; original={originalIndex.Summary.Version}/{originalIndex.Summary.LicenseeVersion} {originalIndex.Summary.NameCount}/{originalIndex.Summary.ImportCount}/{originalIndex.Summary.ExportCount}; compiled={compiledIndex.Summary.Version}/{compiledIndex.Summary.LicenseeVersion} {compiled.NameCount}/{compiled.ImportCount}/{compiled.ExportCount}; protectedBytes={rebuiltProtected.Length}; output={outputPath}.");
    }
    finally
    {
        try { if (File.Exists(temporaryPackage)) File.Delete(temporaryPackage); } catch { }
    }
    return;
}


string dialogTypeSample = """
class DialogBox extends UICommonAPI;
var UICommonAPI.EDialogType m_type;
var UICommonAPI.DialogDefaultAction m_defaultAction;
function ShowDialog(UICommonAPI.ESilentDialogType Style) {}
function SetDefaultAction(UICommonAPI.ESilentDialogDefaultAction Action) {}
""";
string dialogTypeRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "DialogBox", dialogTypeSample, out IReadOnlyList<string> dialogTypeRepairs);
Check(dialogTypeRepairs.Count == 2 &&
      dialogTypeRepaired.Contains("function ShowDialog(UICommonAPI.EDialogType Style)", StringComparison.Ordinal) &&
      dialogTypeRepaired.Contains("function SetDefaultAction(UICommonAPI.DialogDefaultAction Action)", StringComparison.Ordinal),
    "Dunk3l DialogBox enum type references were not reconciled with its declared fields.");
Console.WriteLine("PASS: Dunk3l DialogBox enum references are reconciled to the declared UICommonAPI types.");

var inventoryCountSample = """
class InventoryWnd extends UICommonAPI;
var ItemWindowHandle RecoveredName_2802;
var ItemWindowHandle RecoveredName_2803;
function int EquipItemGetItemNum()
{
    return 0;
}
function SetItemCount()
{
    Count = (RecoveredName_2802.GetItemNum() + RecoveredName_2803.GetItemNum()) + (EquipItemGetItemNum());
}
""";
string inventoryCountRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "InventoryWnd", inventoryCountSample, out IReadOnlyList<string> inventoryCountRepairs);
Check(inventoryCountRepairs.Count == 1 &&
      inventoryCountRepaired.Contains("Count = RecoveredName_2802.GetItemNum();", StringComparison.Ordinal) &&
      inventoryCountRepaired.Contains("Count = Count + RecoveredName_2803.GetItemNum();", StringComparison.Ordinal) &&
      inventoryCountRepaired.Contains("Count = Count + EquipItemGetItemNum();", StringComparison.Ordinal) &&
      !inventoryCountRepaired.Contains("Count = (RecoveredName_2802.GetItemNum()", StringComparison.Ordinal),
    "InventoryWnd SetItemCount nested handle calls were not split into sequential assignments.");
Console.WriteLine("PASS: InventoryWnd equipment count expression is normalized for UCC.");

string inventoryHandleSample = """
class InventoryWnd extends UICommonAPI;
function InitHandle()
{
    InventoryItem_1 = "EquipItem_Underwearss";
    RecoveredName_2802 = ItemWindowHandle(GetHandle("InventoryItem"));
    RecoveredName_2803 = ItemWindowHandle(GetHandle("QuestItem"));
    RecoveredName_2804 = ItemWindowHandle(GetHandle("EquipItem"));
    TrashButton = ButtonHandle(GetHandle("InventoryWnd.TrashButton"));
    m_SearchBar = EditBoxHandle(GetHandle("InventoryWnd.SearchBar"));
}
function OnDropItem(string strTarget, ItemInfo Info)
{
    if(!isDragSrcInventory(Info.DragSrcName))
    {
        return;
    }
    normalinven = getItemWindowHandleBystrTarget(strTarget);
}
function OnDropItemSource(string strTarget, ItemInfo Info)
{
    if(!Class'NWindow.UIAPI_CHECKBOX'.static.IsChecked(RecoveredName_1435()))
    {
        if((RecoveredName_2883.SlotBitType == (RecoveredName_1455())) || RecoveredName_2883.SlotBitType == (RecoveredName_1467()))
        {
            RecoveredName_1266(RecoveredName_2883);
        }
    }
    if((!((((((((Info.DragSrcName == "InventoryItem") || Info.DragSrcName == "QuestItem")) || Info.DragSrcName == InventoryItem_1)) || -1 != InStr(Info.DragSrcName, "EquipItem"))) || Info.DragSrcName == "PetInvenWnd")))
    {
        return;
    }
}
""";
string inventoryHandleRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "InventoryWnd", inventoryHandleSample, out IReadOnlyList<string> inventoryHandleRepairs);
Check(inventoryHandleRepairs.Count == 5 &&
      inventoryHandleRepaired.Contains("var ItemWindowHandle RecoveredName_2802;", StringComparison.Ordinal) &&
      inventoryHandleRepaired.Contains("var ItemWindowHandle RecoveredName_2803;", StringComparison.Ordinal) &&
      inventoryHandleRepaired.Contains("var ItemWindowHandle RecoveredName_2804;", StringComparison.Ordinal) &&
      inventoryHandleRepaired.Contains("var string InventoryItem_1;", StringComparison.Ordinal) &&
      inventoryHandleRepaired.Contains("var ButtonHandle TrashButton;", StringComparison.Ordinal) &&
      inventoryHandleRepaired.Contains("var EditBoxHandle m_SearchBar;", StringComparison.Ordinal) &&
      inventoryHandleRepaired.Contains("if (Info.DragSrcName != \"InventoryItem\" &&", StringComparison.Ordinal) &&
      !inventoryHandleRepaired.Contains("isDragSrcInventory", StringComparison.Ordinal) &&
      inventoryHandleRepaired.Contains("ItemWindowHandle(GetHandle(strTarget))", StringComparison.Ordinal) &&
      !inventoryHandleRepaired.Contains("getItemWindowHandleBystrTarget", StringComparison.Ordinal) &&
      !inventoryHandleRepaired.Contains("RecoveredName_1266", StringComparison.Ordinal) &&
      !inventoryHandleRepaired.Contains("IsChecked(", StringComparison.Ordinal) &&
      !inventoryHandleRepaired.Contains("RecoveredName_1455", StringComparison.Ordinal) &&
      !inventoryHandleRepaired.Contains("if((!((((((((Info.DragSrcName", StringComparison.Ordinal),
    "InventoryWnd missing state fields or malformed drag-source predicate were not repaired.");
Console.WriteLine("PASS: InventoryWnd missing handle fields and malformed drag-source predicate are repaired.");


string enumParameterSwitchSample = """
class GMSnoopWnd extends UICommonAPI;
function Color GetColorByType(UIEventManager.EChatType a_Type)
{
    switch(a_Type)
    {
        case 1:
            return ResultColor;
        case 3:
            return ResultColor;
    }
}
""";
string enumParameterSwitchRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "GMSnoopWnd", enumParameterSwitchSample, out IReadOnlyList<string> enumParameterSwitchRepairs);
Check(enumParameterSwitchRepairs.Count == 1 &&
      enumParameterSwitchRepaired.Contains("case EChatType(1):", StringComparison.Ordinal) &&
      enumParameterSwitchRepaired.Contains("case EChatType(3):", StringComparison.Ordinal),
    "GMSnoopWnd enum parameter switch labels were not cast to UIEventManager.EChatType.");
Console.WriteLine("PASS: GMSnoopWnd function-parameter enum switch labels are typed for UCC.");


string optionLoginFieldSample = """
class OptionWnd extends UICommonAPI;
var bool Log;
function OnLoad()
{
    startMe(bLoginInitialized);
}
""";
string optionLoginFieldRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "OptionWnd", optionLoginFieldSample, out IReadOnlyList<string> optionLoginFieldRepairs);
Check(optionLoginFieldRepairs.Count == 1 &&
      optionLoginFieldRepaired.Contains("var bool bLoginInitialized;", StringComparison.Ordinal),
    "OptionWnd bLoginInitialized field was not restored.");
Console.WriteLine("PASS: OptionWnd missing login initialization field is restored.");


string silentOverlayColorSample = """
class Interface_by_SILENT extends UICommonAPI;
function OnLoad()
{
    local OptionWnd script;
    Class'NWindow.UIAPI_TEXTBOX'.static.SetTextColor("OKTUBRE INTERFACE", RecoveredName_98("white"));
}
""";
string silentOverlayColorRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "Interface_by_SILENT", silentOverlayColorSample, out IReadOnlyList<string> silentOverlayColorRepairs);
Check(silentOverlayColorRepairs.Count == 1 &&
      silentOverlayColorRepaired.Contains("MethosRecoveredName_85(\"white\")", StringComparison.Ordinal) &&
      !silentOverlayColorRepaired.Contains("RecoveredName_98", StringComparison.Ordinal),
    "Silent interface overlay color helper was not redirected to inherited Color API.");
Console.WriteLine("PASS: Silent interface overlay uses the inherited Color API.");



string recoveredColorHelperSample = """
class LoginCharSelectWnd extends UICommonAPI;
function TestColor()
{
    CR.SetNameWithColor(SilentInterfaceVersion(), NCT_Normal, TA_Center, RecoveredName_98("Goldlite"));
}
""";
string recoveredColorHelperRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "LoginCharSelectWnd", recoveredColorHelperSample, out IReadOnlyList<string> recoveredColorHelperRepairs);
Check(recoveredColorHelperRepairs.Count == 1 &&
      recoveredColorHelperRepaired.Contains("MethosRecoveredName_85(\"Goldlite\")", StringComparison.Ordinal) &&
      !recoveredColorHelperRepaired.Contains("RecoveredName_98", StringComparison.Ordinal),
    "Recovered color-name helper was not redirected to inherited Color API.");
Console.WriteLine("PASS: Recovered color-name helper resolves to inherited Color API.");


string logoWndFieldSample = """
class LogoWnd extends UICommonAPI;
var private WindowHandle MethosRecoveredName_2124;
var private bool MethosRecoveredName_2188;
function OnLoad()
{
    MethosRecoveredName_2124 = GetHandle("LogoWnd");
}
function OnRButtonDown(int X, int Y)
{
    if(RecoveredName_2108)
    {
        RecoveredName_2057.SetDraggable(false);
        RecoveredName_2108 = false;
        RecoveredName_255("Logo Locked");
    }
}
""";
string logoWndFieldRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "LogoWnd", logoWndFieldSample, out IReadOnlyList<string> logoWndFieldRepairs);
Check(logoWndFieldRepairs.Count == 2 &&
      logoWndFieldRepaired.Contains("MethosRecoveredName_2124.SetDraggable(false)", StringComparison.Ordinal) &&
      logoWndFieldRepaired.Contains("if(MethosRecoveredName_2188)", StringComparison.Ordinal) &&
      !logoWndFieldRepaired.Contains("RecoveredName_2057", StringComparison.Ordinal) &&
      !logoWndFieldRepaired.Contains("RecoveredName_2108", StringComparison.Ordinal) &&
      logoWndFieldRepaired.Contains("AddSystemMessage(\"Logo Locked\", MethosRecoveredName_85(\"white\"))", StringComparison.Ordinal),
    "LogoWnd recovered handle/lock references were not mapped to declared fields.");
Console.WriteLine("PASS: LogoWnd recovered references map to declared handle and lock fields.");


string manorCropCastSample = """
class ManorCropInfoChangeWnd extends UICommonAPI;
function ShowErrorDialog(int MinValue, int MaxValue, int SystemStringIdx)
{
    local string ParamString;
    ParamAdd(ParamString, "Type", string(int(1)));
}
""";
string manorCropCastRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "ManorCropInfoChangeWnd", manorCropCastSample, out IReadOnlyList<string> manorCropCastRepairs);
Check(manorCropCastRepairs.Count == 1 &&
      manorCropCastRepaired.Contains("string(1)", StringComparison.Ordinal) &&
      !manorCropCastRepaired.Contains("string(int(1))", StringComparison.Ordinal),
    "ManorCropInfoChangeWnd redundant integer cast was not removed.");
Console.WriteLine("PASS: ManorCropInfoChangeWnd redundant integer casts are normalized.");


string manorSeedCastSample = """
class ManorSeedInfoChangeWnd extends UICommonAPI;
function ShowErrorDialog(int MinValue, int MaxValue, int SystemStringIdx)
{
    local string ParamString;
    ParamAdd(ParamString, "Type", string(int(1)));
}
""";
string manorSeedCastRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "ManorSeedInfoChangeWnd", manorSeedCastSample, out IReadOnlyList<string> manorSeedCastRepairs);
Check(manorSeedCastRepairs.Count == 1 &&
      manorSeedCastRepaired.Contains("string(1)", StringComparison.Ordinal) &&
      !manorSeedCastRepaired.Contains("string(int(1))", StringComparison.Ordinal),
    "ManorSeedInfoChangeWnd redundant integer cast was not removed.");
Console.WriteLine("PASS: ManorSeedInfoChangeWnd redundant integer casts are normalized.");


string olympiadEnemyMpSample = """
class OlympiadTargetWnd extends UICommonAPI;
var int DamageDealt;
var int DamageReceived;
function OnLoad()
{
    EnemyMPLost = 0;
}
""";
string olympiadEnemyMpRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "OlympiadTargetWnd", olympiadEnemyMpSample, out IReadOnlyList<string> olympiadEnemyMpRepairs);
Check(olympiadEnemyMpRepairs.Count == 1 &&
      olympiadEnemyMpRepaired.Contains("var int EnemyMPLost;", StringComparison.Ordinal),
    "OlympiadTargetWnd EnemyMPLost field was not restored.");
Console.WriteLine("PASS: OlympiadTargetWnd missing enemy MP counter is restored.");


string chatEnumSample = """
class ChatWnd extends UIScript;
function UIEventManager.EChatType GetChatTypeByTabIndex(int Index)
{
    local UIEventManager.EChatType Type;
    Type = 0;
    switch(m_chatType)
    {
        case 0:
            Type = 0;
            break;
        case 1:
            Type = 8;
            break;
    }
    return Type;
}
""";
string chatEnumRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "ChatWnd", chatEnumSample, out IReadOnlyList<string> chatEnumRepairs);
Check(chatEnumRepairs.Count == 1 &&
      chatEnumRepaired.Contains("Type = EChatType(0);", StringComparison.Ordinal) &&
      chatEnumRepaired.Contains("Type = EChatType(8);", StringComparison.Ordinal),
    "ChatWnd EChatType numeric assignments were not restored as enum casts.");
Console.WriteLine("PASS: ChatWnd chat-tab values are explicitly cast to EChatType.");

string chatHandleSample = """
class ChatWnd extends UICommonAPI;
var EditBoxHandle ChatEditBox;
function OnLoad()
{
    ChatFilterBtn = ButtonHandle(GetHandle("ChatHideWnd.ChatFilterBtn"));
    ChatFilterBtn.SetTooltipCustomType(SetTooltip("Chat Filter\\nMouse R-Click: Hide Chat"));
}
""";
string chatHandleRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "ChatWnd", chatHandleSample, out IReadOnlyList<string> chatHandleRepairs);
Check(chatHandleRepairs.Count == 2 &&
      chatHandleRepaired.Contains("var ButtonHandle ChatFilterBtn;", StringComparison.Ordinal) &&
      chatHandleRepaired.Contains("SetTooltipCustomType(MakeTooltipSimpleText(", StringComparison.Ordinal),
    "Missing ChatWnd ChatFilterBtn handle declaration was not restored.");
Console.WriteLine("PASS: missing ChatWnd ButtonHandle declaration is restored.");

string damageLiteSample = """
class DamageLite extends UICommonAPI;
var bool Critical;
var WindowHandle OnMsgWnd9;
function HandleSystemMessage(string a_Param)
{
    ManaBurnPending = true;
    ManaBurnTargetName = "";
}
""";
string damageLiteRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "DamageLite", damageLiteSample, out IReadOnlyList<string> damageLiteRepairs);
Check(damageLiteRepairs.Count == 2 &&
      damageLiteRepaired.Contains("var bool ManaBurnPending;", StringComparison.Ordinal) &&
      damageLiteRepaired.Contains("var string ManaBurnTargetName;", StringComparison.Ordinal),
    "DamageLite Mana Burn state fields were not restored.");
Console.WriteLine("PASS: DamageLite Mana Burn state fields are restored.");

string damageLiteAniSample = """
class DamageLiteAni extends UICommonAPI;
var private TextBoxHandle MethosRecoveredName_2152[28];
function OnLoad()
{
    RecoveredName_2057 = GetHandle("DamageLiteAni");
    RecoveredName_2070[0] = TextBoxHandle(GetHandle("DamageLiteAni.D-0"));
    RecoveredName_2091 = 1996;
    RecoveredName_2107 = "Miss";
}
function OnTimer(int RecoveredName_2125)
{
    RecoveredName_2057.KillTimer(RecoveredName_2125);
    RecoveredName_2070[0].SetText("");
}
""";
string damageLiteAniRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "DamageLiteAni", damageLiteAniSample, out IReadOnlyList<string> damageLiteAniRepairs);
Check(damageLiteAniRepairs.Count == 1 &&
      damageLiteAniRepaired.Contains("var private WindowHandle RecoveredName_2057;", StringComparison.Ordinal) &&
      damageLiteAniRepaired.Contains("var private TextBoxHandle RecoveredName_2070[29];", StringComparison.Ordinal) &&
      damageLiteAniRepaired.Contains("var private int RecoveredName_2091;", StringComparison.Ordinal) &&
      damageLiteAniRepaired.Contains("var private string RecoveredName_2107;", StringComparison.Ordinal),
    "DamageLiteAni missing typed fields were not restored.");
Console.WriteLine("PASS: DamageLiteAni recovered fields are declared with inferred types.");

string damageLiteAniArrayDimensionSample = """
class DamageLiteAni extends UICommonAPI;
var private TextBoxHandle RecoveredName_2070[28];
function OnLoad()
{
    RecoveredName_2070[28] = TextBoxHandle(GetHandle("DamageLiteAni.D-28"));
}
function OnTimer(int RecoveredName_2125)
{
    RecoveredName_2070[28].SetText("");
}
""";
string damageLiteAniArrayDimensionRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "DamageLiteAni", damageLiteAniArrayDimensionSample, out IReadOnlyList<string> damageLiteAniArrayRepairs);
string[] damageLiteAniArrayDeclarations = damageLiteAniArrayDimensionRepaired
    .Split('\n')
    .Select(line => line.Trim())
    .Where(line => line.StartsWith("var private TextBoxHandle RecoveredName_2070[", StringComparison.Ordinal))
    .ToArray();
Check(damageLiteAniArrayRepairs.Count == 1 &&
      damageLiteAniArrayDeclarations.Length == 1 &&
      damageLiteAniArrayDeclarations[0] == "var private TextBoxHandle RecoveredName_2070[29];" &&
      !damageLiteAniArrayDimensionRepaired.Contains("RecoveredName_2070[28];", StringComparison.Ordinal),
    "DamageLiteAni array dimension repair duplicated the existing property instead of resizing it.");
Console.WriteLine("PASS: DamageLiteAni inferred array dimension replaces the stale declaration without a duplicate.");

string olympiadTargetExistingFieldSample = """
class OlympiadTargetWnd extends UICommonAPI;
var int DamageReceived;
var int EnemyMPLost;
function OnLoad()
{
    EnemyMPLost = 0;
}
""";
string olympiadTargetExistingFieldRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "OlympiadTargetWnd", olympiadTargetExistingFieldSample, out IReadOnlyList<string> olympiadExistingRepairs);
int olympiadExistingFieldCount = olympiadTargetExistingFieldRepaired
    .Split('\n')
    .Select(line => line.Trim())
    .Count(line => line == "var int EnemyMPLost;");
Check(olympiadExistingRepairs.Count == 0 && olympiadExistingFieldCount == 1,
    "OlympiadTargetWnd repair duplicated EnemyMPLost even though it already existed.");
Console.WriteLine("PASS: OlympiadTargetWnd keeps an existing EnemyMPLost field without duplicating it.");

string olympiadTargetMissingFieldSample = """
class OlympiadTargetWnd extends UICommonAPI;
var int DamageReceived;
function OnLoad()
{
    EnemyMPLost = 0;
}
""";
string olympiadTargetMissingFieldRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "OlympiadTargetWnd", olympiadTargetMissingFieldSample, out IReadOnlyList<string> olympiadMissingRepairs);
int olympiadMissingFieldCount = olympiadTargetMissingFieldRepaired
    .Split('\n')
    .Select(line => line.Trim())
    .Count(line => line == "var int EnemyMPLost;");
Check(olympiadMissingRepairs.Count == 1 && olympiadMissingFieldCount == 1,
    "OlympiadTargetWnd repair failed to restore a genuinely missing EnemyMPLost field.");
Console.WriteLine("PASS: OlympiadTargetWnd restores EnemyMPLost only when the field is missing.");






string tradeWndShadowedTypeSample = """
class TradeWnd extends UICommonAPI;
function HandleTradeAddItem(string param)
{
    local ItemInfo ItemInfo;
    local ItemInfo tempInfo;
    ParamToItemInfo(param,ItemInfo);
    tempInfo = ItemInfo;
}
""";
string tradeWndShadowedTypeRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "TradeWnd", tradeWndShadowedTypeSample, out IReadOnlyList<string> tradeWndShadowRepairs);
int tradeWndTempInfoDeclaration = tradeWndShadowedTypeRepaired.IndexOf(
    "local ItemInfo tempInfo;", StringComparison.Ordinal);
int tradeWndItemInfoDeclaration = tradeWndShadowedTypeRepaired.IndexOf(
    "local ItemInfo ItemInfo;", StringComparison.Ordinal);
Check(tradeWndShadowRepairs.Count == 1 &&
      tradeWndTempInfoDeclaration >= 0 &&
      tradeWndItemInfoDeclaration > tradeWndTempInfoDeclaration &&
      tradeWndShadowedTypeRepaired.Contains("ParamToItemInfo(param,ItemInfo);", StringComparison.Ordinal) &&
      tradeWndShadowedTypeRepaired.Contains("tempInfo = ItemInfo;", StringComparison.Ordinal),
    "TradeWnd local declaration order was not repaired without changing the original variable names or references.");
Console.WriteLine("PASS: TradeWnd declares the same-named type variable after other ItemInfo locals.");

string partyMatchQuoteSample = """"
class PartyMatchOutWaitListWnd extends UICommonAPI;
function HandleWaitList(string szData1)
{
    SetChatMessage(UnknownFunction112(UnknownFunction112(""",szData1)," "));
}
function HandleInvite(LVDataRecord Record)
{
    SetChatMessage(UnknownFunction112(UnknownFunction112(""",Record.LVDataList[0].szData)," "));
}
"""";
string partyMatchQuoteRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "PartyMatchOutWaitListWnd", partyMatchQuoteSample, out IReadOnlyList<string> partyMatchQuoteRepairs);
int partyMatchQuoteCount = partyMatchQuoteRepaired.Split("Chr(34)", StringSplitOptions.None).Length - 1;
Check(partyMatchQuoteRepairs.Count == 1 &&
      partyMatchQuoteCount == 2 &&
      !partyMatchQuoteRepaired.Contains("UnknownFunction112(UnknownFunction112(\"\"\",", StringComparison.Ordinal),
    "PartyMatchOutWaitListWnd did not normalize both malformed quote literals.");
Console.WriteLine("PASS: PartyMatchOutWaitListWnd quote literals are emitted as Chr(34) for UCC.");

string partyMatchWaitListQuoteSample = partyMatchQuoteSample.Replace(
    "PartyMatchOutWaitListWnd", "PartyMatchWaitListWnd", StringComparison.Ordinal);
string partyMatchWaitListQuoteRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "PartyMatchWaitListWnd", partyMatchWaitListQuoteSample, out IReadOnlyList<string> partyMatchWaitListQuoteRepairs);
int partyMatchWaitListQuoteCount = partyMatchWaitListQuoteRepaired.Split("Chr(34)", StringSplitOptions.None).Length - 1;
Check(partyMatchWaitListQuoteRepairs.Count == 1 &&
      partyMatchWaitListQuoteCount == 2 &&
      !partyMatchWaitListQuoteRepaired.Contains("UnknownFunction112(UnknownFunction112(\"\"\",", StringComparison.Ordinal),
    "PartyMatchWaitListWnd did not normalize both malformed quote literals.");
Console.WriteLine("PASS: PartyMatchWaitListWnd quote literals are emitted as Chr(34) for UCC.");

string partyMatchPostNormalizedQuoteSample = """"
class PartyMatchOutWaitListWnd extends UICommonAPI;
function string BuildMessage(string szData1)
{
    return (""" $ szData1);
}
"""";
string partyMatchPostNormalizedQuoteRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "PartyMatchOutWaitListWnd", partyMatchPostNormalizedQuoteSample, out IReadOnlyList<string> partyMatchPostNormalizedQuoteRepairs);
Check(partyMatchPostNormalizedQuoteRepairs.Count == 1 &&
      partyMatchPostNormalizedQuoteRepaired.Contains("Chr(34) $ szData1", StringComparison.Ordinal) &&
      !partyMatchPostNormalizedQuoteRepaired.Contains("\"\"\"", StringComparison.Ordinal),
    "PartyMatch quote repair did not handle a triple quote after native-call normalization.");
Console.WriteLine("PASS: PartyMatch quote repair works after native-call normalization.");



string partyWndSetNameSample = """
class PartyWnd extends UICommonAPI;
function UpdatePlayerName()
{
    m_PlayerName[idx].SetName("", 0, 2);
}
""";
string partyWndSetNameRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "PartyWnd", partyWndSetNameSample, out IReadOnlyList<string> partyWndSetNameRepairs);
Check(partyWndSetNameRepairs.Count == 1 &&
      partyWndSetNameRepaired.Contains(
          "m_PlayerName[idx].SetName(\"\", UIEventManager.NCT_Normal, UIEventManager.TA_Center);",
          StringComparison.Ordinal),
    "PartyWnd SetName arguments were not cast to the actual NWindow enum parameter types.");
Console.WriteLine("PASS: PartyWnd.SetName arguments use the declared NWindow enum types.");

string enumSwitchSample = """
class ChatWnd extends UIScript;
function Test()
{
    local UIEventManager.EIMEType imeType;
    switch(imeType)
    {
        case 1:
            if (true)
            {
                Log("case 9: is text, not a label");
            }
            break;
        case 2:
            break;
    }
}
""";
string enumSwitchRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "ChatWnd", enumSwitchSample, out IReadOnlyList<string> enumSwitchRepairs);
Check(enumSwitchRepairs.Count == 1 &&
      enumSwitchRepaired.Contains("case EIMEType(1):", StringComparison.Ordinal) &&
      enumSwitchRepaired.Contains("case EIMEType(2):", StringComparison.Ordinal) &&
      enumSwitchRepaired.Contains("\"case 9: is text, not a label\"", StringComparison.Ordinal),
    "Numeric switch labels for enum locals were not restored safely.");
Console.WriteLine("PASS: numeric enum switch labels are cast without changing string literals.");



string dialogBoxEnumSwitchSample = """
class DialogBox extends UICommonAPI;
var UICommonAPI.DialogDefaultAction m_defaultAction;
function SetDefaultAction(UICommonAPI.DialogDefaultAction defaultAction)
{
    m_defaultAction = defaultAction;
}
function DoDefaultAction()
{
    switch(m_defaultAction)
    {
        case 1:
            HandleOK();
            break;
        case 2:
            HandleCancel();
            break;
        case 0:
            HandleCancel();
            break;
    }
    SetDefaultAction(0);
}
""";
string dialogBoxEnumSwitchRepaired = UnrealScriptTypeReferenceRepair.RepairKnownMismatchedEnumReferences(
    "DialogBox", dialogBoxEnumSwitchSample, out IReadOnlyList<string> dialogBoxEnumSwitchRepairs);
Check(dialogBoxEnumSwitchRepairs.Count == 2 &&
      dialogBoxEnumSwitchRepaired.Contains("case DialogDefaultAction(1):", StringComparison.Ordinal) &&
      dialogBoxEnumSwitchRepaired.Contains("case DialogDefaultAction(2):", StringComparison.Ordinal) &&
      dialogBoxEnumSwitchRepaired.Contains("case DialogDefaultAction(0):", StringComparison.Ordinal) &&
      dialogBoxEnumSwitchRepaired.Contains("SetDefaultAction(DialogDefaultAction(0))", StringComparison.Ordinal),
    "DialogBox numeric switch labels were not cast to DialogDefaultAction.");
Console.WriteLine("PASS: DialogBox enum switch labels are restored for UCC.");

var interfaceSoundSample = """
class SoundSample extends UIScript;
function Test()
{
    PlayConsoleSound(5);
    PlayConsoleSound(6);
    PlayConsoleSound(13);
    PlayConsoleSound(14);
    PlayConsoleSound(99);
    OtherSound(5);
}
""";
string interfaceSoundRepaired = UnrealScriptNativeEnumArgumentRepair.RepairKnownInterfaceSoundArguments(
    interfaceSoundSample, out IReadOnlyList<string> interfaceSoundRepairs);
Check(interfaceSoundRepairs.Count == 4 &&
      interfaceSoundRepaired.Contains("PlayConsoleSound(IFST_WINDOW_OPEN)", StringComparison.Ordinal) &&
      interfaceSoundRepaired.Contains("PlayConsoleSound(IFST_WINDOW_CLOSE)", StringComparison.Ordinal) &&
      interfaceSoundRepaired.Contains("PlayConsoleSound(IFST_INVENWND_OPEN)", StringComparison.Ordinal) &&
      interfaceSoundRepaired.Contains("PlayConsoleSound(IFST_INVENWND_CLOSE)", StringComparison.Ordinal) &&
      interfaceSoundRepaired.Contains("PlayConsoleSound(99)", StringComparison.Ordinal) &&
      interfaceSoundRepaired.Contains("OtherSound(5)", StringComparison.Ordinal),
    "Native Interlude interface sound enum arguments were not restored from their verified values.");
Console.WriteLine("PASS: Interlude interface sound arguments are restored to their native enum identifiers.");

var dialogArgumentSample = """
class UICommonAPI extends UIScript;
function TestDialog()
{
    DialogShow(8, "message");
    DialogShow(99, "unknown");
}
""";
string dialogArgumentRepaired = UnrealScriptNativeEnumArgumentRepair.RepairKnownDialogTypeArguments(
    "UICommonAPI", dialogArgumentSample, out IReadOnlyList<string> dialogArgumentRepairs);
string dialogArgumentRepairedInOtherClass = UnrealScriptNativeEnumArgumentRepair.RepairKnownDialogTypeArguments(
    "EventMatchGMWnd", dialogArgumentSample, out IReadOnlyList<string> otherDialogArgumentRepairs);
Check(dialogArgumentRepairs.Count == 1 &&
      dialogArgumentRepaired.Contains("DialogShow(DIALOG_Block, \"message\")", StringComparison.Ordinal) &&
      dialogArgumentRepaired.Contains("DialogShow(99, \"unknown\")", StringComparison.Ordinal) &&
      otherDialogArgumentRepairs.Count == 1 &&
      dialogArgumentRepairedInOtherClass.Contains("DialogShow(DIALOG_Block, \"message\")", StringComparison.Ordinal),
    "DialogShow enum arguments must be restored safely in all recovered interface classes.");
Console.WriteLine("PASS: DialogShow uses the verified EDialogType identifier across interface classes.");


var dialogDefaultActionSample = """
class UICommonAPI extends UIScript;
function TestDefaultAction()
{
    script.SetDefaultAction(1);
    script.SetDefaultAction(2);
}
""";
string dialogDefaultActionRepaired = UnrealScriptNativeEnumArgumentRepair.RepairKnownDialogDefaultActionArguments(
    "UICommonAPI", dialogDefaultActionSample, out IReadOnlyList<string> dialogDefaultActionRepairs);
Check(dialogDefaultActionRepairs.Count == 2 &&
      dialogDefaultActionRepaired.Contains("SetDefaultAction(EDefaultOK)", StringComparison.Ordinal) &&
      dialogDefaultActionRepaired.Contains("SetDefaultAction(EDefaultCancel)", StringComparison.Ordinal),
    "DialogDefaultAction arguments were not restored to their declared enum identifiers.");
Console.WriteLine("PASS: UICommonAPI DialogBox default-action arguments use declared enum identifiers.");


var drawItemTypeSample = """
class UICommonAPI extends UIScript;
function TestDrawItem()
{
    Info.eType = 1;
    custom_1.DrawList[0].eType = 2;
}
""";
string drawItemTypeRepaired = UnrealScriptNativeEnumArgumentRepair.RepairKnownDrawItemTypeAssignments(
    "UICommonAPI", drawItemTypeSample, out IReadOnlyList<string> drawItemTypeRepairs);
Check(drawItemTypeRepairs.Count == 2 &&
      drawItemTypeRepaired.Contains("Info.eType = DIT_TEXT", StringComparison.Ordinal) &&
      drawItemTypeRepaired.Contains("custom_1.DrawList[0].eType = DIT_TEXTURE", StringComparison.Ordinal),
    "DrawItemInfo EDrawItemType assignments were not restored to verified enum identifiers.");
Console.WriteLine("PASS: UICommonAPI DrawItemInfo assignments use verified EDrawItemType identifiers.");
string autoPotionDrawItemRepaired = UnrealScriptNativeEnumArgumentRepair.RepairKnownDrawItemTypeAssignments(
    "AutoPotionWnd", "function CustomTooltip SetTooltip(string Text) { Info.eType = 1; }", out IReadOnlyList<string> autoPotionDrawItemRepairs);
Check(autoPotionDrawItemRepairs.Count == 1 &&
      autoPotionDrawItemRepaired.Contains("Info.eType = DIT_TEXT", StringComparison.Ordinal),
    "DrawItemInfo enum assignments were not restored in non-UICommonAPI classes.");
Console.WriteLine("PASS: recovered interface classes restore typed DrawItemInfo enum assignments.");


var nativeOperatorSample = """
class UICommonAPI extends UIScript;
function TestAddress(string MethosRecoveredName_5448, string OtherAddress)
{
    if(__NFUN_132__(__NFUN_132__(__NFUN_122__(MethosRecoveredName_5448, OtherAddress), __NFUN_122__(MethosRecoveredName_5448, "192.168.10.121")), __NFUN_122__(MethosRecoveredName_5448, "127.0.0.1")))
    {
        if(__NFUN_122__(MethosRecoveredName_5448, "127.0.0.1"))
        {
            Log("SILENT INTERFACE - for local test server");
        }
    }
    DialogSetParamInt(__NFUN_144__(15, 1000));
}
""";
string nativeOperatorRepaired = UnrealScriptKnownNativeOperatorRepair.RepairKnownSilentInterfaceAddressCheck(
    nativeOperatorSample, out int nativeOperatorRepairs);
Check(nativeOperatorRepairs == 1 &&
      nativeOperatorRepaired.Contains("((MethosRecoveredName_5448 == OtherAddress) || (MethosRecoveredName_5448 == \"192.168.10.121\") || (MethosRecoveredName_5448 == \"127.0.0.1\"))", StringComparison.Ordinal) &&
      nativeOperatorRepaired.Contains("MethosRecoveredName_5448 == \"127.0.0.1\"", StringComparison.Ordinal) &&
      nativeOperatorRepaired.Contains("(15 * 1000)", StringComparison.Ordinal) &&
      !nativeOperatorRepaired.Contains("__NFUN_", StringComparison.Ordinal),
    "Verified native equality, OR and multiply operators were not restored.");
Console.WriteLine("PASS: recovered Silent Interface native operators are restored from the matching Interlude reference.");

var nativeEqualitySample = """
class EqualityOperatorSample extends Object;
function bool Compare(string Value)
{
    if(__NFUN_122__(Value, "Acumen"))
    {
        return true;
    }
    return false;
}
// __NFUN_122__(mustRemainComment, "literal")
function string KeepLiteral()
{
    return "__NFUN_122__(not, code)";
}
""";
string nativeEqualityRepaired = UnrealScriptKnownNativeOperatorRepair.RepairKnownNativeEqualityOperators(
    nativeEqualitySample, out int nativeEqualityRepairs);
Check(nativeEqualityRepairs == 1 &&
      nativeEqualityRepaired.Contains("if((Value == \"Acumen\"))", StringComparison.Ordinal) &&
      nativeEqualityRepaired.Contains("// __NFUN_122__(mustRemainComment, \"literal\")", StringComparison.Ordinal) &&
      nativeEqualityRepaired.Contains("\"__NFUN_122__(not, code)\"", StringComparison.Ordinal),
    "Native 122 equality calls must be restored without modifying comments or string literals.");
Console.WriteLine("PASS: native 122 equality calls are restored safely.");


var nativeUnarySample = """
class UnaryOperatorSample extends Object;
function bool ImMage()
{
    return __NFUN_129__(ImFighter());
}
function bool CheckFlags()
{
    return __NFUN_129__(__NFUN_129__(HasBuff));
}
function string KeepLiteral()
{
    return "__NFUN_129__(not code)";
}
""";
string nativeUnaryRepaired = UnrealScriptKnownNativeOperatorRepair.RepairKnownNativeUnaryOperators(
    nativeUnarySample, out int nativeUnaryRepairs);
Check(nativeUnaryRepairs == 3 &&
      nativeUnaryRepaired.Contains("return !(ImFighter());", StringComparison.Ordinal) &&
      nativeUnaryRepaired.Contains("return !(!(HasBuff));", StringComparison.Ordinal) &&
      nativeUnaryRepaired.Contains("\"__NFUN_129__(not code)\"", StringComparison.Ordinal),
    "Verified native boolean-negation calls were not restored safely.");
Console.WriteLine("PASS: native 129 boolean negation is restored, including nested calls.");



string nativeFunctionNormalizerSample = """
class NativeFunctionNormalizerSample extends Object;
function int Convert(string curChar)
{
    return UnknownFunction237(curChar);
}
function string UnknownFunction112(string left, string right)
{
    return (left $ right);
}
function string Join(string left, string right)
{
    return UnknownFunction112(left, right);
}
function int LegacyConvert(string curChar)
{
    return __NFUN_237__(curChar);
}
function int KeepUnknown()
{
    return UnknownFunction999(1);
}
function string KeepLiteral()
{
    return "UnknownFunction237(not code) __NFUN_112__(left,right)";
}
// UnknownFunction237(comment only)
""";
string nativeFunctionRegistryPath = Path.GetTempFileName();
try
{
    File.WriteAllText(nativeFunctionRegistryPath, """
[
  { "NativeToken": 237, "Name": "Asc", "IsOperator": false },
  { "NativeToken": 112, "Name": "Concat_StrStr", "IsOperator": true }
]
""");
    string nativeFunctionNormalized = UnrealScriptNativeFunctionNormalizer.Normalize(
        nativeFunctionNormalizerSample, nativeFunctionRegistryPath,
        out int normalizedNativeCalls, out int unresolvedNativeCalls);
    Check(normalizedNativeCalls == 3 &&
          unresolvedNativeCalls == 1 &&
          nativeFunctionNormalized.Contains("return Asc(curChar);", StringComparison.Ordinal) &&
          nativeFunctionNormalized.Contains("return (left $ right);", StringComparison.Ordinal) &&
          nativeFunctionNormalized.Contains("function string UnknownFunction112(string left, string right)", StringComparison.Ordinal) &&
          nativeFunctionNormalized.Contains("UnknownFunction999(1)", StringComparison.Ordinal) &&
          nativeFunctionNormalized.Contains("\"UnknownFunction237(not code) __NFUN_112__(left,right)\"", StringComparison.Ordinal) &&
          nativeFunctionNormalized.Contains("// UnknownFunction237(comment only)", StringComparison.Ordinal),
        "Native function normalizer did not resolve both UnknownFunction and __NFUN wrappers safely.");
}
finally
{
    if (File.Exists(nativeFunctionRegistryPath)) File.Delete(nativeFunctionRegistryPath);
}
Console.WriteLine("PASS: native function normalizer resolves UnknownFunction wrappers from the verified token registry.");

var trailingReturnSample = """
class ReturnSample extends Object;
function int GetValue()
{
    return 7;
    return;
}
function void DoNothing()
{
    return;
}
""";
string trailingReturnRepaired = UnrealScriptFailedFunctionRepair.RestoreInvalidTrailingReturns(
    trailingReturnSample, trailingReturnSample, out IReadOnlyList<string> trailingReturnRepairs);
Check(trailingReturnRepairs.SequenceEqual(new[] { "GetValue" }) &&
      trailingReturnRepaired.Contains("return 7;", StringComparison.Ordinal) &&
      !trailingReturnRepaired.Contains("return;\n}", StringComparison.Ordinal) &&
      trailingReturnRepaired.Contains("function void DoNothing()", StringComparison.Ordinal),
    "Unreachable bare return repair must affect only non-void functions with a preceding value return.");
Console.WriteLine("PASS: unreachable trailing bare returns are removed without changing void functions.");

var recoveredBoolSwitchSample = """
class BoolSwitchSample extends Object;
function bool NoblesseCele(int SkillID)
{
    switch(SkillID)
    {
        case 1323:
        case 408:
        case 1418:
            return true;
        default:
            return false;
            break;
    }
    return;
}
""";
var boolSwitchRepair = UnrealScriptSourceIndex.RemoveRedundantTrailingReturns(recoveredBoolSwitchSample);
Check(boolSwitchRepair.Repairs.Any(repair => repair.FunctionName == "NoblesseCele") &&
      boolSwitchRepair.Source.Contains("default:", StringComparison.Ordinal) &&
      boolSwitchRepair.Source.Contains("return false;", StringComparison.Ordinal) &&
      !boolSwitchRepair.Source.Contains("    return;\n}", StringComparison.Ordinal),
    "Recovered bool switch functions must replace UELib's invalid bare trailing return with a false fallback.");
Console.WriteLine("PASS: recovered bool switch trailing returns are normalized to a valid false fallback.");

var recoveredIntSwitchSample = """
class IntSwitchSample extends Object;
function int UnkFuncSomeID_1(int SkillID)
{
    switch(SkillID)
    {
        case 357:
            return 3;
            break;
        default:
            return -1;
            break;
    }
    return;
}
""";
var intSwitchRepair = UnrealScriptSourceIndex.RemoveRedundantTrailingReturns(recoveredIntSwitchSample);
Console.WriteLine($"DEBUG int switch repairs={intSwitchRepair.Repairs.Count}; source={intSwitchRepair.Source}");
Check(intSwitchRepair.Repairs.Any(repair => repair.FunctionName == "UnkFuncSomeID_1") &&
      !intSwitchRepair.Source.Contains("    return;\n}", StringComparison.Ordinal),
    "Recovered int switch functions with exhaustive return branches must drop the invalid trailing bare return.");
Console.WriteLine("PASS: exhaustive int switch trailing bare returns are removed.");

var enchantScrollCountSample = """
class EnchantAIWnd extends Object;
function int GetScrollCount(out ItemInfo ScrollInform)
{
    local int Index;
    local ItemInfo ScrInfo;
    Index = Class'NWindow.UIAPI_ITEMWINDOW'.static.FindItemWithClassID("InventoryWnd.InventoryItem", ScrollInform.ClassID);
    if(Class'NWindow.UIAPI_ITEMWINDOW'.static.GetItem("InventoryWnd.InventoryItem", Index, ScrInfo))
    {
        ScrollInform = ScrInfo;
        return ScrInfo.ItemNum;
    }
    else
    {
        return 0;
    }
    return;
}
""";
var enchantScrollCountRepair = UnrealScriptSourceIndex.RemoveRedundantTrailingReturns(enchantScrollCountSample);
Check(enchantScrollCountRepair.Repairs.Any(repair => repair.FunctionName == "GetScrollCount") &&
      !enchantScrollCountRepair.Source.Contains("    return;\n}", StringComparison.Ordinal),
    "EnchantAIWnd exhaustive inventory lookup must not retain a valueless trailing return.");
Console.WriteLine("PASS: EnchantAIWnd GetScrollCount trailing return is removed.");


static void Check(bool value, string message)
{
    if (!value) throw new Exception(message);
}

byte[] fiveByteCompact = { 0x75, 0xFF, 0xFF, 0xFF, 0x1F };
int fiveBytePosition = 0;
Check(UnrealPackageIndex.DecodeCompactIndex(fiveByteCompact, ref fiveBytePosition) == -11 &&
      fiveBytePosition == fiveByteCompact.Length,
    "UE compact indices with a full fifth byte must preserve 32-bit overflow semantics.");

byte[] smartSample = new byte[0x400];
Convert.FromHexString("FFEDFACE0100").CopyTo(smartSample, 0);
var smart = ProtectionPipelinePlanner.Inspect(smartSample, "interface.xdat");
Check(smart.Status == "recognized-decode-only" && smart.CanDecode && !smart.CanEncode,
    "SmartCrypt sample with an embedded seed block must be recognized as decode-capable but not yet encode-verified.");

byte[] l2 = Encoding.Unicode.GetBytes("Lineage2Ver413");
var lineage = ProtectionPipelinePlanner.Inspect(l2, "systemmsg-e.dat");
Check(lineage.Status == "recognized-supported-encryption" && lineage.CanDecode && lineage.CanEncode,
    "Lineage2Ver413 must report the integrated codec as available.");

var unknown = ProtectionPipelinePlanner.Inspect(new byte[] { 0x12, 0x34, 0x56 }, "unknown.bin");
Check(unknown.Status == "unknown-or-unsupported" && !unknown.CanDecode,
    "Unknown files must fail closed.");

byte[] unreal = new byte[] { 0xC1, 0x83, 0x2A, 0x9E, 0, 0 };
var package = ProtectionPipelinePlanner.Inspect(unreal, "Interface.u");
Check(package.Status == "plain-format-candidate" && package.Layers[0].Kind == ProtectionLayerKind.UnrealPackage,
    "Unreal package signature was not recognized.");
var wrongExtension = ProtectionPipelinePlanner.Inspect(unreal, "Interface.xdat");
Check(wrongExtension.Layers[0].Kind == ProtectionLayerKind.UnrealPackage &&
      !wrongExtension.Explanation.Contains("XDAT válido", StringComparison.OrdinalIgnoreCase),
    "An Unreal package signature must never be mislabeled as XDAT based only on extension.");
var xdatSmart = ProtectionPipelinePlanner.Inspect(smartSample, "Interface.xdat");
Check(xdatSmart.Layers[0].Kind == ProtectionLayerKind.SmartCrypt1522Candidate &&
      xdatSmart.CanDecode && !xdatSmart.CanEncode,
    "Encrypted XDAT with an embedded seed block must be recognized as decode-capable but not encode-verified.");

Console.WriteLine("PASS: SmartCrypt candidate, Lineage II header, unknown fail-closed, and Unreal signature checks.");

var twoLayerPlan = new ProtectionPlan(".dat", new[]
{
    new ProtectionLayer(ProtectionLayerKind.Lineage2Standard, "test-a", "test", true, true, "test vector"),
    new ProtectionLayer(ProtectionLayerKind.Lineage2Standard, "test-b", "test", true, true, "test vector")
}, true, true, "test", "Synthetic known-answer pipeline test.");
var runtime = new ProtectionCodecRuntime(new IProtectionCodec[] { new XorTestCodec("test-a", 0x35), new XorTestCodec("test-b", 0xA7) });
byte[] plain = Encoding.UTF8.GetBytes("Methos reversible pipeline vector");
byte[] encrypted = runtime.Encode(twoLayerPlan, plain, "vector.dat");
byte[] decoded = runtime.Decode(twoLayerPlan, encrypted, "vector.dat");
Check(plain.SequenceEqual(decoded), "Two-layer encode/decode round-trip failed.");

var noSmartCodec = new ProtectionCodecRuntime(Array.Empty<IProtectionCodec>());
bool blocked = false;
try { noSmartCodec.Decode(smart, smartSample, "interface.xdat"); }
catch (NotSupportedException) { blocked = true; }
Check(blocked, "Runtime must refuse SmartCrypt when no codec is registered.");

Console.WriteLine("PASS: synthetic two-layer encode/decode round-trip and missing SmartCrypt codec rejection.");

var standard = ProtectionCodecRuntime.CreateDefault();
byte[] standardPlain = Encoding.UTF8.GetBytes("Methos standard Lineage II codec known-answer round-trip.");
foreach (int protocol in new[] { 111, 413 })
{
    var standardPlan = new ProtectionPlan(".dat", new[]
    {
        new ProtectionLayer(ProtectionLayerKind.Lineage2Standard, $"lineage2-{protocol}", "test", true, true, "synthetic round-trip")
    }, true, true, "test", "Synthetic Lineage II standard codec round-trip.");
    byte[] standardEncrypted = standard.Encode(standardPlan, standardPlain, "vector.dat");
    var detected = ProtectionPipelinePlanner.Inspect(standardEncrypted, "vector.dat");
    Check(detected.CanDecode &&
          (protocol == 413 ? detected.CanEncode :
              !detected.CanEncode && detected.Layers[0].Kind == ProtectionLayerKind.Lineage2CustomTrailer),
        $"Protocol {protocol} header was not recognized with the correct trailer-preservation policy.");
    byte[] standardDecoded = standard.Decode(standardPlan, standardEncrypted, "vector.dat");
    Check(standardPlain.SequenceEqual(standardDecoded), $"Protocol {protocol} round-trip failed.");
}
Console.WriteLine("PASS: integrated Lineage II 111 XOR and 413 RSA/zlib encode/decode round-trips.");

if (File.Exists("sysstring-e.dat"))
{
    byte[] realSysString = File.ReadAllBytes("sysstring-e.dat");
    var realSysPlan = ProtectionPipelinePlanner.Inspect(realSysString, "sysstring-e.dat");
    Check(realSysPlan.Status == "recognized-supported-encryption" &&
          realSysPlan.Layers.Any(x => x.Id == "lineage2-413"),
        "Real SysString-e.dat must be detected as Lineage2Ver413.");

    var realSysCodec = new Lineage2StandardCodec(413);
    byte[] decodedSysString = realSysCodec.Decode(realSysString, "sysstring-e.dat");
    Check(decodedSysString.Length == 29584 &&
          decodedSysString.AsSpan().IndexOf("Equipment"u8) >= 0 &&
          decodedSysString.AsSpan().IndexOf("Lineage II User Agreement"u8) >= 0,
        "Real SysString-e.dat did not decode to the expected English system-text resource.");
    string decodedSysHash = Convert.ToHexString(SHA256.HashData(decodedSysString)).ToLowerInvariant();
    Check(decodedSysHash == "a2c15980a6278c04640fd9163da9f8916d2f0e9349538cc9a6f7d4b74a4065b4",
        "Real SysString-e.dat decoded content hash differs from the independently validated reference.");

    byte[] rebuiltSysString = realSysCodec.Encode(decodedSysString, "sysstring-e.dat");
    byte[] verifiedSysString = realSysCodec.Decode(rebuiltSysString, "sysstring-e.dat");
    Check(decodedSysString.SequenceEqual(verifiedSysString),
        "Real SysString-e.dat protocol 413 decode/re-encode round-trip failed.");
    Console.WriteLine($"PASS: REAL SysString-e.dat Lineage2Ver413 RSA/zlib decode and re-encode round-trip; encrypted={realSysString.Length}, decoded={decodedSysString.Length}, SHA256={decodedSysHash}.");
}
else
{
    Console.WriteLine("SKIP: sysstring-e.dat real protocol 413 fixture is not present.");
}

{
    byte[] seedBlock = Enumerable.Range(0, 24).Select(i => (byte)(0x31 + i * 7)).ToArray();
seedBlock[0] = 1;
seedBlock[1] = 0;
byte[] smartMarker = { 0xFF, 0xED, 0xFA, 0xCE };
byte[] fakeEncryptedHeader = new byte[0x400];
smartMarker.CopyTo(fakeEncryptedHeader, 0);
seedBlock.CopyTo(fakeEncryptedHeader, 4);
var smartProfile = SmartCryptKeyProfile.FromEncryptedFile(fakeEncryptedHeader);
Check(smartProfile.Lineage2Version == 111 && smartProfile.SeedBlock.SequenceEqual(seedBlock),
    "SmartCrypt per-file key derivation from embedded seed block failed.");

var smartCodec = new SmartCrypt1522Codec(smartProfile);
byte[] smartPlain = Enumerable.Range(0, 2048).Select(i => (byte)((i * 37 + 19) & 0xFF)).ToArray();
Encoding.Unicode.GetBytes("Lineage2Ver111").CopyTo(smartPlain, 0);
byte[] smartEncrypted = smartCodec.Encode(smartPlain, "Interface.u");
Check(SmartCrypt1522Codec.HasHeader(smartEncrypted) &&
      smartEncrypted.AsSpan(4, 24).SequenceEqual(seedBlock),
    "SmartCrypt encoder did not restore the original marker and per-file seed block.");

string smartProfileText =
    $"NPK_GLOBAL_KEY=12345678\\nSTATE_SEED=0100\\nSEED_BLOCK={Convert.ToHexString(seedBlock)}\\nMARKER=FFEDFACE\\nLINEAGE2_VERSION=0000006F\\nKEY6={Convert.ToHexString(smartProfile.Key6)}\\nKEY16={Convert.ToHexString(smartProfile.Key16)}\\nFLAG=00\\n";
var parsedSmartProfile = SmartCryptKeyProfile.ParseLog(smartProfileText, "synthetic-log");
Check(parsedSmartProfile.Key6.SequenceEqual(smartProfile.Key6) &&
      parsedSmartProfile.Key16.SequenceEqual(smartProfile.Key16) &&
      parsedSmartProfile.SeedBlock.SequenceEqual(seedBlock) &&
      parsedSmartProfile.Lineage2Version == 111 &&
      parsedSmartProfile.GlobalKey == 0x12345678 &&
      parsedSmartProfile.Origin == "synthetic-log",
    "SmartCrypt captured key profile import failed.");

var smartRuntime = ProtectionCodecRuntime.CreateDefault(smartCryptProfile: parsedSmartProfile);
var smartPlan = ProtectionPipelinePlanner.Inspect(smartEncrypted, "Interface.u",
    smartCryptKeyAvailable: false, smartCryptEncoderVerified: false);
Check(smartPlan.CanDecode && !smartPlan.CanEncode &&
      smartPlan.Layers[0].Kind == ProtectionLayerKind.SmartCrypt1522Candidate,
    "SmartCrypt planner did not derive decode keys from the embedded seed block or kept unverified encoding disabled.");
byte[] smartDecoded = smartRuntime.Decode(smartPlan, smartEncrypted, "Interface.u");
Check(smartPlain.SequenceEqual(smartDecoded), "SmartCrypt 1.5.2.2 header reconstruction and XOR round-trip failed.");

bool smartEncodeBlocked = false;
try { _ = smartRuntime.Encode(smartPlan, smartPlain, "Interface.u"); }
catch (NotSupportedException) { smartEncodeBlocked = true; }
Check(smartEncodeBlocked, "SmartCrypt runtime must block re-encoding until a real-file round trip is verified.");

byte[] externalKeySample = new byte[0x400];
smartMarker.CopyTo(externalKeySample, 0);
byte[] externalSeed = seedBlock.ToArray();
externalSeed[1] = 1;
externalSeed.CopyTo(externalKeySample, 4);
var smartNeedsExternal = ProtectionPipelinePlanner.Inspect(externalKeySample, "Interface.u");
Check(smartNeedsExternal.Status == "recognized-key-required" && !smartNeedsExternal.CanDecode,
    "SmartCrypt must require the external key when the seed block sets its external-key bit.");
Console.WriteLine("PASS: SmartCrypt 1.5.2.2 embedded seed derivation, Lineage2Ver header reconstruction, profile import, and fail-closed encode.");
}

using (RSA aacRsa = RSA.Create(2048))
{
    RSAParameters aacParameters = aacRsa.ExportParameters(true);
    byte[] nLe = aacParameters.Modulus!.Reverse().ToArray();
    byte[] dLe = aacParameters.D!.Reverse().ToArray();
    byte[] eLe = aacParameters.Exponent!.Reverse().ToArray();
    var aacProfile = new ActiveAntiCheatRsaProfile(nLe, dLe, eLe);
    var parsedAacProfile = ActiveAntiCheatRsaProfile.ParseLog(
        $"N_LE={Convert.ToHexString(nLe)}\\nD_LE={Convert.ToHexString(dLe)}\\n", eLe, "synthetic-log");
    Check(parsedAacProfile.Modulus == aacProfile.Modulus &&
          parsedAacProfile.PrivateExponent == aacProfile.PrivateExponent &&
          parsedAacProfile.Origin == "synthetic-log",
        "ActiveAntiCheatCrypt captured N_LE/D_LE profile import failed.");
    var aacRuntime = ProtectionCodecRuntime.CreateDefault(aacProfile);
    var aacPlan = new ProtectionPlan(".u", new[]
    {
        new ProtectionLayer(ProtectionLayerKind.ActiveAntiCheatCrypt,
            "active-anticheat-crypt", "synthetic", true, true, "synthetic RSA/RC4 fixture")
    }, true, true, "test", "Synthetic ActiveAntiCheatCrypt round-trip.");
    byte[] aacPlain = Encoding.UTF8.GetBytes("Methos ActiveAntiCheatCrypt RSA-2048 RC4 test vector.");
    byte[] aacEncrypted = aacRuntime.Encode(aacPlan, aacPlain, "Interface.u");
    Check(ActiveAntiCheatCodec.HasHeader(aacEncrypted), "ActiveAntiCheatCrypt header was not emitted.");
    byte[] rsaMessage = aacRsa.Decrypt(
        aacEncrypted.AsSpan(ActiveAntiCheatCodec.RsaBlockOffset, ActiveAntiCheatCodec.RsaBlockLength).ToArray(),
        RSAEncryptionPadding.Pkcs1);
    Check(rsaMessage.Length == 40 &&
          rsaMessage.AsSpan(0, 20).SequenceEqual("ActiveAnticheatCrypt"u8),
        "ActiveAntiCheatCrypt RSA block did not contain the expected magic and RC4 key.");
    var aacDetected = ProtectionPipelinePlanner.Inspect(aacEncrypted, "Interface.u",
        activeAntiCheatPrivateKeyAvailable: true, activeAntiCheatEncoderVerified: false);
    Check(aacDetected.CanDecode && !aacDetected.CanEncode &&
          aacDetected.Layers[0].Kind == ProtectionLayerKind.ActiveAntiCheatCrypt,
        "ActiveAntiCheatCrypt detection did not expose the configured key capabilities.");
    byte[] aacDecoded = aacRuntime.Decode(aacDetected, aacEncrypted, "Interface.u");
    Check(aacPlain.SequenceEqual(aacDecoded), "ActiveAntiCheatCrypt RSA/RC4 round-trip failed.");
    var aacNoKey = ProtectionPipelinePlanner.Inspect(aacEncrypted, "Interface.u");
    Check(aacNoKey.Status == "recognized-key-required" && !aacNoKey.CanDecode,
        "ActiveAntiCheatCrypt must fail closed when its matching private key is unavailable.");
}
Console.WriteLine("PASS: ActiveAntiCheatCrypt RSA-2048/PKCS#1 v1.5 + RC4 encode/decode and key-required detection.");
var literalBody = "return 42;";
byte[] literalBytecode = UnrealScriptFunctionCompiler.CompileReturnOnlyBody(literalBody);
string restoredLiteralBody = UnrealScriptFunctionDecompiler.DecompileReturnOnly(literalBytecode);
Check(restoredLiteralBody == literalBody,
    "UnrealScript literal return compile/decompile round-trip failed.");
Console.WriteLine("PASS: native UnrealScript literal return compile/decompile round-trip.");


foreach (string targetName in new[] { "interface.u", "interface.xdat" })
{
    if (!File.Exists(targetName)) continue;
    byte[] original = File.ReadAllBytes(targetName);
    var smartProfile = SmartCryptKeyProfile.FromEncryptedFile(original);
    var smartCodec = new SmartCrypt1522Codec(smartProfile);
    byte[] lineageEnvelope = smartCodec.Decode(original, targetName);
    Check(lineageEnvelope.AsSpan(0, 28).SequenceEqual(Encoding.Unicode.GetBytes("Lineage2Ver111")),
        $"{targetName}: SmartCrypt did not reconstruct the Lineage2Ver111 header.");

    bool includeTailInPayload = targetName.EndsWith(".u", StringComparison.OrdinalIgnoreCase);
    var tailProfile = Lineage2CustomTailProfile.FromEnvelope(lineageEnvelope,
        includeTailInPayload: includeTailInPayload);
    var customTailCodec = new Lineage2Ver111CustomTailCodec(tailProfile);
    byte[] plainPayload = customTailCodec.Decode(lineageEnvelope, targetName);
    if (!includeTailInPayload)
    {
        byte[] standardDecoded = new Lineage2StandardCodec(111).Decode(lineageEnvelope, targetName);
        Check(standardDecoded.SequenceEqual(plainPayload),
            $"{targetName}: normal protocol-111 decode should match the original-trailer compatibility payload.");
    }

    if (targetName.EndsWith(".u", StringComparison.OrdinalIgnoreCase))
    {
        var parsedPackage = UnrealPackageIndex.Parse(plainPayload);
        Check(parsedPackage.Summary.Version == 126 &&
              parsedPackage.Summary.LicenseeVersion == 0 &&
              parsedPackage.Names.Count == 5482 &&
              parsedPackage.Exports.Count == 10308 &&
              parsedPackage.Imports.Count == 740,
            "Dunk3l interface.u did not reproduce the known UE 126/0 package tables (5482 names, 10308 exports, 740 imports).");

        string originalObfuscatedName = parsedPackage.Names[31];
        const string temporarySafeName = "MethosRoundTripProbeName";
        byte[] renamedNameTable = parsedPackage.CreateWithReplacedNames(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [originalObfuscatedName] = temporarySafeName
            });
        var renamedNameIndex = UnrealPackageIndex.Parse(renamedNameTable);
        Check(renamedNameIndex.Names.Contains(temporarySafeName) &&
              renamedNameIndex.NameFlags.SequenceEqual(parsedPackage.NameFlags) &&
              renamedNameIndex.Imports.SequenceEqual(parsedPackage.Imports) &&
              renamedNameIndex.Exports.SequenceEqual(parsedPackage.Exports),
            "Dunk3l U FName sanitization changed name flags or object references.");
        byte[] restoredNameTable = renamedNameIndex.CreateWithReplacedNames(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [temporarySafeName] = originalObfuscatedName
            });
        var restoredNameIndex = UnrealPackageIndex.Parse(restoredNameTable);
        Check(restoredNameIndex.Names.SequenceEqual(parsedPackage.Names),
            "Dunk3l U FName restoration did not reproduce the original name table.");

        byte[] tableRoundTrip = parsedPackage.CreateLosslessRoundTrip();
        var tableVerified = UnrealPackageIndex.Parse(tableRoundTrip);
        Check(tableVerified.Names.SequenceEqual(parsedPackage.Names) &&
              tableVerified.Imports.SequenceEqual(parsedPackage.Imports) &&
              tableVerified.Exports.SequenceEqual(parsedPackage.Exports),
            "Dunk3l interface.u lossless table reconstruction changed original package metadata.");
        Console.WriteLine($"PASS: Dunk3l interface.u UE 126/0 index tables lossless round-trip; names={tableVerified.Names.Count}, imports={tableVerified.Imports.Count}, exports={tableVerified.Exports.Count}.");

        string diagnosticRoot = Path.Combine(Path.GetTempPath(), "Methos-Dunk3l-UELib-" + Guid.NewGuid().ToString("N"));
        string diagnosticPackage = Path.Combine(diagnosticRoot, "Interface.u");
        string diagnosticOutput = Path.Combine(diagnosticRoot, "Descompilado");
        Directory.CreateDirectory(diagnosticRoot);
        try
        {
            File.WriteAllBytes(diagnosticPackage, plainPayload);
            var uelibResult = UnrealPackageDecompiler.DecompileToFolder(diagnosticPackage, diagnosticOutput);
            Console.WriteLine($"DUNK3L_UELIB version={uelibResult.PackageVersion}/{uelibResult.LicenseeVersion} objects={uelibResult.ObjectCount} classes={uelibResult.ClassCount} exported={uelibResult.ExportedCount} failed={uelibResult.FailedCount} sanitizedNames={uelibResult.SanitizedNameCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DUNK3L_UELIB_DIAGNOSTIC_FAILED {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            try { if (Directory.Exists(diagnosticRoot)) Directory.Delete(diagnosticRoot, recursive: true); } catch { }
        }
    }
    else
    {
        Check(Encoding.ASCII.GetString(plainPayload).Contains("AbnormalDebuffsStatusWnd", StringComparison.Ordinal),
            "Dunk3l interface.xdat did not expose expected XDAT window names after both layers.");
    }

    var customPlan = ProtectionPipelinePlanner.Inspect(lineageEnvelope, targetName,
        lineage2CustomTailProfileAvailable: true, lineage2CustomTailEncoderVerified: true);
    Check(customPlan.Layers[0].Kind == ProtectionLayerKind.Lineage2CustomTrailer &&
          customPlan.CanDecode && customPlan.CanEncode,
        $"{targetName}: planner did not recognize the original custom protocol-111 trailer.");

    var inspection = ProtectionPipelinePlanner.InspectFullPipeline(original, targetName);
    Check(inspection.Plan.Layers.Count == 2 &&
          inspection.Plan.Layers[0].Kind == ProtectionLayerKind.SmartCrypt1522Candidate &&
          inspection.Plan.Layers[1].Kind == ProtectionLayerKind.Lineage2CustomTrailer &&
          inspection.Plan.CanDecode && inspection.Plan.CanEncode &&
          inspection.IsPlaintextCandidate,
        $"{targetName}: automatic multi-layer inspection did not verify both original protection layers.");
    Check(inspection.DecodedPayload.SequenceEqual(plainPayload),
        $"{targetName}: automatic inspection returned a different plaintext payload.");

    var dunk3lRuntime = ProtectionCodecRuntime.CreateDefault(
        smartCryptProfile: inspection.SmartCryptProfile,
        lineage2CustomTailProfile: inspection.Lineage2CustomTailProfile);

    byte[] runtimeDecoded = dunk3lRuntime.Decode(inspection.Plan, original, targetName);
    Check(runtimeDecoded.SequenceEqual(plainPayload),
        $"{targetName}: complete layered runtime decode did not reproduce the validated payload.");
    byte[] runtimeRebuilt = dunk3lRuntime.Encode(inspection.Plan, plainPayload, targetName);
    Check(runtimeRebuilt.SequenceEqual(original),
        $"{targetName}: complete layered runtime encode did not reproduce the original file byte for byte.");

    Console.WriteLine($"PASS: REAL Dunk3l {targetName}: SmartCrypt + custom Lineage2Ver111 decode/encode exact round-trip; original trailer preserved ({original.Length} bytes).");
}

if (File.Exists("Engine.u"))
{
    byte[] realEngine = File.ReadAllBytes("Engine.u");
    var realDetection = ProtectionPipelinePlanner.Inspect(realEngine, "Engine.u");
    Check(realDetection.Status == "recognized-decode-only" &&
          realDetection.CanDecode && !realDetection.CanEncode &&
          realDetection.Layers.Any(x => x.Id == "lineage2-111-custom-tail"),
        "Real Engine.u must be identified as Lineage2Ver111 with its original trailer required for encoding.");

    var realTailProfile = Lineage2CustomTailProfile.FromEnvelope(realEngine);
    var realPlan = new ProtectionPlan(".u", new[]
    {
        new ProtectionLayer(ProtectionLayerKind.Lineage2CustomTrailer, "lineage2-111-custom-tail",
            "original-trailer-preserved-and-roundtrip-verified", true, true,
            "Real client Engine.u sample")
    }, true, true, "real-sample", "Real Lineage2Ver111 Engine.u decode with original trailer preservation.");
    var realRuntime = ProtectionCodecRuntime.CreateDefault(lineage2CustomTailProfile: realTailProfile);

    byte[] realPlain = realRuntime.Decode(realPlan, realEngine, "Engine.u");
    Check(realPlain.Length > 65536 &&
          BitConverter.ToUInt32(realPlain, 0) == UnrealPackageIndex.PackageTag,
        "Decoded Engine.u must begin with a classic Unreal package signature.");

    var realPackage = UnrealPackageIndex.Parse(realPlain);
    Console.WriteLine($"ENGINE_U_TABLES names={realPackage.Names.Count} imports={realPackage.Imports.Count} exports={realPackage.Exports.Count} header={realPackage.Summary.NameCount}/{realPackage.Summary.ImportCount}/{realPackage.Summary.ExportCount}");
    Check(realPackage.Summary.Version == 123 && realPackage.Summary.LicenseeVersion == 30,
        "Decoded Engine.u package profile must be 123/30.");
    Check(realPackage.Names.Count > 1000 && realPackage.Imports.Count > 50 &&
          realPackage.Exports.Count > 1000,
        "Decoded Engine.u must contain plausible name/import/export tables.");

    byte[] rebuiltPlain = new UnrealPackageBuilder(realPackage).Build();
    var rebuiltPackage = UnrealPackageIndex.Parse(rebuiltPlain);
    Check(rebuiltPackage.Names.SequenceEqual(realPackage.Names) &&
          rebuiltPackage.Imports.SequenceEqual(realPackage.Imports) &&
          rebuiltPackage.Exports.SequenceEqual(realPackage.Exports),
        "Engine.u package rebuild must preserve all name/import/export tables.");

    byte[] rebuiltEncrypted = realRuntime.Encode(realPlan, rebuiltPlain, "Engine.u");
    byte[] finalPlain = realRuntime.Decode(realPlan, rebuiltEncrypted, "Engine.u");
    var finalPackage = UnrealPackageIndex.Parse(finalPlain);
    Check(finalPackage.Names.SequenceEqual(realPackage.Names) &&
          finalPackage.Imports.SequenceEqual(realPackage.Imports) &&
          finalPackage.Exports.SequenceEqual(realPackage.Exports),
        "Engine.u 111 encode/decode after package rebuild must preserve all tables.");

    Console.WriteLine($"PASS: REAL Engine.u Lineage2Ver111 decode, Unreal parse, rebuild, and re-encrypt; names={realPackage.Names.Count}, imports={realPackage.Imports.Count}, exports={realPackage.Exports.Count}.");
}
else
{
    Console.WriteLine("SKIP: Engine.u real-client fixture is not present in this checkout.");
}

sealed class XorTestCodec(string layerId, byte key) : IProtectionCodec
{
    public string LayerId => layerId;
    public byte[] Decode(ReadOnlyMemory<byte> input, string originalFileName, CancellationToken cancellationToken = default)
        => Transform(input, cancellationToken);
    public byte[] Encode(ReadOnlyMemory<byte> input, string originalFileName, CancellationToken cancellationToken = default)
        => Transform(input, cancellationToken);
    private byte[] Transform(ReadOnlyMemory<byte> input, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        byte[] result = input.ToArray();
        for (int i = 0; i < result.Length; i++) result[i] ^= key;
        return result;
    }
}
