```

BenchmarkDotNet v0.15.0, macOS 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  Job-PZICMB : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD

IterationCount=20  WarmupCount=5  

```
| Method                      | Mean       | Error    | StdDev   | Gen0   | Gen1   | Allocated |
|---------------------------- |-----------:|---------:|---------:|-------:|-------:|----------:|
| Parse_SimpleObject          |   886.8 ns | 17.99 ns | 20.72 ns | 0.3576 |      - |   2.92 KB |
| Parse_InlineArray           |   823.2 ns | 15.83 ns | 17.60 ns | 0.5417 |      - |   4.43 KB |
| Parse_ListStyleArray        | 1,253.8 ns |  9.44 ns | 10.50 ns | 0.6123 |      - |   5.01 KB |
| Parse_NestedObject          | 1,891.3 ns | 22.33 ns | 22.93 ns | 0.7324 | 0.0038 |   5.99 KB |
| Parse_MixedContent          | 2,492.6 ns | 53.18 ns | 61.24 ns | 1.1101 | 0.0038 |   9.07 KB |
| ParseAndAccess_SimpleObject |   925.2 ns | 16.47 ns | 18.96 ns | 0.3576 |      - |   2.92 KB |
| ParseAndAccess_NestedObject | 1,928.2 ns | 18.12 ns | 20.14 ns | 0.7324 | 0.0038 |   5.99 KB |
| ParseAndIterate_ArrayCount  | 2,499.1 ns | 48.24 ns | 55.55 ns | 1.1101 | 0.0038 |   9.07 KB |
