# Samples

Small examples of every file a reader receives from `generated/`. They show the shape of the files;
readers never use this folder. Manifest, registry and definition are trimmed copies of real output for XAF 3.2.
Dialects show the proposed format; diagnostics is a subset of the real list in `sources/diagnostics.json`.

```
samples/generated/
├── registry.json
├── diagnostics.json
└── standards/xaf/3_2/
    ├── manifest.json
    ├── definition.json
    └── dialects.json
```

Every file has a `formatVersion`. A reader refuses a format version it does not know.

## registry.json

One entry per type and version.

| Field | Meaning |
| ----- | ------- |
| `key` | `xaf/3.2` — used everywhere to refer to a version |
| `typeName` | `financial` — basis for version object names in every language |
| `folder` | Where the files of this version are, relative to `generated/` |
| `namespaces` | The official namespace and any aliases (`kind`: `official` or `alias`) |
| `origin` | Publisher, URL and publication date of the official sources |

## manifest.json

The structure of one version: which tables and columns exist.

| Field | Meaning |
| ----- | ------- |
| `requiredTables` | Required by the XSD; missing means not a valid audit file |
| `expectedTables` | Optional in the XSD but expected in practice; missing gives a warning |
| `tables[].path` | XML path of the table element — its real identity |
| `tables[].parent` | Name of the parent table (omitted for the root) |
| `tables[].minOccurs` / `maxOccurs` | Relative to the XML parent element; `maxOccurs` omitted means unbounded |
| `tables[].keys` | Natural keys from `xs:key` |
| `tables[].references` | References from `xs:keyref`: own columns → key of another table |
| `columns[].path` | Relative to the table element: `amnt`, `currency/curCode`, `@attr`, or `.` for a value table |
| `columns[].type` | `string`, `decimal`, `integer`, `date`, `datetime`, `time` or `boolean` |

## definition.json

Text per table, column and flattened group, for documentation, database comments and viewers.

| Field | Meaning |
| ----- | ------- |
| `key` | `tr_line` (table), `tr_line.amnt` (column) or `tr_line.currency` (group) |
| `kind` | `table`, `column` or `group` |
| `label` | Official English name from the functional specification |
| `definition` | Meaning, `nl` (official) and `en` (our translation) |
| `description` | Practical explanation, including the 2017 length restrictions |
| `codes` | Meaning of code values, e.g. `D` = Debit |

Missing English text falls back to Dutch.

## dialects.json

Corrections for packages that deviate from the standard. Applied in tolerant mode, before values reach a table;
every applied correction is reported with its code.

| Field | Meaning |
| ----- | ------- |
| `match.namespaces` | Namespaces that select this dialect (usually an alias) |
| `match.softwareDesc` | Value of `header/softwareDesc` that selects this dialect |
| `corrections[].action` | `replaceNamespace`, `decimalComma`, `dateFromDateTime`, … (fixed set, same in every language) |
| `corrections[].paths` | XML paths the correction applies to |
| `testCase` | Conformance case that proves the dialect |

## diagnostics.json

All diagnostic codes, shared by every reader.

| Code family | Layer |
| ----------- | ----- |
| `XAF-X` | XML: well-formed, encoding, characters |
| `XAF-E` | Envelope and digital signature (XAA, XAB, XAR) |
| `XAF-V` | Version resolution: namespace, alias, forced version |
| `XAF-S` | Structure: required tables and fields, occurrences, unknown elements, invalid values, code lists |
| `XAF-D` | XSD validation |
| `XAF-R` | Business rules |
| `XAF-C` | Corrections from a dialect |

Messages contain placeholders such as `{table}`, filled in by the reader, in Dutch and English.
`officialRules` (optional) maps a code to rule numbers in the official specification, per standard key.
