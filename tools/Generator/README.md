# Generator

Reads `generator.json`, checks the sources and writes `generated/`.

```
dotnet run --project tools/Generator              # rewrite generated/
dotnet run --project tools/Generator -- --check   # compare only (CI); exit 1 on any difference
```

Exit codes: `0` ok, `1` invalid input or differences, `2` unexpected failure.

## Steps per standard

| Step | Class |
| ---- | ----- |
| Verify SHA-256 of every official source | `Config/SourceVerifier` |
| Compile the XSD (no DTD, includes only from the source folder) | `Schema/XsdLoader` |
| Walk the schema into a plain tree with effective min/max | `Schema/SchemaTreeBuilder` |
| Apply the table rules | `Mapping/TableMapper` |
| Name tables (overlay first, then path prefixes on collision) | `Mapping/TableNamer` |
| `xs:key` / `xs:keyref` → keys and references | `Mapping/RelationMapper` |
| Merge definitions by XML path | `Mapping/DefinitionBuilder` |
| Write deterministic JSON (LF, no BOM, 2 spaces) | `Output/Json`, `Output/GeneratedFiles` |

After all standards: check every `views.json` against the manifests of its type (`Views/ViewGenerator`,
`Views/ViewChecker`) and copy it to `generated/views/<type>.json`.

## Table rules

- The root element and its complex children are tables.
- Any other complex element that can repeat is a table.
- A complex element that occurs at most once is flattened into its table; its name becomes the column prefix
  (`currency/curCode` → `currency_cur_code`).
- Simple elements and attributes are columns. A repeating simple element becomes a table with one `value` column.
- `minOccurs`/`maxOccurs` are relative to the XML parent element; `maxOccurs` is omitted when unbounded.
