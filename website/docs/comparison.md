# ToonNet vs Toon.Format

[Toon.Format](https://www.nuget.org/packages/Toon.Format) is the official .NET implementation of TOON, from the
authors of the spec. Both libraries implement spec v4.4 and **write the same text**; ToonNet does it **8–27x faster
when serializing and 2.4–7x faster when deserializing, with 6–55x less memory**, and adds a source generator, NativeAOT
support, exact decimals and ASP.NET Core integration.

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
| Employees (tabular) | 45.58 ms · 51.8 MB | **2.01 ms · 1.7 MB** (22.7x faster) | 2.64 ms · 1.7 MB | 2.22 ms · 3.3 MB |
| Orders (nested objects, arrays) | 73.86 ms · 85.0 MB | **2.75 ms · 4.6 MB** (26.9x faster) | 3.93 ms · 4.6 MB | 3.10 ms · 4.1 MB |
| Products (nested field groups) | 49.09 ms · 53.1 MB | **2.74 ms · 1.0 MB** (17.9x faster) | 3.19 ms · 1.0 MB | 2.98 ms · 2.7 MB |
| Servers (keyed table) | 21.47 ms · 27.6 MB | **0.94 ms · 1.0 MB** (22.8x faster) | 0.99 ms · 1.0 MB | 0.93 ms · 1.5 MB |

**Deserialize**

| Data | Toon.Format | ToonNet | ToonNet (reflection) | System.Text.Json |
|------|------------:|--------:|---------------------:|-----------------:|
| Employees (tabular) | 31.61 ms · 34.3 MB | **5.06 ms · 2.3 MB** (6.2x faster) | 5.42 ms · 2.3 MB | 4.17 ms · 2.5 MB |
| Orders (nested objects, arrays) | 64.70 ms · 78.3 MB | **9.29 ms · 5.4 MB** (7.0x faster) | 10.75 ms · 5.4 MB | 8.44 ms · 5.7 MB |
| Products (nested field groups) | 30.68 ms · 28.8 MB | **4.80 ms · 3.4 MB** (6.4x faster) | 5.58 ms · 3.4 MB | 6.31 ms · 3.5 MB |
| Servers (keyed table) | 15.30 ms · 21.9 MB | **3.49 ms · 3.1 MB** (4.4x faster) | 3.72 ms · 3.1 MB | 2.75 ms · 2.5 MB |

With 100 items ToonNet serializes 10–12.7x and deserializes 2.9–3.5x faster than Toon.Format. The overloads that take
a `ToonTypeInfo` (no reflection, for trimming and NativeAOT) are as fast as "ToonNet": 18.6–27x and 4.7–7.1x faster
than Toon.Format with 10,000 items. ToonNet writes as fast as System.Text.Json or faster (0.89–1.01x its time) and
takes 0.76–1.27x its time when reading (less for nested field groups), while its TOON output is much smaller than the JSON. Full results, including 100 items: [benchmark/ToonNet.Comparison/RESULTS.md](https://github.com/selcukgural/ToonNet/blob/master/benchmark/ToonNet.Comparison/RESULTS.md).
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
- **80 KB file buffers** - File-based methods open the file with an 81,920-byte buffer
- **Streaming** - `SerializeDocumentsAsync`/`DeserializeDocumentsAsync` process one document at a time

### Thread Safety
- **Concurrent use:** `ToonSerializer` methods are safe to call from multiple threads.
- **Shared caches:** Type metadata and factory caches use `ConcurrentDictionary`; per-property name caches are filled
  idempotently, so concurrent use is safe.
- **Cache lifetime:** Metadata entries are created on demand and retained for the process lifetime (no eviction).
- **Options caution:** Do not mutate a single `ToonSerializerOptions` instance concurrently across threads.

## See Also

- [Performance Tuning](advanced/performance-tuning): getting the most out of ToonNet
- [Source Generators](advanced/source-generators): generated code for `[ToonSerializable]` types
- [Benchmark results](https://github.com/selcukgural/ToonNet/blob/master/benchmark/ToonNet.Comparison/RESULTS.md)
