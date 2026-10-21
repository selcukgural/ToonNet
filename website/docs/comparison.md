# ToonNet vs Toon.Format

[Toon.Format](https://www.nuget.org/packages/Toon.Format) is the official .NET implementation of TOON, from the
authors of the spec. Both libraries implement spec v4.4 and **write the same text**; ToonNet does it **7–27x faster
when serializing and 2.3–7.6x faster when deserializing, with 6–55x less memory** (100 and 10,000 items, with reflection
or generated code; the lower ends are reflection with 100 items), and adds a source generator, NativeAOT support, exact
decimals and ASP.NET Core integration.

## Same Output

- Both pass all **564** official conformance fixtures.
- **20,000 random JSON documents** (tricky strings and keys, numbers, nested and uniform objects, keyed objects, all
  three delimiters) encode to **byte-identical** text, and each library reads the other's text back to the same JSON.
- Typed data – flat records, nested objects with arrays, nested field groups and keyed tables – serializes to
  identical text and reads back to the same values.

`tests/ToonNet.Compatibility.Tests` checks this on every build. To run the full report yourself:

```bash
dotnet run -c Release --project benchmark/ToonNet.Comparison -- parity
```

The remaining differences are in how .NET values are mapped to the data model, which spec §3 leaves to
implementations:

| Value | ToonNet | Toon.Format 1.0.0 |
|-------|---------|-------------------|
| enum `DayOfWeek.Friday` | `Day: Friday` | `Day: 5` |
| `decimal` 12345678901234567.89 | `12345678901234567.89` | `12345678901234568` (rounded through `double`) |
| `decimal` 123456789012345678901234567 | `1.23456789012345678901234567e+26` | `1.2345678901234568e+26` |
| `ulong.MaxValue` | `18446744073709551615` | `18446744073709552000` (cannot be read back into a `ulong`) |

Toon.Format converts numbers to `double`, so values beyond its precision do not survive a round trip (spec §2 asks
that `decode(encode(x))` equals `x`); ToonNet keeps every digit of `decimal`, `long` and `ulong` values.

## Performance

10,000 items, Apple M3 Max, .NET 10.0.5, BenchmarkDotNet 0.15.8. "ToonNet" uses `[ToonSerializable]` models (generated
code), "reflection" the same models without it; System.Text.Json writes and reads the same data as JSON, for
reference. Setup checks that both TOON libraries produce identical text first.

**Serialize**

| Data | Toon.Format | ToonNet | ToonNet (reflection) | System.Text.Json |
|------|------------:|--------:|---------------------:|-----------------:|
| Employees (tabular) | 46.10 ms · 51.8 MB | **1.97 ms · 1.7 MB** (23.4x faster) | 2.65 ms · 1.7 MB | 2.27 ms · 3.3 MB |
| Orders (nested objects, arrays) | 74.60 ms · 85.0 MB | **2.81 ms · 4.6 MB** (26.6x faster) | 4.05 ms · 4.6 MB | 3.03 ms · 4.1 MB |
| Products (nested field groups) | 53.16 ms · 53.1 MB | **2.77 ms · 1.0 MB** (19.2x faster) | 3.19 ms · 1.0 MB | 2.97 ms · 2.7 MB |
| Servers (keyed table) | 23.59 ms · 27.6 MB | **0.97 ms · 1.0 MB** (24.4x faster) | 1.03 ms · 1.0 MB | 0.94 ms · 1.5 MB |

**Deserialize**

| Data | Toon.Format | ToonNet | ToonNet (reflection) | System.Text.Json |
|------|------------:|--------:|---------------------:|-----------------:|
| Employees (tabular) | 31.98 ms · 34.3 MB | **6.58 ms · 2.3 MB** (4.9x faster) | 5.80 ms · 2.3 MB | 4.18 ms · 2.5 MB |
| Orders (nested objects, arrays) | 71.42 ms · 78.3 MB | **9.40 ms · 5.4 MB** (7.6x faster) | 10.78 ms · 5.4 MB | 8.73 ms · 5.7 MB |
| Products (nested field groups) | 33.29 ms · 28.8 MB | **4.91 ms · 3.4 MB** (6.8x faster) | 6.36 ms · 3.4 MB | 6.31 ms · 3.5 MB |
| Servers (keyed table) | 15.27 ms · 21.9 MB | **3.55 ms · 3.1 MB** (4.3x faster) | 3.68 ms · 3.1 MB | 2.75 ms · 2.5 MB |

With 100 items ToonNet serializes 10.0–13.0x and deserializes 2.8–3.2x faster than Toon.Format. The overloads that take
a `ToonTypeInfo` (no reflection, for trimming and NativeAOT) are as fast as "ToonNet": 19.5–26.9x and 4.6–7.9x faster
than Toon.Format with 10,000 items. ToonNet writes about as fast as System.Text.Json (0.87–1.03x its time) and
takes 0.78–1.58x its time when reading 10,000 items (1.04–1.56x with 100; less for nested field groups), while its TOON output is much smaller than the JSON. Full results, including 100 items: [benchmark/ToonNet.Comparison/RESULTS.md](https://github.com/selcukgural/ToonNet/blob/master/benchmark/ToonNet.Comparison/RESULTS.md).
Run them on your hardware with `dotnet run -c Release --project benchmark/ToonNet.Comparison -- --filter "*"`.

**Why:** Toon.Format serializes a value with System.Text.Json into a `JsonNode` tree and encodes the tree, and decodes
text into a `JsonNode` tree that System.Text.Json then binds. ToonNet writes objects straight to TOON text and reads
them straight from it, with code generated for each type.

## Features

| | ToonNet | Toon.Format 1.0.0 |
|---|---|---|
| TOON spec | v4.4, 564/564 fixtures | v4.4, 564/564 fixtures |
| Targets | .NET 8, .NET 10 | .NET 10, .NET Standard 2.0 |
| Typed API | `ToonSerializer.Serialize`/`Deserialize` (text, streams, files) | `ToonEncoder.Encode<T>`/`ToonDecoder.Decode<T>` via System.Text.Json |
| Source generator | ✅ `[ToonSerializable]` | – |
| NativeAOT / trimming | ✅ `ToonSerializer` with generated type infos, tested in CI | – |
| Document model | `ToonDocument` / `ToonValue` | `JsonNode` |
| Options | indent, delimiter, strict, naming policies, null handling, read-only properties, unknown keys, required members, declared or runtime types, converters, max depth | indent, delimiter, strict |
| Exact `decimal` / `ulong` | ✅ | rounded through `double` |
| Multi-document streaming | ✅ `SerializeDocumentsAsync` / `DeserializeDocumentsAsync` | single document per stream |
| Queries | ✅ JSONPath (RFC 9535, 715/715 compliance cases) over trees and text, string/type/aggregate and your own functions; `DeserializeAt`, `DeserializeAll` | – |
| Business rules | ✅ `ToonRuleSet`: assertions, `Required`, `Forbidden`, `Unique`, with the path of each violation | – |
| Editing | ✅ indexers, `GetValue<T>`, `DeepClone`, `DeepEquals`, `SetValue`/`RemoveAll` by path, JSON Merge Patch | `JsonNode` |
| LLM helpers | ✅ `ToonPrompt`: shortened arrays, answer templates, lenient reading of answers | – |
| ASP.NET Core | input/output formatters, `ToonResult`, configuration provider | – |
| JSON / YAML conversion | ✅ `ToonConvert`, `ToonYamlConvert` | via `JsonNode` |

## How ToonNet Is Built

- **Straight to text** - `ToonSerializer` writes objects to TOON text and reads them from it directly, without an
  intermediate document; the tabular forms (including nested field groups and keyed tables) are planned from the
  objects and written in one pass
- **Source Generator** - `[ToonSerializable]` types get generated writers and readers that `ToonSerializer` uses
  automatically: keys are encoded and matched at compile time, primitives are written without boxing
- **Compiled accessors** - For other types, property getters/setters and constructors are compiled once per type;
  primitive properties are read and written through typed delegates, so their values are not boxed
- **Cursor parser** - One quote-aware scan per line, values read from spans, the line table and buffers rented from
  `ArrayPool`, repeated keys and headers shared instead of allocated again
- **UTF-8 in and out** - Bytes, spans, streams and `IBufferWriter<byte>` (a `PipeWriter`) are read and written through
  pooled buffers, without intermediate strings: reading bytes allocates 18–46% less than going through a string, and
  `SerializeToUtf8Bytes` is 4–13% faster with a third of the allocation
- **NativeAOT and trimming** - `ToonNet.Core` is annotated; `ToonSerializer.Serialize(catalog, Catalog.ToonTypeInfo)`
  and the other overloads that take a generated type info need no reflection and keep the fast path (tested with
  NativeAOT in CI)
- **.NET 8 and .NET 10** - Builds for both; `ToonNet.Core` has no package dependencies

### Async I/O
- **ConfigureAwait(false)** - Awaited I/O calls do not capture the synchronization context
- **Cancellation Support** - `CancellationToken` on all async methods
- **Files** - `SerializeToFileAsync` and `ToonEncoder.EncodeToFileAsync` encode in memory first and write the file
  only when encoding succeeded (no truncated file); the multi-document file methods use an 81,920-byte buffer
- **Streaming** - `SerializeDocumentsAsync`/`DeserializeDocumentsAsync` process one document at a time

### Thread-Safety

`ToonSerializer` and a shared `ToonEncoder` are safe to use from several threads; see [Configuration](core-features/configuration#thread-safety) for the caches and options.

## See Also

- [Performance Tuning](advanced/performance-tuning): getting the most out of ToonNet
- [Source Generators](advanced/source-generators): generated code for `[ToonSerializable]` types
- [Benchmark results](https://github.com/selcukgural/ToonNet/blob/master/benchmark/ToonNet.Comparison/RESULTS.md)
