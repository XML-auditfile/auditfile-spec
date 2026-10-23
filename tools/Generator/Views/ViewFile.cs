using System.Text.Json;
using System.Text.Json.Serialization;

namespace XAFtoolkit.Auditfile.Generator.Views;

/// <summary>
/// views.json: hand-written views for one audit file type, valid for every version whose manifest has the tables
/// and columns they use. Readers get the same file as generated/views/&lt;type&gt;.json.
/// </summary>
internal sealed record ViewFile(int FormatVersion, string Type, IReadOnlyList<View> Views)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ViewFile Load(string root, string relativePath)
    {
        var path = Path.Combine(root, relativePath);
        if (!File.Exists(path))
        {
            throw new GeneratorException($"views not found: {relativePath}");
        }

        try
        {
            return JsonSerializer.Deserialize<ViewFile>(File.ReadAllText(path), Options)
                ?? throw new GeneratorException($"{relativePath} is empty");
        }
        catch (JsonException ex)
        {
            throw new GeneratorException($"{relativePath}: {ex.Message}");
        }
    }
}

/// <summary>
/// One view: rows of <see cref="Source"/>, left-joined with <see cref="Joins"/>, filtered by the parameters that are
/// given, and grouped by every column without an aggregate when at least one column has one.
/// </summary>
internal sealed record View(
    string Name,
    IReadOnlyDictionary<string, string>? Label,
    IReadOnlyDictionary<string, string>? Description,
    string Source,
    IReadOnlyList<ViewJoin>? Joins,
    IReadOnlyList<ViewParameter>? Parameters,
    IReadOnlyList<ViewColumn> Columns,
    IReadOnlyList<string>? OrderBy)
{
    /// <summary>Every "table.column" the view uses.</summary>
    public IEnumerable<string> References() =>
        (Joins ?? []).SelectMany(j => j.On ?? []).SelectMany(pair => pair)
            .Concat((Parameters ?? []).Select(p => p.Column))
            .Concat(Columns.SelectMany(c => c.Value?.References() ?? []))
            .Distinct(StringComparer.Ordinal);
}

/// <summary>
/// Without <see cref="On"/> the table must be an ancestor of the source table and is joined through the parent row.
/// With <see cref="On"/> each pair is [column of an earlier table, column of this table].
/// </summary>
internal sealed record ViewJoin(string Table, IReadOnlyList<IReadOnlyList<string>>? On);

/// <summary>An optional filter: rows match when <c>column operator argument</c> holds. Omitted means no filter.</summary>
internal sealed record ViewParameter(string Name, IReadOnlyDictionary<string, string>? Label, string Column, string Operator);

/// <summary>Aggregate is sum, count, min or max; count without a value counts rows.</summary>
internal sealed record ViewColumn(string Name, IReadOnlyDictionary<string, string>? Label, string? Aggregate, ViewValue? Value);

/// <summary>
/// A column reference ("tr_line.amnt") or an operation on one:
/// <c>signed</c> negates the value when <see cref="By"/> is in <see cref="Negative"/>;
/// <c>only</c> keeps the value when <see cref="By"/> is in <see cref="In"/> and is null otherwise.
/// </summary>
[JsonConverter(typeof(ViewValueConverter))]
internal sealed record ViewValue(string? Op, string Value, string? By, IReadOnlyList<string>? Negative, IReadOnlyList<string>? In)
{
    public IEnumerable<string> References() => By is null ? [Value] : [Value, By];
}

internal sealed class ViewValueConverter : JsonConverter<ViewValue>
{
    public override ViewValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new ViewValue(null, reader.GetString()!, null, null, null);
        }

        var operation = JsonSerializer.Deserialize<Operation>(ref reader, options)
            ?? throw new JsonException("value is null");
        return new ViewValue(operation.Op, operation.Value, operation.By, operation.Negative, operation.In);
    }

    public override void Write(Utf8JsonWriter writer, ViewValue value, JsonSerializerOptions options) =>
        throw new NotSupportedException();

    private sealed record Operation(string Op, string Value, string? By, IReadOnlyList<string>? Negative, IReadOnlyList<string>? In);
}

/// <summary>"table.column" → (table, column).</summary>
internal static class ColumnReference
{
    public static bool TryParse(string? reference, out string table, out string column)
    {
        var dot = reference?.IndexOf('.', StringComparison.Ordinal) ?? -1;
        table = dot > 0 ? reference![..dot] : "";
        column = dot > 0 ? reference![(dot + 1)..] : "";
        return dot > 0 && column.Length > 0 && !column.Contains('.', StringComparison.Ordinal);
    }
}
