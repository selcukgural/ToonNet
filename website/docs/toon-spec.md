# TOON Spec Conformance

ToonNet implements **[TOON spec v4.4](https://github.com/toon-format/spec/blob/v4.4.0/SPEC.md)**
(toon-format/spec, tag `v4.4.0`). This page describes how conformance is verified and documents the
implementation-defined behaviour that the spec requires implementations to document. For the format itself,
read the spec; this page does not restate it.

## ToonNet Implementation Status

### Measured conformance

ToonNet runs the official conformance fixtures of TOON spec **v4.4** in CI
(`tests/ToonNet.Tests/SpecCompliance/Fixtures/v4.4`). Cases that do not pass would be listed in
`tests/ToonNet.Tests/SpecCompliance/KnownNonConformance.txt`; CI fails if a listed case starts passing without being
removed, so the list always reflects the current state. It is empty.

| Fixture set | Cases | Passing |
|-------------|-------|---------|
| Encode | 160 | 160 |
| Decode | 404 | 404 |
| **Total** | **564** | **564 (100%)** |

Every part of the spec is implemented, including comment lines (§5.1), nested field groups (§9.3), keyed tabular
objects (§9.5) and the five non-strict recoveries (§14.4). Spec v4 has no optional features (key folding and path
expansion were removed), so dotted keys such as `user.name` are always literal keys.

### Compatibility with the official .NET library

`tests/ToonNet.Compatibility.Tests` runs with every build against
[Toon.Format](https://www.nuget.org/packages/Toon.Format), the official .NET implementation: both pass all 564
fixtures, encode thousands of random JSON documents to identical text and read each other's output. The differences
that remain are in how .NET values map to the data model (see [the README](https://github.com/selcukgural/ToonNet#toonnet-vs-the-official-toonformat-library)):
ToonNet writes enum names and keeps every digit of `decimal` and `ulong` values.

## Options

| Spec option | ToonNet | Default |
|-------------|---------|---------|
| `indentSize` | `ToonOptions.IndentSize` (1–100) | 2 |
| `delimiter` | `ToonOptions.Delimiter` (`,`, `\t`, `\|`) | `,` |
| `strict` | `ToonOptions.StrictMode` | `true` |

`ToonOptions.MaxDepth` (default 100) additionally limits nesting when encoding and decoding; nested field groups
count as levels.

## Implementation-defined behaviour

### Host type normalization (§3)

The mapping from .NET types to the TOON data model is documented in
[Type System – .NET Type Mapping](core-features/type-system#net-type-mapping). In short:
enums are written by name, `DateTime`/`DateTimeOffset` as ISO 8601 strings without trailing zeros in the fraction
of a second (`2026-01-02T03:04:05Z`), `Guid`, `TimeSpan`, `DateOnly` and `TimeOnly` as strings, dictionaries as
objects and other collections as arrays. NaN and ±Infinity are encoded as `null`. A string with an unpaired surrogate
cannot be represented and throws `ToonEncodingException`. Custom mappings use `IToonConverter` / `[ToonConverter]`.

### Numbers (§2, §4)

- **Numeric domain:** `ToonNumber` holds an IEEE 754 `double` and, when the value is exactly representable,
  a `decimal` (`ToonNumber.DecimalValue`, 28–29 significant digits, |n| < 7.9e28).
- **Encoding:** values with 1e-6 ≤ |n| < 1e21 are written in plain decimal form; others use exponent form with a
  lowercase `e` and explicit sign (`1e-7`, `1.5e+21`), as §2 recommends. `double` values use the shortest
  representation that round-trips; `decimal`, `long` and `ulong` values keep all their digits. Integers outside the
  `decimal` range (`BigInteger`, `Int128`, `UInt128`) are written as quoted decimal strings (lossless).
- **Decoding / out-of-range policy:** tokens matching the JSON number grammar become numbers. A number that fits in a
  `decimal` keeps its exact value; otherwise it is approximated as a `double`. A numeric token that overflows `double`
  (for example `1e400`) decodes as a **string**, not as infinity. When binding to .NET integer types, fractions and
  out-of-range values throw `ToonSerializationException` rather than being truncated.
- `-0` decodes as the number zero: `0` in integers, `decimal` and `object`, negative zero in `double` and `float`
  (as `double.Parse` and System.Text.Json give it). It is written as `0`.

### Key order and keys (§2, §15)

`ToonObject` keeps keys in document order, and objects are bound in that order. Every key, including `__proto__`, is
an ordinary key. Keys that differ only in Unicode normalization are distinct.

### Byte input (§4)

Streams and files are decoded as UTF-8; ill-formed UTF-8 is an error (`ToonParseException`), never U+FFFD. A
byte-order mark at the start is removed; one anywhere else is content.

### Non-strict mode (§14.4)

Non-strict mode applies exactly the five recoveries of §14.4 and reports every other error like strict mode:

1. declared counts (`[N]`) are not checked; every value, item, row and entry row is read,
2. duplicate keys and repeated field names resolve by last-write-wins (the key keeps its first position),
3. a line's depth is its tabs plus `floor(spaces / IndentSize)`, and a line of only spaces and tabs is blank,
4. blank lines inside arrays and keyed objects are ignored,
5. a nested scope whose first line is indented too deep takes that depth.

Row widths are checked in both modes.

### Encoder choices

- The form follows the value's shape (§9): uniform objects use the tabular form, with nested field groups for uniform
  nested objects; objects (and dictionaries) of at least two uniform objects use the keyed tabular form; other arrays
  use the inline or list form. The serializer and generated code write the same text as encoding the `ToonValue`
  tree.
- Empty arrays use the canonical `key: []` / `[]` forms (§9.1); inner arrays of list items use `[0]:` (§9.2).
- The first field of a list-item object is written on the hyphen line (§10).
- Strings are quoted per §7.2 (only SP and HTAB count as whitespace) and escaped per §7.1; other control characters
  use lowercase `\uXXXX`. A root string starting with U+FEFF is quoted.
- Output has LF line endings, no comments, no blank lines, no trailing spaces and no trailing newline.

### Limits and errors

- Errors are reported as `ToonParseException` with line and column. Deserialization errors (type mismatches) are
  `ToonSerializationException` with the path of the value, e.g. `$.items[2].price`.
- Nesting beyond `MaxDepth`, or beyond the available stack space, is rejected with an exception instead of
  overflowing the stack.
- A declared length does not reserve memory beyond the lines that follow it (§15).

## Updating to a newer spec version

1. Replace `tests/ToonNet.Tests/SpecCompliance/Fixtures/<version>` with the fixtures of the new tag and update
   `FixtureVersion` and `TargetSpecVersion` in `SpecFixtureConformanceTests`.
2. Regenerate `KnownNonConformance.txt` with `TOONNET_WRITE_NONCONFORMANCE=<path>` and fix the listed cases.
3. Update this page, the website copy (`website/docs/toon-spec.md`) and the README badge.
