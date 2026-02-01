```

BenchmarkDotNet v0.15.0, macOS 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD
  Job-ZNQJPI : .NET 8.0.11 (8.0.1124.51707), Arm64 RyuJIT AdvSIMD

WarmupCount=3  

```
| Method                | Mean       | Error    | StdDev   | Gen0   | Gen1   | Allocated |
|---------------------- |-----------:|---------:|---------:|-------:|-------:|----------:|
| SerializeGenerated    |   207.0 ns |  2.64 ns |  2.34 ns | 0.1664 | 0.0005 |   1.36 KB |
| SerializeReflection   | 1,694.3 ns | 15.17 ns | 13.45 ns | 0.3662 |      - |   2.99 KB |
| DeserializeGenerated  |   661.6 ns | 13.27 ns | 19.03 ns | 0.2069 |      - |    1.7 KB |
| DeserializeReflection | 4,264.2 ns | 11.76 ns |  9.82 ns | 1.4038 | 0.0076 |  11.52 KB |
