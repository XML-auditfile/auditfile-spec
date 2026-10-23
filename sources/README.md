# Sources

Input for the generator. `generator.json` in the repository root lists which files belong to
which auditfile type and version.

```
sources/<type>/<version>/
├── *.xsd               as published by the Belastingdienst (original file names)
├── *.pdf               explanatory documents
├── overlay.json        hand-written: names, practical expectations, aliases
├── definitions.json    hand-written: definition and description per column
└── dialects.json       hand-written: corrections for deviating packages
```

## Overlay

`overlay.json` uses XML paths, never generated names:

```json
{
  "tableNames":     { "auditfile/company/customersSuppliers/customerSupplier": "customer_supplier" },
  "expectedTables": [ "auditfile/company/transactions/journal" ],
  "aliases":        [ { "namespace": "...", "package": "...", "testCase": "..." } ]
}
```

`expectedTables` are optional in the XSD but expected in practice; readers report a warning when they are missing.

## Definitions

`definitions.json` is keyed by XML path. The generator turns each path into a key (`tr_line.amnt`) and a kind
(`table`, `column` or `group` for a flattened element). Unknown paths are an error.

```json
{
  "path": "auditfile/company/transactions/journal/transaction/trLine/amnt",
  "label": { "en": "Amount" },
  "definition":  { "nl": "Bedrag in locale valuta.", "en": "" },
  "description": { "nl": "...", "en": "" },
  "codes": { "D": { "en": "Debit" } }
}
```

The official text of the Belastingdienst is Dutch; English definitions are our translation.
Readers fall back to Dutch when a translation is missing.

Every `definitions.json` was extracted from the functional specification in the same folder; `source` names the PDF.
Label, definition (first paragraph), description (further paragraphs: "Nadere specificatie 2017", "Bron:",
"Functional message description", official rules such as `[0050]`) and code lists. They still need a review.

| Type | Source document | Label language |
| ---- | --------------- | -------------- |
| XAF 3.2, 4.0.3 | `*_FunHie.pdf` | English |
| XAA 3.2 | `XmlAuditfileAfrekensystemen3.21_spec.pdf` (folder `XAA_3.24`) | English |
| XAB 1.0 | `XmlAuditfileBct1.0_spec.pdf` | Dutch, English codes |
| XAR 1.2, 2.3.6.1 | `XmlAuditfileRrs1.2_spec.pdf`, `XmlAuditfileXAR_2.3.6.pdf` | Dutch, English codes |
| XAS 2018–2027 | `AuditfileSalaris-Versie<year>_FunHie*.pdf` (without revisions) | Dutch |

Paths the document does not describe (usually elements newer than the PDF, such as `@Id`) have no entry.

Every Dutch text (label, definition, description, code) has an English translation made with AI; Dutch remains
leading. Leftovers from the PDF layout (page dates, "© XML Platform", appended appendices) were removed.

## Diagnostics

Diagnostic codes for all types live in `sources/diagnostics.json` (hand-written, NL + EN).
`officialRules` lists, per standard, the rule numbers from the official specification that a code covers
(e.g. XAR 2.3.6 `[0050]` → `XAF-R009`). Rules that need external data (BSN exists, name belongs to BSN,
receipt date at the Belastingdienst) have no code.

The hand-written files are optional; add them to `generator.json` once they exist.

## Output

The generator writes to `generated/`, with fixed names:

```
generated/
├── registry.json
├── diagnostics.json
└── standards/<type>/<version_with_underscores>/
    ├── auditfile.xsd
    ├── manifest.json
    ├── definition.json
    └── dialects.json
```

Never edit `generated/` by hand. CI regenerates and fails on any difference.
