using System.Text.Json;

namespace XAFtoolkit.Auditfile.Generator.Config;

/// <summary>generator.json: which sources belong to which audit file type and version.</summary>
internal sealed record GeneratorConfig(IReadOnlyList<StandardConfig> Standards, string? Diagnostics, IReadOnlyList<string>? Views)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static GeneratorConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new GeneratorException($"not found: {path}");
        }

        var config = JsonSerializer.Deserialize<GeneratorConfig>(File.ReadAllText(path), Options)
            ?? throw new GeneratorException("generator.json is empty");

        var duplicates = config.Standards.GroupBy(s => s.Key).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
        {
            throw new GeneratorException($"duplicate standards in generator.json: {string.Join(", ", duplicates)}");
        }

        return config;
    }
}

internal sealed record StandardConfig(
    string Type,
    string TypeName,
    string Version,
    string Provenance,
    string RootElement,
    OriginConfig? Origin,
    SourcesConfig Sources,
    IReadOnlyDictionary<string, string> Checksums)
{
    /// <summary>Registry key, e.g. "xaf/3.2".</summary>
    public string Key => $"{Type}/{Version}";

    /// <summary>Folder in generated/, e.g. "standards/xaf/3_2".</summary>
    public string Folder => $"standards/{Type}/{Version.Replace('.', '_')}";
}

internal sealed record OriginConfig(string? Publisher, string? Url, string? Published);

internal sealed record SourcesConfig(
    XsdSourceConfig Xsd,
    IReadOnlyList<string>? Documents,
    string? Overlay,
    string? Definitions,
    string? Dialects);

internal sealed record XsdSourceConfig(string Main, IReadOnlyList<string>? Includes);
