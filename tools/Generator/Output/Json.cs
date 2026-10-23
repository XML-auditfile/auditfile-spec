using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace XAFtoolkit.Auditfile.Generator.Output;

/// <summary>Deterministic JSON: 2-space indent, LF line endings, UTF-8 without BOM, final newline.</summary>
internal static class Json
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        IndentSize = 2,
        NewLine = "\n",
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static byte[] Serialize<T>(T value) => Encode(JsonSerializer.Serialize(value, Options));

    public static byte[] Serialize(JsonNode node) => Encode(node.ToJsonString(Options));

    /// <summary>Reads a hand-written JSON file and writes it back in the deterministic format.</summary>
    public static byte[] Normalize(string path)
    {
        var node = JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip })
            ?? throw new GeneratorException($"{path} is empty");
        return Serialize(node);
    }

    private static byte[] Encode(string json) => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(json + "\n");
}
