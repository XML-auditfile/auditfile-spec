using System.Text.RegularExpressions;

namespace XAFtoolkit.Auditfile.Generator.Mapping;

internal static partial class Naming
{
    /// <summary>Longest identifier PostgreSQL accepts; the strictest of the supported databases.</summary>
    public const int MaxLength = 63;

    /// <summary>"custSupID" → "cust_sup_id", "glAccountHistory" → "gl_account_history".</summary>
    public static string Snake(string name)
    {
        var s = LowerUpper().Replace(name, "$1_$2");
        s = UpperUpperLower().Replace(s, "$1_$2");
        return s.Replace('-', '_').ToLowerInvariant();
    }

    public static void EnsureLength(string name, string context)
    {
        if (name.Length > MaxLength)
        {
            throw new GeneratorException($"name '{name}' is longer than {MaxLength} characters ({context}); set a shorter name in overlay.json");
        }
    }

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex LowerUpper();

    [GeneratedRegex("([A-Z]+)([A-Z][a-z])")]
    private static partial Regex UpperUpperLower();
}
