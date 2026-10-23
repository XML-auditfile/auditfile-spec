# auditfile-spec

Open specification for reading Dutch tax audit files (XAF, XAS, XAA, XAR, XAB, XAK) as tables.
Readers use only `generated/`: JSON and XSD files with fixed names. Every reader includes them
from a pinned release and adds its own code.

> Status: draft (0.x).
> Not affiliated with the Belastingdienst, SRA or any software vendor.

## Layout

| Path | Contents |
| ---- | -------- |
| `SPEC.md` | Normative rules |
| `generator.json` | Which sources belong to which type and version |
| `sources/` | Official XSD files and documents, plus hand-written overlays, definitions and dialects |
| `generated/` | Generator output used by every reader — never edit by hand |
| `samples/` | Small examples of every generated file, with explanation |
| `schema/` | JSON Schemas for all JSON files |
| `conformance/` | Test cases and expected output (fictional data only) |
| `docs/` | Background and references (repository only) |
| `tools/Generator/` | `generator.json` → `generated/` |

## Build

Requires the .NET 10 SDK.

```
dotnet build auditfile-spec.slnx
```

## License

Apache License 2.0. The XSD files are published by the Belastingdienst under their own terms.
