using System.Text.RegularExpressions;

namespace MethosUEngine;

/// <summary>
/// Restores native operators in the recovered Silent Interface address check.
/// Meanings are verified against the matching Interlude reference source.
/// </summary>
public static class UnrealScriptKnownNativeOperatorRepair
{
    private static readonly Regex AddressCondition = new(
        """__NFUN_132__\(__NFUN_132__\(__NFUN_122__\((?<left>[A-Za-z_]\w*), (?<right>[A-Za-z_]\w*)\), __NFUN_122__\(\k<left>, "(?<ip1>192\.168\.10\.121)"\)\), __NFUN_122__\(\k<left>, "(?<ip2>127\.0\.0\.1)"\)\)""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string RepairKnownSilentInterfaceAddressCheck(string source, out int replacements)
    {
        ArgumentNullException.ThrowIfNull(source);
        int replacementCount = 0;
        replacements = 0;
        if (!source.Contains("SILENT INTERFACE - for local test server", StringComparison.Ordinal) ||
            !source.Contains("192.168.10.121", StringComparison.Ordinal) ||
            !source.Contains("127.0.0.1", StringComparison.Ordinal))
            return source;

        string result = AddressCondition.Replace(source, match =>
        {
            replacementCount++;
            string left = match.Groups["left"].Value;
            string right = match.Groups["right"].Value;
            string ip1 = match.Groups["ip1"].Value;
            string ip2 = match.Groups["ip2"].Value;
            return $"(({left} == {right}) || ({left} == \"{ip1}\") || ({left} == \"{ip2}\"))";
        });

        result = result.Replace("__NFUN_122__(MethosRecoveredName_5448, \"127.0.0.1\")",
            "MethosRecoveredName_5448 == \"127.0.0.1\"", StringComparison.Ordinal);
        result = result.Replace("__NFUN_144__(15, 1000)", "(15 * 1000)", StringComparison.Ordinal);
        replacements = replacementCount;
        return result;
    }
}
