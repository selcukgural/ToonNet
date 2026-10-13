# TOON spec v4.4 conformance fixtures

Copied unchanged from [toon-format/spec](https://github.com/toon-format/spec) at tag `v4.4.0`
(commit `d09c3d3`, 2026-10-08), directory `tests/fixtures` (and `tests/fixtures.schema.json`). Licensed under the
MIT License, see [LICENSE](LICENSE).

They are executed by `SpecFixtureConformanceTests`, which follows the upstream rules:

- cases are identified by file and index (`decode/objects/12`), never by name or note, which upstream rewrites;
- cases whose `minSpecVersion` is newer than the targeted version are skipped;
- the options are `delimiter`, `indentSize` and `strict`.

To update, replace the `encode/` and `decode/` folders with the fixtures of the new tag, rerun the tests and
regenerate `KnownNonConformance.txt` with `TOONNET_WRITE_NONCONFORMANCE` (see the test class).
