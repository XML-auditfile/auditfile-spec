# Sources

Input for the generator. `generator.json` in the repository root lists which files belong to
which auditfile type and version.

## Output

The generator writes to `generated/`, with fixed names:

```
generated/
├── registry.json
├── diagnostics.json
├── views/<type>.json
└── standards/<type>/<version_with_underscores>/
    ├── auditfile.xsd
    ├── manifest.json
    ├── definition.json
    └── dialects.json
```

Never edit `generated/` by hand. CI regenerates and fails on any difference.

## Views

`<type>/views.json` holds hand-written views for one audit file type. A view is available in every version whose
manifest has the tables and columns it uses; readers report `XAF-Q001` for any other version.

| Field | Meaning |
| ----- | ------- |
| `source` | The table with one input row per row of the view (before grouping) |
| `joins` | Left joins, in order. Without `on` the table must be an ancestor of the source. With `on`, each pair is `[earlier column, column of this table]`. `alternatives` names the same table in versions that call it differently: the first one the version has is joined, still referred to by `table` |
| `parameters` | Optional filters: `column operator value`, with operator `=`, `>=` or `<=` |
| `columns` | Output columns. When any column has an `aggregate` (`sum`, `count`, `min`, `max`), the others group |
| `orderBy` | Output column names to sort by; without it rows keep the order of the source table |

A column `value` is a reference (`"tr_line.acc_id"`) or an operation on one:

| `op` | Fields | Result |
| ---- | ------ | ------ |
| `signed` | `value`, `by`, `negative` | The value, negated when `by` is one of `negative` (an amount with `C` negative) |
| `only` | `value`, `by`, `in` | The value when `by` is one of `in`, otherwise null (debit or credit only) |
| `pseudonym` | `value`, `prefix` | `<prefix>-n` instead of the value; n numbers the distinct values of the column in the order they first occur in its table, so a placeholder does not depend on filters. Null and empty values stay as they are |
