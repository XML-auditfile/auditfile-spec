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

## Release

`.github/workflows/release.yml` runs only manually, with a version such as `1.0.1` (semver, no prefix; the tag
has the same name). From `main` the version must be higher than every release; from `release/X.Y` (maintenance
of an older line, e.g. a fix `0.1.1` after `1.0.0`, branched off tag `0.1.0`) it must be in line X.Y and higher
than the latest release in that line. Only the highest stable version becomes GitHub's latest release.
The workflow creates the tag, builds, checks that `generated/` is up to date, and attaches `generated/` as
`auditfile-spec-<version>.zip` to the GitHub release. The version rules are in `tools/semver.ps1`, shared with
the other projects.

Dependabot (`.github/dependabot.yml`) checks NuGet packages and GitHub Actions weekly.

## License

Apache License 2.0. The XSD files are published by the Belastingdienst under their own terms.
