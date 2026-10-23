using System.Xml.Schema;
using XAFtoolkit.Auditfile.Generator.Schema;

namespace XAFtoolkit.Auditfile.Generator.Mapping;

/// <summary>
/// Applies the table rules to a SchemaNode tree:
/// - the root element and its complex children are tables;
/// - any other complex element that can repeat is a table;
/// - a complex element that occurs at most once is flattened into its table, with its name as column prefix;
/// - simple elements and attributes are columns; a repeating simple element becomes a table with one "value" column.
/// </summary>
internal sealed class TableMapper
{
    private readonly List<TableBuilder> _tables = [];

    /// <summary>Every mapped XML path in document order, for definitions: path → (table, column or group prefix).</summary>
    public List<MappedPath> Paths { get; } = [];

    public IReadOnlyList<TableBuilder> Tables => _tables;

    public static TableMapper Map(SchemaNode root, IReadOnlyDictionary<string, string> fixedNames)
    {
        var mapper = new TableMapper();
        mapper.AddTable(root, parent: null, required: true, isRoot: true);
        TableNamer.Assign(mapper._tables, fixedNames);
        mapper.CheckColumns();
        return mapper;
    }

    private void AddTable(SchemaNode node, TableBuilder? parent, bool required, bool isRoot = false)
    {
        var table = new TableBuilder(node.Name, node.Path, parent, (int)node.MinOccurs, ToInt(node.MaxOccurs), required);
        _tables.Add(table);
        Paths.Add(new(node.Path, table, Column: null, Group: null));

        if (node.Text is not null)
        {
            AddColumn(table, "value", node.Name, ".", "element", node.Text, required ? 1 : 0, node.Path);
        }

        AddContent(node, table, prefix: "", relative: "", required, childrenAreTables: isRoot);
    }

    private void AddContent(SchemaNode node, TableBuilder table, string prefix, string relative, bool required, bool childrenAreTables)
    {
        foreach (var attribute in node.Attributes)
        {
            AddColumn(table, prefix + Naming.Snake(attribute.Name), attribute.Name, $"{relative}@{attribute.Name}", "attribute",
                attribute.Type, attribute.Required ? 1 : 0, $"{node.Path}/@{attribute.Name}");
        }

        foreach (var child in node.Children)
        {
            var childRequired = required && child.MinOccurs >= 1;

            if (child.IsComplex)
            {
                if (childrenAreTables || child.Repeats)
                {
                    AddTable(child, table, childRequired);
                }
                else
                {
                    var groupPrefix = prefix + Naming.Snake(child.Name);
                    Paths.Add(new(child.Path, table, Column: null, Group: groupPrefix));
                    AddContent(child, table, groupPrefix + "_", $"{relative}{child.Name}/", childRequired, childrenAreTables: false);
                }
            }
            else if (child.Repeats)
            {
                var valueTable = new TableBuilder(child.Name, child.Path, table, (int)child.MinOccurs, ToInt(child.MaxOccurs), childRequired);
                _tables.Add(valueTable);
                Paths.Add(new(child.Path, valueTable, Column: null, Group: null));
                AddColumn(valueTable, "value", child.Name, ".", "element", child.Text!, 1, child.Path);
            }
            else
            {
                AddColumn(table, prefix + Naming.Snake(child.Name), child.Name, relative + child.Name, "element",
                    child.Text!, (int)child.MinOccurs, child.Path);
            }
        }
    }

    private void AddColumn(TableBuilder table, string name, string xmlName, string path, string kind, SimpleTypeInfo type, int minOccurs, string absolutePath)
    {
        var column = new ManifestColumn(name, xmlName, path, kind, LogicalType(type.TypeCode), type.XsdTypeName, minOccurs);
        table.Columns.Add(column);
        Paths.Add(new(absolutePath, table, column, Group: null));
    }

    private void CheckColumns()
    {
        foreach (var table in _tables)
        {
            var duplicate = table.Columns.GroupBy(c => c.Name).FirstOrDefault(g => g.Count() > 1);
            if (duplicate is not null)
            {
                throw new GeneratorException($"table '{table.Name}' has two columns named '{duplicate.Key}'");
            }

            foreach (var column in table.Columns)
            {
                Naming.EnsureLength(column.Name, $"column of {table.Name}");
            }
        }
    }

    private static int? ToInt(decimal? value) => value is null ? null : (int)value;

    private static string LogicalType(XmlTypeCode code) => code switch
    {
        XmlTypeCode.Decimal => "decimal",
        XmlTypeCode.Integer or XmlTypeCode.Int or XmlTypeCode.Long or XmlTypeCode.Short or XmlTypeCode.Byte
            or XmlTypeCode.NonNegativeInteger or XmlTypeCode.PositiveInteger or XmlTypeCode.NonPositiveInteger
            or XmlTypeCode.NegativeInteger or XmlTypeCode.UnsignedLong or XmlTypeCode.UnsignedInt
            or XmlTypeCode.UnsignedShort or XmlTypeCode.UnsignedByte => "integer",
        XmlTypeCode.Date => "date",
        XmlTypeCode.DateTime => "datetime",
        XmlTypeCode.Time => "time",
        XmlTypeCode.Boolean => "boolean",
        _ => "string",
    };
}

internal sealed class TableBuilder(string xmlName, string path, TableBuilder? parent, int minOccurs, int? maxOccurs, bool required)
{
    public string XmlName { get; } = xmlName;
    public string Path { get; } = path;
    public TableBuilder? Parent { get; } = parent;
    public int MinOccurs { get; } = minOccurs;
    public int? MaxOccurs { get; } = maxOccurs;

    /// <summary>Required by the standard: this element and all its ancestors have minOccurs ≥ 1.</summary>
    public bool Required { get; } = required;

    public string Name { get; set; } = "";
    public List<ManifestColumn> Columns { get; } = [];
    public List<ManifestKey> Keys { get; } = [];
    public List<ManifestReference> References { get; } = [];
}

/// <summary>An XML path and where it ended up: a table, a column, or a flattened group (column prefix).</summary>
internal sealed record MappedPath(string Path, TableBuilder Table, ManifestColumn? Column, string? Group);
