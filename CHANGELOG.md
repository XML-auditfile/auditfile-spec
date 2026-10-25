# Changelog

All notable changes to this project are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and the project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- Generator: XSD + overlay + definitions → manifest, definition, dialects, registry and diagnostics in `generated/`.
- XAF 3.2: official sources, overlay with readable table names, definitions extracted from the functional specification.
- Diagnostic codes for all readers.
- Samples of every generated file.
- XAA 3.2 and XAS 2021.1.1 in `generator.json`.
- Definitions for every standard, extracted from the functional specification PDF in each source folder,
  with English translations of all labels, definitions, descriptions and codes.
- Diagnostic codes for encoding and characters (`XAF-X004`–`X005`), envelope and signature (`XAF-E`), required fields,
  occurrences, length and code lists (`XAF-S006`–`S009`) and the rule types of the official specifications
  (`XAF-R005`–`R016`), with `officialRules` references.
- Views per type in `generated/views/<type>.json`; readers decide from the manifest whether a view is available
  (`XAF-Q001`–`Q002`). XAF: trial balance, ledger entries, journal totals, VAT summary (3.2 only).
  XAS: employees, payroll summary, wage components, hours.
- Views: a join can name `alternatives` for a table that has another name in some versions; the value operation
  `pseudonym` replaces a value by `<prefix>-n`, numbered per distinct value in the order of its table.
- XAF views `ledger` (journal down to line, with account description, customer or supplier name and a signed
  amount) and `ledger_pseudonymized` (the same, with every description and name replaced by a placeholder).
- `XAF-R017` (warning): the reference check is incomplete because a reader stopped keeping unresolved values
  after its limit, so a reference to nothing after that point is not reported.

### Fixed
- Generator: an `xs:keyref` whose selector points into a flattened group (XAA `cashtransaction/rounding`)
  now resolves to the nearest table.
