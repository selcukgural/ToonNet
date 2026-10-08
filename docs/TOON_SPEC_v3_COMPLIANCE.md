# TOON Spec Conformance

ToonNet implements **[TOON spec v3.3.2](https://github.com/toon-format/spec/blob/v3.3.2/SPEC.md)**
(toon-format/spec, tag `v3.3.2`). This page describes how conformance is verified and documents the
implementation-defined behaviour that the spec requires implementations to document. For the format itself,
read the spec; this page does not restate it.

## ToonNet Implementation Status

### Measured conformance

ToonNet runs the official conformance fixtures of TOON spec **v3.3.2**
(`tests/ToonNet.Tests/SpecCompliance/Fixtures/v3.3.2`, 378 cases excluding the optional
key folding and path expansion features). Cases that do not pass yet are listed in
`tests/ToonNet.Tests/SpecCompliance/KnownNonConformance.txt`; CI fails if a listed case starts
passing without being removed, so the list always reflects the current state.

| Fixture set | Cases | Passing |
|-------------|-------|---------|
| Encode | 142 | 142 |
| Decode | 236 | 236 |
| **Total** | **378** | **378 (100%)** |

Not implemented (optional, §13.4): key folding (`keyFolding`, `flattenDepth`) and path expansion (`expandPaths`).
Dotted keys such as `user.name` are always read and written as literal keys.

TOON spec v4 is not supported yet.

## Options

| Spec option | ToonNet | Default |
|-------------|---------|---------|
| `indentSize` | `ToonOptions.IndentSize` (even, 2–100) | 2 |
| `delimiter` | `ToonOptions.Delimiter` (`,`, `\t`, `\|`) | `,` |
| `strict` | `ToonOptions.StrictMode` | `true` |
| `keyFolding`, `flattenDepth`, `expandPaths` | not implemented | off |

`ToonOptions.MaxDepth` (default 100) additionally limits nesting when encoding and decoding.

## Implementation-defined behaviour

### Host type normalization (§3)

The mapping from .NET types to the TOON data model is documented in
[Type System – .NET Type Mapping](../website/docs/core-features/type-system.md#net-type-mapping).
NaN and ±Infinity are encoded as `null`. Custom mappings use `IToonConverter` / `[ToonConverter]`.

### Numbers (§2, §4)

- **Numeric domain:** `ToonNumber` holds an IEEE 754 `double` and, when the value is exactly representable,
  a `decimal` (`ToonNumber.DecimalValue`, 28–29 significant digits, |n| < 7.9e28).
- **Encoding:** `double` values use the shortest representation that round-trips. Values with 1e-6 ≤ |n| < 1e21 are
  written in plain decimal form; others use exponent form with a lowercase `e` and explicit sign (`1e-7`, `1e+21`).
  `decimal`, `long` and `ulong` values are always written in plain decimal form with their exact digits.
  Integers outside the `decimal` range (`BigInteger`, `Int128`, `UInt128`) are written as quoted decimal strings.
- **Decoding / out-of-range policy:** tokens matching the JSON number grammar become numbers. A number that fits in a
  `decimal` keeps its exact value; otherwise it is approximated as a `double`. A numeric token that overflows `double`
  (for example `1e400`) decodes as a **string**, not as infinity. When binding to .NET integer types, fractions and
  out-of-range values throw `ToonSerializationException` rather than being truncated.
- `-0` decodes as `0`.

### Indentation and tabs (§12)

- Strict mode: leading spaces must be a multiple of `IndentSize`; a tab in indentation is an error.
- Non-strict mode: depth is `floor(spaces / IndentSize)` and **a tab in indentation counts as `IndentSize` spaces**.
- Lines containing only spaces and tabs are blank lines regardless of their indentation.
- `\r\n` line endings are accepted when decoding; the encoder always writes `\n`.

### Non-strict mode

In addition to the relaxations allowed by §14, non-strict mode:

- reads malformed headers such as `foo[bar]: 1` as literal keys (§6),
- resolves duplicate keys with last-write-wins (§14.4),
- ignores blank lines inside arrays and does not check `[N]` counts or row widths,
- fills missing tabular cells with `null` and ignores extra cells,
- reads list items under a bare `key:` (without an `[N]` header) as an array. In strict mode this is an error
  whose message suggests the `key[N]:` header.

### Encoder choices

- Empty arrays use the canonical `key: []` / `[]` forms (§9.1); inner arrays of list items use `[0]:` (§9.2).
- The first field of a list-item object is written on the hyphen line (§10).
- Strings are escaped per §7.1; other control characters use lowercase `\uXXXX`.
- Output has LF line endings, no trailing spaces and no trailing newline.

### Limits and errors

- Errors are reported as `ToonParseException` with line and column. Deserialization errors (type mismatches) are
  `ToonSerializationException` with the path of the value, e.g. `$.items[2].price`.
- Nesting beyond `MaxDepth`, or beyond the available stack space, is rejected with an exception instead of
  overflowing the stack.

## Updating to a newer spec version

1. Replace `tests/ToonNet.Tests/SpecCompliance/Fixtures/<version>` with the fixtures of the new tag and update
   `SpecVersion` in `SpecFixtureConformanceTests`.
2. Regenerate `KnownNonConformance.txt` (see `tests/ToonNet.Tests/README.md`) and fix the listed cases.
3. Update this page and the README badge.
