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
