using System.Text.Json;

namespace XAFtoolkit.Auditfile.Generator.Config;

/// <summary>
/// overlay.json: hand-written additions to what the XSD says.
/// All references use XML paths, e.g. "auditfile/company/customersSuppliers/customerSupplier".
/// </summary>
internal sealed record Overlay(
    IReadOnlyDictionary<string, string>? TableNames,
    IReadOnlyList<string>? ExpectedTables,
    IReadOnlyList<AliasConfig>? Aliases)
{
    public static readonly Overlay Empty = new(null, null, null);

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static Overlay Load(string root, string? relativePath)
    {
        if (relativePath is null)
        {
            return Empty;
        }

        var path = Path.Combine(root, relativePath);
        if (!File.Exists(path))
        {
            throw new GeneratorException($"overlay not found: {relativePath}");
        }

        return JsonSerializer.Deserialize<Overlay>(File.ReadAllText(path), Options) ?? Empty;
    }
}

/// <summary>A namespace used by a package instead of the official one.</summary>
internal sealed record AliasConfig(string Namespace, string? Package, string? TestCase);
