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

    /// <summary>
    /// Restores the verified UnrealScript native boolean-negation operator (native 129).
    /// UELib emits it as __NFUN_129__(expression); Interlude UCC expects !expression.
    /// Nested native calls are transformed recursively, while strings and comments are preserved.
    /// </summary>
    public static string RepairKnownNativeUnaryOperators(string source, out int replacements)
    {
        ArgumentNullException.ThrowIfNull(source);
        replacements = 0;
        return RepairUnarySegment(source, ref replacements);
    }

    private static string RepairUnarySegment(string source, ref int replacements)
    {
        const string marker = "__NFUN_129__";
        var output = new System.Text.StringBuilder(source.Length);
        int i = 0;
        while (i < source.Length)
        {
            if (source.AsSpan(i).StartsWith(marker, StringComparison.Ordinal) &&
                i + marker.Length < source.Length && source[i + marker.Length] == '(' &&
                TryFindUnaryCallEnd(source, i + marker.Length, out int close, out int comma))
            {
                if (!comma.Equals(-1))
                {
                    output.Append(source, i, close - i + 1);
                }
                else
                {
                    string operand = source.Substring(i + marker.Length + 1,
                        close - (i + marker.Length + 1));
                    string repairedOperand = RepairUnarySegment(operand, ref replacements);
                    output.Append("!(").Append(repairedOperand).Append(')');
                    replacements++;
                }
                i = close + 1;
                continue;
            }
            output.Append(source[i]);
            i++;
        }
        return output.ToString();
    }

    private static bool TryFindUnaryCallEnd(string source, int open, out int close, out int topLevelComma)
    {
        close = -1;
        topLevelComma = -1;
        int depth = 0;
        char quote = '\\0';
        bool inString = false, inLineComment = false, inBlockComment = false;
        for (int i = open; i < source.Length; i++)
        {
            char c = source[i];
            char next = i + 1 < source.Length ? source[i + 1] : '\\0';
            if (inLineComment)
            {
                if (c == '\\n') inLineComment = false;
                continue;
            }
            if (inBlockComment)
            {
                if (c == '*' && next == '/') { inBlockComment = false; i++; }
                continue;
            }
            if (inString)
            {
                if (c == '\\\\') { i++; continue; }
                if (c == quote) inString = false;
                continue;
            }
            if (c == '/' && next == '/') { inLineComment = true; i++; continue; }
            if (c == '/' && next == '*') { inBlockComment = true; i++; continue; }
            if (c is '"' or '\\'') { inString = true; quote = c; continue; }
            if (c == '(') depth++;
            else if (c == ')')
            {
                depth--;
                if (depth == 0) { close = i; return true; }
            }
            else if (c == ',' && depth == 1 && topLevelComma < 0) topLevelComma = i;
        }
        return false;
    }

}
