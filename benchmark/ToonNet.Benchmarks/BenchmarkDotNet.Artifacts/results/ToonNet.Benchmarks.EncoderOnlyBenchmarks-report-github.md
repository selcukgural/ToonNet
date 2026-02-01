```

BenchmarkDotNet v0.15.0, macOS 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  Job-PZICMB : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD

IterationCount=20  WarmupCount=5  

```
| Method                           | Mean        | Error     | StdDev    | Gen0   | Gen1   | Allocated |
|--------------------------------- |------------:|----------:|----------:|-------:|-------:|----------:|
| Encode_SimpleObject              |    343.0 ns |   2.04 ns |   2.09 ns | 0.0334 |      - |     280 B |
| Encode_InlineArray               |    230.7 ns |   6.97 ns |   7.75 ns | 0.0172 |      - |     144 B |
| Encode_NestedObject              |    557.8 ns |   4.34 ns |   4.83 ns | 0.0505 |      - |     424 B |
| Encode_LargeArray                | 30,484.1 ns | 534.55 ns | 594.15 ns | 5.3101 | 0.4272 |   44600 B |
| Encode_DeepNesting               |  4,382.4 ns |  89.43 ns | 102.99 ns | 0.7172 |      - |    6040 B |
| Encode_StringLength_SimpleObject |    348.7 ns |   2.23 ns |   2.48 ns | 0.0334 |      - |     280 B |
| Encode_StringLength_LargeArray   | 29,775.8 ns | 170.76 ns | 196.65 ns | 5.3101 | 0.4272 |   44600 B |
